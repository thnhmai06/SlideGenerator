export {}

declare global {
  interface Window {
    electronAPI: {
      openFile: (filters?: { name: string; extensions: string[] }[]) => Promise<string | undefined>
      openMultipleFiles: (filters?: { name: string; extensions: string[] }[]) => Promise<string[] | undefined>
      openFolder: () => Promise<string | undefined>
      saveFile: (filters?: { name: string; extensions: string[] }[]) => Promise<string | undefined>
      openUrl: (url: string) => Promise<void>
      openPath: (path: string) => Promise<void>
      readSettings: (filename: string) => Promise<string | null>
      writeSettings: (filename: string, data: string) => Promise<boolean>
      windowControl: (action: 'minimize' | 'maximize' | 'close') => Promise<void>
      hideToTray: () => Promise<void>
      setProgressBar: (value: number) => Promise<void>
    }
  }
}
