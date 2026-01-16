import { loggers } from '@/shared/services/logging';
import { jobHub } from '../clients'
import type { ControlAction, ResponseBase, SlideImageConfig, SlideTextConfig } from '../common/types'
import { assertSuccess } from '../common/utils'
import {
  mapJobStateToJobStatus,
  normalizeShapeDto,
  normalizeJobDetail,
  normalizeJobSummary,
  parseJobPayload,
} from './normalize'
import type {
  GroupSummary,
  JobStatusInfo,
  SlideGlobalGetGroupsSuccess,
  SlideGroupCreateSuccess,
  SlideGroupRemoveSuccess,
  SlideGroupStatusSuccess,
  SlideJobLogsSuccess,
  SlideJobRemoveSuccess,
  SlideJobStatusSuccess,
  SlideScanPlaceholdersSuccess,
  SlideScanShapesSuccess,
  SlideScanTemplateSuccess,
  JobExportPayload,
  JobDetail,
  JobSummary,
  JobType,
} from './types'

/**
 * Fetches detailed information for a specific job.
 *
 * @param jobId - The unique job identifier
 * @param jobType - Type of job ('Group' or 'Sheet')
 * @param includeSheets - Whether to include child sheet job details
 * @param includePayload - Whether to include the job creation payload
 * @returns Job detail object or null if not found
 */
async function fetchJobDetail(
  jobId: string,
  jobType?: JobType,
  includeSheets = false,
  includePayload = false,
): Promise<JobDetail | null> {
  if (!jobId) return null
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'jobquery',
    jobId: jobId,
    jobType: jobType,
    includeSheets,
    includePayload,
  })
  const data = assertSuccess<ResponseBase>(response)
  const job = (data as Record<string, unknown>).job as Record<string, unknown> | undefined
  if (!job) return null
  return normalizeJobDetail(job)
}

/**
 * Fetches a list of jobs filtered by scope and type.
 *
 * @param scope - Filter by 'Active', 'Completed', or 'All' jobs
 * @param jobType - Optional filter by job type
 * @returns Array of job summary objects
 */
async function fetchJobList(
  scope: 'Active' | 'Completed' | 'All',
  jobType?: JobType,
): Promise<JobSummary[]> {
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'jobquery',
    scope,
    jobType: jobType,
  })
  const data = assertSuccess<ResponseBase>(response)
  const jobs = (((data as Record<string, unknown>).jobs as Array<Record<string, unknown>>) ??
    []) as Array<Record<string, unknown>>
  return jobs.map((job) => normalizeJobSummary(job))
}

/**
 * Scans a PowerPoint template file for available shapes.
 *
 * @param filePath - Path to the PowerPoint template file
 * @returns Shape information including shape IDs and types
 */
export async function scanShapes(filePath: string): Promise<SlideScanShapesSuccess> {
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'scanshapes',
    filePath,
  })
  const raw = assertSuccess<SlideScanShapesSuccess>(response) as unknown as Record<string, unknown>
  return {
    type: 'scanshapes',
    filePath: (raw.filePath as string) ?? filePath,
    shapes: ((raw.shapes ?? []) as Array<Record<string, unknown>>).map((shape) =>
      normalizeShapeDto(shape),
    ),
  } satisfies SlideScanShapesSuccess
}

/**
 * Scans a PowerPoint template for text placeholders.
 *
 * @param filePath - Path to the PowerPoint template file
 * @returns List of placeholder names found in the template
 */
export async function scanPlaceholders(filePath: string): Promise<SlideScanPlaceholdersSuccess> {
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'scanplaceholders',
    filePath,
  })
  const raw = assertSuccess<SlideScanPlaceholdersSuccess>(response) as unknown as Record<
    string,
    unknown
  >
  return {
    type: 'scanplaceholders',
    filePath: (raw.filePath as string) ?? filePath,
    placeholders: (raw.placeholders ?? []) as string[],
  } satisfies SlideScanPlaceholdersSuccess
}

