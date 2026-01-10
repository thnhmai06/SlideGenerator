import { jobHub } from '../clients'
import type { ControlAction, ResponseBase, SlideImageConfig, SlideTextConfig } from '../common/types'
import { assertSuccess, getCaseInsensitive } from '../common/utils'
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
  JobDetail,
  JobSummary,
  JobType,
} from './types'

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
  const job = getCaseInsensitive<Record<string, unknown>>(data, 'Task')
  if (!job) return null
  return normalizeJobDetail(job)
}

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
  const jobs = (getCaseInsensitive<Array<Record<string, unknown>>>(data, 'Tasks') ??
    []) as Array<Record<string, unknown>>
  return jobs.map((job) => normalizeJobSummary(job))
}

export async function scanShapes(filePath: string): Promise<SlideScanShapesSuccess> {
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'scanshapes',
    filePath,
  })
  const data = assertSuccess<SlideScanShapesSuccess>(response)
  return {
    Type: 'scanshapes',
    FilePath: getCaseInsensitive<string>(data, 'FilePath') ?? filePath,
    Shapes: (
      (getCaseInsensitive<Array<Record<string, unknown>>>(data, 'Shapes') ?? []) as Array<
        Record<string, unknown>
      >
    ).map((shape) => normalizeShapeDto(shape)),
  } satisfies SlideScanShapesSuccess
}

export async function scanPlaceholders(filePath: string): Promise<SlideScanPlaceholdersSuccess> {
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'scanplaceholders',
    filePath,
  })
  const data = assertSuccess<SlideScanPlaceholdersSuccess>(response)
  return {
    Type: 'scanplaceholders',
    FilePath: getCaseInsensitive<string>(data, 'FilePath') ?? filePath,
    Placeholders: (getCaseInsensitive<string[]>(data, 'Placeholders') ?? []) as string[],
  } satisfies SlideScanPlaceholdersSuccess
}

export async function scanTemplate(filePath: string): Promise<SlideScanTemplateSuccess> {
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'scantemplate',
    filePath,
  })
  const data = assertSuccess<SlideScanTemplateSuccess>(response)
  return {
    Type: 'scantemplate',
    FilePath: getCaseInsensitive<string>(data, 'FilePath') ?? filePath,
    Shapes: (
      (getCaseInsensitive<Array<Record<string, unknown>>>(data, 'Shapes') ?? []) as Array<
        Record<string, unknown>
      >
    ).map((shape) => normalizeShapeDto(shape)),
    Placeholders: (getCaseInsensitive<string[]>(data, 'Placeholders') ?? []) as string[],
  } satisfies SlideScanTemplateSuccess
}

export async function createGroup(
  request: Record<string, unknown>,
): Promise<SlideGroupCreateSuccess> {
  const templatePath =
    getCaseInsensitive<string>(request, 'TemplatePath') ??
    getCaseInsensitive<string>(request, 'templatePath') ??
    ''
  const spreadsheetPath =
    getCaseInsensitive<string>(request, 'SpreadsheetPath') ??
    getCaseInsensitive<string>(request, 'spreadsheetPath') ??
    ''
  const outputPath =
    getCaseInsensitive<string>(request, 'OutputPath') ??
    getCaseInsensitive<string>(request, 'outputPath') ??
    getCaseInsensitive<string>(request, 'Path') ??
    ''
  const sheetNames =
    (getCaseInsensitive<string[]>(request, 'SheetNames') ??
      getCaseInsensitive<string[]>(request, 'sheetNames')) ??
    undefined
  const textConfigs =
    (getCaseInsensitive<SlideTextConfig[]>(request, 'TextConfigs') ??
      getCaseInsensitive<SlideTextConfig[]>(request, 'textConfigs')) ??
    undefined
  const imageConfigs =
    (getCaseInsensitive<SlideImageConfig[]>(request, 'ImageConfigs') ??
      getCaseInsensitive<SlideImageConfig[]>(request, 'imageConfigs')) ??
    undefined

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
  const job = normalizeJobSummary(getCaseInsensitive<Record<string, unknown>>(data, 'Task') ?? {})
  const sheetJobIds =
    (getCaseInsensitive<Record<string, string>>(data, 'SheetTaskIds') ?? {}) as Record<
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

export async function groupStatus(
  request: Record<string, unknown>,
): Promise<SlideGroupStatusSuccess> {
  const groupId =
    getCaseInsensitive<string>(request, 'GroupId') ??
    getCaseInsensitive<string>(request, 'groupId') ??
    ''
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

export async function groupControl(request: Record<string, unknown>): Promise<ResponseBase> {
  const groupId =
    getCaseInsensitive<string>(request, 'GroupId') ??
    getCaseInsensitive<string>(request, 'groupId') ??
    ''
  const action =
    (getCaseInsensitive<ControlAction>(request, 'Action') ??
      getCaseInsensitive<ControlAction>(request, 'action') ??
      'Pause') as ControlAction

  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'jobcontrol',
    taskId: groupId,
    taskType: 'Group',
    action,
  })
  return assertSuccess(response)
}

