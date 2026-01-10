import React, { useMemo, useState } from 'react'
import { useApp } from '../contexts/useApp'
import { useJobs } from '../contexts/useJobs'
import type { SheetJob } from '../contexts/JobContextType'
import { getBackendBaseUrl } from '../services/signalrClient'
import { getAssetPath } from '../utils/paths'
import '../styles/ProcessMenu.css'

type LogEntry = {
  message: string
  level?: string
  timestamp?: string
  row?: number
  rowStatus?: string
}

type RowLogGroup = {
  key: string
  row?: number
  status?: string
  entries: LogEntry[]
}

type SheetItemProps = {
  sheet: SheetJob
  showLog: boolean
  logGroups: RowLogGroup[]
  collapsedRowGroups: Record<string, boolean>
  statusKey: (status: string) => string
  progressColor: (status: string) => string
  formatLogEntry: (entry: LogEntry, jobLabel?: string) => string
  onToggleLog: () => void
  onToggleRowGroup: (key: string) => void
  onSheetAction: () => void
  onStopSheet: () => void
  onCopyLogs: () => void
  t: (key: string) => string
}

const getSheetStats = (sheet: SheetJob) => {
  const completedSlides = Math.min(sheet.currentRow, sheet.totalRows)
  const failedSlides =
    sheet.status === 'Failed' || sheet.status === 'Cancelled'
      ? Math.max(sheet.totalRows - completedSlides, 0)
      : 0
  const processingSlides =
    sheet.status === 'Running'
      ? Math.max(sheet.totalRows - completedSlides, 0)
      : sheet.status === 'Pending'
        ? sheet.totalRows
        : 0
  return { completedSlides, failedSlides, processingSlides }
}

