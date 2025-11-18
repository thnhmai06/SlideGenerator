/**
 * Backend API Service
 * Communicates with Flask backend server
 */

const BASE_URL = 'http://localhost:5000/api'

export interface LoadedFile {
  group_id: string
  file_path: string
  file_type: string
  num_sheets: number
}

export interface SheetInfo {
  sheet_id: string
  sheet_name: string
  num_rows: number
  num_cols: number
}

export interface LoadFileResponse {
  success: boolean
  group_id: string
  file_type: string
  num_sheets: number
  sheets: string[]
}

export interface FileListResponse {
  files: LoadedFile[]
}

export interface SheetListResponse {
  sheets: SheetInfo[]
}

export interface ColumnListResponse {
  columns: string[]
}

export interface SheetDetailInfo {
  sheet_id: string
  sheet_name: string
  num_rows: number
  num_cols: number
  columns: string[]
  start_row: number
  start_col: number
}

export interface SheetDataResponse {
  columns: string[]
  data: Record<string, any>[]
  num_rows: number
  offset: number
  total_rows: number
}

/**
 * Load a CSV/Excel file into the backend
 */
export async function loadFile(filePath: string, fileId?: string): Promise<LoadFileResponse> {
  const response = await fetch(`${BASE_URL}/data/load`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      file_path: filePath,
      file_id: fileId,
    }),
  })

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to load file')
  }

  return response.json()
}

/**
 * Unload a file from backend
 */
export async function unloadFile(fileId: string): Promise<{ success: boolean }> {
  const response = await fetch(`${BASE_URL}/data/unload/${fileId}`, {
    method: 'DELETE',
  })

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to unload file')
  }

  return response.json()
}

/**
 * Get list of all loaded files
 */
export async function getLoadedFiles(): Promise<FileListResponse> {
  const response = await fetch(`${BASE_URL}/data/files`)

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to get loaded files')
  }

  return response.json()
}

/**
 * Get list of sheets in a file
 */
export async function getSheets(fileId: string): Promise<SheetListResponse> {
  const response = await fetch(`${BASE_URL}/data/${fileId}/sheets`)

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to get sheets')
  }

  return response.json()
}

/**
 * Get list of columns in a sheet
 */
export async function getColumns(fileId: string, sheetId: string): Promise<ColumnListResponse> {
  const response = await fetch(`${BASE_URL}/data/${fileId}/sheets/${sheetId}/columns`)

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to get columns')
  }

  return response.json()
}

/**
 * Get detailed information about a sheet
 */
export async function getSheetInfo(fileId: string, sheetId: string): Promise<SheetDetailInfo> {
  const response = await fetch(`${BASE_URL}/data/${fileId}/sheets/${sheetId}/info`)

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to get sheet info')
  }

  return response.json()
}

/**
 * Get sheet data with pagination
 */
export async function getSheetData(
  fileId: string,
  sheetId: string,
  offset: number = 0,
  limit?: number
): Promise<SheetDataResponse> {
  const params = new URLSearchParams({
    offset: offset.toString(),
  })

  if (limit !== undefined) {
    params.append('limit', limit.toString())
  }

  const response = await fetch(`${BASE_URL}/data/${fileId}/sheets/${sheetId}/data?${params}`)

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to get sheet data')
  }

  return response.json()
}

/**
 * Get a specific row from a sheet
 */
export async function getSheetRow(
  fileId: string,
  sheetId: string,
  rowIndex: number
): Promise<{ row_index: number; data: Record<string, any> }> {
  const response = await fetch(`${BASE_URL}/data/${fileId}/sheets/${sheetId}/rows/${rowIndex}`)

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.error || 'Failed to get row')
  }

  return response.json()
}

/**
 * Health check
 */
export async function checkHealth(): Promise<{ status: string; message: string }> {
  const response = await fetch(`${BASE_URL}/health`)

  if (!response.ok) {
    throw new Error('Backend server is not responding')
  }

  return response.json()
}

/**
 * Get all unique columns from multiple files (merged from all sheets)
 */
export async function getAllColumns(fileIds: string[]): Promise<string[]> {
  const allColumns = new Set<string>()

  for (const fileId of fileIds) {
    try {
      const sheetsResponse = await getSheets(fileId)
      
      for (const sheet of sheetsResponse.sheets) {
        const columnsResponse = await getColumns(fileId, sheet.sheet_id)
        columnsResponse.columns.forEach(col => allColumns.add(col))
      }
    } catch (error) {
      console.error(`Error getting columns for file ${fileId}:`, error)
    }
  }

  return Array.from(allColumns).sort()
}
