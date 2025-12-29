import { app, ipcMain } from 'electron'
import path from 'path'
import fsSync from 'fs'
import { promises as fs } from 'fs'
import log from 'electron-log'

export interface LogPaths {
  sessionLogFolder: string
  processLogPath: string
  rendererLogPath: string
  backendLogPath: string
}

const padNumber = (value: number, length = 2) => String(value).padStart(length, '0')
const formatTimestamp = (time = new Date()) => {
  return (
    `${time.getFullYear()}-${padNumber(time.getMonth() + 1)}-${padNumber(time.getDate())} ` +
    `${padNumber(time.getHours())}:${padNumber(time.getMinutes())}:${padNumber(time.getSeconds())}.` +
    padNumber(time.getMilliseconds(), 3)
  )
}

const formatFolderTimestamp = (time = new Date()) => {
  return (
    `${time.getFullYear()}-${padNumber(time.getMonth() + 1)}-${padNumber(time.getDate())}_` +
    `${padNumber(time.getHours())}-${padNumber(time.getMinutes())}-${padNumber(time.getSeconds())}-` +
    padNumber(time.getMilliseconds(), 3)
  )
}

export const initLogging = (): LogPaths => {
  const runTimestamp = formatFolderTimestamp()
  const appFolder = app.isPackaged ? path.dirname(app.getPath('exe')) : process.cwd()
  const logFolder = path.join(appFolder, 'logs')
  const sessionLogFolder = path.join(logFolder, runTimestamp)

  if (!fsSync.existsSync(sessionLogFolder)) {
    fsSync.mkdirSync(sessionLogFolder, { recursive: true })
  }

  const processLogPath = path.join(sessionLogFolder, 'process.log')
  const rendererLogPath = path.join(sessionLogFolder, 'renderer.log')
  const backendLogPath = path.join(sessionLogFolder, 'backend.log')

  log.initialize({ preload: true })
  const fileTransport = log.transports?.file as unknown as
    | {
        resolvePathFn?: (variables: unknown, message?: unknown) => string
        format?: string
      }
    | undefined
  if (fileTransport) {
    fileTransport.resolvePathFn = () => processLogPath
    fileTransport.format = '{y}-{m}-{d} {h}:{i}:{s}.{ms} [{level}] {text}'
  }

  Object.assign(console, log.functions)
  log.info('Process logger initialized')

  return { sessionLogFolder, processLogPath, rendererLogPath, backendLogPath }
}

export const attachProcessOutputCapture = (processLogPath: string) => {
  const appendProcessOutput = (chunk: unknown) => {
    if (typeof chunk !== 'string' && !Buffer.isBuffer(chunk)) return
    const text = typeof chunk === 'string' ? chunk : chunk.toString('utf-8')
    if (!text) return
    fs.appendFile(processLogPath, text).catch(() => undefined)
  }

  const stdoutWrite = process.stdout.write.bind(process.stdout) as (...args: unknown[]) => boolean
  const stderrWrite = process.stderr.write.bind(process.stderr) as (...args: unknown[]) => boolean
  process.stdout.write = ((chunk: unknown, ...args: unknown[]) => {
    appendProcessOutput(chunk)
    return stdoutWrite(chunk, ...args)
  }) as typeof process.stdout.write
  process.stderr.write = ((chunk: unknown, ...args: unknown[]) => {
    appendProcessOutput(chunk)
    return stderrWrite(chunk, ...args)
  }) as typeof process.stderr.write
}

export const registerRendererLogIpc = (rendererLogPath: string) => {
  ipcMain.on('logs:renderer', (_, payload: { level?: string; message?: string }) => {
    const level = payload?.level ?? 'info'
    const message = payload?.message ?? ''
    const line = `${formatTimestamp()} [${level}]  ${message}\n`
    fs.appendFile(rendererLogPath, line).catch((error) => {
      log.warn('Failed to append renderer log:', error)
    })
  })
}