/**
 * Scans a PowerPoint template for both shapes and placeholders.
 *
 * @param filePath - Path to the PowerPoint template file
 * @returns Combined shape and placeholder information
 */
export async function scanTemplate(filePath: string): Promise<SlideScanTemplateSuccess> {
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'scantemplate',
    filePath,
  })
  const raw = assertSuccess<SlideScanTemplateSuccess>(response) as unknown as Record<
    string,
    unknown
  >
  return {
    type: 'scantemplate',
    filePath: (raw.filePath as string) ?? filePath,
    shapes: ((raw.shapes ?? []) as Array<Record<string, unknown>>).map((shape) =>
      normalizeShapeDto(shape),
    ),
    placeholders: (raw.placeholders ?? []) as string[],
  } satisfies SlideScanTemplateSuccess
}

/**
 * Creates a new slide generation group job.
 *
 * @param request - Job creation parameters including template, spreadsheet, and output paths
 * @returns Created group ID and associated sheet job IDs
 */
export async function createGroup(
  request: Record<string, unknown>,
): Promise<SlideGroupCreateSuccess> {
  const templatePath = request.templatePath as string
  const spreadsheetPath = request.spreadsheetPath as string
  const outputPath = request.outputPath as string
  const sheetNames = request.sheetNames as string[] | undefined
  const textConfigs = request.textConfigs as SlideTextConfig[] | undefined
  const imageConfigs = request.imageConfigs as SlideImageConfig[] | undefined

  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'jobcreate',
    jobType: 'Group',
    templatePath,
    spreadsheetPath,
    outputPath,
    sheetNames,
    textConfigs,
    imageConfigs,
  })
  const data = assertSuccess<ResponseBase>(response)
  const dataRecord = data as Record<string, unknown>
  const job = normalizeJobSummary((dataRecord.job as Record<string, unknown>) ?? {})
  const sheetJobIds = ((dataRecord.sheetJobIds as Record<string, string>) ?? {}) as Record<
    string,
    string
  >
  return {
    type: 'groupcreate',
    groupId: job.jobId,
    outputFolder: job.outputPath ?? '',
    jobIds: sheetJobIds,
  } satisfies SlideGroupCreateSuccess
}

/**
 * Gets detailed status for a group job including all child sheet jobs.
 *
 * @param request - Object containing groupId
 * @returns Group status with progress and individual sheet job statuses
 */
export async function groupStatus(
  request: Record<string, unknown>,
): Promise<SlideGroupStatusSuccess> {
  const groupId = request.groupId as string
  const detail = await fetchJobDetail(groupId, 'Group', true, true)
  if (!detail) {
    throw new Error(`Group job ${groupId} not found`)
  }

  const sheets = detail.sheets ?? {}
  const sheetEntries = Object.entries(sheets)
  const sheetDetails = await Promise.all(
    sheetEntries.map(async ([sheetId, summary]) => {
      const sheetDetail = await fetchJobDetail(sheetId, 'Sheet').catch(() => null)
      return { sheetId, summary, sheetDetail }
    }),
  )

  const normalizedJobs: Record<string, JobStatusInfo> = {}
  sheetDetails.forEach(({ sheetId, summary, sheetDetail }) => {
    const status = mapJobStateToJobStatus(sheetDetail?.status ?? summary.status)
    normalizedJobs[sheetId] = {
      jobId: sheetId,
      sheetName: summary.sheetName ?? sheetDetail?.sheetName ?? '',
      status: status,
      currentRow: sheetDetail?.currentRow ?? 0,
      totalRows: sheetDetail?.totalRows ?? 0,
      progress: summary.progress ?? sheetDetail?.progress ?? 0,
      outputPath: sheetDetail?.outputPath ?? summary.outputPath,
      errorMessage: sheetDetail?.errorMessage ?? undefined,
      errorCount: summary.errorCount ?? sheetDetail?.errorCount ?? 0,
      hangfireJobId: sheetDetail?.hangfireJobId ?? summary.hangfireJobId ?? undefined,
    }
  })

  return {
    type: 'groupstatus',
    groupId: detail.jobId,
    status: mapJobStateToJobStatus(detail.status),
    progress: detail.progress ?? 0,
    jobs: normalizedJobs,
    errorCount: detail.errorCount ?? 0,
  } satisfies SlideGroupStatusSuccess
}

