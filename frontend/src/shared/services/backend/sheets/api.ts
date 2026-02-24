import { loggers } from '@/shared/services/logging'
import type {
  ColumnListResponse,
  FileListResponse,
  LoadFileResponse,
  SheetDataResponse,
  SheetDetailInfo,
  SheetInfo,
  SheetListResponse,
  SheetWorkbookGetInfoSuccess,
} from './types'

type SheetScanItem = {
  sheetName: string
  headers: string[]
  recordCount: number
}

type SheetScanResult = {
  filePath: string
  sheets: SheetScanItem[]
}

const workbookCache = new Map<string, SheetScanResult>()

const toStringOrEmpty = (value: unknown): string => (typeof value === 'string' ? value : '')

const toNumberOrZero = (value: unknown): number => {
  if (typeof value === 'number') return value
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : 0
}

const normalizeScanResult = (filePath: string, raw: Record<string, unknown>): SheetScanResult => {
  const rawSheets = ((raw.sheets ?? raw.Sheets ?? []) as Array<Record<string, unknown>>) ?? []

  const sheets = rawSheets.map((sheet) => {
    const rawHeaders =
      ((sheet.headers ?? sheet.Headers ?? []) as Array<string | null | undefined>) ?? []

    return {
      sheetName: toStringOrEmpty(sheet.sheetName ?? sheet.SheetName),
      headers: rawHeaders.filter((header): header is string => typeof header === 'string'),
      recordCount: toNumberOrZero(sheet.recordCount ?? sheet.RecordCount),
    } satisfies SheetScanItem
  })

  return {
    filePath: toStringOrEmpty(raw.filePath ?? raw.FilePath) || filePath,
    sheets,
  }
}

const scanWorkbook = async (filePath: string, force = false): Promise<SheetScanResult> => {
  if (!force) {
    const cached = workbookCache.get(filePath)
    if (cached) return cached
  }

  const raw =
    (await window.electronAPI.backendRequest<Record<string, unknown>>('sheet.scan', {
      filePath,
    })) ?? {}

  const normalized = normalizeScanResult(filePath, raw)
  workbookCache.set(filePath, normalized)
  return normalized
}

const findSheet = (workbook: SheetScanResult, sheetName: string): SheetScanItem | undefined =>
  workbook.sheets.find((sheet) => sheet.sheetName === sheetName)

export async function loadFile(filePath: string): Promise<LoadFileResponse> {
  const workbook = await scanWorkbook(filePath)
  return {
    success: true,
    group_id: workbook.filePath,
    file_type: 'sheet',
    num_sheets: workbook.sheets.length,
    sheets: workbook.sheets.map((sheet) => sheet.sheetName),
  }
}

export async function unloadFile(filePath: string): Promise<{ success: boolean }> {
  workbookCache.delete(filePath)
  return { success: true }
}

export async function getLoadedFiles(): Promise<FileListResponse> {
  return {
    files: Array.from(workbookCache.values()).map((item) => ({
      group_id: item.filePath,
      file_path: item.filePath,
      file_type: 'sheet',
      num_sheets: item.sheets.length,
    })),
  }
}

export async function getSheets(filePath: string): Promise<SheetListResponse> {
  const workbook = await scanWorkbook(filePath)
  const sheets: SheetInfo[] = workbook.sheets.map((sheet) => ({
    sheet_id: sheet.sheetName,
    sheet_name: sheet.sheetName,
    num_rows: sheet.recordCount,
    num_cols: sheet.headers.length,
  }))

  return { sheets }
}

export async function getColumns(filePath: string, sheetName: string): Promise<ColumnListResponse> {
  const workbook = await scanWorkbook(filePath)
  const sheet = findSheet(workbook, sheetName)
  return {
    columns: sheet?.headers ?? [],
  }
}

export async function getSheetInfo(filePath: string, sheetName: string): Promise<SheetDetailInfo> {
  const workbook = await scanWorkbook(filePath)
  const sheet = findSheet(workbook, sheetName)

  return {
    sheet_id: sheetName,
    sheet_name: sheetName,
    num_rows: sheet?.recordCount ?? 0,
    num_cols: sheet?.headers.length ?? 0,
    columns: sheet?.headers ?? [],
    start_row: 1,
    start_col: 1,
  }
}

export async function getSheetData(
  filePath: string,
  sheetName: string,
  offset = 0,
  _limit?: number,
): Promise<SheetDataResponse> {
  const info = await getSheetInfo(filePath, sheetName)
  loggers.jobs.warn('Sheet row preview is not supported by current JSON-RPC backend.')
  return {
    columns: info.columns,
    data: [],
    num_rows: 0,
    offset,
    total_rows: info.num_rows,
  }
}

export async function getSheetRow(
  filePath: string,
  sheetName: string,
  rowIndex: number,
): Promise<{ row_index: number; data: Record<string, string | null> }> {
  await getSheetInfo(filePath, sheetName)
  loggers.jobs.warn('Sheet row preview is not supported by current JSON-RPC backend.')
  return {
    row_index: rowIndex,
    data: {},
  }
}

export async function getAllColumns(filePaths: string[]): Promise<string[]> {
  const allColumns = new Set<string>()

  for (const filePath of filePaths) {
    try {
      const workbook = await scanWorkbook(filePath)
      workbook.sheets.forEach((sheet) => {
        sheet.headers.forEach((header) => allColumns.add(header))
      })
    } catch (error) {
      loggers.jobs.error(`Error getting columns for file ${filePath}:`, error)
    }
  }

  return Array.from(allColumns).sort()
}

export async function getWorkbookInfo(filePath: string): Promise<SheetWorkbookGetInfoSuccess> {
  const workbook = await scanWorkbook(filePath)

  return {
    Type: 'getworkbookinfo',
    FilePath: workbook.filePath,
    WorkbookName: undefined,
    Sheets: workbook.sheets.map((sheet) => ({
      Name: sheet.sheetName,
      Headers: sheet.headers,
      RowCount: sheet.recordCount,
    })),
  }
}
