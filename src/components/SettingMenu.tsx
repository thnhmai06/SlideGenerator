import React, { useState, useEffect } from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/SettingMenu.css'

type SettingTab = 'appearance' | 'download' | 'network' | 'server'

const SettingMenu: React.FC = () => {
  const { theme, language, enableAnimations, setTheme, setLanguage, setEnableAnimations, t } = useApp()
  const [activeTab, setActiveTab] = useState<SettingTab>('appearance')
  const [autoSave, setAutoSave] = useState(() => {
    const saved = localStorage.getItem('autoSave')
    return saved === 'true'
  })
  const [showNotifications, setShowNotifications] = useState(() => {
    const saved = localStorage.getItem('showNotifications')
    return saved !== 'false' // default true
  })
  
  // Backend config state organized by section
  const [serverConfig, setServerConfig] = useState<any>(null)
  const [downloadConfig, setDownloadConfig] = useState<any>(null)
  const [networkConfig, setNetworkConfig] = useState<any>(null)
  
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [apiUrl] = useState('http://127.0.0.1:5000')
  const [message, setMessage] = useState<{ type: 'success' | 'error' | 'warning', text: string } | null>(null)

  const handleThemeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setTheme(e.target.value as 'dark' | 'light')
  }

  const handleLanguageChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setLanguage(e.target.value as 'vi' | 'en')
  }

  const handleAnimationsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setEnableAnimations(e.target.checked)
  }

  const handleAutoSaveChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.checked
    setAutoSave(value)
    localStorage.setItem('autoSave', value.toString())
  }

  const handleNotificationsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.checked
    setShowNotifications(value)
    localStorage.setItem('showNotifications', value.toString())
  }

  // Backend config functions
  const showMessage = (type: 'success' | 'error' | 'warning', text: string) => {
    setMessage({ type, text })
    setTimeout(() => setMessage(null), 5000)
  }

  useEffect(() => {
    if (activeTab === 'server' && !serverConfig) {
      loadConfig('server')
    } else if (activeTab === 'download' && !downloadConfig) {
      loadConfig('download')
    } else if (activeTab === 'network' && !networkConfig) {
      loadConfig('network')
    }
  }, [activeTab, serverConfig, downloadConfig, networkConfig])

  const loadConfig = async (section: string) => {
    try {
      setLoading(true)
      const response = await fetch(`${apiUrl}/api/config/${section}`)
      if (response.ok) {
        const data = await response.json()
        if (section === 'server') setServerConfig(data)
        else if (section === 'download') setDownloadConfig(data)
        else if (section === 'network') setNetworkConfig(data)
        showMessage('success', `Loaded ${section} configuration`)
      } else {
        throw new Error(`Failed to load ${section} configuration`)
      }
    } catch (error) {
      showMessage('error', `Failed to load ${section} config: ${error}`)
    } finally {
      setLoading(false)
    }
  }

  const saveCurrentSection = async () => {
    let section: string
    let sectionConfig: any

    if (activeTab === 'server') {
      section = 'server'
      sectionConfig = serverConfig
    } else if (activeTab === 'download') {
      section = 'download'
      sectionConfig = downloadConfig
    } else if (activeTab === 'network') {
      section = 'network'
      sectionConfig = networkConfig
    } else {
      return
    }

    if (!sectionConfig) return

    try {
      setSaving(true)
      const response = await fetch(`${apiUrl}/api/config/${section}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(sectionConfig)
      })

      if (response.ok) {
        const saveResponse = await fetch(`${apiUrl}/api/config/save`, {
          method: 'POST'
        })
        
        if (saveResponse.ok) {
          showMessage('success', 'Configuration saved successfully to TOML file')
        } else {
          showMessage('warning', 'Configuration updated but not saved to file')
        }
      } else {
        throw new Error('Failed to save configuration')
      }
    } catch (error) {
      showMessage('error', `Failed to save configuration: ${error}`)
    } finally {
      setSaving(false)
    }
  }

  const resetConfig = async () => {
    if (!window.confirm('Are you sure you want to reset to default configuration?')) {
      return
    }

    try {
      const response = await fetch(`${apiUrl}/api/config/reset`, {
        method: 'POST'
      })

      if (response.ok) {
        // Reload current section
        if (activeTab === 'server') {
          setServerConfig(null)
          await loadConfig('server')
        } else if (activeTab === 'download') {
          setDownloadConfig(null)
          await loadConfig('download')
        } else if (activeTab === 'network') {
          setNetworkConfig(null)
          await loadConfig('network')
        }
        showMessage('success', 'Configuration reset to defaults')
      } else {
        throw new Error('Failed to reset configuration')
      }
    } catch (error) {
      showMessage('error', `Failed to reset configuration: ${error}`)
    }
  }

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
  }

  // Render functions for each tab
  const renderAppearanceTab = () => (
    <>
      <div className="setting-section">
        <h3>{t('settings.appearanceSettings')}</h3>
        
        <div className="settings-grid">
          <div className="setting-item">
            <label className="setting-label">
              {t('settings.theme')}
            </label>
            <select 
              className="setting-select" 
              value={theme}
              onChange={handleThemeChange}
            >
              <option value="dark">{t('settings.themeDark')}</option>
              <option value="light">{t('settings.themeLight')}</option>
            </select>
            <span className="setting-hint">{t('settings.themeHint')}</span>
          </div>
          
          <div className="setting-item">
            <label className="setting-label">
              {t('settings.language')}
            </label>
            <select 
              className="setting-select"
              value={language}
              onChange={handleLanguageChange}
            >
              <option value="vi">{t('settings.languageVi')}</option>
              <option value="en">{t('settings.languageEn')}</option>
            </select>
            <span className="setting-hint">{t('settings.languageHint')}</span>
          </div>
        </div>

        <div className="setting-item setting-item-toggle">
          <div className="toggle-content">
            <div className="toggle-label">
              <div>
                <div className="label-text">{t('settings.enableAnimations')}</div>
                <div className="label-description">{t('settings.animationsDesc')}</div>
              </div>
            </div>
            <label className="toggle-switch">
              <input 
                type="checkbox" 
                checked={enableAnimations}
                onChange={handleAnimationsChange}
              />
              <span className="toggle-slider"></span>
            </label>
          </div>
        </div>
      </div>

      <div className="setting-section">
        <h3>{t('settings.processingOptions')}</h3>
        
        <div className="setting-item setting-item-toggle">
          <div className="toggle-content">
            <div className="toggle-label">
              <div>
                <div className="label-text">{t('settings.autoSave')}</div>
                <div className="label-description">{t('settings.autoSaveDesc')}</div>
              </div>
            </div>
            <label className="toggle-switch">
              <input 
                type="checkbox" 
                checked={autoSave}
                onChange={handleAutoSaveChange}
              />
              <span className="toggle-slider"></span>
            </label>
          </div>
        </div>
        
        <div className="setting-item setting-item-toggle">
          <div className="toggle-content">
            <div className="toggle-label">
              <div>
                <div className="label-text">{t('settings.showNotifications')}</div>
                <div className="label-description">{t('settings.notificationsDesc')}</div>
              </div>
            </div>
            <label className="toggle-switch">
              <input 
                type="checkbox" 
                checked={showNotifications}
                onChange={handleNotificationsChange}
              />
              <span className="toggle-slider"></span>
            </label>
          </div>
        </div>
      </div>
    </>
  )

  const renderServerTab = () => {
    if (loading) return <div className="loading">{t('settings.loading')}</div>
    if (!serverConfig) return null

    return (
      <div className="setting-section">
        <h3>{t('settings.serverSettings')}</h3>
        
        <div className="settings-grid">
          <div className="setting-item">
            <label className="setting-label">
              {t('settings.host')}
            </label>
            <input
              type="text"
              className="setting-input"
              value={serverConfig.host}
              onChange={(e) => setServerConfig({...serverConfig, host: e.target.value})}
              placeholder="127.0.0.1"
            />
            <span className="setting-hint">{t('settings.hostHint')}</span>
          </div>

          <div className="setting-item">
            <label className="setting-label">
              {t('settings.port')}
            </label>
            <input
              type="number"
              className="setting-input"
              value={serverConfig.port}
              onChange={(e) => setServerConfig({...serverConfig, port: parseInt(e.target.value)})}
              min="1"
              max="65535"
              placeholder="5000"
            />
            <span className="setting-hint">{t('settings.portHint')}</span>
          </div>
        </div>

        <div className="setting-item setting-item-toggle">
          <div className="toggle-content">
            <div className="toggle-label">
              <div>
                <div className="label-text">{t('settings.debugMode')}</div>
                <div className="label-description">{t('settings.debugModeDesc')}</div>
              </div>
            </div>
            <label className="toggle-switch">
              <input
                type="checkbox"
                checked={serverConfig.debug}
                onChange={(e) => setServerConfig({...serverConfig, debug: e.target.checked})}
              />
              <span className="toggle-slider"></span>
            </label>
          </div>
        </div>
      </div>
    )
  }

  const renderDownloadTab = () => {
    if (loading) return <div className="loading">{t('settings.loading')}</div>
    if (!downloadConfig) return null

    return (
      <>
        <div className="setting-section">
          <h3>{t('settings.downloadSettings')}</h3>

          <div className="setting-item">
            <label className="setting-label">
              {t('settings.downloadDir')}
            </label>
            <div className="input-group">
              <input
                type="text"
                className="setting-input"
                value={downloadConfig.download_dir}
                onChange={(e) => setDownloadConfig({...downloadConfig, download_dir: e.target.value})}
                placeholder="./downloads"
              />
              <button 
                className="browse-btn"
                onClick={async () => {
                  console.log('Browse button clicked')
                  if (!window.electronAPI) {
                    alert('Folder browser is only available in desktop app')
                    return
                  }
                  try {
                    const result = await window.electronAPI.openFolder()
                    console.log('Selected folder:', result)
                    if (result) {
                      setDownloadConfig({...downloadConfig, download_dir: result})
                    }
                  } catch (error) {
                    console.error('Error opening folder dialog:', error)
                  }
                }}
              >
                {t('input.browse')}
              </button>
            </div>
            <span className="setting-hint">{t('settings.downloadDirHint')}</span>
          </div>

          <div className="settings-grid">
            <div className="setting-item">
              <label className="setting-label">
                {t('settings.maxConcurrent')}
              </label>
              <input
                type="number"
                className="setting-input"
                value={downloadConfig.max_concurrent_downloads}
                onChange={(e) => setDownloadConfig({...downloadConfig, max_concurrent_downloads: parseInt(e.target.value)})}
                min="1"
                max="50"
              />
              <span className="setting-hint">{t('settings.maxConcurrentHint')}</span>
            </div>

            <div className="setting-item">
              <label className="setting-label">
                {t('settings.workersPerDownload')}
              </label>
              <input
                type="number"
                className="setting-input"
                value={downloadConfig.max_workers_per_download}
                onChange={(e) => setDownloadConfig({...downloadConfig, max_workers_per_download: parseInt(e.target.value)})}
                min="1"
                max="16"
              />
              <span className="setting-hint">{t('settings.workersPerDownloadHint')}</span>
            </div>
          </div>

          <div className="settings-grid">
            <div className="setting-item">
              <label className="setting-label">
                {t('settings.chunkSize')}
              </label>
              <div className="setting-input-with-display">
                <input
                  type="number"
                  className="setting-input"
                  value={downloadConfig.chunk_size}
                  onChange={(e) => setDownloadConfig({...downloadConfig, chunk_size: parseInt(e.target.value)})}
                  min="65536"
                  max="10485760"
                  step="65536"
                />
                <span className="setting-display">{formatBytes(downloadConfig.chunk_size)}</span>
              </div>
              <span className="setting-hint">{t('settings.chunkSizeHint')}</span>
            </div>

            <div className="setting-item">
              <label className="setting-label">
                {t('settings.minSizeParallel')}
              </label>
              <div className="setting-input-with-display">
                <input
                  type="number"
                  className="setting-input"
                  value={downloadConfig.min_file_size_for_parallel}
                  onChange={(e) => setDownloadConfig({...downloadConfig, min_file_size_for_parallel: parseInt(e.target.value)})}
                  min="1048576"
                  max="104857600"
                  step="1048576"
                />
                <span className="setting-display">{formatBytes(downloadConfig.min_file_size_for_parallel)}</span>
              </div>
              <span className="setting-hint">{t('settings.minSizeParallelHint')}</span>
            </div>
          </div>

          <div className="setting-item setting-item-toggle">
            <div className="toggle-content">
              <div className="toggle-label">
                <div>
                  <div className="label-text">{t('settings.enableParallel')}</div>
                  <div className="label-description">{t('settings.enableParallelDesc')}</div>
                </div>
              </div>
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={downloadConfig.enable_parallel_chunks}
                  onChange={(e) => setDownloadConfig({...downloadConfig, enable_parallel_chunks: e.target.checked})}
                />
                <span className="toggle-slider"></span>
              </label>
            </div>
          </div>
        </div>
      </>
    )
  }

  const renderNetworkTab = () => {
    if (loading) return <div className="loading">{t('settings.loading')}</div>
    if (!networkConfig) return null

    return (
      <>
        <div className="setting-section">
          <h3>{t('settings.retrySettings')}</h3>

          <div className="settings-grid">
            <div className="setting-item">
              <label className="setting-label">
                {t('settings.maxRetries')}
              </label>
              <input
                type="number"
                className="setting-input"
                value={networkConfig.max_retries}
                onChange={(e) => setNetworkConfig({...networkConfig, max_retries: parseInt(e.target.value)})}
                min="0"
                max="10"
              />
              <span className="setting-hint">{t('settings.maxRetriesHint')}</span>
            </div>

            <div className="setting-item">
              <label className="setting-label">
                {t('settings.initialRetryDelay')}
              </label>
              <div className="setting-input-with-unit">
                <input
                  type="number"
                  className="setting-input"
                  value={networkConfig.initial_retry_delay}
                  onChange={(e) => setNetworkConfig({...networkConfig, initial_retry_delay: parseFloat(e.target.value)})}
                  min="0.1"
                  max="60"
                  step="0.1"
                />
                <span className="input-unit">{t('settings.seconds')}</span>
              </div>
              <span className="setting-hint">{t('settings.initialRetryDelayHint')}</span>
            </div>
          </div>

          <div className="settings-grid">
            <div className="setting-item">
              <label className="setting-label">
                {t('settings.maxRetryDelay')}
              </label>
              <div className="setting-input-with-unit">
                <input
                  type="number"
                  className="setting-input"
                  value={networkConfig.max_retry_delay}
                  onChange={(e) => setNetworkConfig({...networkConfig, max_retry_delay: parseFloat(e.target.value)})}
                  min="1"
                  max="600"
                />
                <span className="input-unit">{t('settings.seconds')}</span>
              </div>
              <span className="setting-hint">{t('settings.maxRetryDelayHint')}</span>
            </div>

            <div className="setting-item">
              <label className="setting-label">
                {t('settings.backoffMultiplier')}
              </label>
              <input
                type="number"
                className="setting-input"
                value={networkConfig.retry_backoff_multiplier}
                onChange={(e) => setNetworkConfig({...networkConfig, retry_backoff_multiplier: parseFloat(e.target.value)})}
                min="1"
                max="10"
                step="0.1"
              />
              <span className="setting-hint">{t('settings.backoffMultiplierHint')}</span>
            </div>
          </div>
        </div>

        <div className="setting-section">
          <h3>{t('settings.networkSettings')}</h3>

          <div className="settings-grid">
            <div className="setting-item">
              <label className="setting-label">
                {t('settings.requestTimeout')}
              </label>
              <div className="setting-input-with-unit">
                <input
                  type="number"
                  className="setting-input"
                  value={networkConfig.request_timeout}
                  onChange={(e) => setNetworkConfig({...networkConfig, request_timeout: parseInt(e.target.value)})}
                  min="5"
                  max="300"
                />
                <span className="input-unit">{t('settings.seconds')}</span>
              </div>
              <span className="setting-hint">{t('settings.requestTimeoutHint')}</span>
            </div>

            <div className="setting-item">
              <label className="setting-label">
                {t('settings.connectTimeout')}
              </label>
              <div className="setting-input-with-unit">
                <input
                  type="number"
                  className="setting-input"
                  value={networkConfig.connect_timeout}
                  onChange={(e) => setNetworkConfig({...networkConfig, connect_timeout: parseInt(e.target.value)})}
                  min="1"
                  max="60"
                />
                <span className="input-unit">{t('settings.seconds')}</span>
              </div>
              <span className="setting-hint">{t('settings.connectTimeoutHint')}</span>
            </div>
          </div>
        </div>
      </>
    )
  }

  return (
    <div className="setting-menu">
      <h1 className="menu-title">{t('settings.title')}</h1>
      
      {/* Tabs */}
      <div className="setting-tabs">
        <button 
          className={`tab-button ${activeTab === 'appearance' ? 'active' : ''}`}
          onClick={() => setActiveTab('appearance')}
        >
          {t('settings.appearance')}
        </button>
        <button 
          className={`tab-button ${activeTab === 'download' ? 'active' : ''}`}
          onClick={() => setActiveTab('download')}
        >
          {t('settings.download')}
        </button>
        <button 
          className={`tab-button ${activeTab === 'network' ? 'active' : ''}`}
          onClick={() => setActiveTab('network')}
        >
          {t('settings.network')}
        </button>
        <button 
          className={`tab-button ${activeTab === 'server' ? 'active' : ''}`}
          onClick={() => setActiveTab('server')}
        >
          {t('settings.server')}
        </button>
      </div>

      {/* Message Display */}
      {message && (
        <div className={`message message-${message.type}`}>
          {message.text}
        </div>
      )}

      {/* Tab Content */}
      {activeTab === 'appearance' && renderAppearanceTab()}
      {activeTab === 'server' && renderServerTab()}
      {activeTab === 'download' && renderDownloadTab()}
      {activeTab === 'network' && renderNetworkTab()}

      {/* Action Buttons (for backend settings tabs) */}
      {activeTab !== 'appearance' && (serverConfig || downloadConfig || networkConfig) && (
        <div className="setting-actions">
          <button 
            className="btn btn-primary" 
            onClick={saveCurrentSection}
            disabled={saving}
          >
            {saving ? 'Saving...' : t('settings.save')}
          </button>
          <button 
            className="btn btn-secondary" 
            onClick={() => loadConfig(activeTab)}
          >
            {t('settings.reload')}
          </button>
          <button 
            className="btn btn-danger" 
            onClick={resetConfig}
          >
            {t('settings.resetToDefaults')}
          </button>
        </div>
      )}
    </div>
  )
}

export default SettingMenu