const SheetItem: React.FC<SheetItemProps> = ({
  sheet,
  showLog,
  logGroups,
  collapsedRowGroups,
  statusKey,
  progressColor,
  formatLogEntry,
  onToggleLog,
  onToggleRowGroup,
  onSheetAction,
  onStopSheet,
  onCopyLogs,
  t,
}) => {
  const { completedSlides, failedSlides, processingSlides } = getSheetStats(sheet)
  const isPaused = sheet.status === 'Paused'
  const canControl = ['Running', 'Paused', 'Pending'].includes(sheet.status)
  const jobIdLabel = sheet.hangfireJobId ? `#${sheet.hangfireJobId}` : '-'
  const logJobLabel = sheet.hangfireJobId ? `#${sheet.hangfireJobId}` : sheet.id

  return (
    <div className="file-item">
      <div className="file-header-clickable" onClick={onToggleLog}>
        <span className="file-expand-icon">{showLog ? 'v' : '>'}</span>
        <div className="file-info">
          <div className="file-name-row">
            <div className="file-name">{sheet.sheetName}</div>
            <span className="file-job-id">
              {t('process.jobId')}: {jobIdLabel}
            </span>
          </div>
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
              {failedSlides}
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
          {canControl && (
            <button
              className="file-action-btn"
              onClick={(e) => {
                e.stopPropagation()
                onSheetAction()
              }}
              title={isPaused ? t('process.resume') : t('process.pause')}
            >
              <img
                src={
                  isPaused
                    ? getAssetPath('images', 'resume.png')
                    : getAssetPath('images', 'pause.png')
                }
                alt={isPaused ? 'Resume' : 'Pause'}
                className="btn-icon-small"
              />
            </button>
          )}
          {canControl && (
            <button
              className="file-action-btn file-action-btn-danger"
              onClick={(e) => {
                e.stopPropagation()
                onStopSheet()
              }}
              title={t('process.stop')}
            >
              <img src={getAssetPath('images', 'stop.png')} alt="Stop" className="btn-icon-small" />
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
            <button className="copy-log-btn" onClick={onCopyLogs} title="Copy log">
              <img src={getAssetPath('images', 'clipboard.png')} alt="Copy" className="log-icon" />
            </button>
          </div>
          <div className="log-content">
            {sheet.logs.length === 0 ? (
              <div className="log-empty">{t('process.noLogs')}</div>
            ) : (
              logGroups.map((group) => {
                const rowKey = `${sheet.id}:${group.key}`
                const isCollapsed = collapsedRowGroups[rowKey] ?? true
                return (
                  <div
                    key={group.key}
                    className="log-row-group"
                    data-status={(group.status ?? 'info').toLowerCase()}
                  >
                    <div className="log-row-header" onClick={() => onToggleRowGroup(rowKey)}>
                      <span className="log-row-toggle">{isCollapsed ? '>' : 'v'}</span>
                      <span className="log-row-title">
                        {group.row != null ? `Row ${group.row}` : t('process.logGeneral')}
                      </span>
                      {group.status && (
                        <span className="log-row-status">{group.status.toUpperCase()}</span>
                      )}
                    </div>
                    {!isCollapsed && (
                      <div className="log-row-entries">
                        {group.entries.map((entry, index) => (
                          <div
                            key={`${group.key}-${index}`}
                            className={`log-entry log-${(entry.level ?? 'info').toLowerCase()}`}
                          >
                            {formatLogEntry(entry, logJobLabel)}
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )
              })
            )}
          </div>
        </div>
      )}
    </div>
  )
}

const ProcessMenu: React.FC = () => {
  const { t } = useApp()
  const {
    groups,
    groupControl,
    jobControl,
    globalControl,
    loadSheetLogs,
    exportGroupConfig,
    hasGroupConfig,
  } = useJobs()
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({})
  const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>({})
  const [collapsedRowGroups, setCollapsedRowGroups] = useState<Record<string, boolean>>({})

  const toggleGroup = (groupId: string) => {
    setExpandedGroups((prev) => ({ ...prev, [groupId]: !prev[groupId] }))
  }

  const toggleLog = (sheetId: string) => {
    setExpandedLogs((prev) => {
      const next = !prev[sheetId]
      if (next) {
        void loadSheetLogs(sheetId)
      }
      return { ...prev, [sheetId]: next }
    })
  }

  const statusKey = (status: string) => {
    const normalized = status.toLowerCase()
    if (normalized === 'running') return 'processing'
    if (normalized === 'failed') return 'error'
    return normalized
  }

  const progressColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return '#9ca3af'
      case 'running':
        return '#3b82f6'
      case 'paused':
        return '#f59e0b'
      case 'completed':
        return '#10b981'
      case 'error':
      case 'failed':
      case 'cancelled':
        return '#ef4444'
      default:
        return 'var(--accent-primary)'
    }
  }

  const activeGroups = useMemo(
    () =>
      groups.filter(
        (group) => !['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase()),
      ),
    [groups],
  )

  const hasProcessing = useMemo(
    () => activeGroups.some((group) => ['running', 'pending'].includes(group.status.toLowerCase())),
    [activeGroups],
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

  const handleExportGroup = async (groupId: string) => {
    await exportGroupConfig(groupId)
  }

  const handleStopSheet = async (sheetId: string) => {
    if (confirm(`${t('process.stop')}?`)) {
      await jobControl(sheetId, 'Stop')
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

  const formatLogEntry = (entry: LogEntry, jobLabel?: string) => {
    const time = entry.timestamp ? `[${new Date(entry.timestamp).toLocaleTimeString()}] ` : ''
    const level = entry.level ? `${entry.level}: ` : ''
    const job = jobLabel ? `${jobLabel}: ` : ''
    return `${time}${level}${job}${entry.message}`
  }

  const groupLogsByRow = (logs: LogEntry[]): RowLogGroup[] => {
    const groups: RowLogGroup[] = []
    const map = new Map<string, RowLogGroup>()
    logs.forEach((entry) => {
      const key = entry.row != null ? `row:${entry.row}` : 'general'
      let group = map.get(key)
      if (!group) {
        group = {
          key,
          row: entry.row,
          status: entry.rowStatus,
          entries: [],
        }
        map.set(key, group)
        groups.push(group)
      }
      group.entries.push(entry)
      if (entry.rowStatus) group.status = entry.rowStatus
    })
    return groups
  }

  const summarizeSheets = (
    sheets: Array<{ status: string; totalRows?: number; currentRow?: number }>,
  ) => {
    let completed = 0
    let processing = 0
    let failed = 0
    let totalRows = 0
    let completedRows = 0

    sheets.forEach((sheet) => {
      if (sheet.status === 'Completed') completed += 1
      if (sheet.status === 'Running' || sheet.status === 'Pending') processing += 1
      if (sheet.status === 'Failed' || sheet.status === 'Cancelled') failed += 1

      const total = sheet.totalRows ?? 0
      totalRows += total
      completedRows += Math.min(sheet.currentRow ?? 0, total)
    })

    return { completed, processing, failed, totalRows, completedRows }
  }

  const toggleRowGroup = (key: string) => {
    setCollapsedRowGroups((prev) => {
      const current = prev[key] ?? true
      return { ...prev, [key]: !current }
    })
  }

  const formatTime = (value?: string) => {
    if (!value) return ''
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return ''
    return date.toLocaleString()
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
            <img src={getAssetPath('images', 'open.png')} alt="Dashboard" className="btn-icon" />
            <span>{t('process.viewDetails')}</span>
          </button>
          <button
            className="btn btn-primary"
            onClick={handlePauseResumeAll}
            disabled={activeGroups.length === 0}
            title={hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}
          >
            <img
              src={
                hasProcessing
                  ? getAssetPath('images', 'pause.png')
                  : getAssetPath('images', 'resume.png')
              }
              alt={hasProcessing ? 'Pause All' : 'Resume All'}
              className="btn-icon"
            />
            <span>{hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}</span>
          </button>
          <button
            className="btn btn-danger"
            onClick={handleStopAll}
            disabled={activeGroups.length === 0}
            title={t('process.stopAll')}
          >
            <img src={getAssetPath('images', 'stop.png')} alt="Stop All" className="btn-icon" />
            <span>{t('process.stopAll')}</span>
          </button>
        </div>
      </div>

      <div className="process-section">
        {activeGroups.length === 0 ? (
          <div className="empty-state">{t('process.empty')}</div>
        ) : (
          <div className="process-list">
            {activeGroups.map((group) => {
              const sheets = Object.values(group.sheets)
              const { completed, processing, failed, totalRows, completedRows } =
                summarizeSheets(sheets)
              const totalSheets = sheets.length
              const groupProgress =
                totalRows > 0 ? (completedRows / totalRows) * 100 : group.progress
              const groupName = deriveGroupName(group.workbookPath, group.id)
              const showDetails = expandedGroups[group.id] ?? false

              return (
                <div key={group.id} className="process-group">
                  <div className="group-header" onClick={() => toggleGroup(group.id)}>
                    <div className="group-main-info">
                      <span className={`expand-icon ${showDetails ? 'expanded' : ''}`}>
                        {showDetails ? 'v' : '>'}
                      </span>
                      <div className="group-info">
                        <div className="group-name-row">
                          <div className="group-name">{groupName}</div>
                          {group.createdAt && (
                            <span className="group-time">
                              {t('process.createdAt')}: {formatTime(group.createdAt)}
                            </span>
                          )}
                        </div>
                        <div className="group-stats-line">
                          <span>
                            {completed}/{totalSheets} - {Math.round(groupProgress)}%
                          </span>
                          <span
                            className="stat-badge stat-success"
                            title={t('process.successSlides')}
                          >
                            {completed}
                          </span>
                          <span className="stat-divider">|</span>
                          <span
                            className="stat-badge stat-processing"
                            title={t('process.processingSlides')}
                          >
                            {processing}
                          </span>
                          <span className="stat-divider">|</span>
                          <span
                            className="stat-badge stat-failed"
                            title={t('process.failedSlides')}
                          >
                            {failed}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div className="group-actions" onClick={(e) => e.stopPropagation()}>
                      <button
                        className="process-btn process-btn-icon-only"
                        onClick={() => handleExportGroup(group.id)}
                        disabled={!hasGroupConfig(group.id)}
                        aria-label={t('results.exportConfig')}
                        title={t('results.exportConfig')}
                      >
                        <img
                          src={getAssetPath('images', 'export-settings.png')}
                          alt=""
                          className="btn-icon"
                        />
                      </button>
                      <button
                        className="process-btn process-btn-icon"
                        onClick={() => handleGroupAction(group.id, group.status)}
                        title={group.status === 'Paused' ? t('process.resume') : t('process.pause')}
                      >
                        <img
                          src={
                            group.status === 'Paused'
                              ? getAssetPath('images', 'resume.png')
                              : getAssetPath('images', 'pause.png')
                          }
                          alt={group.status === 'Paused' ? 'Resume' : 'Pause'}
                          className="btn-icon"
                        />
                      </button>
                      <button
                        className="process-btn process-btn-danger process-btn-icon"
                        onClick={() => handleStopGroup(group.id)}
                        title={t('process.stop')}
                      >
                        <img
                          src={getAssetPath('images', 'stop.png')}
                          alt="Stop"
                          className="btn-icon"
                        />
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
                        const logGroups = showLog ? groupLogsByRow(sheet.logs as LogEntry[]) : []

                        return (
                          <SheetItem
                            key={sheet.id}
                            sheet={sheet}
                            showLog={showLog}
                            logGroups={logGroups}
                            collapsedRowGroups={collapsedRowGroups}
                            statusKey={statusKey}
                            progressColor={progressColor}
                            formatLogEntry={formatLogEntry}
                            onToggleLog={() => toggleLog(sheet.id)}
                            onToggleRowGroup={toggleRowGroup}
                            onSheetAction={() => handleSheetAction(sheet.id, sheet.status)}
                            onStopSheet={() => handleStopSheet(sheet.id)}
                            onCopyLogs={() => {
                              const logJobLabel = sheet.hangfireJobId
                                ? `#${sheet.hangfireJobId}`
                                : sheet.id
                              navigator.clipboard.writeText(
                                sheet.logs
                                  .map((entry) => formatLogEntry(entry as LogEntry, logJobLabel))
                                  .join('\n'),
                              )
                            }}
                            t={t}
                          />
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
