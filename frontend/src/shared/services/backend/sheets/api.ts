import { loggers } from '@/shared/services/logging';
import { sheetHub } from '../clients'
import type { ResponseBase } from '../common/types'
import { assertSuccess } from '../common/utils'
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

/** Response type for opening a workbook file */
interface OpenBookSheetSuccess {
  Type: 'openfile'
  FilePath: string
}

/** Response type for closing a workbook file */
interface SheetWorkbookCloseSuccess {
  Type: 'closefile'
  FilePath: string
}

/** Response type for getting sheet table information */
interface SheetWorkbookGetSheetInfoSuccess {
  Type: 'gettables'
  FilePath: string
  Sheets: Record<string, number>
}

/** Response type for getting sheet column headers */
interface SheetWorksheetGetHeadersSuccess {
  Type: 'getheaders'
  FilePath: string
  SheetName: string
  Headers: Array<string | null>
}

/** Response type for getting a single row */
interface SheetWorksheetGetRowSuccess {
  Type: 'getrow'
  FilePath: string
  TableName: string
  RowNumber: number
  Row: Record<string, string | null>
}

/**
 * Opens and loads an Excel/spreadsheet file for processing.
 *
 * @param filePath - Path to the spreadsheet file
 * @returns File load result with sheet information
 */
export async function loadFile(filePath: string): Promise<LoadFileResponse> {
  const open = await sheetHub.sendRequest<ResponseBase>({
    type: 'openfile',
    filePath,
  })
  assertSuccess<OpenBookSheetSuccess>(open)

  const info = await sheetHub.sendRequest<ResponseBase>({
    type: 'gettables',
    filePath,
  })
  const tables = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(info)
  const sheets = tables.Sheets ?? {}
  const sheetNames = Object.keys(sheets)

  return {
    success: true,
    group_id: filePath,
    file_type: 'sheet',
    num_sheets: sheetNames.length,
    sheets: sheetNames,
  }
}

/**
 * Closes and unloads a previously loaded spreadsheet file.
 *
 * @param filePath - Path to the spreadsheet file
 * @returns Success indicator
 */
export async function unloadFile(filePath: string): Promise<{ success: boolean }> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'closefile',
    filePath,
  })
  assertSuccess<SheetWorkbookCloseSuccess>(response)
  return { success: true }
}

/**
 * Gets the list of currently loaded files.
 *
 * @returns List of loaded file information
 */
export async function getLoadedFiles(): Promise<FileListResponse> {
  return { files: [] }
}

/**
 * Gets all sheets in a workbook file.
 *
 * @param filePath - Path to the workbook file
 * @returns List of sheets with metadata
 */
export async function getSheets(filePath: string): Promise<SheetListResponse> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'gettables',
    filePath,
  })
  const raw = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(response) as unknown as Record<
    string,
    unknown
  >

  const tables = ((raw.sheets as Record<string, number>) ?? {}) as Record<string, number>
  const sheets: SheetInfo[] = Object.entries(tables).map(([name, rows]) => ({
    sheet_id: name,
    sheet_name: name,
    num_rows: rows,
    num_cols: 0,
  }))

  return { sheets }
}

/**
 * Gets column headers for a specific sheet.
 *
 * @param filePath - Path to the workbook file
 * @param sheetName - Name of the sheet
 * @returns List of column names
 */
export async function getColumns(filePath: string, sheetName: string): Promise<ColumnListResponse> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'getheaders',
    filePath,
    sheetName,
  })
  const raw = assertSuccess<SheetWorksheetGetHeadersSuccess>(response) as unknown as Record<
    string,
    unknown
  >
  const headers = ((raw.headers as Array<string | null>) ?? []) as Array<string | null>
  const columns = headers.filter((header): header is string => Boolean(header))
  return { columns }
}

/**
 * Gets detailed information about a specific sheet.
 *
 * @param filePath - Path to the workbook file
 * @param sheetName - Name of the sheet
 * @returns Sheet details including row/column counts and headers
 */
export async function getSheetInfo(filePath: string, sheetName: string): Promise<SheetDetailInfo> {
  const tableResponse = await sheetHub.sendRequest<ResponseBase>({
    type: 'gettables',
    filePath,
  })
  const tablesRaw = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(
    tableResponse,
  ) as unknown as Record<string, unknown>
  const tableMap = ((tablesRaw.sheets as Record<string, number>) ?? {}) as Record<string, number>

  const headerResponse = await sheetHub.sendRequest<ResponseBase>({
    type: 'getheaders',
    filePath,
    sheetName,
  })
  const headersRaw = assertSuccess<SheetWorksheetGetHeadersSuccess>(
    headerResponse,
  ) as unknown as Record<string, unknown>
  const headerList = ((headersRaw.headers as Array<string | null>) ?? []) as Array<string | null>
  const columns = headerList.filter((header): header is string => Boolean(header))

  return {
    sheet_id: sheetName,
    sheet_name: sheetName,
    num_rows: tableMap[sheetName] ?? 0,
    num_cols: columns.length,
    columns,
    start_row: 1,
    start_col: 1,
  }
}