/**
 * Sends a control action (pause, resume, cancel) to a group job.
 *
 * @param request - Object containing groupId and action
 * @returns Response indicating success or failure
 */
export async function groupControl(request: Record<string, unknown>): Promise<ResponseBase> {
  const groupId = request.groupId as string
  const action = request.action as ControlAction

  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'jobcontrol',
    jobId: groupId,
    jobType: 'Group',
    action,
  })
  return assertSuccess(response)
}

/**
 * Removes a group job and all its associated sheet jobs.
 *
 * @param request - Object containing groupId
 * @returns Confirmation of removal
 */
export async function removeGroup(
  request: Record<string, unknown>,
): Promise<SlideGroupRemoveSuccess> {
  const groupId = request.groupId as string
  if (groupId) {
    await jobHub.sendRequest<ResponseBase>({
      type: 'jobcontrol',
      jobId: groupId,
      jobType: 'Group',
      action: 'Remove',
    })
  }
  return { type: 'groupremove', groupId: groupId, removed: true }
}

/**
 * Gets detailed status for a single sheet job.
 *
 * @param request - Object containing jobId
 * @returns Sheet job status including progress and error information
 */
export async function jobStatus(request: Record<string, unknown>): Promise<SlideJobStatusSuccess> {
  const jobId = request.jobId as string
  const detail = await fetchJobDetail(jobId, 'Sheet')
  if (!detail) {
    throw new Error(`Sheet job ${jobId} not found`)
  }
  return {
    type: 'jobstatus',
    jobId: detail.jobId,
    sheetName: detail.sheetName ?? '',
    status: mapJobStateToJobStatus(detail.status),
    currentRow: detail.currentRow ?? 0,
    totalRows: detail.totalRows ?? 0,
    progress: detail.progress ?? 0,
    outputPath: detail.outputPath ?? undefined,
    errorMessage: detail.errorMessage ?? undefined,
    errorCount: detail.errorCount ?? undefined,
    hangfireJobId: detail.hangfireJobId ?? undefined,
  } satisfies SlideJobStatusSuccess
}

/**
 * Sends a control action (pause, resume, cancel) to a sheet job.
 *
 * @param request - Object containing jobId and action
 * @returns Response indicating success or failure
 */
export async function jobControl(request: Record<string, unknown>): Promise<ResponseBase> {
  const jobId = request.jobId as string
  const action = request.action as ControlAction
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'jobcontrol',
    jobId: jobId,
    jobType: 'Sheet',
    action,
  })
  return assertSuccess(response)
}

/**
 * Removes a single sheet job.
 *
 * @param request - Object containing jobId
 * @returns Confirmation of removal
 */
export async function removeJob(request: Record<string, unknown>): Promise<SlideJobRemoveSuccess> {
  const jobId = request.jobId as string
  if (jobId) {
    await jobHub.sendRequest<ResponseBase>({
      type: 'jobcontrol',
      jobId: jobId,
      jobType: 'Sheet',
      action: 'Remove',
    })
  }
  return { type: 'jobremove', jobId: jobId, removed: true }
}

/**
 * Retrieves logs for a specific job.
 *
 * @param request - Object containing jobId
 * @returns Job logs array
 */
export async function getJobLogs(request: Record<string, unknown>): Promise<SlideJobLogsSuccess> {
  const jobId = request.jobId as string
  return {
    type: 'joblogs',
    jobId: jobId,
    logs: [],
  } satisfies SlideJobLogsSuccess
}

