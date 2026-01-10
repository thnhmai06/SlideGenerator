export interface SheetWorkbookGetInfoSuccess {
  Type: 'getworkbookinfo'
  FilePath: string
  WorkbookName?: string
  Sheets: Array<{
    Name: string
    Headers: Array<string | null>
    RowCount: number
  }>
}

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
  data: Record<string, string | null>[]
  num_rows: number
  offset: number
  total_rows: number
}