export async function removeGroup(
  request: Record<string, unknown>,
): Promise<SlideGroupRemoveSuccess> {
  const groupId =
    getCaseInsensitive<string>(request, 'GroupId') ??
    getCaseInsensitive<string>(request, 'groupId') ??
    ''
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

export async function jobStatus(request: Record<string, unknown>): Promise<SlideJobStatusSuccess> {
  const jobId =
    getCaseInsensitive<string>(request, 'JobId') ??
    getCaseInsensitive<string>(request, 'jobId') ??
    ''
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

export async function jobControl(request: Record<string, unknown>): Promise<ResponseBase> {
  const jobId =
    getCaseInsensitive<string>(request, 'JobId') ??
    getCaseInsensitive<string>(request, 'jobId') ??
    ''
  const action =
    (getCaseInsensitive<ControlAction>(request, 'Action') ??
      getCaseInsensitive<ControlAction>(request, 'action') ??
      'Pause') as ControlAction
  const response = await jobHub.sendRequest<ResponseBase>({
    type: 'jobcontrol',
    taskId: jobId,
    taskType: 'Sheet',
    action,
  })
  return assertSuccess(response)
}

export async function removeJob(request: Record<string, unknown>): Promise<SlideJobRemoveSuccess> {
  const jobId =
    getCaseInsensitive<string>(request, 'JobId') ??
    getCaseInsensitive<string>(request, 'jobId') ??
    ''
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

export async function getJobLogs(request: Record<string, unknown>): Promise<SlideJobLogsSuccess> {
  const jobId =
    getCaseInsensitive<string>(request, 'JobId') ??
    getCaseInsensitive<string>(request, 'jobId') ??
    ''
  return {
    Type: 'joblogs',
    JobId: jobId,
    Logs: [],
  } satisfies SlideJobLogsSuccess
}

export async function getGroupPayload(groupId: string): Promise<JobExportPayload | null> {
  if (!groupId) return null
  try {
    const detail = await fetchJobDetail(groupId, 'Group', false, true)
    return parseJobPayload(detail?.PayloadJson ?? null)
  } catch {
    return null
  }
}

export async function globalControl(request: Record<string, unknown>): Promise<unknown> {
  const action =
    (getCaseInsensitive<ControlAction>(request, 'Action') ??
      getCaseInsensitive<ControlAction>(request, 'action') ??
      'Pause') as ControlAction
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
        console.warn('Failed to load job payload:', error)
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

export async function subscribeGroup(groupId: string): Promise<void> {
  await jobHub.invoke('SubscribeGroup', groupId)
}

export async function subscribeSheet(sheetId: string): Promise<void> {
  await jobHub.invoke('SubscribeSheet', sheetId)
}

export function onSlideNotification(handler: (payload: unknown) => void): () => void {
  return jobHub.onNotification(handler)
}

export function onSlideReconnected(handler: (connectionId?: string) => void): () => void {
  return jobHub.onReconnected(handler)
}

export function onSlideConnected(handler: (connectionId?: string) => void): () => void {
  return jobHub.onConnected(handler)
}
