import React, { useEffect, useRef, useState } from 'react'
import Sidebar from './components/Sidebar'
import CreateTaskMenu from './components/CreateTaskMenu'
import SettingMenu from './components/SettingMenu'
import ProcessMenu from './components/ProcessMenu'
import ResultMenu from './components/ResultMenu'
import AboutMenu from './components/AboutMenu'
import TitleBar from './components/TitleBar'
import { checkHealth } from './services/backendApi'
import { useApp } from './contexts/useApp'
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
        <div className="main-content">{renderMenu()}</div>
      </div>
    </div>
  )
}

export default App
