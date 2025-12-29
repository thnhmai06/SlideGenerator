import { app, ipcMain } from 'electron'
import path from 'path'
import { promises as fs } from 'fs'

export const registerSettingsHandlers = () => {
  ipcMain.handle('settings:read', async (_, filename: string) => {
    try {
      const settingsPath = path.isAbsolute(filename)
        ? filename
        : path.join(app.getPath('userData'), filename)
      const data = await fs.readFile(settingsPath, 'utf-8')
      return data
    } catch (_error) {
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
}
