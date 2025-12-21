import React, { useMemo, useState } from 'react'
import { useApp } from '../contexts/AppContext'
import { useJobs } from '../contexts/JobContext'
import { getBackendBaseUrl } from '../services/signalrClient'
import '../styles/ProcessMenu.css'

const ProcessMenu: React.FC = () => {
  const { t } = useApp()
  const { groups, groupControl, jobControl, globalControl } = useJobs()
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({})
  const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>({})

  const toggleGroup = (groupId: string) => {
    setExpandedGroups((prev) => ({ ...prev, [groupId]: !prev[groupId] }))
  }

  const toggleLog = (sheetId: string) => {
    setExpandedLogs((prev) => ({ ...prev, [sheetId]: !prev[sheetId] }))
  }

  const statusKey = (status: string) => {
    const normalized = status.toLowerCase()
    if (normalized === 'running') return 'processing'
    if (normalized === 'failed') return 'error'
    return normalized
  }

  const progressColor = (status: string) => {
    switch (statusKey(status)) {
      case 'paused':
        return '#f59e0b'
      case 'completed':
        return '#10b981'
      case 'error':
      case 'failed':
        return '#ef4444'
      case 'cancelled':
        return '#ef4444'
      default:
        return 'var(--accent-primary)'
    }
  }

  const hasProcessing = useMemo(
    () => groups.some((group) => ['running', 'pending'].includes(group.status.toLowerCase())),
    [groups]
  )

  const handlePauseResumeAll = async () => {
    const action = hasProcessing ? 'Pause' : 'Resume'
    await globalControl(action)
  }

  const handleStopAll = async () => {
    if (confirm(`${t('process.stopAll')}?`)) {
      await globalControl('Stop')
    }
  }

  const handleOpenDashboard = async () => {
    const url = `${getBackendBaseUrl()}/hangfire`
    if (window.electronAPI?.openUrl) {
      await window.electronAPI.openUrl(url)
      return
    }
    window.open(url, '_blank')
  }

  const handleGroupAction = async (groupId: string, status: string) => {
    const normalized = status.toLowerCase()
    if (normalized === 'paused') {
      await groupControl(groupId, 'Resume')
      return
    }
    if (normalized === 'running' || normalized === 'pending') {
      await groupControl(groupId, 'Pause')
    }
  }

  const handleStopGroup = async (groupId: string) => {
    if (confirm(`${t('process.stop')}?`)) {
      await groupControl(groupId, 'Stop')
    }
  }

  const handleSheetAction = async (sheetId: string, status: string) => {
    const normalized = status.toLowerCase()
    if (normalized === 'paused') {
      await jobControl(sheetId, 'Resume')
      return
    }
    if (normalized === 'running' || normalized === 'pending') {
      await jobControl(sheetId, 'Pause')
    }
  }

  const deriveGroupName = (workbookPath: string, fallback: string) => {
    if (!workbookPath) return fallback
    const parts = workbookPath.split(/[/\\]/)
    return parts[parts.length - 1] || fallback
  }

  return (
    <div className="process-menu">
      <div className="menu-header">
        <h1 className="menu-title">{t('process.title')}</h1>
        <div className="header-actions">
          <button
            className="btn btn-secondary"
            onClick={handleOpenDashboard}
            title={t('process.viewDetails')}
          >
            <img src="/assets/images/log.png" alt="Dashboard" className="btn-icon" />
            <span>{t('process.viewDetails')}</span>
          </button>
          <button
            className="btn btn-primary"
            onClick={handlePauseResumeAll}
            disabled={groups.length === 0}
            title={hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}
          >
            <img
              src={hasProcessing ? '/assets/images/pause.png' : '/assets/images/resume.png'}
              alt={hasProcessing ? 'Pause All' : 'Resume All'}
              className="btn-icon"
            />
            <span>{hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}</span>
          </button>
          <button
            className="btn btn-danger"
            onClick={handleStopAll}
            disabled={groups.length === 0}
            title={t('process.stopAll')}
          >
            <img
              src="/assets/images/stop.png"
              alt="Stop All"
              className="btn-icon"
            />
            <span>{t('process.stopAll')}</span>
          </button>
        </div>
      </div>

      <div className="process-section">
        {groups.length === 0 ? (
          <div className="empty-state">{t('process.empty')}</div>
        ) : (
          <div className="process-list">
            {groups.map((group) => {
              const sheets = Object.values(group.sheets)
              const completed = sheets.filter((sheet) => sheet.status === 'Completed').length
              const processing = sheets.filter((sheet) =>
                ['Running', 'Pending'].includes(sheet.status)
              ).length
              const failed = sheets.filter((sheet) =>
                ['Failed', 'Cancelled'].includes(sheet.status)
              ).length
              const totalSheets = sheets.length
              const groupProgress = totalSheets
                ? sheets.reduce((sum, sheet) => sum + sheet.progress, 0) / totalSheets
                : group.progress
              const groupName = deriveGroupName(group.workbookPath, group.id)
              const showDetails = expandedGroups[group.id] ?? false

              return (
                <div key={group.id} className="process-group">
                  <div className="group-header" onClick={() => toggleGroup(group.id)}>
                    <div className="group-main-info">
                      <span className={`expand-icon ${showDetails ? 'expanded' : ''}`}>{showDetails ? 'v' : '>'}</span>
                      <div className="group-info">
                        <div className="group-name">{groupName}</div>
                        <div className="group-stats-line">
                          <span>{completed}/{totalSheets} - {Math.round(groupProgress)}%</span>
                          <span className="stat-badge stat-success" title={t('process.successSlides')}>
                            {completed}
                          </span>
                          <span className="stat-divider">|</span>
                          <span className="stat-badge stat-processing" title={t('process.processingSlides')}>
                            {processing}
                          </span>
                          <span className="stat-divider">|</span>
                          <span className="stat-badge stat-failed" title={t('process.failedSlides')}>
                            {failed}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div className="group-actions" onClick={(e) => e.stopPropagation()}>
                      <button
                        className="process-btn process-btn-icon"
                        onClick={() => handleGroupAction(group.id, group.status)}
                        title={group.status === 'Paused' ? t('process.resume') : t('process.pause')}
                      >
                        <img
                          src={group.status === 'Paused' ? '/assets/images/resume.png' : '/assets/images/pause.png'}
                          alt={group.status === 'Paused' ? 'Resume' : 'Pause'}
                          className="btn-icon"
                        />
                      </button>
                      <button
                        className="process-btn process-btn-danger process-btn-icon"
                        onClick={() => handleStopGroup(group.id)}
                        title={t('process.stop')}
                      >
                        <img src="/assets/images/stop.png" alt="Stop" className="btn-icon" />
                      </button>
                    </div>
                  </div>

                  <div className="progress-bar-container">
                    <div
                      className="progress-bar-fill"
                      style={{
                        width: `${Math.round(groupProgress)}%`,
                        backgroundColor: progressColor(group.status),
                      }}
                    />
                  </div>

                  {showDetails && (
                    <div className="files-list">
                      {sheets.map((sheet) => {
                        const showLog = expandedLogs[sheet.id] ?? false
                        const completedSlides = Math.min(sheet.currentRow, sheet.totalRows)
                        const processingSlides =
                          sheet.status === 'Running' ? Math.max(sheet.totalRows - sheet.currentRow, 0) : 0

                        return (
                          <div key={sheet.id} className="file-item">
                            <div className="file-header-clickable" onClick={() => toggleLog(sheet.id)}>
                              <span className="file-expand-icon">{showLog ? 'v' : '>'}</span>
                              <div className="file-info">
                                <div className="file-name">{sheet.sheetName}</div>
                                <div className="file-stats">
                                  <span className="file-stat-badge stat-success" title={t('process.successSlides')}>
                                    {completedSlides}
                                  </span>
                                  <span className="stat-divider">|</span>
                                  <span className="file-stat-badge stat-processing" title={t('process.processingSlides')}>
                                    {processingSlides}
                                  </span>
                                  <span className="stat-divider">|</span>
                                  <span className="file-stat-badge stat-failed" title={t('process.failedSlides')}>
                                    {sheet.errorCount}
                                  </span>
                                  <span className="file-progress-text">
                                    / {sheet.totalRows} {t('process.slides')} - {Math.round(sheet.progress)}%
                                  </span>
                                </div>
                              </div>
                              <div className="file-status-and-actions">
                                <div className="file-status" data-status={statusKey(sheet.status)}>
                                  {t(`process.status.${statusKey(sheet.status)}`)}
                                </div>
                                {(sheet.status === 'Running' || sheet.status === 'Paused' || sheet.status === 'Pending') && (
                                  <button
                                    className="file-action-btn"
                                    onClick={(e) => {
                                      e.stopPropagation()
                                      handleSheetAction(sheet.id, sheet.status)
                                    }}
                                    title={sheet.status === 'Paused' ? t('process.resume') : t('process.pause')}
                                  >
                                    <img
                                      src={sheet.status === 'Paused' ? '/assets/images/resume.png' : '/assets/images/pause.png'}
                                      alt={sheet.status === 'Paused' ? 'Resume' : 'Pause'}
                                      className="btn-icon-small"
                                    />
                                  </button>
                                )}
                              </div>
                            </div>

                            <div className="file-progress-bar">
                              <div
                                className="file-progress-fill"
                                style={{
                                  width: `${Math.round(sheet.progress)}%`,
                                  backgroundColor: progressColor(sheet.status),
                                }}
                              />
                            </div>

                            {showLog && (
                              <div className="file-log-content">
                                <div className="log-header">
                                  {t('process.log')}
                                  <button
                                    className="copy-log-btn"
                                    onClick={(e) => {
                                      e.stopPropagation()
                                      navigator.clipboard.writeText(sheet.logs.join('\n'))
                                    }}
                                    title="Copy log"
                                  >
                                    <img src="/assets/images/clipboard.png" alt="Copy" className="log-icon" />
                                  </button>
                                </div>
                                <div className="log-content">
                                  {sheet.logs.length === 0 ? (
                                    <div className="log-empty">{t('process.noLogs')}</div>
                                  ) : (
                                    sheet.logs.map((entry, index) => (
                                      <div key={index} className="log-entry">{entry}</div>
                                    ))
                                  )}
                                </div>
                              </div>
                            )}
                          </div>
                        )
                      })}
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}

export default ProcessMenu
