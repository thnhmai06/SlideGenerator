import React, { useMemo, useState } from 'react'
import { useApp } from '../contexts/useApp'
import { useJobs } from '../contexts/useJobs'
import { getAssetPath } from '../utils/paths'
import '../styles/ResultMenu.css'

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

const ResultMenu: React.FC = () => {
  const { t } = useApp()
  const {
    groups,
    clearCompleted,
    removeGroup,
    removeSheet,
    loadSheetLogs,
    exportGroupConfig,
    hasGroupConfig,
  } = useJobs()
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({})
  const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>({})
  const [collapsedRowGroups, setCollapsedRowGroups] = useState<Record<string, boolean>>({})

  const completedGroups = useMemo(
    () =>
      groups.filter((group) =>
        ['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase()),
      ),
    [groups],
  )

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

  const deriveGroupName = (workbookPath: string, fallback: string) => {
    if (!workbookPath) return fallback
    const parts = workbookPath.split(/[/\\]/)
    return parts[parts.length - 1] || fallback
  }

  const handleOpenFolder = async (folderPath: string | undefined) => {
    if (!folderPath || !window.electronAPI) return
    await window.electronAPI.openPath(folderPath)
  }

  const handleExportGroup = async (groupId: string) => {
    await exportGroupConfig(groupId)
  }

  const handleOpenFile = async (filePath: string | undefined) => {
    if (!filePath || !window.electronAPI) return
    await window.electronAPI.openPath(filePath)
  }

  const handleRemoveSheet = async (sheetId: string) => {
    await removeSheet(sheetId)
  }

  const handleRemoveGroup = async (groupId: string) => {
    if (confirm(`${t('output.removeGroup')}?`)) {
      await removeGroup(groupId)
    }
  }

  const handleClearAll = async () => {
    if (confirm(`${t('output.clearAll')}?`)) {
      await clearCompleted()
    }
  }

  const formatLogEntry = (entry: LogEntry) => {
    const time = entry.timestamp ? `[${new Date(entry.timestamp).toLocaleTimeString()}] ` : ''
    const level = entry.level ? `${entry.level}: ` : ''
    return `${time}${level}${entry.message}`
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

  const summarizeSheets = (sheets: Array<{ status: string; progress: number }>) => {
    let completed = 0
    let failed = 0
    let totalProgress = 0

    sheets.forEach((sheet) => {
      if (sheet.status === 'Completed') completed += 1
      if (sheet.status === 'Failed' || sheet.status === 'Cancelled') failed += 1
      totalProgress += sheet.progress
    })

    return { completed, failed, totalProgress }
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
    <div className="output-menu">
      <div className="menu-header">
        <h1 className="menu-title">{t('output.title')}</h1>
        <div className="header-actions">
          <button
            className="btn btn-danger"
            onClick={handleClearAll}
            disabled={completedGroups.length === 0}
            title={t('output.clearAll')}
          >
            <img src={getAssetPath('images', 'close.png')} alt="Clear" className="btn-icon" />
            {t('output.clearAll')}
          </button>
        </div>
      </div>

      <div className="output-section">
        {completedGroups.length === 0 ? (
          <div className="empty-state">{t('output.empty')}</div>
        ) : (
          <div className="output-list">
            {completedGroups.map((group) => {
              const sheets = Object.values(group.sheets)
              const { completed, failed, totalProgress } = summarizeSheets(sheets)
              const groupProgress = sheets.length ? totalProgress / sheets.length : group.progress
              const groupName = deriveGroupName(group.workbookPath, group.id)
              const showDetails = expandedGroups[group.id] ?? false

              return (
                <div key={group.id} className="output-group">
                  <div className="group-header" onClick={() => toggleGroup(group.id)}>
                    <div className="group-main-info">
                      <span className={`expand-icon ${showDetails ? 'expanded' : ''}`}>
                        {showDetails ? 'v' : '>'}
                      </span>
                      <div className="group-info">
                        <div className="group-name-row">
                          <div className="group-name">{groupName}</div>
                          {group.completedAt && (
                            <span className="group-time">
                              {t('output.completedAt')}: {formatTime(group.completedAt)}
                            </span>
                          )}
                        </div>
                        <div className="group-stats-line">
                          <span>
                            {completed}/{sheets.length} - {Math.round(groupProgress)}%
                          </span>
                          <span
                            className="stat-badge stat-success"
                            title={t('process.successSlides')}
                          >
                            {completed}
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
                        className="output-btn output-btn-icon-only"
                        onClick={() => handleExportGroup(group.id)}
                        disabled={!hasGroupConfig(group.id)}
                        aria-label={t('output.exportConfig')}
                        title={t('output.exportConfig')}
                      >
                        <img src={getAssetPath('images', 'open.png')} alt="" className="btn-icon" />
                      </button>
                      <button
                        className="output-btn"
                        onClick={() => handleOpenFolder(group.outputFolder)}
                        disabled={!group.outputFolder}
                      >
                        <img
                          src={getAssetPath('images', 'folder.png')}
                          alt="Open Folder"
                          className="btn-icon"
                        />
                        <span>{t('output.openFolder')}</span>
                      </button>
                      <button
                        className="output-btn-danger"
                        onClick={() => handleRemoveGroup(group.id)}
                      >
                        <img
                          src={getAssetPath('images', 'close.png')}
                          alt="Clear Group"
                          className="btn-icon"
                        />
                        <span>{t('output.removeGroup')}</span>
                      </button>
                    </div>
                  </div>

                  {showDetails && (
                    <div className="files-list">
                      {sheets.map((sheet) => {
                        const showLog = expandedLogs[sheet.id] ?? false
                        const completedSlides = Math.min(sheet.currentRow, sheet.totalRows)
                        const failedSlides =
                          sheet.status === 'Failed' || sheet.status === 'Cancelled'
                            ? Math.max(sheet.totalRows - completedSlides, 0)
                            : 0
                        const logGroups = showLog ? groupLogsByRow(sheet.logs as LogEntry[]) : []

                        return (
                          <div key={sheet.id} className="file-item">
                            <div
                              className="file-header-clickable"
                              onClick={() => toggleLog(sheet.id)}
                            >
                              <span className="file-expand-icon">{showLog ? 'v' : '>'}</span>
                              <div className="file-info">
                                <div className="file-name">{sheet.sheetName}</div>
                                <div className="file-stats">
                                  <span
                                    className="file-stat-badge stat-success"
                                    title={t('process.successSlides')}
                                  >
                                    {completedSlides}
                                  </span>
                                  <span className="stat-divider">|</span>
                                  <span
                                    className="file-stat-badge stat-failed"
                                    title={t('process.failedSlides')}
                                  >
                                    {failedSlides}
                                  </span>
                                  <span className="file-progress-text">
                                    / {sheet.totalRows} {t('process.slides')} -{' '}
                                    {Math.round(sheet.progress)}%
                                  </span>
                                </div>
                              </div>
                              <div className="file-status-and-actions">
                                <div className="file-status" data-status={statusKey(sheet.status)}>
                                  {t(`process.status.${statusKey(sheet.status)}`)}
                                </div>
                                <div className="file-action-buttons">
                                  <button
                                    className="file-action-btn"
                                    onClick={(e) => {
                                      e.stopPropagation()
                                      handleOpenFile(sheet.outputPath)
                                    }}
                                    disabled={!sheet.outputPath}
                                    aria-label={t('output.open')}
                                    title={t('output.open')}
                                  >
                                    <img
                                      src={getAssetPath('images', 'open.png')}
                                      alt="Open"
                                      className="file-btn-icon"
                                    />
                                  </button>
                                  <button
                                    className="file-action-btn file-action-btn-danger"
                                    onClick={(e) => {
                                      e.stopPropagation()
                                      handleRemoveSheet(sheet.id)
                                    }}
                                    aria-label={t('output.remove')}
                                    title={t('output.remove')}
                                  >
                                    <img
                                      src={getAssetPath('images', 'close.png')}
                                      alt="Clear"
                                      className="file-btn-icon"
                                    />
                                  </button>
                                </div>
                              </div>
                            </div>

                            {showLog && (
                              <div className="file-log-content">
                                <div className="log-header">
                                  {t('process.log')}
                                  <button
                                    className="copy-log-btn"
                                    onClick={(e) => {
                                      e.stopPropagation()
                                      navigator.clipboard.writeText(
                                        sheet.logs
                                          .map((entry) => formatLogEntry(entry as LogEntry))
                                          .join('\n'),
                                      )
                                    }}
                                    title="Copy log"
                                  >
                                    <img
                                      src={getAssetPath('images', 'clipboard.png')}
                                      alt="Copy"
                                      className="log-icon"
                                    />
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
                                          <div
                                            className="log-row-header"
                                            onClick={() => toggleRowGroup(rowKey)}
                                          >
                                            <span className="log-row-toggle">
                                              {isCollapsed ? '>' : 'v'}
                                            </span>
                                            <span className="log-row-title">
                                              {group.row != null
                                                ? `Row ${group.row}`
                                                : t('process.logGeneral')}
                                            </span>
                                            {group.status && (
                                              <span className="log-row-status">
                                                {group.status.toUpperCase()}
                                              </span>
                                            )}
                                          </div>
                                          {!isCollapsed && (
                                            <div className="log-row-entries">
                                              {group.entries.map((entry, index) => (
                                                <div
                                                  key={`${group.key}-${index}`}
                                                  className={`log-entry log-${(entry.level ?? 'info').toLowerCase()}`}
                                                >
                                                  {formatLogEntry(entry)}
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

export default ResultMenu
