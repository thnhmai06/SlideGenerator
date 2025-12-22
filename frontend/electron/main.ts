function getAssetPath(...p: string[]) {
  if (process.env.NODE_ENV === 'development') {
    return path.join('assets', ...p).replace(/\\/g, '/')
  }
  const base = app.getAppPath()
  return path.join(base, 'assets', ...p)
}

ipcMain.handle('assets:getPath', async (_, ...p: string[]) => {
  return getAssetPath(...p)
})

ipcMain.on('assets:getPathSync', (event, ...p: string[]) => {
  event.returnValue = getAssetPath(...p)
})
import { app, BrowserWindow, ipcMain, dialog, shell, Menu, Tray } from 'electron'
import { spawn, ChildProcess } from 'child_process'
import path from 'path'
import { fileURLToPath } from 'url'
import fsSync from 'fs'
import { promises as fs } from 'fs'
import log from 'electron-log'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

// Configure logging
const runTimestamp = new Date().toISOString().replace(/[:.]/g, '-')
const appFolder = app.isPackaged ? path.dirname(app.getPath('exe')) : process.cwd()
const logFolder = path.join(appFolder, 'logs')
const sessionLogFolder = path.join(logFolder, runTimestamp)

// Ensure session log directory exists
if (!fsSync.existsSync(sessionLogFolder)) {
  fsSync.mkdirSync(sessionLogFolder, { recursive: true })
}

const frontendLogPath = path.join(sessionLogFolder, 'frontend.log')
const backendLogPath = path.join(sessionLogFolder, 'backend.log')

log.transports.file.resolvePathFn = () => frontendLogPath
Object.assign(console, log.functions)

app.commandLine.appendSwitch('remote-debugging-port', '9222')

let mainWindow: BrowserWindow | null = null
let tray: Tray | null = null
let isQuitting = false
let backendProcess: ChildProcess | null = null

const shouldStartBackend = () => {
  if (process.env.NODE_ENV === 'development') return false
  return process.env.SLIDEGEN_DISABLE_BACKEND !== '1'
}

const resolveBackendCommand = () => {
  const override = process.env.SLIDEGEN_BACKEND_PATH

  if (override && fsSync.existsSync(override)) {
    const ext = path.extname(override).toLowerCase()

    if (ext === '.dll') {
      return {
        command: 'dotnet',
        args: [override],
        cwd: path.dirname(override),
      }
    }

    return {
      command: override,
      args: [],
      cwd: path.dirname(override),
    }
  }

  if (app.isPackaged) {
    const backendRoot = path.join(process.resourcesPath, 'backend')
    const exePath = path.join(backendRoot, 'SlideGenerator.Presentation.exe')

    if (fsSync.existsSync(exePath)) {
      return { command: exePath, args: [], cwd: backendRoot }
    }

    const dllPath = path.join(backendRoot, 'SlideGenerator.Presentation.dll')
    if (fsSync.existsSync(dllPath)) {
      return { command: 'dotnet', args: [dllPath], cwd: backendRoot }
    }
  }

  return null
}

const startBackend = () => {
  if (!shouldStartBackend() || backendProcess) return
  const launch = resolveBackendCommand()
  if (!launch) return

  backendProcess = spawn(launch.command, launch.args, {
    cwd: launch.cwd,
    windowsHide: true,
    stdio: 'ignore',
    detached: false,
    env: {
      ...process.env,
      SLIDEGEN_LOG_PATH: backendLogPath, // Pass log path to backend
    },
  })

  backendProcess.on('exit', (code) => {
    log.info(`Backend process exited with code ${code}`)
    backendProcess = null
  })
}

const stopBackend = async () => {
  if (!backendProcess) return
  const proc = backendProcess
  backendProcess = null

  return new Promise<void>((resolve) => {
    // Force kill if not exited within 5 seconds
    const timeout = setTimeout(() => {
      if (!proc.killed) {
        log.info('Backend timed out, force killing...')
        proc.kill()
      }
      resolve()
    }, 5000)

    proc.once('exit', () => {
      clearTimeout(timeout)
      resolve()
    })

    try {
      // Send SIGINT (Ctrl+C equivalent) first as .NET handles it well for graceful shutdown
      proc.kill('SIGINT')

      // Also send SIGTERM after a short delay if SIGINT doesn't work,
      // but the 5s timeout above will force kill anyway.
      setTimeout(() => {
        if (!proc.killed) proc.kill('SIGTERM')
      }, 1000)
    } catch (e) {
      log.error('Error stopping backend:', e)
      proc.kill()
      resolve()
    }
  })
}

