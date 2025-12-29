import { app, ipcMain } from 'electron'
import path from 'path'

export const getAssetPath = (...parts: string[]): string => {
  if (process.env.NODE_ENV === 'development') {
    return path.join('assets', ...parts).replace(/\\/g, '/')
  }
  const base = app.getAppPath()
  return path.join(base, 'assets', ...parts)
}

export const registerAssetHandlers = () => {
  ipcMain.handle('assets:getPath', async (_, ...parts: string[]) => {
    return getAssetPath(...parts)
  })

  ipcMain.on('assets:getPathSync', (event, ...parts: string[]) => {
    event.returnValue = getAssetPath(...parts)
  })
}