/**
 * Gets paginated row data from a sheet.
 *
 * @param filePath - Path to the workbook file
 * @param sheetName - Name of the sheet
 * @param offset - Row offset to start from (0-based)
 * @param limit - Maximum number of rows to return
 * @returns Sheet data with row content
 */
export async function getSheetData(
  filePath: string,
  sheetName: string,
  offset = 0,
  limit?: number,
): Promise<SheetDataResponse> {
  const info = await getSheetInfo(filePath, sheetName)
  const totalRows = info.num_rows
  const startRow = offset + 1
  const endRow = limit ? Math.min(totalRows, offset + limit) : totalRows

  const data: Record<string, string | null>[] = []
  for (let rowIndex = startRow; rowIndex <= endRow; rowIndex += 1) {
    const response = await sheetHub.sendRequest<ResponseBase>({
      type: 'getrow',
      filePath,
      tableName: sheetName,
      rowNumber: rowIndex,
    })
    const rowRaw = assertSuccess<SheetWorksheetGetRowSuccess>(response) as unknown as Record<
      string,
      unknown
    >
    const rowData = ((rowRaw.row as Record<string, string | null>) ?? {}) as Record<
      string,
      string | null
    >
    data.push(rowData)
  }

  return {
    columns: info.columns,
    data,
    num_rows: data.length,
    offset,
    total_rows: totalRows,
  }
}

/**
 * Gets a single row from a sheet by index.
 *
 * @param filePath - Path to the workbook file
 * @param sheetName - Name of the sheet
 * @param rowIndex - 1-based row index
 * @returns Row data as key-value pairs
 */
export async function getSheetRow(
  filePath: string,
  sheetName: string,
  rowIndex: number,
): Promise<{ row_index: number; data: Record<string, string | null> }> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'getrow',
    filePath,
    tableName: sheetName,
    rowNumber: rowIndex,
  })
  const rowRaw = assertSuccess<SheetWorksheetGetRowSuccess>(response) as unknown as Record<
    string,
    unknown
  >
  const rowData = ((rowRaw.row as Record<string, string | null>) ?? {}) as Record<
    string,
    string | null
  >
  const rowNumber = ((rowRaw.rowNumber as number) ?? rowIndex) as number
  return { row_index: rowNumber, data: rowData }
}

/**
 * Gets all unique column headers from multiple workbook files.
 *
 * @param filePaths - Array of workbook file paths
 * @returns Sorted array of unique column names
 */
export async function getAllColumns(filePaths: string[]): Promise<string[]> {
  const allColumns = new Set<string>()

  for (const filePath of filePaths) {
    try {
      const infoResponse = await sheetHub.sendRequest<ResponseBase>({
        type: 'getworkbookinfo',
        filePath,
      })
      const infoRaw = assertSuccess<SheetWorkbookGetInfoSuccess>(
        infoResponse,
      ) as unknown as Record<string, unknown>
      const sheets = ((infoRaw.sheets as Array<Record<string, unknown>>) ?? []) as Array<
        Record<string, unknown>
      >
      sheets.forEach((sheet) => {
        const headers = ((sheet.headers as Array<string | null>) ?? []) as Array<string | null>
        headers
          .filter((header): header is string => Boolean(header))
          .forEach((header) => allColumns.add(header))
      })
    } catch (error) {
      loggers.jobs.error(`Error getting columns for file ${filePath}:`, error)
    }
  }

  return Array.from(allColumns).sort()
}

/**
 * Gets comprehensive workbook information including all sheets and their headers.
 *
 * @param filePath - Path to the workbook file
 * @returns Workbook info with sheet details
 */
export async function getWorkbookInfo(filePath: string): Promise<SheetWorkbookGetInfoSuccess> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'getworkbookinfo',
    filePath,
  })
  const raw = assertSuccess<SheetWorkbookGetInfoSuccess>(response) as unknown as Record<
    string,
    unknown
  >
  const sheets = ((raw.sheets as Array<Record<string, unknown>>) ?? []) as Array<
    Record<string, unknown>
  >

  return {
    Type: 'getworkbookinfo',
    FilePath: (raw.filePath as string) ?? filePath,
    WorkbookName: (raw.workbookName as string) ?? undefined,
    Sheets: sheets.map((sheet) => ({
      Name: ((sheet.name as string) ?? '') as string,
      Headers: ((sheet.headers as Array<string | null>) ?? []) as Array<string | null>,
      RowCount: ((sheet.rowCount as number) ?? 0) as number,
    })),
  }
}
