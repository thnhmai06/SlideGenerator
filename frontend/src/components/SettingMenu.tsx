import React, { useEffect, useState } from 'react'
import { useApp } from '../contexts/AppContext'
import { useJobs } from '../contexts/JobContext'
import * as backendApi from '../services/backendApi'
import '../styles/SettingMenu.css'

type SettingTab = 'appearance' | 'server' | 'download' | 'job' | 'image'

interface ConfigState {
  server: {
    host: string
    port: number
    debug: boolean
  }
  download: {
    maxChunks: number
    limitBytesPerSecond: number
    saveFolder: string
    retryTimeout: number
    maxRetries: number
  }
  job: {
    maxConcurrentJobs: number
  }
  image: {
    face: {
      confidence: number
      paddingTop: number
      paddingBottom: number
      paddingLeft: number
      paddingRight: number
      unionAll: boolean
    }
    saliency: {
      paddingTop: number
      paddingBottom: number
      paddingLeft: number
      paddingRight: number
    }
  }
}

const SettingMenu: React.FC = () => {
  const {
    theme,
    language,
    enableAnimations,
    closeToTray,
    setTheme,
    setLanguage,
    setEnableAnimations,
    setCloseToTray,
    t,
  } = useApp()
  const { groups } = useJobs()
  const [activeTab, setActiveTab] = useState<SettingTab>('appearance')
  const [config, setConfig] = useState<ConfigState | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState<{ type: 'success' | 'error' | 'warning'; text: string } | null>(null)

  const getErrorDetail = (error: unknown): string => {
    if (error instanceof Error && error.message) return error.message
    if (typeof error === 'string') return error
    if (error && typeof error === 'object' && 'message' in error) {
      const value = (error as { message?: string }).message
      if (value) return value
    }
    return ''
  }

  const formatErrorMessage = (key: string, error: unknown) => {
    const detail = getErrorDetail(error)
    return detail ? `${t(key)}: ${detail}` : t(key)
  }

  const showMessage = (type: 'success' | 'error' | 'warning', text: string) => {
    setMessage({ type, text })
    setTimeout(() => setMessage(null), 5000)
  }

  const storeBackendUrl = (host: string, port: number) => {
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
    const url = hasPort ? normalizedBase : `${normalizedBase}:${port}`
    localStorage.setItem('slidegen.backend.url', url)
  }

  const getCaseInsensitive = <T extends Record<string, unknown>>(obj: T | null | undefined, key: string) => {
    if (!obj) return undefined
    if (key in obj) return obj[key]
    const lowered = key.toLowerCase()
    for (const [entryKey, value] of Object.entries(obj)) {
      if (entryKey.toLowerCase() === lowered) {
        return value
      }
    }
    return undefined
  }

  const loadConfig = async () => {
    try {
      setLoading(true)
      const response = await backendApi.getConfig()
      const data = response as backendApi.ConfigGetSuccess
      const server = getCaseInsensitive(data as unknown as Record<string, unknown>, 'Server') as
        | Record<string, unknown>
        | undefined
      const download = getCaseInsensitive(data as unknown as Record<string, unknown>, 'Download') as
        | Record<string, unknown>
        | undefined
      const job = getCaseInsensitive(data as unknown as Record<string, unknown>, 'Job') as
        | Record<string, unknown>
        | undefined
      const image = getCaseInsensitive(data as unknown as Record<string, unknown>, 'Image') as
        | Record<string, unknown>
        | undefined

      if (!server || !download || !job || !image) {
        throw new Error('Invalid config response.')
      }

      const retry = getCaseInsensitive(download, 'Retry') as Record<string, unknown> | undefined
      const face = getCaseInsensitive(image, 'Face') as Record<string, unknown> | undefined
      const saliency = getCaseInsensitive(image, 'Saliency') as Record<string, unknown> | undefined
      if (!retry) {
        throw new Error('Invalid config response.')
      }
      if (!face || !saliency) {
        throw new Error('Invalid config response.')
      }

      const host = String(getCaseInsensitive(server, 'Host') ?? '')
      const port = Number(getCaseInsensitive(server, 'Port') ?? 0)
      const debug = Boolean(getCaseInsensitive(server, 'Debug'))

      const maxChunks = Number(getCaseInsensitive(download, 'MaxChunks') ?? 0)
      const limitBytesPerSecond = Number(getCaseInsensitive(download, 'LimitBytesPerSecond') ?? 0)
      const saveFolder = String(getCaseInsensitive(download, 'SaveFolder') ?? '')
      const retryTimeout = Number(getCaseInsensitive(retry, 'Timeout') ?? 0)
      const maxRetries = Number(getCaseInsensitive(retry, 'MaxRetries') ?? 0)

      const maxConcurrentJobs = Number(getCaseInsensitive(job, 'MaxConcurrentJobs') ?? 0)
      const faceConfidence = Number(getCaseInsensitive(face, 'Confidence') ?? 0)
      const facePaddingTop = Number(getCaseInsensitive(face, 'PaddingTop') ?? 0)
      const facePaddingBottom = Number(getCaseInsensitive(face, 'PaddingBottom') ?? 0)
      const facePaddingLeft = Number(getCaseInsensitive(face, 'PaddingLeft') ?? 0)
      const facePaddingRight = Number(getCaseInsensitive(face, 'PaddingRight') ?? 0)
      const faceUnionAll = Boolean(getCaseInsensitive(face, 'UnionAll'))

      const saliencyPaddingTop = Number(getCaseInsensitive(saliency, 'PaddingTop') ?? 0)
      const saliencyPaddingBottom = Number(getCaseInsensitive(saliency, 'PaddingBottom') ?? 0)
      const saliencyPaddingLeft = Number(getCaseInsensitive(saliency, 'PaddingLeft') ?? 0)
      const saliencyPaddingRight = Number(getCaseInsensitive(saliency, 'PaddingRight') ?? 0)

      setConfig({
        server: {
          host,
          port,
          debug,
        },
        download: {
          maxChunks,
          limitBytesPerSecond,
          saveFolder,
          retryTimeout,
          maxRetries,
        },
        job: {
          maxConcurrentJobs,
        },
        image: {
          face: {
            confidence: faceConfidence,
            paddingTop: facePaddingTop,
            paddingBottom: facePaddingBottom,
            paddingLeft: facePaddingLeft,
            paddingRight: facePaddingRight,
            unionAll: faceUnionAll,
          },
          saliency: {
            paddingTop: saliencyPaddingTop,
            paddingBottom: saliencyPaddingBottom,
            paddingLeft: saliencyPaddingLeft,
            paddingRight: saliencyPaddingRight,
          },
        },
      })
      storeBackendUrl(host, port)
    } catch (error) {
      console.error('Failed to load config:', error)
      showMessage('error', formatErrorMessage('settings.loadError', error))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadConfig().catch(() => undefined)
  }, [])

  const saveConfig = async () => {
    if (!config) return
    try {
      setSaving(true)
      await backendApi.updateConfig({
        Server: {
          Host: config.server.host,
          Port: config.server.port,
          Debug: config.server.debug,
        },
        Download: {
          MaxChunks: config.download.maxChunks,
          LimitBytesPerSecond: config.download.limitBytesPerSecond,
          SaveFolder: config.download.saveFolder,
          Retry: {
            Timeout: config.download.retryTimeout,
            MaxRetries: config.download.maxRetries,
          },
        },
        Job: {
          MaxConcurrentJobs: config.job.maxConcurrentJobs,
        },
        Image: {
          Face: {
            Confidence: config.image.face.confidence,
            PaddingTop: config.image.face.paddingTop,
            PaddingBottom: config.image.face.paddingBottom,
            PaddingLeft: config.image.face.paddingLeft,
            PaddingRight: config.image.face.paddingRight,
            UnionAll: config.image.face.unionAll,
          },
          Saliency: {
            PaddingTop: config.image.saliency.paddingTop,
            PaddingBottom: config.image.saliency.paddingBottom,
            PaddingLeft: config.image.saliency.paddingLeft,
            PaddingRight: config.image.saliency.paddingRight,
          },
        },
      })
      storeBackendUrl(config.server.host, config.server.port)
      showMessage('success', t('settings.saveSuccess'))
    } catch (error) {
      console.error('Failed to save config:', error)
      showMessage('error', formatErrorMessage('settings.saveError', error))
    } finally {
      setSaving(false)
    }
  }

  const reloadConfig = async () => {
    try {
      setLoading(true)
      await backendApi.reloadConfig()
      await loadConfig()
      showMessage('success', t('settings.reloadSuccess'))
    } catch (error) {
      console.error('Failed to reload config:', error)
      showMessage('error', formatErrorMessage('settings.reloadError', error))
    } finally {
      setLoading(false)
    }
  }

  const resetConfig = async () => {
    if (!window.confirm(t('settings.confirmReset'))) return
    try {
      setLoading(true)
      await backendApi.resetConfig()
      await loadConfig()
      showMessage('success', t('settings.resetSuccess'))
    } catch (error) {
      console.error('Failed to reset config:', error)
      showMessage('error', formatErrorMessage('settings.resetError', error))
    } finally {
      setLoading(false)
    }
  }

  const isActiveStatus = (status: string) =>
    ['pending', 'running'].includes(status.toLowerCase())
  const hasActiveJobs = groups.some((group) => {
    if (isActiveStatus(group.status)) return true
    return Object.values(group.sheets).some((sheet) => isActiveStatus(sheet.status))
  })
  const canEditConfig = !hasActiveJobs
  const isEditable = !loading && !!config && canEditConfig

  const updateFace = (patch: Partial<ConfigState['image']['face']>) => {
    if (!config) return
    setConfig({
      ...config,
      image: {
        ...config.image,
        face: { ...config.image.face, ...patch },
      },
    })
  }

  const updateSaliency = (patch: Partial<ConfigState['image']['saliency']>) => {
    if (!config) return
    setConfig({
      ...config,
      image: {
        ...config.image,
        saliency: { ...config.image.saliency, ...patch },
      },
    })
  }

  const createPadStyles = (padding: {
    paddingTop: number
    paddingBottom: number
    paddingLeft: number
    paddingRight: number
  }) => {
    const baseInset = 10
    const detectInset = 25
    const range = detectInset - baseInset
    const clamp01 = (value: number) => Math.min(1, Math.max(0, Number.isFinite(value) ? value : 0))
    const resolveInset = (value: number) => `${detectInset - range * clamp01(value)}%`
    return {
      base: { inset: `${baseInset}%` },
      detect: { inset: `${detectInset}%` },
      crop: {
        top: resolveInset(padding.paddingTop),
        right: resolveInset(padding.paddingRight),
        bottom: resolveInset(padding.paddingBottom),
        left: resolveInset(padding.paddingLeft),
      },
    }
  }

  return (
    <div className="setting-menu">
      <h1 className="menu-title">{t('settings.title')}</h1>

      {message && <div className={`message message-${message.type}`}>{message.text}</div>}
      {activeTab !== 'appearance' && hasActiveJobs && (
        <div className="message message-warning">{t('settings.locked')}</div>
      )}
      <div className="setting-tabs">
        <button
          className={`tab-button ${activeTab === 'appearance' ? 'active' : ''}`}
          onClick={() => setActiveTab('appearance')}
        >
          {t('settings.appearance')}
        </button>
        <button
          className={`tab-button ${activeTab === 'server' ? 'active' : ''}`}
          onClick={() => setActiveTab('server')}
        >
          {t('settings.server')}
        </button>
        <button
          className={`tab-button ${activeTab === 'download' ? 'active' : ''}`}
          onClick={() => setActiveTab('download')}
        >
          {t('settings.download')}
        </button>
        <button
          className={`tab-button ${activeTab === 'job' ? 'active' : ''}`}
          onClick={() => setActiveTab('job')}
        >
          {t('settings.job')}
        </button>
        <button
          className={`tab-button ${activeTab === 'image' ? 'active' : ''}`}
          onClick={() => setActiveTab('image')}
        >
          {t('settings.image')}
        </button>
      </div>

      {activeTab === 'appearance' && (
        <div className="setting-section">
          <h3>{t('settings.appearanceSettings')}</h3>

          <div className="settings-grid">
            <div className="setting-item">
              <label className="setting-label">{t('settings.theme')}</label>
              <select
                className="setting-select"
                value={theme}
                onChange={(e) => setTheme(e.target.value as 'dark' | 'light' | 'system')}
              >
                <option value="dark">{t('settings.themeDark')}</option>
                <option value="light">{t('settings.themeLight')}</option>
                <option value="system">{t('settings.themeSystem')}</option>
              </select>
              <span className="setting-hint">{t('settings.themeHint')}</span>
            </div>

            <div className="setting-item">
              <label className="setting-label">{t('settings.language')}</label>
              <select
                className="setting-select"
                value={language}
                onChange={(e) => setLanguage(e.target.value as 'vi' | 'en')}
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
                <div className="label-text">{t('settings.enableAnimations')}</div>
                <div className="label-description">{t('settings.animationsDesc')}</div>
              </div>
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={enableAnimations}
                  onChange={(e) => setEnableAnimations(e.target.checked)}
                />
                <span className="toggle-slider"></span>
              </label>
            </div>
          </div>

          <div className="setting-item setting-item-toggle">
            <div className="toggle-content">
              <div className="toggle-label">
                <div className="label-text">{t('settings.closeToTray')}</div>
                <div className="label-description">{t('settings.closeToTrayDesc')}</div>
              </div>
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={closeToTray}
                  onChange={(e) => setCloseToTray(e.target.checked)}
                />
                <span className="toggle-slider"></span>
              </label>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'server' && (
        <div className="setting-section">
          <h3>{t('settings.serverSettings')}</h3>
          {loading || !config ? (
            <div className="loading">{t('settings.loading')}</div>
          ) : (
            <>
              <div className="settings-grid">
                <div className="setting-item">
                  <label className="setting-label">{t('settings.host')}</label>
                  <input
                    type="text"
                    className="setting-input"
                    value={config.server.host}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      setConfig({ ...config, server: { ...config.server, host: e.target.value } })
                    }
                    placeholder="127.0.0.1"
                  />
                  <span className="setting-hint">{t('settings.hostHint')}</span>
                </div>

                <div className="setting-item">
                  <label className="setting-label">{t('settings.port')}</label>
                  <input
                    type="number"
                    className="setting-input"
                    value={config.server.port}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      setConfig({
                        ...config,
                        server: { ...config.server, port: Number(e.target.value) },
                      })
                    }
                    min="1"
                    max="65535"
                  />
                  <span className="setting-hint">{t('settings.portHint')}</span>
                </div>
              </div>

              <div className="setting-item setting-item-toggle">
                <div className="toggle-content">
                  <div className="toggle-label">
                    <div className="label-text">{t('settings.debugMode')}</div>
                    <div className="label-description">{t('settings.debugModeDesc')}</div>
                  </div>
                  <label className="toggle-switch">
                    <input
                      type="checkbox"
                      checked={config.server.debug}
                      disabled={!canEditConfig}
                      onChange={(e) =>
                        setConfig({
                          ...config,
                          server: { ...config.server, debug: e.target.checked },
                        })
                      }
                    />
                    <span className="toggle-slider"></span>
                  </label>
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {activeTab === 'download' && (
        <div className="setting-section">
          <h3>{t('settings.downloadSettings')}</h3>
          {loading || !config ? (
            <div className="loading">{t('settings.loading')}</div>
          ) : (
            <>
              <div className="setting-item">
                <label className="setting-label">{t('settings.saveFolder')}</label>
                <div className="input-group">
                  <input
                    type="text"
                    className="setting-input"
                    value={config.download.saveFolder}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      setConfig({
                        ...config,
                        download: { ...config.download, saveFolder: e.target.value },
                      })
                    }
                    placeholder="./downloads"
                  />
                  <button
                    className="browse-btn"
                    disabled={!canEditConfig}
                    onClick={async () => {
                      if (!window.electronAPI) return
                      const folder = await window.electronAPI.openFolder()
                      if (folder) {
                        setConfig({
                          ...config,
                          download: { ...config.download, saveFolder: folder },
                        })
                      }
                    }}
                  >
                    {t('input.browse')}
                  </button>
                </div>
                <span className="setting-hint">{t('settings.saveFolderHint')}</span>
              </div>

              <div className="settings-grid">
                <div className="setting-item">
                  <label className="setting-label">{t('settings.maxChunks')}</label>
                  <input
                    type="number"
                    className="setting-input"
                    value={config.download.maxChunks}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      setConfig({
                        ...config,
                        download: { ...config.download, maxChunks: Number(e.target.value) },
                      })
                    }
                    min="1"
                    max="128"
                  />
                  <span className="setting-hint">{t('settings.maxChunksHint')}</span>
                </div>

                <div className="setting-item">
                  <label className="setting-label">{t('settings.speedLimit')}</label>
                  <input
                    type="number"
                    className="setting-input"
                    value={config.download.limitBytesPerSecond}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      setConfig({
                        ...config,
                        download: { ...config.download, limitBytesPerSecond: Number(e.target.value) },
                      })
                    }
                    min="0"
                  />
                  <span className="setting-hint">{t('settings.speedLimitHint')}</span>
                </div>
              </div>

              <div className="settings-grid">
                <div className="setting-item">
                  <label className="setting-label">{t('settings.retryTimeout')}</label>
                  <input
                    type="number"
                    className="setting-input"
                    value={config.download.retryTimeout}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      setConfig({
                        ...config,
                        download: { ...config.download, retryTimeout: Number(e.target.value) },
                      })
                    }
                    min="1"
                  />
                  <span className="setting-hint">{t('settings.retryTimeoutHint')}</span>
                </div>

                <div className="setting-item">
                  <label className="setting-label">{t('settings.maxRetries')}</label>
                  <input
                    type="number"
                    className="setting-input"
                    value={config.download.maxRetries}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      setConfig({
                        ...config,
                        download: { ...config.download, maxRetries: Number(e.target.value) },
                      })
                    }
                    min="0"
                    max="10"
                  />
                  <span className="setting-hint">{t('settings.maxRetriesHint')}</span>
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {activeTab === 'job' && (
        <div className="setting-section">
          <h3>{t('settings.jobSettings')}</h3>
          {loading || !config ? (
            <div className="loading">{t('settings.loading')}</div>
          ) : (
            <div className="setting-item">
              <label className="setting-label">{t('settings.maxConcurrentJobs')}</label>
              <input
                type="number"
                className="setting-input"
                value={config.job.maxConcurrentJobs}
                disabled={!canEditConfig}
                onChange={(e) =>
                  setConfig({
                    ...config,
                    job: { maxConcurrentJobs: Number(e.target.value) },
                  })
                }
                min="1"
                max="32"
              />
              <span className="setting-hint">{t('settings.maxConcurrentJobsHint')}</span>
            </div>
          )}
        </div>
      )}

      {activeTab === 'image' && (
        <div className="setting-section">
          <h3>{t('settings.imageSettings')}</h3>
          {loading || !config ? (
            <div className="loading">{t('settings.loading')}</div>
          ) : (
            <>
              <div className="image-config-block">
                <div className="image-config-header">
                  <div>
                    <h4>{t('settings.imageFace')}</h4>
                    <span className="setting-hint">{t('settings.imageFaceHint')}</span>
                  </div>
                </div>
                <div className="image-config-grid">
                  <div className="image-padding-layout">
                    <div className="pad-item pad-top">
                      <label className="setting-label">{t('settings.paddingTop')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.face.paddingTop}
                        disabled={!canEditConfig}
                        onChange={(e) => updateFace({ paddingTop: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                    <div className="pad-item pad-left">
                      <label className="setting-label">{t('settings.paddingLeft')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.face.paddingLeft}
                        disabled={!canEditConfig}
                        onChange={(e) => updateFace({ paddingLeft: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                    <div className="pad-center">
                      <div className="pad-diagram">
                        <div
                          className="pad-box pad-base"
                          style={createPadStyles(config.image.face).base}
                        ></div>
                        <div
                          className="pad-box pad-detect"
                          style={createPadStyles(config.image.face).detect}
                        ></div>
                        <div
                          className="pad-box pad-crop"
                          style={createPadStyles(config.image.face).crop}
                        ></div>
                      </div>
                    </div>
                    <div className="pad-item pad-right">
                      <label className="setting-label">{t('settings.paddingRight')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.face.paddingRight}
                        disabled={!canEditConfig}
                        onChange={(e) => updateFace({ paddingRight: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                    <div className="pad-item pad-bottom">
                      <label className="setting-label">{t('settings.paddingBottom')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.face.paddingBottom}
                        disabled={!canEditConfig}
                        onChange={(e) => updateFace({ paddingBottom: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                  </div>
                  <div className="image-config-side">
                    <div className="setting-item">
                      <label className="setting-label">{t('settings.imageConfidence')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.face.confidence}
                        disabled={!canEditConfig}
                        onChange={(e) => updateFace({ confidence: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                    <div className="setting-item setting-item-toggle">
                      <div className="toggle-content">
                        <div className="toggle-label">
                          <div className="label-text">{t('settings.imageUnionAll')}</div>
                        </div>
                        <label className="toggle-switch">
                          <input
                            type="checkbox"
                            checked={config.image.face.unionAll}
                            disabled={!canEditConfig}
                            onChange={(e) => updateFace({ unionAll: e.target.checked })}
                          />
                          <span className="toggle-slider"></span>
                        </label>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="image-config-block">
                <div className="image-config-header">
                  <div>
                    <h4>{t('settings.imageSaliency')}</h4>
                    <span className="setting-hint">{t('settings.imageSaliencyHint')}</span>
                  </div>
                </div>
                <div className="image-config-grid">
                  <div className="image-padding-layout">
                    <div className="pad-item pad-top">
                      <label className="setting-label">{t('settings.paddingTop')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.saliency.paddingTop}
                        disabled={!canEditConfig}
                        onChange={(e) => updateSaliency({ paddingTop: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                    <div className="pad-item pad-left">
                      <label className="setting-label">{t('settings.paddingLeft')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.saliency.paddingLeft}
                        disabled={!canEditConfig}
                        onChange={(e) => updateSaliency({ paddingLeft: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                    <div className="pad-center">
                      <div className="pad-diagram">
                        <div
                          className="pad-box pad-base"
                          style={createPadStyles(config.image.saliency).base}
                        ></div>
                        <div
                          className="pad-box pad-detect"
                          style={createPadStyles(config.image.saliency).detect}
                        ></div>
                        <div
                          className="pad-box pad-crop"
                          style={createPadStyles(config.image.saliency).crop}
                        ></div>
                      </div>
                    </div>
                    <div className="pad-item pad-right">
                      <label className="setting-label">{t('settings.paddingRight')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.saliency.paddingRight}
                        disabled={!canEditConfig}
                        onChange={(e) => updateSaliency({ paddingRight: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                    <div className="pad-item pad-bottom">
                      <label className="setting-label">{t('settings.paddingBottom')}</label>
                      <input
                        type="number"
                        className="setting-input"
                        value={config.image.saliency.paddingBottom}
                        disabled={!canEditConfig}
                        onChange={(e) => updateSaliency({ paddingBottom: Number(e.target.value) })}
                        min="0"
                        max="1"
                        step="0.01"
                      />
                      <span className="setting-hint">{t('settings.paddingHint')}</span>
                    </div>
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {activeTab !== 'appearance' && (
        <div className="setting-actions">
          <button className="btn btn-primary" onClick={saveConfig} disabled={!isEditable || saving}>
            {saving ? t('settings.saving') : t('settings.save')}
          </button>
          <button className="btn btn-secondary" onClick={reloadConfig} disabled={!isEditable}>
            {t('settings.reload')}
          </button>
          <button className="btn btn-danger" onClick={resetConfig} disabled={!isEditable}>
            {t('settings.resetToDefaults')}
          </button>
        </div>
      )}
    </div>
  )
}

export default SettingMenu
