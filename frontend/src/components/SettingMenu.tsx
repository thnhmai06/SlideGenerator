import React, { useEffect, useState, useCallback, useRef } from 'react'
import { useApp } from '../contexts/useApp'
import { useJobs } from '../contexts/useJobs'
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

const getErrorDetail = (error: unknown): string => {
  if (error instanceof Error && error.message) return error.message
  if (typeof error === 'string') return error
  if (error && typeof error === 'object' && 'message' in error) {
    const value = (error as { message?: string }).message
    if (value) return value
  }
  return ''
}

const getCaseInsensitive = <T extends Record<string, unknown>>(
  obj: T | null | undefined,
  key: string,
) => {
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
  const [initialServer, setInitialServer] = useState<ConfigState['server'] | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState<{
    type: 'success' | 'error' | 'warning'
    text: string
  } | null>(null)
  const [restartRequired, setRestartRequired] = useState(false)
  const [showRestartBanner, setShowRestartBanner] = useState(false)
  const [isRestartBannerClosing, setIsRestartBannerClosing] = useState(false)
  const [showStatusBanner, setShowStatusBanner] = useState(false)
  const [isStatusBannerClosing, setIsStatusBannerClosing] = useState(false)
  const statusHideTimeoutRef = useRef<number | null>(null)
  const statusCloseTimeoutRef = useRef<number | null>(null)

  const formatErrorMessage = useCallback(
    (key: string, error: unknown) => {
      const detail = getErrorDetail(error)
      return detail ? `${t(key)}: ${detail}` : t(key)
    },
    [t],
  )

  const clearStatusTimeouts = useCallback(() => {
    if (statusHideTimeoutRef.current) {
      window.clearTimeout(statusHideTimeoutRef.current)
      statusHideTimeoutRef.current = null
    }
    if (statusCloseTimeoutRef.current) {
      window.clearTimeout(statusCloseTimeoutRef.current)
      statusCloseTimeoutRef.current = null
    }
  }, [])

  const hideStatusBanner = useCallback(() => {
    clearStatusTimeouts()
    setIsStatusBannerClosing(true)
    statusCloseTimeoutRef.current = window.setTimeout(() => {
      setShowStatusBanner(false)
      setIsStatusBannerClosing(false)
      setMessage(null)
      statusCloseTimeoutRef.current = null
    }, 180)
  }, [clearStatusTimeouts])

  const showMessage = useCallback(
    (type: 'success' | 'error' | 'warning', text: string) => {
      clearStatusTimeouts()
      setMessage({ type, text })
      setShowStatusBanner(true)
      setIsStatusBannerClosing(false)
      statusHideTimeoutRef.current = window.setTimeout(() => {
        hideStatusBanner()
        statusHideTimeoutRef.current = null
      }, 5000)
    },
    [clearStatusTimeouts, hideStatusBanner],
  )

  const handleNumberChange = useCallback((value: string, apply: (next: number) => void) => {
    const next = value === '' ? Number.NaN : Number(value)
    apply(next)
  }, [])

  const handleNumberBlur = useCallback((value: string, apply: (next: number) => void) => {
    if (value === '') apply(0)
  }, [])

  const handleNumberFocus = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
    event.currentTarget.select()
  }, [])

  const PENDING_BACKEND_URL_KEY = 'slidegen.backend.url.pending'
  const PENDING_BACKEND_URL_SESSION_KEY = 'slidegen.backend.url.pending.defer'

  const buildBackendUrl = useCallback((host: string, port: number) => {
    if (!host || !port) return
    const trimmedHost = host.trim()
    if (!trimmedHost) return

    const hasScheme = /^https?:\/\//i.test(trimmedHost)
    const base = hasScheme ? trimmedHost : `http://${trimmedHost}`
    const normalizedHost = base.replace(/^(https?:\/\/)localhost(?=[:/]|$)/i, '$1127.0.0.1')
    const normalizedBase = normalizedHost.endsWith('/')
      ? normalizedHost.slice(0, -1)
      : normalizedHost
    const hasPort = /:\d+$/.test(normalizedBase)
    return hasPort ? normalizedBase : `${normalizedBase}:${port}`
  }, [])

  const normalizeBackendUrl = useCallback((url: string) => {
    const trimmed = url.trim()
    if (!trimmed) return ''
    const withScheme = /^https?:\/\//i.test(trimmed) ? trimmed : `http://${trimmed}`
    const normalizedHost = withScheme.replace(/^(https?:\/\/)localhost(?=[:/]|$)/i, '$1127.0.0.1')
    return normalizedHost.endsWith('/') ? normalizedHost.slice(0, -1) : normalizedHost
  }, [])

  const storeBackendUrl = useCallback(
    (host: string, port: number) => {
      const url = buildBackendUrl(host, port)
      if (!url) return
      localStorage.setItem('slidegen.backend.url', url)
    },
    [buildBackendUrl],
  )

  const storePendingBackendUrl = useCallback(
    (host: string, port: number) => {
      const url = buildBackendUrl(host, port)
      if (!url) return
      localStorage.setItem(PENDING_BACKEND_URL_KEY, url)
      sessionStorage.setItem(PENDING_BACKEND_URL_SESSION_KEY, '1')
    },
    [PENDING_BACKEND_URL_KEY, PENDING_BACKEND_URL_SESSION_KEY, buildBackendUrl],
  )

  const clearPendingBackendUrl = useCallback(() => {
    localStorage.removeItem(PENDING_BACKEND_URL_KEY)
    sessionStorage.removeItem(PENDING_BACKEND_URL_SESSION_KEY)
  }, [PENDING_BACKEND_URL_KEY, PENDING_BACKEND_URL_SESSION_KEY])

  const hasPendingBackendUrl = useCallback(() => {
    return Boolean(localStorage.getItem(PENDING_BACKEND_URL_KEY))
  }, [PENDING_BACKEND_URL_KEY])

  const loadConfig = useCallback(async () => {
    try {
      setLoading(true)
      const response = await backendApi.getConfig()
      const data = response as backendApi.ConfigGetSuccess
      const server = getCaseInsensitive(data as unknown as Record<string, unknown>, 'Server') as
        | Record<string, unknown>
        | undefined
      const download = getCaseInsensitive(
        data as unknown as Record<string, unknown>,
        'Download',
      ) as Record<string, unknown> | undefined
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
      setInitialServer({ host, port, debug })
      const pendingRestart = hasPendingBackendUrl()
      setRestartRequired(pendingRestart)
      if (!pendingRestart) {
        storeBackendUrl(host, port)
      }
    } catch (error) {
      console.error('Failed to load config:', error)
      showMessage('error', formatErrorMessage('settings.loadError', error))
    } finally {
      setLoading(false)
    }
  }, [formatErrorMessage, hasPendingBackendUrl, showMessage, storeBackendUrl])

  useEffect(() => {
    loadConfig().catch(() => undefined)
  }, [loadConfig])

  useEffect(() => {
    if (restartRequired) {
      setShowRestartBanner(true)
      setIsRestartBannerClosing(false)
      return undefined
    }

    if (!showRestartBanner) return undefined
    setIsRestartBannerClosing(true)
    const timeoutId = window.setTimeout(() => {
      setShowRestartBanner(false)
      setIsRestartBannerClosing(false)
    }, 180)
    return () => window.clearTimeout(timeoutId)
  }, [restartRequired, showRestartBanner])

  const hasServerChanged = (server: ConfigState['server']) => {
    if (!initialServer) return false
    return (
      server.host !== initialServer.host ||
      server.port !== initialServer.port ||
      server.debug !== initialServer.debug
    )
  }

  const saveConfig = async () => {
    if (!config) return
    try {
      setSaving(true)
      let pendingRestart = hasPendingBackendUrl()
      const serverChanged = hasServerChanged(config.server)
      const desiredUrl = buildBackendUrl(config.server.host, config.server.port) ?? ''
      const currentUrl = normalizeBackendUrl(localStorage.getItem('slidegen.backend.url') ?? '')
      if (pendingRestart && desiredUrl && desiredUrl === currentUrl) {
        clearPendingBackendUrl()
        pendingRestart = false
      }
      const requiresRestart = pendingRestart || serverChanged
      const normalizeNumber = (value: number) => (Number.isFinite(value) ? value : 0)
      await backendApi.updateConfig({
        Server: {
          Host: config.server.host,
          Port: normalizeNumber(config.server.port),
          Debug: config.server.debug,
        },
        Download: {
          MaxChunks: normalizeNumber(config.download.maxChunks),
          LimitBytesPerSecond: normalizeNumber(config.download.limitBytesPerSecond),
          SaveFolder: config.download.saveFolder,
          Retry: {
            Timeout: normalizeNumber(config.download.retryTimeout),
            MaxRetries: normalizeNumber(config.download.maxRetries),
          },
        },
        Job: {
          MaxConcurrentJobs: normalizeNumber(config.job.maxConcurrentJobs),
        },
        Image: {
          Face: {
            Confidence: normalizeNumber(config.image.face.confidence),
            PaddingTop: normalizeNumber(config.image.face.paddingTop),
            PaddingBottom: normalizeNumber(config.image.face.paddingBottom),
            PaddingLeft: normalizeNumber(config.image.face.paddingLeft),
            PaddingRight: normalizeNumber(config.image.face.paddingRight),
            UnionAll: config.image.face.unionAll,
          },
          Saliency: {
            PaddingTop: normalizeNumber(config.image.saliency.paddingTop),
            PaddingBottom: normalizeNumber(config.image.saliency.paddingBottom),
            PaddingLeft: normalizeNumber(config.image.saliency.paddingLeft),
            PaddingRight: normalizeNumber(config.image.saliency.paddingRight),
          },
        },
      })
      if (!serverChanged) {
        setInitialServer({ ...config.server })
      }
      setRestartRequired(requiresRestart)
      if (serverChanged) {
        storePendingBackendUrl(config.server.host, config.server.port)
      } else if (!pendingRestart) {
        storeBackendUrl(config.server.host, config.server.port)
      }
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

  const handleRestartServer = async () => {
    if (!window.electronAPI?.restartBackend) {
      showMessage('warning', t('settings.restartUnavailable'))
      return
    }
    try {
      const restarted = await window.electronAPI.restartBackend()
      if (restarted) {
        setRestartRequired(false)
        if (config) {
          storeBackendUrl(config.server.host, config.server.port)
          clearPendingBackendUrl()
          await loadConfig()
        }
        showMessage('success', t('settings.restartSuccess'))
      } else {
        showMessage('error', t('settings.restartError'))
      }
    } catch (error) {
      console.error('Failed to restart server:', error)
      showMessage('error', formatErrorMessage('settings.restartError', error))
    }
  }

  const isActiveStatus = (status: string) => ['pending', 'running'].includes(status.toLowerCase())
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

      {showRestartBanner && (
        <div
          className={`message message-warning restart-banner${
            isRestartBannerClosing ? ' restart-banner--closing' : ''
          }`}
        >
          <span className="restart-banner__text">{t('settings.restartRequired')}</span>
          <button
            className="btn btn-secondary restart-banner__action"
            onClick={handleRestartServer}
          >
            {t('settings.restartServer')}
          </button>
        </div>
      )}
      {message && showStatusBanner && (
        <div
          className={`message message-${message.type} status-banner${
            isStatusBannerClosing ? ' status-banner--closing' : ''
          }`}
        >
          {message.text}
        </div>
      )}
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
                    value={Number.isFinite(config.server.port) ? config.server.port : ''}
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      handleNumberChange(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          server: { ...config.server, port: next },
                        }),
                      )
                    }
                    onBlur={(e) =>
                      handleNumberBlur(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          server: { ...config.server, port: next },
                        }),
                      )
                    }
                    onFocus={handleNumberFocus}
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
                    value={
                      Number.isFinite(config.download.maxChunks) ? config.download.maxChunks : ''
                    }
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      handleNumberChange(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: { ...config.download, maxChunks: next },
                        }),
                      )
                    }
                    onBlur={(e) =>
                      handleNumberBlur(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: { ...config.download, maxChunks: next },
                        }),
                      )
                    }
                    onFocus={handleNumberFocus}
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
                    value={
                      Number.isFinite(config.download.limitBytesPerSecond)
                        ? config.download.limitBytesPerSecond
                        : ''
                    }
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      handleNumberChange(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: {
                            ...config.download,
                            limitBytesPerSecond: next,
                          },
                        }),
                      )
                    }
                    onBlur={(e) =>
                      handleNumberBlur(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: {
                            ...config.download,
                            limitBytesPerSecond: next,
                          },
                        }),
                      )
                    }
                    onFocus={handleNumberFocus}
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
                    value={
                      Number.isFinite(config.download.retryTimeout)
                        ? config.download.retryTimeout
                        : ''
                    }
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      handleNumberChange(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: { ...config.download, retryTimeout: next },
                        }),
                      )
                    }
                    onBlur={(e) =>
                      handleNumberBlur(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: { ...config.download, retryTimeout: next },
                        }),
                      )
                    }
                    onFocus={handleNumberFocus}
                    min="1"
                  />
                  <span className="setting-hint">{t('settings.retryTimeoutHint')}</span>
                </div>

                <div className="setting-item">
                  <label className="setting-label">{t('settings.maxRetries')}</label>
                  <input
                    type="number"
                    className="setting-input"
                    value={
                      Number.isFinite(config.download.maxRetries) ? config.download.maxRetries : ''
                    }
                    disabled={!canEditConfig}
                    onChange={(e) =>
                      handleNumberChange(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: { ...config.download, maxRetries: next },
                        }),
                      )
                    }
                    onBlur={(e) =>
                      handleNumberBlur(e.target.value, (next) =>
                        setConfig({
                          ...config,
                          download: { ...config.download, maxRetries: next },
                        }),
                      )
                    }
                    onFocus={handleNumberFocus}
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
                value={
                  Number.isFinite(config.job.maxConcurrentJobs) ? config.job.maxConcurrentJobs : ''
                }
                disabled={!canEditConfig}
                onChange={(e) =>
                  handleNumberChange(e.target.value, (next) =>
                    setConfig({
                      ...config,
                      job: { maxConcurrentJobs: next },
                    }),
                  )
                }
                onBlur={(e) =>
                  handleNumberBlur(e.target.value, (next) =>
                    setConfig({
                      ...config,
                      job: { maxConcurrentJobs: next },
                    }),
                  )
                }
                onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.face.paddingTop)
                            ? config.image.face.paddingTop
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateFace({ paddingTop: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateFace({ paddingTop: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.face.paddingLeft)
                            ? config.image.face.paddingLeft
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateFace({ paddingLeft: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateFace({ paddingLeft: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.face.paddingRight)
                            ? config.image.face.paddingRight
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateFace({ paddingRight: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateFace({ paddingRight: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.face.paddingBottom)
                            ? config.image.face.paddingBottom
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateFace({ paddingBottom: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateFace({ paddingBottom: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.face.confidence)
                            ? config.image.face.confidence
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateFace({ confidence: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateFace({ confidence: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.saliency.paddingTop)
                            ? config.image.saliency.paddingTop
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateSaliency({ paddingTop: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateSaliency({ paddingTop: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.saliency.paddingLeft)
                            ? config.image.saliency.paddingLeft
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateSaliency({ paddingLeft: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateSaliency({ paddingLeft: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.saliency.paddingRight)
                            ? config.image.saliency.paddingRight
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateSaliency({ paddingRight: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateSaliency({ paddingRight: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
                        value={
                          Number.isFinite(config.image.saliency.paddingBottom)
                            ? config.image.saliency.paddingBottom
                            : ''
                        }
                        disabled={!canEditConfig}
                        onChange={(e) =>
                          handleNumberChange(e.target.value, (next) =>
                            updateSaliency({ paddingBottom: next }),
                          )
                        }
                        onBlur={(e) =>
                          handleNumberBlur(e.target.value, (next) =>
                            updateSaliency({ paddingBottom: next }),
                          )
                        }
                        onFocus={handleNumberFocus}
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
