export {}

declare global {
  const __APP_VERSION__: string

  interface Window {
    electronAPI: {
      openFile: (filters?: { name: string; extensions: string[] }[]) => Promise<string | undefined>
      openMultipleFiles: (
        filters?: { name: string; extensions: string[] }[],
      ) => Promise<string[] | undefined>
      openFolder: () => Promise<string | undefined>
      saveFile: (filters?: { name: string; extensions: string[] }[]) => Promise<string | undefined>
      openUrl: (url: string) => Promise<void>
      openPath: (path: string) => Promise<void>
      readSettings: (filename: string) => Promise<string | null>
      writeSettings: (filename: string, data: string) => Promise<boolean>
      windowControl: (action: 'minimize' | 'maximize' | 'close') => Promise<void>
      hideToTray: () => Promise<void>
      setProgressBar: (value: number) => Promise<void>
      restartBackend: () => Promise<boolean>
      logRenderer: (level: 'debug' | 'info' | 'warn' | 'error', message: string) => void
      onNavigate: (handler: (menu: 'input' | 'process' | 'download' | 'setting' | 'about') => void) => () => void
      setTrayLocale: (locale: 'vi' | 'en') => Promise<void>
    }
    getAssetPath: (...p: string[]) => string
  }
}
