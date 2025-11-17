import React, { useState } from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/ProcessMenu.css'

type ProcessStatus = 'processing' | 'paused' | 'completed' | 'error'
type FileStatus = 'processing' | 'paused' | 'completed' | 'failed'

interface ProcessFile {
  id: number
  name: string
  progress: number
  status: FileStatus
  totalSlides: number
  completedSlides: number
  processingSlides: number
  failedSlides: number
  log: string[]
  showLog: boolean
}

interface ProcessGroup {
  id: number
  name: string
  totalFiles: number
  completed: number
  processing: number
  failed: number
  status: ProcessStatus
  files: ProcessFile[]
  showDetails: boolean
}

const ProcessMenu: React.FC = () => {
  const { t } = useApp()
  const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([
    { 
      id: 1, 
      name: 'Batch 2024-11-17 14:30', 
      totalFiles: 100,
      completed: 65,
      processing: 30,
      failed: 5,
      status: 'processing',
      showDetails: false,
      files: [
        { 
          id: 1, 
          name: 'output_001.pptx', 
          progress: 100,
          status: 'completed',
          totalSlides: 100,
          completedSlides: 100,
          processingSlides: 0,
          failedSlides: 0,
          log: ['Started processing...', 'Completed successfully'],
          showLog: false
        },
        { 
          id: 2, 
          name: 'output_002.pptx', 
          progress: 75,
          status: 'processing',
          totalSlides: 100,
          completedSlides: 70,
          processingSlides: 25,
          failedSlides: 5,
          log: ['Started processing...', 'Processing slide 75...'],
          showLog: false
        },
        { 
          id: 3, 
          name: 'output_003.pptx', 
          progress: 50,
          status: 'paused',
          totalSlides: 100,
          completedSlides: 50,
          processingSlides: 0,
          failedSlides: 0,
          log: ['Started processing...', 'Paused at slide 50'],
          showLog: false
        },
        { 
          id: 4, 
          name: 'output_004.pptx', 
          progress: 30,
          status: 'failed',
          totalSlides: 100,
          completedSlides: 20,
          processingSlides: 0,
          failedSlides: 80,
          log: ['Started processing...', 'Error: File not found'],
          showLog: false
        },
      ]
    },
    { 
      id: 2, 
      name: 'Batch 2024-11-17 15:45', 
      totalFiles: 50,
      completed: 10,
      processing: 20,
      failed: 0,
      status: 'paused',
      showDetails: false,
      files: [
        { 
          id: 5, 
          name: 'output_101.pptx', 
          progress: 40,
          status: 'paused',
          totalSlides: 50,
          completedSlides: 20,
          processingSlides: 0,
          failedSlides: 0,
          log: ['Started processing...', 'Paused'],
          showLog: false
        },
      ]
    },
  ])

  const handleToggleDetails = (groupId: number) => {
    setProcessGroups(processGroups.map(group => 
      group.id === groupId ? { ...group, showDetails: !group.showDetails } : group
    ))
  }

  const handlePauseResumeGroup = (groupId: number) => {
    setProcessGroups(processGroups.map(group => {
      if (group.id === groupId) {
        const newStatus = group.status === 'processing' ? 'paused' : 'processing'
        return { 
          ...group, 
          status: newStatus,
          files: group.files.map(file => ({
            ...file,
            status: file.status === 'processing' ? 'paused' : 
                   file.status === 'paused' ? 'processing' : file.status
          }))
        }
      }
      return group
    }))
  }

  const handleStopGroup = (groupId: number) => {
    const group = processGroups.find(g => g.id === groupId)
    if (confirm(`${t('process.stop')} "${group?.name}"?`)) {
      setProcessGroups(processGroups.filter(g => g.id !== groupId))
    }
  }

  const handleToggleFileLog = (groupId: number, fileId: number) => {
    setProcessGroups(processGroups.map(group => {
      if (group.id === groupId) {
        return {
          ...group,
          files: group.files.map(file =>
            file.id === fileId ? { ...file, showLog: !file.showLog } : file
          )
        }
      }
      return group
    }))
  }

  const handlePauseResumeFile = (groupId: number, fileId: number) => {
    setProcessGroups(processGroups.map(group => {
      if (group.id === groupId) {
        return {
          ...group,
          files: group.files.map(file => {
            if (file.id === fileId) {
              const newStatus = file.status === 'processing' ? 'paused' : 
                               file.status === 'paused' ? 'processing' : file.status
              return { ...file, status: newStatus }
            }
            return file
          })
        }
      }
      return group
    }))
  }

  const handlePauseResumeAll = () => {
    const hasProcessing = processGroups.some(g => g.status === 'processing')
    const newStatus = hasProcessing ? 'paused' : 'processing'
    
    setProcessGroups(processGroups.map(group => ({
      ...group,
      status: newStatus,
      files: group.files.map(file => ({
        ...file,
        status: file.status === 'processing' ? 'paused' : 
               file.status === 'paused' ? 'processing' : file.status
      }))
    })))
  }

  const handleStopAll = () => {
    if (confirm(t('process.stopAll') + '?')) {
      setProcessGroups([])
    }
  }

  const getProgressBarColor = (status: ProcessStatus): string => {
    switch (status) {
      case 'processing': return 'var(--accent-primary)'
      case 'paused': return '#f59e0b'
      case 'completed': return '#10b981'
      case 'error': return '#ef4444'
      default: return 'var(--accent-primary)'
    }
  }

  const getFileProgressBarColor = (status: FileStatus): string => {
    switch (status) {
      case 'processing': return 'var(--accent-primary)'
      case 'paused': return '#f59e0b'
      case 'completed': return '#10b981'
      case 'failed': return '#ef4444'
      default: return 'var(--accent-primary)'
    }
  }

  const calculateGroupProgress = (group: ProcessGroup): number => {
    if (group.totalFiles === 0) return 0
    return Math.round((group.completed / group.totalFiles) * 100)
  }

  const hasProcessing = processGroups.some(g => g.status === 'processing')

  return (
    <div className="process-menu">
      <div className="menu-header">
        <h1 className="menu-title">{t('process.title')}</h1>
        <div className="header-actions">
          <button 
            className="header-btn header-btn-icon"
            onClick={handlePauseResumeAll}
            disabled={processGroups.length === 0}
            title={hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}
          >
            <img 
              src={hasProcessing ? '/assets/pause.png' : '/assets/resume.png'} 
              alt={hasProcessing ? 'Pause All' : 'Resume All'}
              className="btn-icon"
            />
            <span>{hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}</span>
          </button>
          <button 
            className="header-btn header-btn-danger header-btn-icon"
            onClick={handleStopAll}
            disabled={processGroups.length === 0}
            title={t('process.stopAll')}
          >
            <img 
              src="/assets/stop.png" 
              alt="Stop All"
              className="btn-icon"
            />
            <span>{t('process.stopAll')}</span>
          </button>
        </div>
      </div>

      <div className="process-section">
        {processGroups.length === 0 ? (
          <div className="empty-state">{t('process.empty')}</div>
        ) : (
          <div className="process-list">{processGroups.map((group) => (
            <div key={group.id} className="process-group">
              <div className="group-header" onClick={() => handleToggleDetails(group.id)}>
                <div className="group-main-info">
                  <span className={`expand-icon ${group.showDetails ? 'expanded' : ''}`}>▶</span>
                  <div className="group-info">
                    <div className="group-name">{group.name}</div>
                    <div className="group-stats-line">
                      <span>{group.completed}/{group.totalFiles} • {calculateGroupProgress(group)}%</span>
                      <span className="stat-badge stat-success" title={t('process.successSlides')}>
                        {group.completed}
                      </span>
                      <span className="stat-divider">|</span>
                      <span className="stat-badge stat-processing" title={t('process.processingSlides')}>
                        {group.processing}
                      </span>
                      <span className="stat-divider">|</span>
                      <span className="stat-badge stat-failed" title={t('process.failedSlides')}>
                        {group.failed}
                      </span>
                    </div>
                  </div>
                </div>
                <div className="group-actions" onClick={(e) => e.stopPropagation()}>
                  <button 
                    className="process-btn process-btn-icon"
                    onClick={() => handlePauseResumeGroup(group.id)}
                    title={group.status === 'processing' ? t('process.pause') : t('process.resume')}
                  >
                    <img 
                      src={group.status === 'processing' ? '/assets/pause.png' : '/assets/resume.png'} 
                      alt={group.status === 'processing' ? 'Pause' : 'Resume'}
                      className="btn-icon"
                    />
                  </button>
                  <button 
                    className="process-btn process-btn-danger process-btn-icon"
                    onClick={() => handleStopGroup(group.id)}
                    title={t('process.stop')}
                  >
                    <img 
                      src="/assets/stop.png" 
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
                    width: `${calculateGroupProgress(group)}%`,
                    backgroundColor: getProgressBarColor(group.status)
                  }}
                />
              </div>

              {group.showDetails && (
                <div className="files-list">
                      {group.files.map((file) => (
                        <div key={file.id} className="file-item">
                          <div 
                            className="file-header-clickable"
                            onClick={() => handleToggleFileLog(group.id, file.id)}
                          >
                            <span className="file-expand-icon">{file.showLog ? '▼' : '▶'}</span>
                            <div className="file-info">
                              <div className="file-name">{file.name}</div>
                              <div className="file-stats">
                                <span className="file-stat-badge stat-success" title={t('process.successSlides')}>
                                  {file.completedSlides}
                                </span>
                                <span className="stat-divider">|</span>
                                <span className="file-stat-badge stat-processing" title={t('process.processingSlides')}>
                                  {file.processingSlides}
                                </span>
                                <span className="stat-divider">|</span>
                                <span className="file-stat-badge stat-failed" title={t('process.failedSlides')}>
                                  {file.failedSlides}
                                </span>
                                <span className="file-progress-text">/ {file.totalSlides} {t('process.slides')} • {file.progress}%</span>
                              </div>
                            </div>
                            <div className="file-status-and-actions">
                              <div className="file-status" data-status={file.status}>
                                {t(`process.status.${file.status === 'failed' ? 'error' : file.status}`)}
                              </div>
                              {(file.status === 'processing' || file.status === 'paused') && (
                                <button 
                                  className="file-action-btn"
                                  onClick={(e) => {
                                    e.stopPropagation()
                                    handlePauseResumeFile(group.id, file.id)
                                  }}
                                  title={file.status === 'processing' ? t('process.pause') : t('process.resume')}
                                >
                                  <img 
                                    src={file.status === 'processing' ? '/assets/pause.png' : '/assets/resume.png'} 
                                    alt={file.status === 'processing' ? 'Pause' : 'Resume'}
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
                                width: `${file.progress}%`,
                                backgroundColor: getFileProgressBarColor(file.status)
                              }}
                            />
                          </div>

                          {file.showLog && (
                            <div className="file-log-content">
                              <div className="log-dropdown">
                                <div className="log-header">
                                  {t('process.log')}
                                  <button 
                                    className="copy-log-btn"
                                    onClick={(e) => {
                                      e.stopPropagation()
                                      navigator.clipboard.writeText(file.log.join('\n'))
                                    }}
                                    title="Copy log"
                                  >
                                    <img 
                                      src="/assets/clipboard.png" 
                                      alt="Copy"
                                      className="log-icon"
                                    />
                                  </button>
                                </div>
                                <div className="log-content">
                                  {file.log.length === 0 ? (
                                    <div className="log-empty">{t('process.noLogs')}</div>
                                  ) : (
                                    file.log.map((entry, index) => (
                                      <div key={index} className="log-entry">{entry}</div>
                                    ))
                                  )}
                                </div>
                              </div>
                            </div>
                          )}
                        </div>
                      ))}
                </div>
              )}
            </div>
          ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default ProcessMenu
