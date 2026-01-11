import type { ShapeDto, SlideImageConfig, SlideTextConfig } from '../common/types'

export type JobType = 'Group' | 'Sheet'
export type JobState = 'Pending' | 'Processing' | 'Paused' | 'Done' | 'Cancelled' | 'Error'

export interface JobSummary {
  JobId: string
  JobType: JobType
  Status: JobState
  Progress: number
  GroupId?: string
  SheetName?: string
  OutputPath?: string
  ErrorCount?: number
  HangfireJobId?: string
}

export interface JobDetail extends JobSummary {
  ErrorMessage?: string | null
  CurrentRow?: number
  TotalRows?: number
  OutputFolder?: string
  Sheets?: Record<string, JobSummary>
  PayloadJson?: string | null
}

export interface JobExportPayload {
  taskType: JobType
  templatePath: string
  spreadsheetPath: string
  outputPath: string
  sheetNames?: string[]
  sheetName?: string
  textConfigs?: SlideTextConfig[]
  imageConfigs?: SlideImageConfig[]
}

export interface SlideScanShapesSuccess {
  Type: 'scanshapes'
  FilePath: string
  Shapes: ShapeDto[]
}

export interface SlideScanPlaceholdersSuccess {
  Type: 'scanplaceholders'
  FilePath: string
  Placeholders: string[]
}

export interface SlideScanTemplateSuccess {
  Type: 'scantemplate'
  FilePath: string
  Shapes: ShapeDto[]
  Placeholders: string[]
}

export interface SlideGroupCreateSuccess {
  Type: 'groupcreate'
  GroupId: string
  OutputFolder: string
  JobIds: Record<string, string>
}

export interface JobStatusInfo {
  JobId: string
  SheetName: string
  Status: string
  CurrentRow: number
  TotalRows: number
  Progress: number
  OutputPath?: string
  ErrorMessage?: string | null
  ErrorCount?: number
  HangfireJobId?: string
}

export interface SlideGroupStatusSuccess {
  Type: 'groupstatus'
  GroupId: string
  Status: string
  Progress: number
  Jobs: Record<string, JobStatusInfo>
  ErrorCount?: number
}

export interface SlideGroupRemoveSuccess {
  Type: 'groupremove'
  GroupId: string
  Removed: boolean
}

export interface SlideJobStatusSuccess {
  Type: 'jobstatus'
  JobId: string
  SheetName: string
  Status: string
  CurrentRow: number
  TotalRows: number
  Progress: number
  OutputPath?: string
  ErrorMessage?: string | null
  ErrorCount?: number
  HangfireJobId?: string
}

export interface SlideJobRemoveSuccess {
  Type: 'jobremove'
  JobId: string
  Removed: boolean
}

export interface JobLogEntry {
  Level: string
  Message: string
  Timestamp: string
  Data?: Record<string, unknown>
}

export interface SlideJobLogsSuccess {
  Type: 'joblogs'
  JobId: string
  Logs: JobLogEntry[]
}

export interface GroupSummary {
  GroupId: string
  WorkbookPath: string
  OutputFolder?: string
  Status: string
  Progress: number
  SheetCount: number
  CompletedSheets: number
  ErrorCount?: number
}

export interface SlideGlobalGetGroupsSuccess {
  Type: 'getallgroups'
  Groups: GroupSummary[]
}
