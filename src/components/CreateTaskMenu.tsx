import React, { useState, useEffect, useRef } from 'react'
import { useApp } from '../contexts/AppContext'
import { useJobs } from '../contexts/JobContext'
import ShapeSelector from './ShapeSelector'
import TagInput from './TagInput'
import * as backendApi from '../services/backendApi'
import '../styles/CreateTaskMenu.css'

interface CreateTaskMenuProps {
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

const CreateTaskMenu: React.FC<CreateTaskMenuProps> = ({ onStart }) => {
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
  
  const STORAGE_KEYS = {
    inputMenuState: 'slidegen.ui.inputMenu.state',
  }

  // Load saved state from sessionStorage
  const loadSavedState = () => {
    try {
      const saved = sessionStorage.getItem(STORAGE_KEYS.inputMenuState)
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
  const [showTextConfigs, setShowTextConfigs] = useState(false)
  const [showImageConfigs, setShowImageConfigs] = useState(false)
  const [previewShape, setPreviewShape] = useState<Shape | null>(null)
  const [previewClosing, setPreviewClosing] = useState(false)
  const [previewZoom, setPreviewZoom] = useState(1)
  const [previewSize, setPreviewSize] = useState<{ width: number; height: number } | null>(null)
  const [previewOffset, setPreviewOffset] = useState({ x: 0, y: 0 })
  const isDraggingRef = useRef(false)
  const dragStartRef = useRef({ x: 0, y: 0 })
  const dragMovedRef = useRef(false)
  const [banner, setBanner] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const isHydratingRef = useRef(false)
  const hasHydratedRef = useRef(false)
  const templateErrorAtRef = useRef(0)
  const pptxPathRef = useRef(pptxPath)
  const dataErrorAtRef = useRef(0)
  const dataPathRef = useRef(dataPath)
  const lastLoadedDataPathRef = useRef(
    savedState?.dataLoaded ? savedState?.dataPath || '' : ''
  )
  const lastLoadedTemplatePathRef = useRef(
    savedState?.templateLoaded ? savedState?.pptxPath || '' : ''
  )
  
  const [shapes, setShapes] = useState<Shape[]>(savedState?.shapes || [])
  const [placeholders, setPlaceholders] = useState<string[]>(savedState?.placeholders || [])
  const [sheetCount, setSheetCount] = useState(savedState?.sheetCount || 0)
  const [totalRows, setTotalRows] = useState(savedState?.totalRows || 0)
  const [templateLoaded, setTemplateLoaded] = useState(Boolean(savedState?.templateLoaded))
  const [dataLoaded, setDataLoaded] = useState(Boolean(savedState?.dataLoaded))
  
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
      roiType: item.roiType ?? 'Attention',
      cropType: item.cropType ?? 'Fit',
    }))
  )
  
  useEffect(() => {
    localStorage.removeItem('config')
  }, [])

  // Save state to sessionStorage whenever it changes
  useEffect(() => {
    const state = {
      pptxPath,
      dataPath,
      savePath,
      textReplacements,
      imageReplacements,
      shapes,
      placeholders,
      columns,
      sheetCount,
      totalRows,
      templateLoaded,
      dataLoaded
    }
    sessionStorage.setItem(STORAGE_KEYS.inputMenuState, JSON.stringify(state))
  }, [
    pptxPath,
    dataPath,
    savePath,
    textReplacements,
    imageReplacements,
    shapes,
    placeholders,
    columns,
    sheetCount,
    totalRows,
    templateLoaded,
    dataLoaded,
  ])
  
  useEffect(() => {
    pptxPathRef.current = pptxPath
  }, [pptxPath])

  useEffect(() => {
    dataPathRef.current = dataPath
  }, [dataPath])

  const hydrateFromSavedState = async () => {
    if (!savedState) return
    if (hasHydratedRef.current) return
    hasHydratedRef.current = true
    isHydratingRef.current = true

    const nextPptxPath = savedState.pptxPath || ''
    const nextDataPath = savedState.dataPath || ''
    const nextSavePath = savedState.savePath || ''

    setPptxPath(nextPptxPath)
    setDataPath(nextDataPath)
    setSavePath(nextSavePath)

    const cachedShapes = savedState.shapes || []
    const cachedPlaceholders = savedState.placeholders || []
    const cachedColumns = savedState.columns || []
    const cachedSheetCount = savedState.sheetCount || 0
    const cachedTotalRows = savedState.totalRows || 0
    const cachedTemplateLoaded = savedState.templateLoaded || false
    const cachedDataLoaded = savedState.dataLoaded || false

    setShapes(cachedShapes)
    setPlaceholders(cachedPlaceholders)
    setColumns(cachedColumns)
    setSheetCount(cachedSheetCount)
    setTotalRows(cachedTotalRows)
    setTemplateLoaded(cachedTemplateLoaded)
    setDataLoaded(cachedDataLoaded)
    lastLoadedTemplatePathRef.current = cachedTemplateLoaded ? nextPptxPath : ''
    lastLoadedDataPathRef.current = cachedDataLoaded ? nextDataPath : ''

    setIsLoadingShapes(true)
    setIsLoadingPlaceholders(true)
    setIsLoadingColumns(true)

    try {
      let nextShapes: Shape[] = []
      let nextPlaceholders: string[] = []
      let nextColumns: string[] = []

      if (!cachedTemplateLoaded && nextPptxPath) {
        const response = await backendApi.scanTemplate(nextPptxPath)
        const template = response as backendApi.SlideScanTemplateSuccess
        nextShapes = (template.Shapes ?? [])
          .filter((shape) => shape.IsImage === true)
          .map((shape) => ({
            id: String(shape.Id),
            name: shape.Name,
            preview: shape.Data ? `data:image/png;base64,${shape.Data}` : '/assets/images/app.png',
          }))
        nextPlaceholders = (template.Placeholders ?? [])
          .map((item) => item.trim())
          .filter((item) => item.length > 0)
        nextPlaceholders = Array.from(new Set(nextPlaceholders)).sort((a, b) =>
          a.localeCompare(b)
        )
        setTemplateLoaded(true)
        lastLoadedTemplatePathRef.current = nextPptxPath
      } else {
        nextShapes = cachedShapes
        nextPlaceholders = cachedPlaceholders
      }

      if (!cachedDataLoaded && nextDataPath) {
        await backendApi.loadFile(nextDataPath)
        nextColumns = await backendApi.getAllColumns([nextDataPath])
        const workbookInfo = await backendApi.getWorkbookInfo(nextDataPath)
        const workbookData = workbookInfo as backendApi.SheetWorkbookGetInfoSuccess
        const sheetsInfo = workbookData.Sheets ?? []
        const rowsSum = sheetsInfo.reduce((acc, sheet) => acc + (sheet.RowCount ?? 0), 0)
        setSheetCount(sheetsInfo.length)
        setTotalRows(rowsSum)
        setDataLoaded(true)
        lastLoadedDataPathRef.current = nextDataPath
      } else {
        nextColumns = cachedColumns
      }

      const placeholderSet = new Set(nextPlaceholders)
      const shapeIdSet = new Set(nextShapes.map((shape) => shape.id))
      const columnSet = new Set(nextColumns)

      const importedText = (savedState.textReplacements || []).map(
        (item: { id: number; searchText?: string; placeholder?: string; columns?: string[] }) => ({
          id: item.id ?? 1,
          placeholder: item.placeholder || item.searchText || '',
          columns: item.columns || []
        })
      )
      const filteredText = importedText
        .map((item: { placeholder: string; columns: any[] }) => ({
          ...item,
          placeholder: item.placeholder.trim(),
          columns: item.columns.filter((col: string) => columnSet.has(col))
        }))
        .filter((item: { placeholder: string; columns: string | any[] }) =>
          item.placeholder &&
          item.columns.length > 0 &&
          placeholderSet.has(item.placeholder)
        )

      const importedImages = (savedState.imageReplacements || []).map((item: {
        id?: number
        shapeId?: string
        columns?: string[]
        roiType?: string
        cropType?: string
      }) => ({
        id: item.id ?? 1,
        shapeId: item.shapeId ?? '',
        columns: item.columns ?? [],
        roiType: item.roiType ?? 'Attention',
        cropType: item.cropType ?? 'Fit',
      }))
      const filteredImages = importedImages
        .map((item: { shapeId: string; columns: any[] }) => ({
          ...item,
          shapeId: item.shapeId.trim(),
          columns: item.columns.filter((col: string) => columnSet.has(col)),
        }))
        .filter((item: { shapeId: string; columns: string | any[] }) =>
          item.shapeId &&
          item.columns.length > 0 &&
          shapeIdSet.has(item.shapeId)
        )

      setShapes(nextShapes)
      setPlaceholders(nextPlaceholders)
      setColumns(nextColumns)
      setTextReplacements(filteredText)
      setImageReplacements(filteredImages)
    } catch (error) {
      showBanner('error', formatErrorMessage('input.jsonError', error))
    } finally {
      setIsLoadingShapes(false)
      setIsLoadingPlaceholders(false)
      setIsLoadingColumns(false)
      isHydratingRef.current = false
    }
  }

  useEffect(() => {
    hydrateFromSavedState().catch(() => undefined)
  }, [])

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

  const showBanner = (type: 'success' | 'error', text: string) => {
    setBanner({ type, text })
    setTimeout(() => setBanner(null), 4000)
  }

  const notifyTemplateError = (error: unknown) => {
    const now = Date.now()
    if (now - templateErrorAtRef.current < 800) return
    templateErrorAtRef.current = now
    showBanner('error', formatErrorMessage('input.templateLoadError', error))
  }

  const notifyDataError = (error: unknown) => {
    const now = Date.now()
    if (now - dataErrorAtRef.current < 800) return
    dataErrorAtRef.current = now
    showBanner('error', formatErrorMessage('input.columnLoadError', error))
  }

  const loadTemplateAssets = async (filePath: string) => {
    if (!filePath) {
      setShapes([])
      setPlaceholders([])
      setTemplateLoaded(false)
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
          preview: shape.Data ? `data:image/png;base64,${shape.Data}` : '/assets/images/app.png',
        }))
      setShapes(mappedShapes)
      lastLoadedTemplatePathRef.current = filePath

      const items = (data.Placeholders ?? [])
        .map((item) => item.trim())
        .filter((item) => item.length > 0)
      const unique = Array.from(new Set(items))
      unique.sort((a, b) => a.localeCompare(b))
      setPlaceholders(unique)
      setTemplateLoaded(true)
    } catch (error) {
      if (!isHydratingRef.current && filePath === pptxPathRef.current) {
        notifyTemplateError(error)
        setPptxPath('')
        setShapes([])
        setPlaceholders([])
        setTemplateLoaded(false)
      }
    } finally {
      setIsLoadingShapes(false)
      setIsLoadingPlaceholders(false)
    }
  }

  const loadDataAssets = async (filePath: string) => {
    if (!filePath) {
      setColumns([])
      setSheetCount(0)
      setTotalRows(0)
      setDataLoaded(false)
      return
    }

    setIsLoadingColumns(true)
    try {
      await backendApi.loadFile(filePath)
      const allColumns = await backendApi.getAllColumns([filePath])
      const workbookInfo = await backendApi.getWorkbookInfo(filePath)
      const workbookData = workbookInfo as backendApi.SheetWorkbookGetInfoSuccess
      const sheetsInfo = workbookData.Sheets ?? []
      const rowsSum = sheetsInfo.reduce((acc, sheet) => acc + (sheet.RowCount ?? 0), 0)

      setColumns(allColumns)
      setSheetCount(sheetsInfo.length)
      setTotalRows(rowsSum)
      setDataLoaded(true)
      lastLoadedDataPathRef.current = filePath
    } catch (error) {
      if (!isHydratingRef.current && filePath === dataPathRef.current) {
        notifyDataError(error)
        setDataPath('')
        setColumns([])
        setSheetCount(0)
        setTotalRows(0)
        setDataLoaded(false)
      }
    } finally {
      setIsLoadingColumns(false)
    }
  }

  useEffect(() => {
    if (isHydratingRef.current) return
    if (!pptxPath) {
      setShapes([])
      setPlaceholders([])
      setTemplateLoaded(false)
      return
    }

    if (templateLoaded && lastLoadedTemplatePathRef.current === pptxPath && shapes.length > 0) {
      return
    }

    setShapes([])
    setPlaceholders([])
    setTemplateLoaded(false)

    const timer = setTimeout(() => {
      loadTemplateAssets(pptxPath).catch(() => undefined)
    }, 400)

    return () => clearTimeout(timer)
  }, [pptxPath])

  useEffect(() => {
    if (isHydratingRef.current) return
    if (!dataPath) {
      setColumns([])
      setSheetCount(0)
      setTotalRows(0)
      setDataLoaded(false)
      return
    }

    if (isLoadingColumns || (dataLoaded && lastLoadedDataPathRef.current === dataPath)) {
      return
    }

    setColumns([])
    setSheetCount(0)
    setTotalRows(0)
    setDataLoaded(false)

    const timer = setTimeout(() => {
      loadDataAssets(dataPath).catch(() => undefined)
    }, 400)

    return () => clearTimeout(timer)
  }, [dataPath])

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
      setShapes([])
      setPlaceholders([])
    }
  }

  const handleBrowseData = async () => {
    const path = await window.electronAPI.openFile([
      { name: 'Spreadsheets Files', extensions: ['xlsx', 'xlsm'] }
    ])
    
    if (path) {
      setDataPath(path)
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
      roiType: 'Attention',
      cropType: 'Fit',
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
        showBanner('success', t('input.exportSuccess'))
      } catch (error) {
        showBanner('error', t('input.jsonError'))
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
          const nextPptxPath = config.pptxPath || ''
          const nextDataPath = config.dataPath || ''
          const nextSavePath = config.savePath || ''

          setPptxPath(nextPptxPath)
          setDataPath(nextDataPath)
          setSavePath(nextSavePath)
          setShapes([])
          setPlaceholders([])
          setColumns([])
          setSheetCount(0)
          setTotalRows(0)
          setTemplateLoaded(false)
          setDataLoaded(false)
          clearReplacements()

          setIsLoadingShapes(true)
          setIsLoadingPlaceholders(true)
          setIsLoadingColumns(true)

          let nextShapes: Shape[] = []
          let nextPlaceholders: string[] = []
          let nextColumns: string[] = []

          if (nextPptxPath) {
            const response = await backendApi.scanTemplate(nextPptxPath)
            const template = response as backendApi.SlideScanTemplateSuccess
            nextShapes = (template.Shapes ?? [])
              .filter((shape) => shape.IsImage === true)
              .map((shape) => ({
                id: String(shape.Id),
                name: shape.Name,
                preview: shape.Data ? `data:image/png;base64,${shape.Data}` : '/assets/images/app.png',
              }))
            nextPlaceholders = (template.Placeholders ?? [])
              .map((item) => item.trim())
              .filter((item) => item.length > 0)
            nextPlaceholders = Array.from(new Set(nextPlaceholders)).sort((a, b) =>
              a.localeCompare(b)
            )
            setTemplateLoaded(true)
          }

          if (nextDataPath) {
            await backendApi.loadFile(nextDataPath)
            nextColumns = await backendApi.getAllColumns([nextDataPath])
            const workbookInfo = await backendApi.getWorkbookInfo(nextDataPath)
            const workbookData = workbookInfo as backendApi.SheetWorkbookGetInfoSuccess
            const sheetsInfo = workbookData.Sheets ?? []
            const rowsSum = sheetsInfo.reduce((acc, sheet) => acc + (sheet.RowCount ?? 0), 0)
            setSheetCount(sheetsInfo.length)
            setTotalRows(rowsSum)
            setDataLoaded(true)
          }

          const placeholderSet = new Set(nextPlaceholders)
          const shapeIdSet = new Set(nextShapes.map((shape) => shape.id))
          const columnSet = new Set(nextColumns)

          const importedText = (config.textReplacements || []).map(
            (item: { id: number; searchText?: string; placeholder?: string; columns?: string[] }) => ({
              id: item.id ?? 1,
              placeholder: item.placeholder || item.searchText || '',
              columns: item.columns || []
            })
          )
          const filteredText = importedText
            .map((item: { placeholder: string; columns: any[] }) => ({
              ...item,
              placeholder: item.placeholder.trim(),
              columns: item.columns.filter((col: string) => columnSet.has(col))
            }))
            .filter((item: { placeholder: string; columns: string | any[] }) => item.placeholder && item.columns.length > 0 && placeholderSet.has(item.placeholder))

          const importedImages = (config.imageReplacements || []).map((item: {
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
          const filteredImages = importedImages
            .map((item: { shapeId: string; columns: any[] }) => ({
              ...item,
              shapeId: item.shapeId.trim(),
              columns: item.columns.filter((col: string) => columnSet.has(col)),
            }))
            .filter((item: { shapeId: string; columns: string | any[] }) => item.shapeId && item.columns.length > 0 && shapeIdSet.has(item.shapeId))

          setShapes(nextShapes)
          setPlaceholders(nextPlaceholders)
          setColumns(nextColumns)
          setTextReplacements(filteredText)
          setImageReplacements(filteredImages)
          setTemplateLoaded(nextPptxPath.length > 0)
          setDataLoaded(nextDataPath.length > 0)

          showBanner('success', t('input.importSuccess'))
        }
      } catch (error) {
        showBanner('error', t('input.jsonError'))
      } finally {
        setIsLoadingShapes(false)
        setIsLoadingPlaceholders(false)
        setIsLoadingColumns(false)
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
      lastLoadedTemplatePathRef.current = ''
      lastLoadedDataPathRef.current = ''
      sessionStorage.removeItem(STORAGE_KEYS.inputMenuState)
    }
  }

  const openPreview = (shape: Shape) => {
    setPreviewShape(shape)
    setPreviewClosing(false)
    setPreviewZoom(1)
    setPreviewSize(null)
    setPreviewOffset({ x: 0, y: 0 })
  }

  const closePreview = () => {
    setPreviewClosing(true)
    setTimeout(() => {
      setPreviewShape(null)
      setPreviewClosing(false)
    }, 180)
  }

  const adjustPreviewZoom = (delta: number) => {
    setPreviewZoom((prev) => {
      const next = Math.min(3, Math.max(0.5, Number((prev + delta).toFixed(2))))
      if (next === 1) {
        setPreviewOffset({ x: 0, y: 0 })
      }
      return next
    })
  }

  const togglePreviewZoom = () => {
    setPreviewZoom((prev) => {
      const next = prev === 1 ? 2 : 1
      if (next === 1) {
        setPreviewOffset({ x: 0, y: 0 })
      }
      return next
    })
  }

  const handlePreviewPointerDown = (event: React.PointerEvent<HTMLImageElement>) => {
    if (previewZoom <= 1) return
    if (event.button !== 0) return
    isDraggingRef.current = true
    dragMovedRef.current = false
    dragStartRef.current = {
      x: event.clientX - previewOffset.x,
      y: event.clientY - previewOffset.y,
    }
    event.currentTarget.setPointerCapture(event.pointerId)
  }

  const handlePreviewPointerMove = (event: React.PointerEvent<HTMLImageElement>) => {
    if (!isDraggingRef.current) return
    const nextX = event.clientX - dragStartRef.current.x
    const nextY = event.clientY - dragStartRef.current.y
    if (!dragMovedRef.current) {
      const dx = Math.abs(nextX - previewOffset.x)
      const dy = Math.abs(nextY - previewOffset.y)
      if (dx > 2 || dy > 2) {
        dragMovedRef.current = true
      }
    }
    setPreviewOffset({ x: nextX, y: nextY })
  }

  const handlePreviewPointerUp = (event: React.PointerEvent<HTMLImageElement>) => {
    isDraggingRef.current = false
    event.currentTarget.releasePointerCapture(event.pointerId)
  }

  const handlePreviewWheel = (event: React.WheelEvent<HTMLImageElement>) => {
    event.preventDefault()
    const delta = event.deltaY > 0 ? -0.1 : 0.1
    adjustPreviewZoom(delta)
  }

  const handleSavePreview = async () => {
    if (!previewShape) return
    try {
      const response = await fetch(previewShape.preview)
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `${previewShape.name || 'shape'}.png`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Failed to save preview image:', error)
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

  const textShapeCount = placeholders.length
  const imageShapeCount = shapes.length
  const uniqueColumnCount = columns.length

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
        showBanner('error', message)
      } finally {
        setIsStarting(false)
      }
    } else {
      showBanner('error', t('input.error'))
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

      {banner && (
        <div className={`import-banner${banner.type === 'error' ? ' import-banner-error' : ''}`}>
          <span>{banner.text}</span>
          <button
            type="button"
            className="banner-close"
            onClick={() => setBanner(null)}
            aria-label={t('common.close')}
          >
            ×
          </button>
        </div>
      )}
      
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
        {templateLoaded && !isLoadingShapes && !isLoadingPlaceholders && (
          <div className="input-meta">
            <span className="input-meta-title">{t('input.templateInfoLabel')}</span>
            <span>{t('input.textShapeCount')}: {textShapeCount}</span>
            <span>{t('input.imageShapeCount')}: {imageShapeCount}</span>
          </div>
        )}
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
        {dataLoaded && !isLoadingColumns && (
          <div className="input-meta">
            <span className="input-meta-title">{t('input.dataInfoLabel')}</span>
            <span>{t('input.sheetCount')}: {sheetCount}</span>
            <span>{t('input.columnCount')}: {uniqueColumnCount}</span>
            <span>{t('input.rowCount')}: {totalRows}</span>
          </div>
        )}
      </div>

      {/* Replacement Tables - Separated */}
      <div className="replacement-section-separated">
        {/* Text Replacement */}
        <div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
          <div className="panel-header">
            <div className="panel-title">
              <button
                type="button"
                className="panel-title-toggle"
                onClick={() => setShowTextConfigs((prev) => !prev)}
                disabled={!canConfigure}
                aria-expanded={showTextConfigs}
              >
                <img
                  src="/assets/images/chevron-down.png"
                  alt=""
                  className={`panel-title-icon ${showTextConfigs ? 'expanded' : ''}`}
                />
                <h3>
                  {t('replacement.textTitle')}{' '}
                  <span className="panel-count">({textReplacements.length})</span>
                </h3>
              </button>
            </div>
            <button
              className="btn btn-success"
              onClick={addTextReplacement}
              disabled={!canConfigure || placeholders.length === 0}
            >
              + {t('replacement.add')}
            </button>
          </div>
          <div className={`panel-content ${showTextConfigs ? 'is-open' : ''}`}>
            <div className="replacement-table replacement-table-text">
              <table className="replacement-table-grid">
                <colgroup>
                  <col className="col-main" />
                  <col className="col-main" />
                  <col className="col-action" />
                </colgroup>
                <thead>
                  <tr>
                    <th>{t('replacement.searchText')}</th>
                    <th>{t('replacement.column')}</th>
                    <th className="cell-action">{t('replacement.delete')}</th>
                  </tr>
                </thead>
                <tbody>
                  {textReplacements.map(item => {
                    const available = getAvailablePlaceholders(item.placeholder)
                    return (
                      <tr key={item.id}>
                        <td>
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
                        </td>
                        <td>
                          <TagInput
                            value={item.columns}
                            onChange={(tags) => updateTextReplacement(item.id, 'columns', tags)}
                            suggestions={columns}
                            placeholder={t('replacement.columnPlaceholder')}
                          />
                        </td>
                        <td className="cell-action">
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
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* Image Replacement */}
        <div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
          <div className="panel-header">
            <div className="panel-title">
              <button
                type="button"
                className="panel-title-toggle"
                onClick={() => setShowImageConfigs((prev) => !prev)}
                disabled={!canConfigure}
                aria-expanded={showImageConfigs}
              >
                <img
                  src="/assets/images/chevron-down.png"
                  alt=""
                  className={`panel-title-icon ${showImageConfigs ? 'expanded' : ''}`}
                />
                <h3>
                  {t('replacement.imageTitle')}{' '}
                  <span className="panel-count">({imageReplacements.length})</span>
                </h3>
              </button>
            </div>
            <button
              className="btn btn-success"
              onClick={addImageReplacement}
              disabled={!canConfigure || shapes.length === 0}
            >
              + {t('replacement.add')}
            </button>
          </div>
          <div className={`panel-content ${showImageConfigs ? 'is-open' : ''}`}>
            <div className="replacement-table replacement-table-image">
              <div className="shape-gallery">
                <div className="shape-gallery-header">{t('replacement.availableShapes')}</div>
              <div className="shape-gallery-list">
                  {shapes.length === 0 ? (
                    <div className="shape-gallery-empty">{t('replacement.noShapes')}</div>
                  ) : (
                    shapes.map((shape) => (
                      <button
                        type="button"
                        key={shape.id}
                        className="shape-gallery-item"
                        onClick={() => openPreview(shape)}
                      >
                        <img
                          src={shape.preview}
                          alt={shape.name}
                          className="shape-gallery-preview"
                        />
                        <div className="shape-gallery-info">
                          <span className="shape-gallery-name">{shape.name}</span>
                          <span className="shape-gallery-id">{shape.id}</span>
                        </div>
                      </button>
                    ))
                  )}
                </div>
              </div>
              <table className="replacement-table-grid">
                <colgroup>
                  <col className="col-main" />
                  <col className="col-main" />
                  <col className="col-narrow" />
                  <col className="col-narrow" />
                  <col className="col-action" />
                </colgroup>
                <thead>
                  <tr>
                    <th>{t('replacement.shape')}</th>
                    <th>{t('replacement.column')}</th>
                    <th>{t('replacement.roi')}</th>
                    <th>{t('replacement.crop')}</th>
                    <th className="cell-action">{t('replacement.delete')}</th>
                  </tr>
                </thead>
                <tbody>
                  {imageReplacements.map(item => (
                    <tr key={item.id}>
                      <td>
                        <ShapeSelector
                          shapes={getAvailableShapes(item.shapeId)}
                          value={item.shapeId}
                          onChange={(shapeId) => updateImageReplacement(item.id, 'shapeId', shapeId)}
                          placeholder={t('replacement.shapePlaceholder')}
                        />
                      </td>
                      <td>
                        <TagInput
                          value={item.columns}
                          onChange={(tags) => updateImageReplacement(item.id, 'columns', tags)}
                          suggestions={columns}
                          placeholder={t('replacement.columnPlaceholder')}
                        />
                      </td>
                      <td>
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
                      </td>
                      <td>
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
                      </td>
                      <td className="cell-action">
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
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
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

      {previewShape && (
        <div
          className={`shape-preview-overlay ${previewClosing ? 'is-closing' : ''}`}
          onClick={closePreview}
        >
          <div
            className={`shape-preview-modal ${previewClosing ? 'is-closing' : ''}`}
            onClick={(event) => event.stopPropagation()}
          >
            <div className="shape-preview-header">
              <div className="shape-preview-title">
                {t('input.previewTitle')}
              </div>
              <button className="shape-preview-close" onClick={closePreview}>
                {t('common.close')}
              </button>
            </div>
            <div className="shape-preview-meta">
              <span className="shape-preview-name">{previewShape.name}</span>
              <span className="shape-preview-id">ID: {previewShape.id}</span>
              <span className="shape-preview-size">
                {t('input.previewSize')}: {previewSize ? `${previewSize.width}x${previewSize.height}px` : '...'}
              </span>
            </div>
            <div className="shape-preview-actions">
              <button className="shape-preview-btn" onClick={() => adjustPreviewZoom(-0.1)}>
                -
              </button>
              <span className="shape-preview-zoom">
                {t('input.previewZoom')}: {Math.round(previewZoom * 100)}%
              </span>
              <button className="shape-preview-btn" onClick={() => adjustPreviewZoom(0.1)}>
                +
              </button>
              <button className="shape-preview-btn" onClick={() => setPreviewZoom(1)}>
                {t('input.previewReset')}
              </button>
              <button className="shape-preview-btn" onClick={handleSavePreview}>
                <img src="/assets/images/download.png" alt="" className="shape-preview-icon" />
                {t('input.previewSave')}
              </button>
            </div>
            <div className="shape-preview-body">
              <div className="shape-preview-frame">
                <img
                  src={previewShape.preview}
                  alt={previewShape.name}
                  className={`shape-preview-image ${previewZoom > 1 ? 'zoomed' : ''}`}
                  style={{
                    transform: `translate(${previewOffset.x}px, ${previewOffset.y}px) scale(${previewZoom})`,
                  }}
                  onClick={() => {
                    if (!dragMovedRef.current) {
                      togglePreviewZoom()
                    }
                    dragMovedRef.current = false
                  }}
                  onPointerDown={handlePreviewPointerDown}
                  onPointerMove={handlePreviewPointerMove}
                  onPointerUp={handlePreviewPointerUp}
                  onPointerLeave={handlePreviewPointerUp}
                  onWheel={handlePreviewWheel}
                  draggable={false}
                  onLoad={(event) => {
                    const target = event.currentTarget
                    setPreviewSize({ width: target.naturalWidth, height: target.naturalHeight })
                  }}
                />
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default CreateTaskMenu
