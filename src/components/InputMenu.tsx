import React, { useState } from 'react'
import { useApp } from '../contexts/AppContext'
import ShapeSelector from './ShapeSelector'
import '../styles/InputMenu.css'

interface InputMenuProps {
  onStart: () => void
}

interface TextReplacement {
  id: number
  regex: string
  column: string
}

interface ImageReplacement {
  id: number
  shapeId: string
  column: string
}

interface Shape {
  id: string
  name: string
  preview: string
}

const InputMenu: React.FC<InputMenuProps> = ({ onStart }) => {
  const { t } = useApp()
  const [pptxPath, setPptxPath] = useState('')
  const [dataPath, setDataPath] = useState('')
  const [savePath, setSavePath] = useState('')
  const [columns, setColumns] = useState<string[]>(['HoTen', 'MSSV', 'NgaySinh', 'AnhDaiDien']) // Demo columns
  
  // Demo shapes - trong thực tế sẽ parse từ file PPTX
  const [shapes, setShapes] = useState<Shape[]>([
    { id: 'Shape1', name: 'Hình ảnh chính', preview: '/assets/UET-logo.png' },
    { id: 'Shape2', name: 'Ảnh đại diện', preview: '/assets/UETAPP-icon.png' },
  ])
  
  const [textReplacements, setTextReplacements] = useState<TextReplacement[]>([
    { id: 1, regex: '', column: '' }
  ])
  const [imageReplacements, setImageReplacements] = useState<ImageReplacement[]>([
    { id: 1, shapeId: '', column: '' }
  ])

  // Resolve relative paths to absolute paths
  const resolvePath = (inputPath: string): string => {
    if (!inputPath) return inputPath
    
    // Check if already absolute path (Windows: starts with drive letter, Unix: starts with /)
    if (/^[a-zA-Z]:[/\\]/.test(inputPath) || inputPath.startsWith('/')) {
      return inputPath
    }
    
    // Convert relative path to absolute using current working directory
    // In Electron, we can use process.cwd() or __dirname
    const cwd = process.cwd()
    return `${cwd}\\${inputPath.replace(/\//g, '\\')}`
  }

  const handleBrowsePptx = async () => {
    const path = await window.electronAPI.openFile([
      { name: 'PowerPoint Files', extensions: ['pptx'] },
      { name: 'All Files', extensions: ['*'] }
    ])
    if (path) setPptxPath(path)
  }

  const handleBrowseData = async () => {
    const path = await window.electronAPI.openFile([
      { name: 'Excel Files', extensions: ['xlsx', 'csv'] },
      { name: 'All Files', extensions: ['*'] }
    ])
    if (path) {
      setDataPath(path)
      // TODO: Parse file to extract columns
      setColumns(prev => prev) // Keep setColumns to avoid warning
    }
  }

  const handleBrowseSave = async () => {
    const path = await window.electronAPI.saveFile([
      { name: 'PowerPoint Files', extensions: ['pptx'] }
    ])
    if (path) setSavePath(path)
  }

  const addTextReplacement = () => {
    setTextReplacements([...textReplacements, { 
      id: textReplacements.length + 1, 
      regex: '', 
      column: '' 
    }])
  }

  const removeTextReplacement = (id: number) => {
    setTextReplacements(textReplacements.filter(item => item.id !== id))
  }

  const updateTextReplacement = (id: number, field: 'regex' | 'column', value: string) => {
    setTextReplacements(textReplacements.map(item => 
      item.id === id ? { ...item, [field]: value } : item
    ))
  }

  const addImageReplacement = () => {
    setImageReplacements([...imageReplacements, { 
      id: imageReplacements.length + 1, 
      shapeId: '', 
      column: '' 
    }])
    setShapes(prev => prev) // Keep setShapes to avoid warning
  }

  const removeImageReplacement = (id: number) => {
    setImageReplacements(imageReplacements.filter(item => item.id !== id))
  }

  const updateImageReplacement = (id: number, field: 'shapeId' | 'column', value: string) => {
    setImageReplacements(imageReplacements.map(item => 
      item.id === id ? { ...item, [field]: value } : item
    ))
  }

  const exportConfig = async () => {
    const config = {
      pptxPath,
      dataPath,
      savePath,
      textReplacements,
      imageReplacements
    }
    
    const path = await window.electronAPI.saveFile([
      { name: 'JSON Files', extensions: ['json'] },
      { name: 'All Files', extensions: ['*'] }
    ])
    
    if (path) {
      try {
        await window.electronAPI.writeSettings(path, JSON.stringify(config, null, 2))
        alert(t('input.exportConfig') + ' ' + t('common.ok'))
      } catch (error) {
        alert(t('input.jsonError'))
      }
    }
  }

  const importConfig = async () => {
    const path = await window.electronAPI.openFile([
      { name: 'JSON Files', extensions: ['json'] },
      { name: 'All Files', extensions: ['*'] }
    ])
    
    if (path) {
      try {
        const data = await window.electronAPI.readSettings(path)
        if (data) {
          const config = JSON.parse(data)
          setPptxPath(config.pptxPath || '')
          setDataPath(config.dataPath || '')
          setSavePath(config.savePath || '')
          setTextReplacements(config.textReplacements || [{ id: 1, regex: '', column: '' }])
          setImageReplacements(config.imageReplacements || [{ id: 1, shapeId: '', column: '' }])
          alert(t('input.importConfig') + ' ' + t('common.ok'))
        }
      } catch (error) {
        alert(t('input.jsonError'))
      }
    }
  }

  const handleStart = () => {
    // Resolve paths to absolute
    const resolvedPptxPath = resolvePath(pptxPath)
    const resolvedDataPath = resolvePath(dataPath)
    const resolvedSavePath = resolvePath(savePath)
    
    if (resolvedPptxPath && resolvedDataPath && resolvedSavePath) {
      // Update displayed paths to show resolved absolute paths
      setPptxPath(resolvedPptxPath)
      setDataPath(resolvedDataPath)
      setSavePath(resolvedSavePath)
      
      localStorage.setItem('config', JSON.stringify({ 
        pptxPath: resolvedPptxPath, 
        dataPath: resolvedDataPath, 
        savePath: resolvedSavePath,
        textReplacements,
        imageReplacements
      }))
      onStart()
    } else {
      alert(t('input.error'))
    }
  }

  return (
    <div className="input-menu">
      <div className="menu-header">
        <h1 className="menu-title">{t('input.title')}</h1>
        <div className="config-actions">
          <button className="btn btn-secondary" onClick={importConfig} title={t('input.importConfig')}>
            📥 {t('input.import')}
          </button>
          <button className="btn btn-secondary" onClick={exportConfig} title={t('input.exportConfig')}>
            📤 {t('input.export')}
          </button>
        </div>
      </div>
      
      {/* File Inputs */}
      <div className="input-section">
        <label className="input-label">{t('input.pptxFile')}</label>
        <div className="input-group">
          <input 
            type="text" 
            className="input-field" 
            value={pptxPath}
            onChange={(e) => setPptxPath(e.target.value)}
            placeholder={t('input.pptxPlaceholder')}
          />
          <button className="browse-btn" onClick={handleBrowsePptx}>
            {t('input.browse')}
          </button>
        </div>
      </div>

      <div className="input-section">
        <label className="input-label">{t('input.dataFile')}</label>
        <div className="input-group">
          <input 
            type="text" 
            className="input-field" 
            value={dataPath}
            onChange={(e) => setDataPath(e.target.value)}
            placeholder={t('input.dataPlaceholder')}
          />
          <button className="browse-btn" onClick={handleBrowseData}>
            {t('input.browse')}
          </button>
        </div>
      </div>

      {/* Replacement Tables - Separated */}
      <div className="replacement-section-separated">
        {/* Text Replacement */}
        <div className="replacement-full-panel">
          <div className="panel-header">
            <h3>{t('replacement.textTitle')}</h3>
            <button className="btn btn-success" onClick={addTextReplacement}>+ {t('replacement.add')}</button>
          </div>
          <div className="replacement-table">
            <div className="table-header">
              <div className="col-uniform">{t('replacement.regex')}</div>
              <div className="col-uniform">{t('replacement.column')}</div>
              <div className="col-action-fixed">{t('replacement.delete')}</div>
            </div>
            {textReplacements.map(item => (
              <div key={item.id} className="table-row">
                <input
                  type="text"
                  className="table-input"
                  value={item.regex}
                  onChange={(e) => updateTextReplacement(item.id, 'regex', e.target.value)}
                  placeholder={t('replacement.regexPlaceholder')}
                />
                <input
                  type="text"
                  className="table-input"
                  list="columns-datalist"
                  value={item.column}
                  onChange={(e) => updateTextReplacement(item.id, 'column', e.target.value)}
                  placeholder={t('replacement.columnPlaceholder')}
                />
                <button 
                  className="delete-btn"
                  onClick={() => removeTextReplacement(item.id)}
                  title={t('replacement.delete')}
                >
                  <img 
                    src="/assets/remove.png" 
                    alt="Delete"
                    className="delete-icon"
                  />
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* Image Replacement */}
        <div className="replacement-full-panel">
          <div className="panel-header">
            <h3>{t('replacement.imageTitle')}</h3>
            <button className="btn btn-success" onClick={addImageReplacement}>+ {t('replacement.add')}</button>
          </div>
          <div className="replacement-table">
            <div className="table-header">
              <div className="col-uniform">{t('replacement.shape')}</div>
              <div className="col-uniform">{t('replacement.column')}</div>
              <div className="col-action-fixed">{t('replacement.delete')}</div>
            </div>
            {imageReplacements.map(item => (
              <div key={item.id} className="table-row">
                <ShapeSelector
                  shapes={shapes}
                  value={item.shapeId}
                  onChange={(shapeId) => updateImageReplacement(item.id, 'shapeId', shapeId)}
                  placeholder={t('replacement.shapePlaceholder')}
                />
                <input
                  type="text"
                  className="table-input"
                  list="columns-datalist"
                  value={item.column}
                  onChange={(e) => updateImageReplacement(item.id, 'column', e.target.value)}
                  placeholder={t('replacement.columnPlaceholder')}
                />
                <button 
                  className="delete-btn"
                  onClick={() => removeImageReplacement(item.id)}
                  title={t('replacement.delete')}
                >
                  <img 
                    src="/assets/remove.png" 
                    alt="Delete"
                    className="delete-icon"
                  />
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Datalist for columns */}
      <datalist id="columns-datalist">
        {columns.map(col => (
          <option key={col} value={col} />
        ))}
      </datalist>

      <div className="input-section">
        <label className="input-label">{t('input.saveLocation')}</label>
        <div className="input-group">
          <input 
            type="text" 
            className="input-field" 
            value={savePath}
            onChange={(e) => setSavePath(e.target.value)}
            placeholder={t('input.savePlaceholder')}
          />
          <button className="browse-btn" onClick={handleBrowseSave}>
            {t('input.browse')}
          </button>
        </div>
      </div>

      <button className="start-btn" onClick={handleStart}>
        {t('input.start')}
      </button>
    </div>
  )
}

export default InputMenu
