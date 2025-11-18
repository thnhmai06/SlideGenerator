import React, { useState } from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/OutputMenu.css'

interface OutputFile {
  id: number
  name: string
  path: string
  status: 'completed' | 'failed'
  totalSlides: number
  completedSlides: number
  failedSlides: number
  log: string[]
  showLog: boolean
}

interface OutputGroup {
  id: number
  name: string
  completedAt: string
  totalFiles: number
  completed: number
  failed: number
  files: OutputFile[]
  showDetails: boolean
  log: string[]
  showLog: boolean
}

const OutputMenu: React.FC = () => {
  const { t } = useApp()
  const [outputGroups, setOutputGroups] = useState<OutputGroup[]>([
    { 
      id: 1, 
      name: 'Batch 2024-11-17 14:30',
      completedAt: '2024-11-17 14:30:25',
      totalFiles: 100,
      completed: 95,
      failed: 5,
      showDetails: false,
      showLog: false,
      log: ['Batch processing started...', 'Processed 100 files', 'Completed with 5 failures'],
      files: [
        { 
          id: 1, 
          name: 'output_001.pptx', 
          path: 'D:\\Output\\Batch_1\\output_001.pptx',
          status: 'completed',
          totalSlides: 100,
          completedSlides: 100,
          failedSlides: 0,
          log: ['Started processing...', 'Completed successfully'],
          showLog: false
        },
        { 
          id: 2, 
          name: 'output_002.pptx', 
          path: 'D:\\Output\\Batch_1\\output_002.pptx',
          status: 'completed',
          totalSlides: 100,
          completedSlides: 100,
          failedSlides: 0,
          log: ['Started processing...', 'Completed successfully'],
          showLog: false
        },
        { 
          id: 3, 
          name: 'output_003.pptx', 
          path: 'D:\\Output\\Batch_1\\output_003.pptx',
          status: 'failed',
          totalSlides: 100,
          completedSlides: 30,
          failedSlides: 70,
          log: ['Started processing...', 'Error: File not found'],
          showLog: false
        },
      ]
    },
    { 
      id: 2, 
      name: 'Batch 2024-11-17 15:45',
      completedAt: '2024-11-17 15:45:10',
      totalFiles: 50,
      completed: 50,
      failed: 0,
      showDetails: false,
      showLog: false,
      log: ['Batch processing started...', 'All files processed successfully'],
      files: [
        { 
          id: 4, 
          name: 'output_101.pptx', 
          path: 'D:\\Output\\Batch_2\\output_101.pptx',
          status: 'completed',
          totalSlides: 50,
          completedSlides: 50,
          failedSlides: 0,
          log: ['Started processing...', 'Completed successfully'],
          showLog: false
        },
      ]
    },
  ])

  const handleToggleDetails = (groupId: number) => {
    setOutputGroups(outputGroups.map(group => 
      group.id === groupId ? { ...group, showDetails: !group.showDetails } : group
    ))
  }

  const handleOpen = async (filePath: string) => {
    await window.electronAPI.openPath(filePath)
  }

  const handleOpenFolder = async (filePath: string) => {
    const folderPath = filePath.substring(0, filePath.lastIndexOf('\\'))
    await window.electronAPI.openPath(folderPath)
  }

  const handleToggleFileLog = (groupId: number, fileId: number) => {
    setOutputGroups(outputGroups.map(group => {
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

  const handleRemoveGroup = (id: number) => {
    if (confirm(t('output.remove') + '?')) {
      setOutputGroups(outputGroups.filter(group => group.id !== id))
    }
  }

  const handleClearAll = () => {
    if (confirm(t('output.clearAll') + '?')) {
      setOutputGroups([])
    }
  }

  return (
    <div className="output-menu">
      <div className="menu-header">
        <h1 className="menu-title">{t('output.title')}</h1>
        {outputGroups.length > 0 && (
          <button className="btn btn-danger" onClick={handleClearAll}>
            <img 
              src="/assets/remove.png" 
              alt="Clear All"
              className="btn-icon"
            />
            <span>{t('output.clearAll')}</span>
          </button>
        )}
      </div>
      
      <div className="output-section">
        {outputGroups.length > 0 ? (
          <div className="output-list">
            {outputGroups.map(group => (
              <div key={group.id} className="output-group">
                <div className="group-header" onClick={() => handleToggleDetails(group.id)}>
                  <div className="group-main-info">
                    <span className={`expand-icon ${group.showDetails ? 'expanded' : ''}`}>▶</span>
                    <div className="group-info">
                      <div className="group-name">{group.name}</div>
                      <div className="group-stats-line">
                        <span>{t('output.completedAt')}: {group.completedAt}</span>
                        <span className="stat-badge stat-success" title={t('process.successSlides')}>
                          {group.completed}
                        </span>
                        <span className="stat-divider">|</span>
                        <span className="stat-badge stat-failed" title={t('process.failedSlides')}>
                          {group.failed}
                        </span>
                        <span>• {group.totalFiles} {t('process.slides')}</span>
                      </div>
                    </div>
                  </div>
                  <div className="group-actions" onClick={(e) => e.stopPropagation()}>
                    <button 
                      className="output-btn" 
                      onClick={() => handleOpenFolder(group.files[0]?.path || '')}
                    >
                      <img 
                        src="/assets/folder.png" 
                        alt="Open Folder"
                        className="btn-icon"
                      />
                      <span>{t('output.openFolder')}</span>
                    </button>
                    <button 
                      className="output-btn-danger"
                      onClick={() => handleRemoveGroup(group.id)}
                      title={t('output.remove')}
                    >
                      <img 
                        src="/assets/remove.png" 
                        alt="Remove"
                        className="btn-icon"
                      />
                      <span>{t('output.remove')}</span>
                    </button>
                  </div>
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
                              <span className="file-stat-badge stat-failed" title={t('process.failedSlides')}>
                                {file.failedSlides}
                              </span>
                              <span className="file-progress-text">/ {file.totalSlides} {t('process.slides')}</span>
                            </div>
                          </div>
                          <div className="file-status" data-status={file.status}>
                            {t(`process.status.${file.status === 'failed' ? 'error' : file.status}`)}
                          </div>
                        </div>

                        <div className="file-actions">
                          <button 
                            className="file-btn"
                            onClick={() => handleOpen(file.path)}
                            disabled={file.status === 'failed'}
                          >
                            <img 
                              src="/assets/open.png" 
                              alt="Open"
                              className="file-btn-icon"
                            />
                            <span>{t('output.open')}</span>
                          </button>
                          <button 
                            className="file-btn"
                            onClick={() => handleOpenFolder(file.path)}
                          >
                            <img 
                              src="/assets/folder.png" 
                              alt="Open Folder"
                              className="file-btn-icon"
                            />
                            <span>{t('output.openFolder')}</span>
                          </button>
                        </div>

                        {file.showLog && (
                          <div className="file-log-content">
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
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        ) : (
          <div className="empty-state">
            {t('output.empty')}
          </div>
        )}
      </div>
    </div>
  )
}

export default OutputMenu
