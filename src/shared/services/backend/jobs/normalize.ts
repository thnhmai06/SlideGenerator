import type { ShapeDto } from '../common/types'
import type { JobDetail, JobExportPayload, JobState, JobSummary, JobType } from './types'

export function normalizeShapeDto(input: Record<string, unknown>): ShapeDto {
  return {
    id: ((input.id as number) ?? 0) as number,
    name: typeof input.name === 'string' ? input.name : '',
    data: typeof input.data === 'string' ? input.data : '',
    kind: typeof input.kind === 'string' ? input.kind : undefined,
    isImage: (input.isImage as boolean) ?? undefined,
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
    jobId: ((input.jobId as string) ?? '') as string,
    jobType: normalizeJobType(input.jobType),
    status: normalizeJobState(input.status),
    progress: ((input.progress as number) ?? 0) as number,
    groupId: typeof input.groupId === 'string' ? input.groupId : undefined,
    sheetName: typeof input.sheetName === 'string' ? input.sheetName : undefined,
    outputPath: typeof input.outputPath === 'string' ? input.outputPath : undefined,
    errorCount: typeof input.errorCount === 'number' ? input.errorCount : undefined,
    hangfireJobId: typeof input.hangfireJobId === 'string' ? input.hangfireJobId : undefined,
  }
}

export function normalizeJobDetail(input: Record<string, unknown>): JobDetail {
  const summary = normalizeJobSummary(input)
  const sheetsRaw = ((input.sheets as Record<string, Record<string, unknown>>) ?? {}) as Record<
    string,
    Record<string, unknown>
  >
  const sheets: Record<string, JobSummary> = {}
  Object.entries(sheetsRaw).forEach(([sheetId, sheet]) => {
    sheets[sheetId] = normalizeJobSummary({ jobId: sheetId, ...sheet })
  })

  return {
    ...summary,
    errorMessage:
      typeof input.errorMessage === 'string' || input.errorMessage === null
        ? (input.errorMessage as string | null | undefined)
        : undefined,
    currentRow: (input.currentRow as number) ?? undefined,
    totalRows: (input.totalRows as number) ?? undefined,
    outputFolder: typeof input.outputFolder === 'string' ? input.outputFolder : undefined,
    sheets: Object.keys(sheets).length > 0 ? sheets : undefined,
    payloadJson:
      typeof input.payloadJson === 'string' || input.payloadJson === null
        ? (input.payloadJson as string | null | undefined)
        : undefined,
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
