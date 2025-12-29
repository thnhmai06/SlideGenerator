import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import { AppProvider } from './contexts/AppContext'
import { JobProvider } from './contexts/JobContext'
import './styles/theme.css'
import './styles/index.css'

const formatLogArg = (arg: unknown): string => {
  if (arg instanceof Error) {
    return arg.stack || `${arg.name}: ${arg.message}`
  }
  if (typeof arg === 'string') return arg
  if (typeof arg === 'number' || typeof arg === 'boolean' || typeof arg === 'bigint') {
    return String(arg)
  }
  if (arg === null || arg === undefined) return String(arg)
  try {
    return JSON.stringify(arg)
  } catch {
    return String(arg)
  }
}

const initRendererLogging = () => {
  if (!window.electronAPI?.logRenderer) return
  const flagKey = '__rendererLoggerInstalled'
  const windowFlags = window as unknown as Record<string, unknown>
  if (windowFlags[flagKey]) return
  windowFlags[flagKey] = true

  const originalConsole = {
    log: console.log.bind(console),
    info: console.info.bind(console),
    warn: console.warn.bind(console),
    error: console.error.bind(console),
    debug: console.debug.bind(console),
  }

  const sendRendererLog = (level: 'debug' | 'info' | 'warn' | 'error', args: unknown[]) => {
    const message = args.map(formatLogArg).join(' ')
    window.electronAPI.logRenderer(level, message)
  }

  console.log = (...args: unknown[]) => {
    originalConsole.log(...args)
    sendRendererLog('info', args)
  }
  console.info = (...args: unknown[]) => {
    originalConsole.info(...args)
    sendRendererLog('info', args)
  }
  console.warn = (...args: unknown[]) => {
    originalConsole.warn(...args)
    sendRendererLog('warn', args)
  }
  console.error = (...args: unknown[]) => {
    originalConsole.error(...args)
    sendRendererLog('error', args)
  }
  console.debug = (...args: unknown[]) => {
    originalConsole.debug(...args)
    sendRendererLog('debug', args)
  }

  window.addEventListener(
    'error',
    (event: Event) => {
      if (event instanceof ErrorEvent) {
        sendRendererLog('error', [
          'Uncaught error:',
          event.message,
          event.filename,
          event.lineno,
          event.colno,
          event.error,
        ])
        return
      }
      const target = event.target as { src?: string; href?: string } | null
      const resourceUrl = target?.src ?? target?.href ?? ''
      if (resourceUrl) {
        sendRendererLog('error', ['Resource load error:', resourceUrl])
      } else {
        sendRendererLog('error', ['Resource load error: unknown target'])
      }
    },
    true,
  )

  window.addEventListener('unhandledrejection', (event) => {
    sendRendererLog('error', ['Unhandled rejection:', event.reason])
  })

  sendRendererLog('info', ['Renderer logger initialized'])
}

initRendererLogging()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppProvider>
      <JobProvider>
        <App />
      </JobProvider>
    </AppProvider>
  </React.StrictMode>,
)
