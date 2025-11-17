import React from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/LogWindow.css'

interface LogWindowProps {
  title: string
  log: string[]
  onClose: () => void
}

const LogWindow: React.FC<LogWindowProps> = ({ title, log, onClose }) => {
  const { t } = useApp()
  
  return (
    <div className="log-window-overlay" onClick={onClose}>
      <div className="log-window" onClick={(e) => e.stopPropagation()}>
        <div className="log-header">
          <h3>{title} - {t('process.log')}</h3>
          <button className="close-btn" onClick={onClose}>×</button>
        </div>
        <div className="log-content">
          {log.length > 0 ? (
            log.map((entry, index) => (
              <div key={index} className="log-entry">
                {entry}
              </div>
            ))
          ) : (
            <div className="log-empty">{t('process.noLogs')}</div>
          )}
        </div>
      </div>
    </div>
  )
}

export default LogWindow
