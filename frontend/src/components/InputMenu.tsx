import React, { useState, useEffect, useRef } from 'react'
import { useApp } from '../contexts/AppContext'
import { useJobs } from '../contexts/JobContext'
import ShapeSelector from './ShapeSelector'
import TagInput from './TagInput'
import * as backendApi from '../services/backendApi'
import '../styles/InputMenu.css'

interface InputMenuProps {
  onStart: () => void
}

interface TextReplacement {
  id: number
  placeholder: string
  columns: string[]
}

interface ImageReplacement {
  id: number
  shapeId: string
  columns: string[]
  roiType: string
  cropType: string
}

interface Shape {
  id: string
  name: string
  preview: string
}

const InputMenu: React.FC<InputMenuProps> = ({ onStart }) => {
  const { t } = useApp()
  const { createGroup } = useJobs()

  const roiOptions = [
    {
      value: 'Attention',
      label: t('replacement.roiAttention'),
      description: t('replacement.roiAttentionDesc'),
    },
    {
      value: 'Prominent',
      label: t('replacement.roiProminent'),
      description: t('replacement.roiProminentDesc'),
    },
    {
      value: 'Center',
      label: t('replacement.roiCenter'),
      description: t('replacement.roiCenterDesc'),
    },
  ]

  const cropOptions = [
    {
      value: 'Crop',
      label: t('replacement.cropCrop'),
      description: t('replacement.cropCropDesc'),
    },
    {
      value: 'Fit',
      label: t('replacement.cropFit'),
      description: t('replacement.cropFitDesc'),
    },
  ]

  const getOptionDescription = (options: { value: string; description: string }[], value: string) => {
    return options.find((option) => option.value === value)?.description ?? ''
  }
  
  // Load saved state from localStorage
  const loadSavedState = () => {
    try {
      const saved = localStorage.getItem('inputMenuState')
      if (saved) {
        return JSON.parse(saved)
      }
    } catch (error) {
      console.error('Error loading saved state:', error)
    }
    return null
  }
  
  const savedState = loadSavedState()
  
  const [pptxPath, setPptxPath] = useState(savedState?.pptxPath || '')
  const [dataPath, setDataPath] = useState(savedState?.dataPath || '')
  const [savePath, setSavePath] = useState(savedState?.savePath || '')
  const [columns, setColumns] = useState<string[]>(savedState?.columns || [])
  const [isLoadingColumns, setIsLoadingColumns] = useState(false)
  const [isLoadingShapes, setIsLoadingShapes] = useState(false)
  const [isLoadingPlaceholders, setIsLoadingPlaceholders] = useState(false)
  const [isStarting, setIsStarting] = useState(false)
  const [showTextConfigs, setShowTextConfigs] = useState(true)
  const [showImageConfigs, setShowImageConfigs] = useState(true)
  const templateErrorAtRef = useRef(0)
  const pptxPathRef = useRef(pptxPath)
  
  const [shapes, setShapes] = useState<Shape[]>([])
  const [placeholders, setPlaceholders] = useState<string[]>([])
  
  const [textReplacements, setTextReplacements] = useState<TextReplacement[]>(
    savedState?.textReplacements?.map((item: { id: number; searchText?: string; placeholder?: string; columns?: string[] }) => ({
      id: item.id,
      placeholder: item.placeholder || item.searchText || '',
      columns: item.columns || []
    })) || []
  )
  const [imageReplacements, setImageReplacements] = useState<ImageReplacement[]>(
    (savedState?.imageReplacements ?? []).map((item: {
      id?: number
      shapeId?: string
      columns?: string[]
      roiType?: string
      cropType?: string
    }) => ({
      id: item.id ?? 1,
      shapeId: item.shapeId ?? '',
      columns: item.columns ?? [],
      roiType: item.roiType ?? 'Center',
      cropType: item.cropType ?? 'Crop',
    }))
  )
  
  // Save state to localStorage whenever it changes
  useEffect(() => {
    const state = {
      pptxPath,
      dataPath,
      savePath,
      columns,
      textReplacements,
      imageReplacements
    }
    localStorage.setItem('inputMenuState', JSON.stringify(state))
  }, [pptxPath, dataPath, savePath, columns, textReplacements, imageReplacements])
  
  useEffect(() => {
    pptxPathRef.current = pptxPath
  }, [pptxPath])

  const getErrorDetail = (error: unknown): string => {
    if (error instanceof Error && error.message) return error.message
    if (typeof error === 'string') return error
    if (error && typeof error === 'object' && 'message' in error) {
      const value = (error as { message?: string }).message
      if (value) return value
    }
    return ''
  }

  const formatErrorMessage = (key: string, error: unknown): string => {
    const detail = getErrorDetail(error)
    return detail ? `${t(key)}: ${detail}` : t(key)
  }

  const notifyTemplateError = (error: unknown) => {
    const now = Date.now()
    if (now - templateErrorAtRef.current < 800) return
    templateErrorAtRef.current = now
    alert(formatErrorMessage('input.templateLoadError', error))
  }

  const loadTemplateAssets = async (filePath: string) => {
    if (!filePath) {
      setShapes([])
      setPlaceholders([])
      return
    }
    setIsLoadingShapes(true)
    setIsLoadingPlaceholders(true)
    try {
      const response = await backendApi.scanTemplate(filePath)
      const data = response as backendApi.SlideScanTemplateSuccess
      const mappedShapes = (data.Shapes ?? [])
        .filter((shape) => shape.IsImage === true)
        .map((shape) => ({
          id: String(shape.Id),
          name: shape.Name,
          preview: shape.Data ? `data:image/png;base64,${shape.Data}` : '/assets/images/app-icon.png',
        }))
      setShapes(mappedShapes)

      const items = (data.Placeholders ?? [])
        .map((item) => item.trim())
        .filter((item) => item.length > 0)
      const unique = Array.from(new Set(items))
      unique.sort((a, b) => a.localeCompare(b))
      setPlaceholders(unique)
    } catch (error) {
      if (filePath === pptxPathRef.current) {
        notifyTemplateError(error)
        setPptxPath('')
        setShapes([])
        setPlaceholders([])
      }
    } finally {
      setIsLoadingShapes(false)
      setIsLoadingPlaceholders(false)
    }
  }

  useEffect(() => {
    if (!pptxPath) {
      setShapes([])
      setPlaceholders([])
      return
    }

    const timer = setTimeout(() => {
      loadTemplateAssets(pptxPath).catch(() => undefined)
    }, 400)

    return () => clearTimeout(timer)
  }, [pptxPath])

  // Resolve relative paths to absolute paths
  const resolvePath = (inputPath: string): string => {
    if (!inputPath) return inputPath
    
    // Check if already absolute path (Windows: starts with drive letter, Unix: starts with /)
    if (/^[a-zA-Z]:[/\\]/.test(inputPath) || inputPath.startsWith('/')) {
      return inputPath
    }
    
    // Convert relative path to absolute using current working directory
    // In Electron, we can use process.cwd() or __dirname
    if (typeof process === 'undefined' || !process.cwd) {
      return inputPath
    }

    const cwd = process.cwd()
    return `${cwd}\\${inputPath.replace(/\//g, '\\')}`
  }

  const handleBrowsePptx = async () => {
    const path = await window.electronAPI.openFile([
      { name: 'PowerPoint Files', extensions: ['pptx', 'potx'] }
    ])
    if (path) {
      setPptxPath(path)
      clearReplacements()
    }
  }

  const handleBrowseData = async () => {
    const path = await window.electronAPI.openFile([
      { name: 'Spreadsheets Files', extensions: ['xlsx', 'xlsm'] }
    ])
    
    if (path) {
      setDataPath(path)
      setColumns([])
      clearReplacements()
      setIsLoadingColumns(true)
      try {
        // Load file into backend
        await backendApi.loadFile(path)
        
        // Get all unique columns from all sheets in this file
        const allColumns = await backendApi.getAllColumns([path])
        
        setColumns(allColumns)
      } catch (error) {
        console.error('Error loading file:', error)
        alert(formatErrorMessage('input.columnLoadError', error))
        setDataPath('');
      } finally {
        setIsLoadingColumns(false)
      }
    }
  }

  const handleBrowseSave = async () => {
    const path = await window.electronAPI.openFolder()
    if (path) setSavePath(path)
  }

  const addTextReplacement = () => {
    setTextReplacements([...textReplacements, {
      id: textReplacements.length + 1,
      placeholder: '',
      columns: []
    }])
  }

  const removeTextReplacement = (id: number) => {
    setTextReplacements(textReplacements.filter(item => item.id !== id))
  }

  const updateTextReplacement = (id: number, field: 'placeholder' | 'columns', value: string | string[]) => {
    setTextReplacements(textReplacements.map(item => 
      item.id === id ? { ...item, [field]: value } : item
    ))
  }

  const addImageReplacement = () => {
    setImageReplacements([...imageReplacements, { 
      id: imageReplacements.length + 1, 
      shapeId: '', 
      columns: [],
      roiType: 'Center',
      cropType: 'Crop',
    }])
  }

  const removeImageReplacement = (id: number) => {
    setImageReplacements(imageReplacements.filter(item => item.id !== id))
  }

  const updateImageReplacement = (
    id: number,
    field: 'shapeId' | 'columns' | 'roiType' | 'cropType',
    value: string | string[]
  ) => {
    setImageReplacements(imageReplacements.map(item => 
      item.id === id ? { ...item, [field]: value } : item
    ))
  }

  const exportConfig = async () => {
    const config = {
      pptxPath,
      dataPath,
      savePath,
      columns,
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
          setColumns(config.columns || [])
          setTextReplacements(
            (config.textReplacements || []).map((item: { id: number; searchText?: string; placeholder?: string; columns?: string[] }) => ({
              id: item.id ?? 1,
              placeholder: item.placeholder || item.searchText || '',
              columns: item.columns || []
            }))
          )
          setImageReplacements(
            (config.imageReplacements || []).map((item: {
              id?: number
              shapeId?: string
              columns?: string[]
              roiType?: string
              cropType?: string
            }) => ({
              id: item.id ?? 1,
              shapeId: item.shapeId ?? '',
              columns: item.columns ?? [],
              roiType: item.roiType ?? 'Center',
              cropType: item.cropType ?? 'Crop',
            }))
          )
          alert(t('input.importConfig') + ' ' + t('common.ok'))
        }
      } catch (error) {
        alert(t('input.jsonError'))
      }
    }
  }
  
  const clearReplacements = () => {
    setTextReplacements([])
    setImageReplacements([])
  }

  const clearAll = () => {
    if (confirm(t('input.confirmClear') || 'Clear all data?')) {
      setPptxPath('')
      setDataPath('')
      setSavePath('')
      setColumns([])
      setShapes([])
      setPlaceholders([])
      clearReplacements()
      localStorage.removeItem('inputMenuState')
    }
  }

  const templateExtPattern = /\.(pptx|potx)$/i
  const sheetExtPattern = /\.(xlsx|xlsm)$/i
  const isTemplateValid = Boolean(pptxPath && templateExtPattern.test(pptxPath))
  const isDataValid = Boolean(dataPath && sheetExtPattern.test(dataPath))
  const isOutputValid = Boolean(savePath && savePath.trim().length > 0)
  const canConfigure =
    isTemplateValid &&
    isDataValid &&
    !isLoadingColumns &&
    !isLoadingShapes &&
    !isLoadingPlaceholders

  const placeholderSet = new Set(placeholders)
  const shapeIdSet = new Set(shapes.map((shape) => shape.id))

  const normalizedTextPlaceholders = textReplacements.map((item) => item.placeholder.trim())
  const usedTextPlaceholders = new Set(
    normalizedTextPlaceholders.filter((value) => value.length > 0)
  )
  const hasDuplicateTextPlaceholders =
    usedTextPlaceholders.size !== normalizedTextPlaceholders.filter((value) => value.length > 0).length

  const normalizedShapeIds = imageReplacements.map((item) => item.shapeId.trim())
  const usedShapeIds = new Set(
    normalizedShapeIds.filter((value) => value.length > 0)
  )
  const hasDuplicateShapeIds =
    usedShapeIds.size !== normalizedShapeIds.filter((value) => value.length > 0).length

  const invalidTextItems = textReplacements.filter((item) => {
    const placeholder = item.placeholder.trim()
    if (!placeholder || item.columns.length === 0) return true
    return !placeholderSet.has(placeholder)
  })

  const invalidImageItems = imageReplacements.filter((item) => {
    const shapeId = item.shapeId.trim()
    if (!shapeId || item.columns.length === 0) return true
    return !shapeIdSet.has(shapeId)
  })

  const validTextCount = textReplacements.length - invalidTextItems.length
  const validImageCount = imageReplacements.length - invalidImageItems.length
  const hasAnyConfig = validTextCount + validImageCount > 0

  const hasInvalidConfig =
    invalidTextItems.length > 0 ||
    invalidImageItems.length > 0 ||
    hasDuplicateTextPlaceholders ||
    hasDuplicateShapeIds

  const canStart =
    isTemplateValid &&
    isDataValid &&
    isOutputValid &&
    hasAnyConfig &&
    !hasInvalidConfig

  const getAvailablePlaceholders = (current: string) => {
    const taken = new Set(
      textReplacements
        .map((item) => item.placeholder.trim())
        .filter((value) => value && value !== current)
    )
    return placeholders.filter((value) => !taken.has(value))
  }

  const getAvailableShapes = (current: string) => {
    const taken = new Set(
      imageReplacements
        .map((item) => item.shapeId.trim())
        .filter((value) => value && value !== current)
    )
    return shapes.filter((shape) => !taken.has(shape.id))
  }

  const handleStart = async () => {
    // Resolve paths to absolute
    const resolvedPptxPath = resolvePath(pptxPath)
    const resolvedDataPath = resolvePath(dataPath)
    const resolvedSavePath = resolvePath(savePath)
    
    if (resolvedPptxPath && resolvedDataPath && resolvedSavePath && canStart) {
      const textConfigs: backendApi.SlideTextConfig[] = textReplacements
        .filter(item => item.placeholder.trim() && item.columns.length > 0)
        .map(item => ({
          Pattern: item.placeholder.trim(),
          Columns: item.columns
        }))

      const imageConfigs: backendApi.SlideImageConfig[] = imageReplacements
        .filter(item => item.shapeId && item.columns.length > 0)
        .map(item => ({
          ShapeId: Number(item.shapeId),
          Columns: item.columns,
          RoiType: item.roiType || 'Center',
          CropType: item.cropType || 'Crop'
        }))
        .filter(item => Number.isFinite(item.ShapeId))

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
      try {
        setIsStarting(true)
        await createGroup({
          templatePath: resolvedPptxPath,
          spreadsheetPath: resolvedDataPath,
          outputPath: resolvedSavePath,
          textConfigs,
          imageConfigs
        })
        onStart()
      } catch (error) {
        console.error('Failed to start job:', error)
        const message = error instanceof Error ? error.message : t('input.error')
        alert(message)
      } finally {
        setIsStarting(false)
      }
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
            {t('input.import')}
          </button>
          <button className="btn btn-secondary" onClick={exportConfig} title={t('input.exportConfig')}>
            {t('input.export')}
          </button>
          <button className="btn btn-danger" onClick={clearAll} title={t('input.clearAll')}>
            <img 
              src="/assets/images/remove.png" 
              alt={t('input.clearAll')}
              className="btn-icon"
            /> <span>{t('input.clearAll')}</span>
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
          <button className="browse-btn" onClick={handleBrowsePptx} disabled={isLoadingShapes}>
            {isLoadingShapes ? t('input.loadingShapes') : t('input.browse')}
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
          <button className="browse-btn" onClick={handleBrowseData} disabled={isLoadingColumns}>
            {isLoadingColumns ? t('input.loadingColumns') : t('input.browse')}
          </button>
        </div>
      </div>

      {/* Replacement Tables - Separated */}
      <div className="replacement-section-separated">
        {/* Text Replacement */}
        <div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
          <div className="panel-header">
            <div className="panel-title">
              <button
                type="button"
                className="panel-toggle"
                onClick={() => setShowTextConfigs((prev) => !prev)}
                disabled={!canConfigure}
                aria-expanded={showTextConfigs}
              >
                {showTextConfigs ? '-' : '+'}
              </button>
              <h3>
                {t('replacement.textTitle')} ({textReplacements.length})
              </h3>
            </div>
            <button
              className="btn btn-success"
              onClick={addTextReplacement}
              disabled={!canConfigure || placeholders.length === 0}
            >
              + {t('replacement.add')}
            </button>
          </div>
          {showTextConfigs && (
            <div className="replacement-table">
              <div className="table-header">
                <div className="col-uniform">{t('replacement.searchText')}</div>
                <div className="col-uniform">{t('replacement.column')}</div>
                <div className="col-action-fixed">{t('replacement.delete')}</div>
              </div>
              {textReplacements.map(item => {
                const available = getAvailablePlaceholders(item.placeholder)
                return (
                  <div key={item.id} className="table-row">
                    <select
                      className="table-input"
                      value={item.placeholder}
                      onChange={(e) => updateTextReplacement(item.id, 'placeholder', e.target.value)}
                      disabled={!canConfigure || isLoadingPlaceholders}
                    >
                      <option value="">{t('replacement.searchPlaceholder')}</option>
                      {available.map((placeholder) => (
                        <option key={placeholder} value={placeholder}>
                          {placeholder}
                        </option>
                      ))}
                    </select>
                    <TagInput
                      value={item.columns}
                      onChange={(tags) => updateTextReplacement(item.id, 'columns', tags)}
                      suggestions={columns}
                      placeholder={t('replacement.columnPlaceholder')}
                    />
                    <button
                      className="delete-btn"
                      onClick={() => removeTextReplacement(item.id)}
                      title={t('replacement.delete')}
                    >
                      <img
                        src="/assets/images/remove.png"
                        alt="Delete"
                        className="delete-icon"
                      />
                    </button>
                  </div>
                )
              })}
            </div>
          )}
        </div>

        {/* Image Replacement */}
        <div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
          <div className="panel-header">
            <div className="panel-title">
              <button
                type="button"
                className="panel-toggle"
                onClick={() => setShowImageConfigs((prev) => !prev)}
                disabled={!canConfigure}
                aria-expanded={showImageConfigs}
              >
                {showImageConfigs ? '-' : '+'}
              </button>
              <h3>
                {t('replacement.imageTitle')} ({imageReplacements.length})
              </h3>
            </div>
            <button
              className="btn btn-success"
              onClick={addImageReplacement}
              disabled={!canConfigure || shapes.length === 0}
            >
              + {t('replacement.add')}
            </button>
          </div>
          {showImageConfigs && (
            <div className="replacement-table">
              <div className="shape-gallery">
                <div className="shape-gallery-header">{t('replacement.availableShapes')}</div>
                <div className="shape-gallery-list">
                  {shapes.length === 0 ? (
                    <div className="shape-gallery-empty">{t('replacement.noShapes')}</div>
                  ) : (
                    shapes.map((shape) => (
                      <div key={shape.id} className="shape-gallery-item">
                        <img
                          src={shape.preview}
                          alt={shape.name}
                          className="shape-gallery-preview"
                        />
                        <div className="shape-gallery-info">
                          <span className="shape-gallery-name">{shape.name}</span>
                          <span className="shape-gallery-id">{shape.id}</span>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
              <div className="table-header table-header-image">
                <div className="col-uniform">{t('replacement.shape')}</div>
                <div className="col-uniform">{t('replacement.column')}</div>
                <div className="col-uniform">{t('replacement.roi')}</div>
                <div className="col-uniform">{t('replacement.crop')}</div>
                <div className="col-action-fixed">{t('replacement.delete')}</div>
              </div>
              {imageReplacements.map(item => (
                <div key={item.id} className="table-row table-row-image">
                  <ShapeSelector
                    shapes={getAvailableShapes(item.shapeId)}
                    value={item.shapeId}
                    onChange={(shapeId) => updateImageReplacement(item.id, 'shapeId', shapeId)}
                    placeholder={t('replacement.shapePlaceholder')}
                  />
                  <TagInput
                    value={item.columns}
                    onChange={(tags) => updateImageReplacement(item.id, 'columns', tags)}
                    suggestions={columns}
                    placeholder={t('replacement.columnPlaceholder')}
                  />
                  <div className="select-with-hint">
                    <select
                      className="table-input"
                      value={item.roiType}
                      onChange={(e) => updateImageReplacement(item.id, 'roiType', e.target.value)}
                      title={getOptionDescription(roiOptions, item.roiType)}
                    >
                      {roiOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                    <span className="select-hint">
                      {getOptionDescription(roiOptions, item.roiType)}
                    </span>
                  </div>
                  <div className="select-with-hint">
                    <select
                      className="table-input"
                      value={item.cropType}
                      onChange={(e) => updateImageReplacement(item.id, 'cropType', e.target.value)}
                      title={getOptionDescription(cropOptions, item.cropType)}
                    >
                      {cropOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                    <span className="select-hint">
                      {getOptionDescription(cropOptions, item.cropType)}
                    </span>
                  </div>
                  <button 
                    className="delete-btn"
                    onClick={() => removeImageReplacement(item.id)}
                    title={t('replacement.delete')}
                  >
                    <img 
                      src="/assets/images/remove.png" 
                      alt="Delete"
                      className="delete-icon"
                    />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

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

      <button className="start-btn" onClick={handleStart} disabled={isStarting || !canStart}>
        {isStarting ? t('process.processing') : t('input.start')}
      </button>
    </div>
  )
}

export default InputMenu
