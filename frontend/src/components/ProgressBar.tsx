import React, { useState } from 'react'
import { useApp } from '../contexts/AppContext'
import LogWindow from './LogWindow'
import '../styles/ProgressBar.css'

interface ProgressBarProps {
  id: number
  value: number
  label: string
  log: string[]
  onAppendLog: (id: number, text: string) => void
}

const ProgressBar: React.FC<ProgressBarProps> = ({ value, label, log }) => {
  const { t } = useApp()
  const [showLog, setShowLog] = useState(false)

  return (
    <div className="progress-bar-widget">
      <div className="progress-header">
        <span className="progress-label">{label}</span>
        <button 
          className="log-btn"
          onClick={() => setShowLog(true)}
        >
          <img src="/assets/log.png" alt="Log" className="log-icon" />
          {t('process.viewLog')}
        </button>
      </div>
      
      <div className="progress-bar-container">
        <div 
          className="progress-bar-fill"
          style={{ width: `${value}%` }}
        >
          <span className="progress-percentage">{value}%</span>
        </div>
      </div>

      {showLog && (
        <LogWindow 
          title={label}
          log={log}
          onClose={() => setShowLog(false)}
        />
      )}
    </div>
  )
}

export default ProgressBar
