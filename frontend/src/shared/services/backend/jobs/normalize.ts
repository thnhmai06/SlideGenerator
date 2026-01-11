import type { ShapeDto } from '../common/types'
import { getCaseInsensitive } from '../common/utils'
import type { JobDetail, JobExportPayload, JobState, JobSummary, JobType } from './types'

export function normalizeShapeDto(input: Record<string, unknown>): ShapeDto {
  return {
    Id: (getCaseInsensitive<number>(input, 'Id') ?? 0) as number,
    Name: (() => {
      const val = getCaseInsensitive<string>(input, 'Name')
      return typeof val === 'string' ? val : ''
    })(),
    Data: (() => {
      const val = getCaseInsensitive<string>(input, 'Data')
      return typeof val === 'string' ? val : ''
    })(),
    Kind: (() => {
      const val = getCaseInsensitive<string>(input, 'Kind')
      return typeof val === 'string' ? val : undefined
    })(),
    IsImage: getCaseInsensitive<boolean>(input, 'IsImage') ?? undefined,
  }
}

export function normalizeJobType(value: unknown): JobType {
  const raw = typeof value === 'string' ? value.toLowerCase() : ''
  return raw === 'sheet' ? 'Sheet' : 'Group'
}

export function normalizeJobState(value: unknown): JobState {
  const raw = typeof value === 'string' ? value.toLowerCase() : ''
  switch (raw) {
    case 'processing':
      return 'Processing'
    case 'paused':
      return 'Paused'
    case 'done':
      return 'Done'
    case 'cancelled':
      return 'Cancelled'
    case 'error':
      return 'Error'
    case 'pending':
    default:
      return 'Pending'
  }
}

export function mapJobStateToJobStatus(state: JobState): string {
  switch (state) {
    case 'Processing':
      return 'Running'
    case 'Done':
      return 'Completed'
    case 'Error':
      return 'Failed'
    default:
      return state
  }
}

export function normalizeJobSummary(input: Record<string, unknown>): JobSummary {
  return {
    JobId: (getCaseInsensitive<string>(input, 'TaskId') ?? '') as string,
    JobType: normalizeJobType(getCaseInsensitive(input, 'TaskType')),
    Status: normalizeJobState(getCaseInsensitive(input, 'Status')),
    Progress: (getCaseInsensitive(input, 'Progress') ?? 0) as number,
    GroupId: (() => {
      const val = getCaseInsensitive(input, 'GroupId')
      return typeof val === 'string' ? val : undefined
    })(),
    SheetName: (() => {
      const val = getCaseInsensitive(input, 'SheetName')
      return typeof val === 'string' ? val : undefined
    })(),
    OutputPath: (() => {
      const val = getCaseInsensitive(input, 'OutputPath')
      return typeof val === 'string' ? val : undefined
    })(),
    ErrorCount: (() => {
      const val = getCaseInsensitive(input, 'ErrorCount')
      return typeof val === 'number' ? val : undefined
    })(),
    HangfireJobId: (() => {
      const val = getCaseInsensitive(input, 'HangfireJobId')
      return typeof val === 'string' ? val : undefined
    })(),
  }
}

export function normalizeJobDetail(input: Record<string, unknown>): JobDetail {
  const summary = normalizeJobSummary(input)
  const sheetsRaw = (getCaseInsensitive<Record<string, Record<string, unknown>>>(
    input,
    'Sheets',
  ) ?? {}) as Record<string, Record<string, unknown>>
  const sheets: Record<string, JobSummary> = {}
  Object.entries(sheetsRaw).forEach(([sheetId, sheet]) => {
    sheets[sheetId] = normalizeJobSummary({ TaskId: sheetId, ...sheet })
  })

  return {
    ...summary,
    ErrorMessage: (() => {
      const val = getCaseInsensitive(input, 'ErrorMessage')
      return typeof val === 'string' || val === null ? val : undefined
    })(),
    CurrentRow: (getCaseInsensitive(input, 'CurrentRow') ?? undefined) as number | undefined,
    TotalRows: (getCaseInsensitive(input, 'TotalRows') ?? undefined) as number | undefined,
    OutputFolder: (() => {
      const val = getCaseInsensitive(input, 'OutputFolder')
      return typeof val === 'string' ? val : undefined
    })(),
    Sheets: Object.keys(sheets).length > 0 ? sheets : undefined,
    PayloadJson: (() => {
      const val = getCaseInsensitive(input, 'PayloadJson')
      return typeof val === 'string' || val === null ? val : undefined
    })(),
  }
}

export function parseJobPayload(payloadJson?: string | null): JobExportPayload | null {
  if (!payloadJson) return null
  try {
    const payload = JSON.parse(payloadJson) as JobExportPayload
    if (!payload || typeof payload !== 'object') return null
    return payload
  } catch {
    return null
  }
}
