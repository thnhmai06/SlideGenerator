import React, { useEffect, useRef, useState } from 'react'
import Sidebar from './components/Sidebar'
import CreateTaskMenu from './components/CreateTaskMenu'
import SettingMenu from './components/SettingMenu'
import ProcessMenu from './components/ProcessMenu'
import ResultMenu from './components/ResultMenu'
import AboutMenu from './components/AboutMenu'
import TitleBar from './components/TitleBar'
import { checkHealth } from './services/backendApi'
import { getBackendBaseUrl } from './services/signalrClient'
import { useApp } from './contexts/AppContext'
import './styles/App.css'

type MenuType = 'input' | 'setting' | 'download' | 'process' | 'about'

const App: React.FC = () => {
  const { t } = useApp()
  const [currentMenu, setCurrentMenu] = useState<MenuType>('input')
  const [bannerState, setBannerState] = useState<'hidden' | 'connected' | 'disconnected'>('hidden')
  const bannerTimeoutRef = useRef<number | null>(null)
  const connectionRef = useRef<'connected' | 'disconnected' | 'unknown'>('unknown')

  useEffect(() => {
    const clearBannerTimeout = () => {
      if (bannerTimeoutRef.current !== null) {
        window.clearTimeout(bannerTimeoutRef.current)
        bannerTimeoutRef.current = null
      }
    }

    const buildBackendUrl = (host: string, port: number) => {
      if (!host || !port) return
      const trimmedHost = host.trim()
      if (!trimmedHost) return

      const hasScheme = /^https?:\/\//i.test(trimmedHost)
      const base = hasScheme ? trimmedHost : `http://${trimmedHost}`
      const normalizedHost = base.replace(
        /^(https?:\/\/)localhost(?=[:/]|$)/i,
        '$1127.0.0.1'
      )
      const normalizedBase = normalizedHost.endsWith('/') ? normalizedHost.slice(0, -1) : normalizedHost
      const hasPort = /:\d+$/.test(normalizedBase)
      return hasPort ? normalizedBase : `${normalizedBase}:${port}`
    }

    const storeBackendUrl = (host: string, port: number) => {
      const url = buildBackendUrl(host, port)
      if (url) localStorage.setItem('slidegen.backend.url', url)
    }

    const parseBackendConfig = (raw: string | null) => {
      if (!raw) return null
      const hostMatch = raw.match(/^\s*host:\s*([^\r\n#]+)/im)
      const portMatch = raw.match(/^\s*port:\s*(\d+)/im)
      const host = hostMatch?.[1]?.trim() ?? ''
      const port = portMatch ? Number(portMatch[1]) : 0
      if (!host || !port) return null
      return { host, port }
    }

    const showConnected = () => {
      clearBannerTimeout()
      setBannerState('connected')
      bannerTimeoutRef.current = window.setTimeout(() => {
        if (connectionRef.current === 'connected') {
          setBannerState('hidden')
        }
      }, 2000)
    }

    const showDisconnected = () => {
      clearBannerTimeout()
      setBannerState('disconnected')
    }

    const updateStatus = async () => {
      try {
        await checkHealth()
        if (connectionRef.current !== 'connected') {
          connectionRef.current = 'connected'
          showConnected()
        }
      } catch {
        const configText = await window.electronAPI?.getBackendConfig?.()
        const parsed = parseBackendConfig(configText ?? null)
        const currentUrl = getBackendBaseUrl()
        if (parsed) {
          const candidate = buildBackendUrl(parsed.host, parsed.port)
          if (candidate && candidate !== currentUrl) {
            storeBackendUrl(parsed.host, parsed.port)
          }
          try {
            await checkHealth()
            if (connectionRef.current !== 'connected') {
              connectionRef.current = 'connected'
              showConnected()
            }
            return
          } catch {
            // keep falling through to disconnected state
          }
        }

        if (connectionRef.current !== 'disconnected') {
          connectionRef.current = 'disconnected'
          showDisconnected()
        }
      }
    }

    updateStatus().catch(() => undefined)
    const intervalId = window.setInterval(updateStatus, 5000)
    return () => {
      clearBannerTimeout()
      window.clearInterval(intervalId)
    }
  }, [])

  const renderMenu = () => {
    switch (currentMenu) {
      case 'input':
        return <CreateTaskMenu onStart={() => setCurrentMenu('process')} />
      case 'setting':
        return <SettingMenu />
      case 'download':
        return <ResultMenu />
      case 'process':
        return <ProcessMenu />
      case 'about':
        return <AboutMenu />
      default:
        return <CreateTaskMenu onStart={() => setCurrentMenu('process')} />
    }
  }

  return (
    <div className="app-shell">
      <TitleBar />
      <div className={`connection-banner ${bannerState}`}>
        <div className="connection-banner__content">
          {bannerState === 'disconnected' && t('connection.disconnected')}
          {bannerState === 'connected' && t('connection.connected')}
        </div>
      </div>
      <div className="app-container">
        <Sidebar currentMenu={currentMenu} onMenuChange={setCurrentMenu} />
        <div className="main-content">
          {renderMenu()}
        </div>
      </div>
    </div>
  )
}

export default App