/**
 * Retrieves the original creation payload for a group job.
 *
 * @param groupId - The group job identifier
 * @returns The job export payload or null if not found
 */
export async function getGroupPayload(groupId: string): Promise<JobExportPayload | null> {
  if (!groupId) return null
  try {
    const detail = await fetchJobDetail(groupId, 'Group', false, true)
    return parseJobPayload(detail?.payloadJson ?? null)
  } catch {
    return null
  }
}

/**
 * Sends a control action to all active group jobs.
 *
 * @param request - Object containing action
 * @returns Success indicator
 */
export async function globalControl(request: Record<string, unknown>): Promise<unknown> {
  const action = request.action as ControlAction
  const groups = await fetchJobList('Active', 'Group')
  await Promise.all(
    groups.map((group) =>
      jobHub.sendRequest<ResponseBase>({
        type: 'jobcontrol',
        jobId: group.jobId,
        jobType: 'Group',
        action,
      }),
    ),
  )
  return { ok: true }
}

/**
 * Retrieves all group jobs with their summary information.
 *
 * @returns List of all group summaries
 */
export async function getAllGroups(): Promise<SlideGlobalGetGroupsSuccess> {
  const groups = await fetchJobList('All', 'Group')
  const summaries = await Promise.all(
    groups.map(async (group) => {
      let workbookPath = ''
      let outputFolder = group.outputPath
      let sheetCount = 0
      let completedSheets = 0
      try {
        const detail = await fetchJobDetail(group.jobId, 'Group', true, true)
        if (detail) {
          outputFolder = detail.outputFolder ?? outputFolder
          const sheets = detail.sheets ?? {}
          sheetCount = Object.keys(sheets).length
          completedSheets = Object.values(sheets).filter(
            (sheet) => mapJobStateToJobStatus(sheet.status) === 'Completed',
          ).length
          const payload = parseJobPayload(detail.payloadJson)
          workbookPath = payload?.spreadsheetPath ?? ''
        }
      } catch (error) {
        loggers.jobs.warn('Failed to load job payload:', error)
      }

      return {
        groupId: group.jobId,
        workbookPath: workbookPath,
        outputFolder: outputFolder ?? '',
        status: mapJobStateToJobStatus(group.status),
        progress: group.progress ?? 0,
        sheetCount: sheetCount,
        completedSheets: completedSheets,
        errorCount: group.errorCount ?? 0,
      } satisfies GroupSummary
    }),
  )

  return {
    type: 'getallgroups',
    groups: summaries,
  } satisfies SlideGlobalGetGroupsSuccess
}

/**
 * Subscribes to real-time updates for a group job.
 *
 * @param groupId - The group job identifier to subscribe to
 */
export async function subscribeGroup(groupId: string): Promise<void> {
  await jobHub.invoke('SubscribeGroup', groupId)
}

/**
 * Subscribes to real-time updates for a sheet job.
 *
 * @param sheetId - The sheet job identifier to subscribe to
 */
export async function subscribeSheet(sheetId: string): Promise<void> {
  await jobHub.invoke('SubscribeSheet', sheetId)
}

/**
 * Registers a handler for slide notification events.
 *
 * @param handler - Callback function for notification payloads
 * @returns Cleanup function to unsubscribe
 */
export function onSlideNotification(handler: (payload: unknown) => void): () => void {
  return jobHub.onNotification(handler)
}

/**
 * Registers a handler for reconnection events.
 *
 * @param handler - Callback function invoked when connection is re-established
 * @returns Cleanup function to unsubscribe
 */
export function onSlideReconnected(handler: (connectionId?: string) => void): () => void {
  return jobHub.onReconnected(handler)
}

/**
 * Registers a handler for initial connection events.
 *
 * @param handler - Callback function invoked when connection is first established
 * @returns Cleanup function to unsubscribe
 */
export function onSlideConnected(handler: (connectionId?: string) => void): () => void {
  return jobHub.onConnected(handler)
}
