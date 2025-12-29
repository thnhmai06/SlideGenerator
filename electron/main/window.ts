import { BrowserWindow, Menu, Tray, ipcMain } from 'electron'

let mainWindow: BrowserWindow | null = null
let tray: Tray | null = null
let isQuitting = false

export const getMainWindow = () => mainWindow

export const setIsQuitting = (value: boolean) => {
  isQuitting = value
}

export const createMainWindow = (options: {
  preloadPath: string
  getAssetPath: (...parts: string[]) => string
  isDev: boolean
  devUrl: string
  indexPath: string
}) => {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    icon: options.getAssetPath('images', 'app-icon.png'),
    frame: false,
    autoHideMenuBar: true,
    webPreferences: {
      preload: options.preloadPath,
      contextIsolation: true,
      nodeIntegration: false,
    },
  })
  mainWindow.maximize()

  if (options.isDev) {
    mainWindow.loadURL(options.devUrl)
    mainWindow.webContents.openDevTools()
  } else {
    mainWindow.loadFile(options.indexPath)
  }

  mainWindow.on('closed', () => {
    mainWindow = null
  })
}

const ensureTray = (getAssetPath: (...parts: string[]) => string) => {
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
          mainWindow?.close()
        },
      },
    ]),
  )
  tray.on('click', () => {
    mainWindow?.show()
    mainWindow?.focus()
  })
}

export const registerWindowHandlers = (getAssetPath: (...parts: string[]) => string) => {
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
    ensureTray(getAssetPath)
    mainWindow.hide()
  })

  ipcMain.handle('window:setProgress', async (_, value: number) => {
    if (!mainWindow) return
    mainWindow.setProgressBar(value)
  })
}