const restartBackend = async () => {
  await stopBackend()
  startBackend()
  return Boolean(backendProcess)
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    icon: getAssetPath('images', 'app-icon.png'),
    frame: false,
    autoHideMenuBar: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  })
  mainWindow.maximize()

  // Load URL based on dev/prod mode
  if (process.env.NODE_ENV === 'development') {
    mainWindow.loadURL('http://localhost:65000')
    mainWindow.webContents.openDevTools()
  } else {
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'))
  }

  mainWindow.on('closed', () => {
    mainWindow = null
  })
}

function ensureTray() {
  if (tray || !mainWindow) return

  tray = new Tray(getAssetPath('images', 'app-icon.png'))
  tray.setToolTip('Slide Generator')
  tray.setContextMenu(
    Menu.buildFromTemplate([
      {
        label: 'Show',
        click: () => {
          mainWindow?.show()
          mainWindow?.focus()
        },
      },
      {
        label: 'Hide',
        click: () => {
          mainWindow?.hide()
        },
      },
      { type: 'separator' },
      {
        label: 'Quit',
        click: () => {
          isQuitting = true
          app.quit()
        },
      },
    ]),
  )
  tray.on('click', () => {
    mainWindow?.show()
    mainWindow?.focus()
  })
}

app.whenReady().then(() => {
  startBackend()
  createWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
    }
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
  }
})

let isAppQuitting = false

app.on('before-quit', async (e) => {
  if (isAppQuitting) return

  e.preventDefault()
  isAppQuitting = true
  isQuitting = true // For window control logic

  await stopBackend()
  app.quit()
})

// IPC handlers for file dialogs
ipcMain.handle('dialog:openFile', async (_, filters: Electron.FileFilter[]) => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile'],
    filters: filters || [{ name: 'All Files', extensions: ['*'] }],
  })
  return result.filePaths[0]
})

ipcMain.handle('dialog:openMultipleFiles', async (_, filters: Electron.FileFilter[]) => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile', 'multiSelections'],
    filters: filters || [{ name: 'All Files', extensions: ['*'] }],
  })
  return result.filePaths
})

ipcMain.handle('dialog:openFolder', async () => {
  const result = await dialog.showOpenDialog({
    properties: ['openDirectory'],
  })
  return result.filePaths[0]
})

ipcMain.handle('dialog:saveFile', async (_, filters: Electron.FileFilter[]) => {
  const result = await dialog.showSaveDialog({
    filters: filters || [{ name: 'All Files', extensions: ['*'] }],
    defaultPath: 'task-config.json',
  })
  return result.filePath
})

ipcMain.handle('dialog:openUrl', async (_, url: string) => {
  await shell.openExternal(url)
})

ipcMain.handle('dialog:openPath', async (_, filePath: string) => {
  await shell.openPath(filePath)
})

ipcMain.handle('window:control', async (_, action: string) => {
  if (!mainWindow) return

  switch (action) {
    case 'minimize':
      mainWindow.minimize()
      break
    case 'maximize':
      if (mainWindow.isMaximized()) {
        mainWindow.unmaximize()
      } else {
        mainWindow.maximize()
      }
      break
    case 'close':
      if (isQuitting) {
        mainWindow.close()
      } else {
        mainWindow.close()
      }
      break
  }
})

ipcMain.handle('window:hideToTray', async () => {
  if (!mainWindow) return
  ensureTray()
  mainWindow.hide()
})

ipcMain.handle('window:setProgress', async (_, value: number) => {
  if (!mainWindow) return
  mainWindow.setProgressBar(value)
})

ipcMain.handle('backend:restart', async () => {
  return restartBackend()
})

ipcMain.handle('settings:read', async (_, filename: string) => {
  try {
    const settingsPath = path.isAbsolute(filename)
      ? filename
      : path.join(app.getPath('userData'), filename)
    const data = await fs.readFile(settingsPath, 'utf-8')
    return data
  } catch (error) {
    // File doesn't exist or can't be read, return null
    return null
  }
})

ipcMain.handle('settings:write', async (_, filename: string, data: string) => {
  try {
    const settingsPath = path.isAbsolute(filename)
      ? filename
      : path.join(app.getPath('userData'), filename)
    await fs.writeFile(settingsPath, data, 'utf-8')
    return true
  } catch (error) {
    console.error('Error writing settings:', error)
    return false
  }
})
