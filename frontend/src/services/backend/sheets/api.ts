import { sheetHub } from '../clients'
import type { ResponseBase } from '../common/types'
import { assertSuccess, getCaseInsensitive } from '../common/utils'
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

interface OpenBookSheetSuccess {
  Type: 'openfile'
  FilePath: string
}

interface SheetWorkbookCloseSuccess {
  Type: 'closefile'
  FilePath: string
}

interface SheetWorkbookGetSheetInfoSuccess {
  Type: 'gettables'
  FilePath: string
  Sheets: Record<string, number>
}

interface SheetWorksheetGetHeadersSuccess {
  Type: 'getheaders'
  FilePath: string
  SheetName: string
  Headers: Array<string | null>
}

interface SheetWorksheetGetRowSuccess {
  Type: 'getrow'
  FilePath: string
  TableName: string
  RowNumber: number
  Row: Record<string, string | null>
}

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

export async function unloadFile(filePath: string): Promise<{ success: boolean }> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'closefile',
    filePath,
  })
  assertSuccess<SheetWorkbookCloseSuccess>(response)
  return { success: true }
}

export async function getLoadedFiles(): Promise<FileListResponse> {
  return { files: [] }
}

export async function getSheets(filePath: string): Promise<SheetListResponse> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'gettables',
    filePath,
  })
  const data = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(response)

  const tables = (getCaseInsensitive<Record<string, number>>(data as unknown, 'Sheets') ??
    {}) as Record<string, number>
  const sheets: SheetInfo[] = Object.entries(tables).map(([name, rows]) => ({
    sheet_id: name,
    sheet_name: name,
    num_rows: rows,
    num_cols: 0,
  }))

  return { sheets }
}

export async function getColumns(filePath: string, sheetName: string): Promise<ColumnListResponse> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'getheaders',
    filePath,
    sheetName,
  })
  const data = assertSuccess<SheetWorksheetGetHeadersSuccess>(response)
  const headers = (getCaseInsensitive<Array<string | null>>(data, 'Headers') ?? []) as Array<
    string | null
  >
  const columns = headers.filter((header): header is string => Boolean(header))
  return { columns }
}

export async function getSheetInfo(filePath: string, sheetName: string): Promise<SheetDetailInfo> {
  const tableResponse = await sheetHub.sendRequest<ResponseBase>({
    type: 'gettables',
    filePath,
  })
  const tables = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(tableResponse)
  const tableMap = (getCaseInsensitive<Record<string, number>>(tables, 'Sheets') ?? {}) as Record<
    string,
    number
  >

  const headerResponse = await sheetHub.sendRequest<ResponseBase>({
    type: 'getheaders',
    filePath,
    sheetName,
  })
  const headers = assertSuccess<SheetWorksheetGetHeadersSuccess>(headerResponse)
  const headerList = (getCaseInsensitive<Array<string | null>>(headers, 'Headers') ?? []) as Array<
    string | null
  >
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
    const row = assertSuccess<SheetWorksheetGetRowSuccess>(response)
    const rowData = (getCaseInsensitive<Record<string, string | null>>(row, 'Row') ?? {}) as Record<
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
  const row = assertSuccess<SheetWorksheetGetRowSuccess>(response)
  const rowData = (getCaseInsensitive<Record<string, string | null>>(row, 'Row') ?? {}) as Record<
    string,
    string | null
  >
  const rowNumber = (getCaseInsensitive<number>(row, 'RowNumber') ?? rowIndex) as number
  return { row_index: rowNumber, data: rowData }
}

export async function getAllColumns(filePaths: string[]): Promise<string[]> {
  const allColumns = new Set<string>()

  for (const filePath of filePaths) {
    try {
      const infoResponse = await sheetHub.sendRequest<ResponseBase>({
        type: 'getworkbookinfo',
        filePath,
      })
      const info = assertSuccess<SheetWorkbookGetInfoSuccess>(infoResponse)
      const sheets = (getCaseInsensitive<Array<Record<string, unknown>>>(info, 'Sheets') ??
        []) as Array<Record<string, unknown>>
      sheets.forEach((sheet) => {
        const headers = (getCaseInsensitive<Array<string | null>>(sheet, 'Headers') ?? []) as Array<
          string | null
        >
        headers
          .filter((header): header is string => Boolean(header))
          .forEach((header) => allColumns.add(header))
      })
    } catch (error) {
      console.error(`Error getting columns for file ${filePath}:`, error)
    }
  }

  return Array.from(allColumns).sort()
}

export async function getWorkbookInfo(filePath: string): Promise<SheetWorkbookGetInfoSuccess> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: 'getworkbookinfo',
    filePath,
  })
  const data = assertSuccess<SheetWorkbookGetInfoSuccess>(response)
  const sheets = (getCaseInsensitive<Array<Record<string, unknown>>>(data, 'Sheets') ??
    []) as Array<Record<string, unknown>>

  return {
    Type: 'getworkbookinfo',
    FilePath: getCaseInsensitive<string>(data, 'FilePath') ?? filePath,
    WorkbookName: getCaseInsensitive<string>(data, 'WorkbookName') ?? undefined,
    Sheets: sheets.map((sheet) => ({
      Name: (getCaseInsensitive<string>(sheet, 'Name') ?? '') as string,
      Headers: (getCaseInsensitive<Array<string | null>>(sheet, 'Headers') ?? []) as Array<
        string | null
      >,
      RowCount: (getCaseInsensitive<number>(sheet, 'RowCount') ?? 0) as number,
    })),
  }
}
