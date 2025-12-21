import React, { useMemo, useState } from 'react'
import { useApp } from '../contexts/AppContext'
import { useJobs } from '../contexts/JobContext'
import '../styles/OutputMenu.css'

const OutputMenu: React.FC = () => {
  const { t } = useApp()
  const { groups, clearCompleted } = useJobs()
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({})
  const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>({})

  const completedGroups = useMemo(
    () => groups.filter((group) =>
      ['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase())
    ),
    [groups]
  )

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

  const deriveGroupName = (workbookPath: string, fallback: string) => {
    if (!workbookPath) return fallback
    const parts = workbookPath.split(/[/\\]/)
    return parts[parts.length - 1] || fallback
  }

  const handleOpenFolder = async (folderPath: string | undefined) => {
    if (!folderPath || !window.electronAPI) return
    await window.electronAPI.openPath(folderPath)
  }

  return (
    <div className="output-menu">
        <div className="menu-header">
          <h1 className="menu-title">{t('output.title')}</h1>
          <div className="header-actions">
            <button
              className="btn btn-danger"
              onClick={() => clearCompleted()}
              disabled={completedGroups.length === 0}
              title={t('output.clearAll')}
            >
              <img src="/assets/images/remove.png" alt="Clear" className="btn-icon" />
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
              const completed = sheets.filter((sheet) => sheet.status === 'Completed').length
              const failed = sheets.filter((sheet) =>
                ['Failed', 'Cancelled'].includes(sheet.status)
              ).length
              const groupProgress = sheets.length
                ? sheets.reduce((sum, sheet) => sum + sheet.progress, 0) / sheets.length
                : group.progress
              const groupName = deriveGroupName(group.workbookPath, group.id)
              const showDetails = expandedGroups[group.id] ?? false

              return (
                <div key={group.id} className="output-group">
                  <div className="group-header" onClick={() => toggleGroup(group.id)}>
                    <div className="group-main-info">
                      <span className={`expand-icon ${showDetails ? 'expanded' : ''}`}>{showDetails ? 'v' : '>'}</span>
                      <div className="group-info">
                        <div className="group-name">{groupName}</div>
                        <div className="group-stats-line">
                          <span>{completed}/{sheets.length} - {Math.round(groupProgress)}%</span>
                          <span className="stat-badge stat-success" title={t('process.successSlides')}>
                            {completed}
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
                        className="output-btn"
                        onClick={() => handleOpenFolder(group.outputFolder)}
                        disabled={!group.outputFolder}
                      >
                        <img src="/assets/images/folder.png" alt="Open Folder" className="btn-icon" />
                        <span>{t('output.openFolder')}</span>
                      </button>
                    </div>
                  </div>

                  {showDetails && (
                    <div className="files-list">
                      {sheets.map((sheet) => {
                        const showLog = expandedLogs[sheet.id] ?? false

                        return (
                          <div key={sheet.id} className="file-item">
                            <div className="file-header-clickable" onClick={() => toggleLog(sheet.id)}>
                              <span className="file-expand-icon">{showLog ? 'v' : '>'}</span>
                              <div className="file-info">
                                <div className="file-name">{sheet.sheetName}</div>
                                <div className="file-stats">
                                  <span className="file-stat-badge stat-success" title={t('process.successSlides')}>
                                    {Math.min(sheet.currentRow, sheet.totalRows)}
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
                              <div className="file-status" data-status={statusKey(sheet.status)}>
                                {t(`process.status.${statusKey(sheet.status)}`)}
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

export default OutputMenu
