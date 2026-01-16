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
    taskId: jobId,
    taskType: jobType,
    includeSheets,
    includePayload,
  })
  const data = assertSuccess<ResponseBase>(response)
  const job = (data as Record<string, unknown>).task as Record<string, unknown> | undefined
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
    taskType: jobType,
  })
  const data = assertSuccess<ResponseBase>(response)
  const jobs = (((data as Record<string, unknown>).tasks as Array<Record<string, unknown>>) ??
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
    Type: 'scanshapes',
    FilePath: (raw.filePath as string) ?? filePath,
    Shapes: ((raw.shapes ?? []) as Array<Record<string, unknown>>).map((shape) =>
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
    Type: 'scanplaceholders',
    FilePath: (raw.filePath as string) ?? filePath,
    Placeholders: (raw.placeholders ?? []) as string[],
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
    Type: 'scantemplate',
    FilePath: (raw.filePath as string) ?? filePath,
    Shapes: ((raw.shapes ?? []) as Array<Record<string, unknown>>).map((shape) =>
      normalizeShapeDto(shape),
    ),
    Placeholders: (raw.placeholders ?? []) as string[],
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
    taskType: 'Group',
    templatePath,
    spreadsheetPath,
    outputPath,
    sheetNames,
    textConfigs,
    imageConfigs,
  })
  const data = assertSuccess<ResponseBase>(response)
  const dataRecord = data as Record<string, unknown>
  const job = normalizeJobSummary((dataRecord.task as Record<string, unknown>) ?? {})
  const sheetJobIds = ((dataRecord.sheetTaskIds as Record<string, string>) ?? {}) as Record<
    string,
    string
  >
  return {
    Type: 'groupcreate',
    GroupId: job.JobId,
    OutputFolder: job.OutputPath ?? '',
    JobIds: sheetJobIds,
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

  const sheets = detail.Sheets ?? {}
  const sheetEntries = Object.entries(sheets)
  const sheetDetails = await Promise.all(
    sheetEntries.map(async ([sheetId, summary]) => {
      const sheetDetail = await fetchJobDetail(sheetId, 'Sheet').catch(() => null)
      return { sheetId, summary, sheetDetail }
    }),
  )

  const normalizedJobs: Record<string, JobStatusInfo> = {}
  sheetDetails.forEach(({ sheetId, summary, sheetDetail }) => {
    const status = mapJobStateToJobStatus(sheetDetail?.Status ?? summary.Status)
    normalizedJobs[sheetId] = {
      JobId: sheetId,
      SheetName: summary.SheetName ?? sheetDetail?.SheetName ?? '',
      Status: status,
      CurrentRow: sheetDetail?.CurrentRow ?? 0,
      TotalRows: sheetDetail?.TotalRows ?? 0,
      Progress: summary.Progress ?? sheetDetail?.Progress ?? 0,
      OutputPath: sheetDetail?.OutputPath ?? summary.OutputPath,
      ErrorMessage: sheetDetail?.ErrorMessage ?? undefined,
      ErrorCount: summary.ErrorCount ?? sheetDetail?.ErrorCount ?? 0,
      HangfireJobId: sheetDetail?.HangfireJobId ?? summary.HangfireJobId ?? undefined,
    }
  })

  return {
    Type: 'groupstatus',
    GroupId: detail.JobId,
    Status: mapJobStateToJobStatus(detail.Status),
    Progress: detail.Progress ?? 0,
    Jobs: normalizedJobs,
    ErrorCount: detail.ErrorCount ?? 0,
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
    taskId: groupId,
    taskType: 'Group',
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
      taskId: groupId,
      taskType: 'Group',
      action: 'Remove',
    })
  }
  return { Type: 'groupremove', GroupId: groupId, Removed: true }
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
    Type: 'jobstatus',
    JobId: detail.JobId,
    SheetName: detail.SheetName ?? '',
    Status: mapJobStateToJobStatus(detail.Status),
    CurrentRow: detail.CurrentRow ?? 0,
    TotalRows: detail.TotalRows ?? 0,
    Progress: detail.Progress ?? 0,
    OutputPath: detail.OutputPath ?? undefined,
    ErrorMessage: detail.ErrorMessage ?? undefined,
    ErrorCount: detail.ErrorCount ?? undefined,
    HangfireJobId: detail.HangfireJobId ?? undefined,
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
    taskId: jobId,
    taskType: 'Sheet',
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
      taskId: jobId,
      taskType: 'Sheet',
      action: 'Remove',
    })
  }
  return { Type: 'jobremove', JobId: jobId, Removed: true }
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
    Type: 'joblogs',
    JobId: jobId,
    Logs: [],
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
    return parseJobPayload(detail?.PayloadJson ?? null)
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
        taskId: group.JobId,
        taskType: 'Group',
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
      let outputFolder = group.OutputPath
      let sheetCount = 0
      let completedSheets = 0
      try {
        const detail = await fetchJobDetail(group.JobId, 'Group', true, true)
        if (detail) {
          outputFolder = detail.OutputFolder ?? outputFolder
          const sheets = detail.Sheets ?? {}
          sheetCount = Object.keys(sheets).length
          completedSheets = Object.values(sheets).filter(
            (sheet) => mapJobStateToJobStatus(sheet.Status) === 'Completed',
          ).length
          const payload = parseJobPayload(detail.PayloadJson)
          workbookPath = payload?.spreadsheetPath ?? ''
        }
      } catch (error) {
        loggers.jobs.warn('Failed to load job payload:', error)
      }

      return {
        GroupId: group.JobId,
        WorkbookPath: workbookPath,
        OutputFolder: outputFolder ?? '',
        Status: mapJobStateToJobStatus(group.Status),
        Progress: group.Progress ?? 0,
        SheetCount: sheetCount,
        CompletedSheets: completedSheets,
        ErrorCount: group.ErrorCount ?? 0,
      } satisfies GroupSummary
    }),
  )

  return {
    Type: 'getallgroups',
    Groups: summaries,
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
