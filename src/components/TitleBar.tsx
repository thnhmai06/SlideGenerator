import React from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/TitleBar.css'

const TitleBar: React.FC = () => {
  const { closeToTray } = useApp()
  const handleAction = (action: 'minimize' | 'maximize' | 'close') => {
    if (action === 'close' && closeToTray) {
      window.electronAPI?.hideToTray()
      return
    }
    window.electronAPI?.windowControl(action)
  }

  return (
    <div className="title-bar">
      <div className="title-bar-left">
        <img src="/assets/images/app-icon.png" alt="Slide Generator" className="title-bar-icon" />
        <span className="title-bar-title">Slide Generator</span>
      </div>
      <div className="title-bar-controls">
        <button
          className="title-bar-btn"
          onClick={() => handleAction('minimize')}
          aria-label="Minimize"
        >
          <img src="/assets/images/window-minimize.png" alt="" />
        </button>
        <button
          className="title-bar-btn"
          onClick={() => handleAction('maximize')}
          aria-label="Maximize"
        >
          <img src="/assets/images/window-maximize.png" alt="" />
        </button>
        <button
          className="title-bar-btn title-bar-btn-danger"
          onClick={() => handleAction('close')}
          aria-label="Close"
        >
          <img src="/assets/images/window-close.png" alt="" />
        </button>
      </div>
    </div>
  )
}

export default TitleBar
