import type { ShapeDto, SlideImageConfig, SlideTextConfig } from '../common/types'

export type JobType = 'Group' | 'Sheet'
export type JobState = 'Pending' | 'Processing' | 'Paused' | 'Done' | 'Cancelled' | 'Error'

export interface JobSummary {
  jobId: string
  jobType: JobType
  status: JobState
  progress: number
  groupId?: string
  sheetName?: string
  outputPath?: string
  errorCount?: number
  hangfireJobId?: string
}

export interface JobDetail extends JobSummary {
  errorMessage?: string | null
  currentRow?: number
  totalRows?: number
  outputFolder?: string
  sheets?: Record<string, JobSummary>
  payloadJson?: string | null
}

export interface JobExportPayload {
  jobType: JobType
  templatePath: string
  spreadsheetPath: string
  outputPath: string
  sheetNames?: string[]
  sheetName?: string
  textConfigs?: SlideTextConfig[]
  imageConfigs?: SlideImageConfig[]
}

export interface SlideScanShapesSuccess {
  type: 'scanshapes'
  filePath: string
  shapes: ShapeDto[]
}

export interface SlideScanPlaceholdersSuccess {
  type: 'scanplaceholders'
  filePath: string
  placeholders: string[]
}

export interface SlideScanTemplateSuccess {
  type: 'scantemplate'
  filePath: string
  shapes: ShapeDto[]
  placeholders: string[]
}

export interface SlideGroupCreateSuccess {
  type: 'groupcreate'
  groupId: string
  outputFolder: string
  jobIds: Record<string, string>
}

export interface JobStatusInfo {
  jobId: string
  sheetName: string
  status: string
  currentRow: number
  totalRows: number
  progress: number
  outputPath?: string
  errorMessage?: string | null
  errorCount?: number
  hangfireJobId?: string
}

export interface SlideGroupStatusSuccess {
  type: 'groupstatus'
  groupId: string
  status: string
  progress: number
  jobs: Record<string, JobStatusInfo>
  errorCount?: number
}

export interface SlideGroupRemoveSuccess {
  type: 'groupremove'
  groupId: string
  removed: boolean
}

export interface SlideJobStatusSuccess {
  type: 'jobstatus'
  jobId: string
  sheetName: string
  status: string
  currentRow: number
  totalRows: number
  progress: number
  outputPath?: string
  errorMessage?: string | null
  errorCount?: number
  hangfireJobId?: string
}

export interface SlideJobRemoveSuccess {
  type: 'jobremove'
  jobId: string
  removed: boolean
}

export interface JobLogEntry {
  level: string
  message: string
  timestamp: string
  data?: Record<string, unknown>
}

export interface SlideJobLogsSuccess {
  type: 'joblogs'
  jobId: string
  logs: JobLogEntry[]
}

export interface GroupSummary {
  groupId: string
  workbookPath: string
  outputFolder?: string
  status: string
  progress: number
  sheetCount: number
  completedSheets: number
  errorCount?: number
}

export interface SlideGlobalGetGroupsSuccess {
  type: 'getallgroups'
  groups: GroupSummary[]
}
