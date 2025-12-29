import { dialog, ipcMain, shell } from 'electron'

export const registerDialogHandlers = () => {
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
}
