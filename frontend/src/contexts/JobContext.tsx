import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  ReactNode,
} from 'react'
import * as backendApi from '../services/backendApi'

type JobStatus = 'Pending' | 'Running' | 'Paused' | 'Completed' | 'Failed' | 'Cancelled'

interface SheetJob {
  id: string
  sheetName: string
  status: JobStatus
  currentRow: number
  totalRows: number
  progress: number
  errorCount: number
  errorMessage?: string
  logs: string[]
}

interface GroupJob {
  id: string
  workbookPath: string
  outputFolder?: string
  status: JobStatus
  progress: number
  errorCount: number
  sheets: Record<string, SheetJob>
  logs: string[]
}

interface CreateGroupPayload {
  templatePath: string
  spreadsheetPath: string
  outputPath: string
  textConfigs: backendApi.SlideTextConfig[]
  imageConfigs: backendApi.SlideImageConfig[]
  sheetNames?: string[]
}

interface JobContextValue {
  groups: GroupJob[]
  createGroup: (payload: CreateGroupPayload) => Promise<GroupJob>
  refreshGroups: () => Promise<void>
  clearCompleted: () => void
  groupControl: (groupId: string, action: backendApi.ControlAction) => Promise<void>
  jobControl: (jobId: string, action: backendApi.ControlAction) => Promise<void>
  globalControl: (action: backendApi.ControlAction) => Promise<void>
}

const JobContext = createContext<JobContextValue | undefined>(undefined)

const GROUP_META_KEY = 'slidegen.groupMeta'
const MAX_LOGS = 200

type GroupMeta = {
  outputFolder?: string
  workbookPath?: string
}

const loadGroupMeta = (): Record<string, GroupMeta> => {
  try {
    const raw = localStorage.getItem(GROUP_META_KEY)
    return raw ? (JSON.parse(raw) as Record<string, GroupMeta>) : {}
  } catch (error) {
    console.error('Failed to load group meta:', error)
    return {}
  }
}

const saveGroupMeta = (meta: Record<string, GroupMeta>) => {
  try {
    localStorage.setItem(GROUP_META_KEY, JSON.stringify(meta))
  } catch (error) {
    console.error('Failed to save group meta:', error)
  }
}

const createEmptyGroup = (groupId: string): GroupJob => ({
  id: groupId,
  workbookPath: '',
  status: 'Pending',
  progress: 0,
  errorCount: 0,
  sheets: {},
  logs: [],
})

const createEmptySheet = (sheetId: string): SheetJob => ({
  id: sheetId,
  sheetName: sheetId,
  status: 'Pending',
  currentRow: 0,
  totalRows: 0,
  progress: 0,
  errorCount: 0,
  logs: [],
})

export const JobProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [groupsById, setGroupsById] = useState<Record<string, GroupJob>>({})
  const groupsRef = useRef<Record<string, GroupJob>>({})
  const groupMetaRef = useRef<Record<string, GroupMeta>>(loadGroupMeta())
  const subscribedGroups = useRef(new Set<string>())
  const subscribedSheets = useRef(new Set<string>())
  const sheetToGroup = useRef<Record<string, string>>({})

  const rememberGroupMeta = useCallback((groupId: string, meta: GroupMeta) => {
    const current = groupMetaRef.current
    current[groupId] = { ...current[groupId], ...meta }
    saveGroupMeta(current)
  }, [])

  const updateGroup = useCallback((groupId: string, updater: (group: GroupJob) => GroupJob) => {
    setGroupsById((prev) => {
      const current = prev[groupId] ?? createEmptyGroup(groupId)
      const updated = updater(current)
      return { ...prev, [groupId]: updated }
    })
  }, [])

  const updateSheet = useCallback(
    (sheetId: string, updater: (sheet: SheetJob) => SheetJob) => {
      const groupId = sheetToGroup.current[sheetId]
      if (!groupId) return

      setGroupsById((prev) => {
        const group = prev[groupId]
        if (!group) return prev

        const currentSheet = group.sheets[sheetId] ?? createEmptySheet(sheetId)
        const updatedSheet = updater(currentSheet)
        const updatedGroup: GroupJob = {
          ...group,
          sheets: { ...group.sheets, [sheetId]: updatedSheet },
        }
        return { ...prev, [groupId]: updatedGroup }
      })
    },
    []
  )

  const ensureGroupSubscription = useCallback(async (groupId: string) => {
    if (subscribedGroups.current.has(groupId)) return
    await backendApi.subscribeGroup(groupId)
    subscribedGroups.current.add(groupId)
  }, [])

  const ensureSheetSubscription = useCallback(async (sheetId: string) => {
    if (subscribedSheets.current.has(sheetId)) return
    await backendApi.subscribeSheet(sheetId)
    subscribedSheets.current.add(sheetId)
  }, [])

  const upsertGroupFromSummary = useCallback(
    (summary: backendApi.GroupSummary) => {
      updateGroup(summary.GroupId, (group) => {
        const meta = groupMetaRef.current[summary.GroupId] ?? {}
        return {
          ...group,
          id: summary.GroupId,
          workbookPath: summary.WorkbookPath ?? meta.workbookPath ?? group.workbookPath,
          outputFolder: meta.outputFolder ?? group.outputFolder,
          status: summary.Status as JobStatus,
          progress: summary.Progress ?? group.progress,
          errorCount: summary.ErrorCount ?? group.errorCount,
        }
      })
    },
    [updateGroup]
  )

  const syncGroupStatus = useCallback(
    async (groupId: string) => {
      const response = await backendApi.groupStatus({ GroupId: groupId })
      const status = response as backendApi.SlideGroupStatusSuccess
      const jobs = status.Jobs ?? {}

      updateGroup(groupId, (group) => {
        const sheets: Record<string, SheetJob> = { ...group.sheets }
        Object.values(jobs).forEach((job) => {
          const sheetId = job.JobId
          sheetToGroup.current[sheetId] = groupId
          sheets[sheetId] = {
            id: sheetId,
            sheetName: job.SheetName,
            status: job.Status as JobStatus,
            currentRow: job.CurrentRow ?? 0,
            totalRows: job.TotalRows ?? 0,
            progress: job.Progress ?? 0,
            errorCount: job.ErrorCount ?? 0,
            errorMessage: job.ErrorMessage ?? undefined,
            logs: sheets[sheetId]?.logs ?? [],
          }
        })

        return {
          ...group,
          status: status.Status as JobStatus,
          progress: status.Progress ?? group.progress,
          errorCount: status.ErrorCount ?? group.errorCount,
          sheets,
        }
      })

      await ensureGroupSubscription(groupId)
      await Promise.all(Object.keys(jobs).map((jobId) => ensureSheetSubscription(jobId)))
    },
    [ensureGroupSubscription, ensureSheetSubscription, updateGroup]
  )

  const refreshGroups = useCallback(async () => {
    const response = await backendApi.getAllGroups()
    const data = response as backendApi.SlideGlobalGetGroupsSuccess
    const summaries = data.Groups ?? []

    summaries.forEach((summary) => {
      upsertGroupFromSummary(summary)
      rememberGroupMeta(summary.GroupId, { workbookPath: summary.WorkbookPath })
    })

    await Promise.allSettled(summaries.map((summary) => syncGroupStatus(summary.GroupId)))
  }, [rememberGroupMeta, syncGroupStatus, upsertGroupFromSummary])

  const createGroup = useCallback(
    async (payload: CreateGroupPayload) => {
      const response = await backendApi.createGroup({
        TemplatePath: payload.templatePath,
        SpreadsheetPath: payload.spreadsheetPath,
        OutputPath: payload.outputPath,
        TextConfigs: payload.textConfigs,
        ImageConfigs: payload.imageConfigs,
        SheetNames: payload.sheetNames,
      })

      const data = response as backendApi.SlideGroupCreateSuccess
      const groupId = data.GroupId

      let createdGroup: GroupJob = createEmptyGroup(groupId)
      updateGroup(groupId, (group) => {
        const sheets: Record<string, SheetJob> = { ...group.sheets }
        Object.entries(data.JobIds ?? {}).forEach(([sheetName, jobId]) => {
          sheetToGroup.current[jobId] = groupId
          sheets[jobId] = {
            ...(sheets[jobId] ?? createEmptySheet(jobId)),
            id: jobId,
            sheetName,
          }
        })

        createdGroup = {
          ...group,
          id: groupId,
          workbookPath: payload.spreadsheetPath,
          outputFolder: data.OutputFolder,
          status: 'Running',
          progress: 0,
          errorCount: 0,
          sheets,
        }
        return createdGroup
      })

      rememberGroupMeta(groupId, {
        outputFolder: data.OutputFolder,
        workbookPath: payload.spreadsheetPath,
      })

      await ensureGroupSubscription(groupId)
      await Promise.all(
        Object.values(data.JobIds ?? {}).map((jobId) => ensureSheetSubscription(jobId))
      )

      await syncGroupStatus(groupId)

      return createdGroup
    },
    [ensureGroupSubscription, ensureSheetSubscription, rememberGroupMeta, syncGroupStatus, updateGroup]
  )

  const groupControl = useCallback(async (groupId: string, action: backendApi.ControlAction) => {
    await backendApi.groupControl({ GroupId: groupId, Action: action })
  }, [])

  const jobControl = useCallback(async (jobId: string, action: backendApi.ControlAction) => {
    await backendApi.jobControl({ JobId: jobId, Action: action })
  }, [])

  const globalControl = useCallback(async (action: backendApi.ControlAction) => {
    await backendApi.globalControl({ Action: action })
  }, [])

  const clearCompleted = useCallback(() => {
    setGroupsById((prev) => {
      const next: Record<string, GroupJob> = {}
      Object.values(prev).forEach((group) => {
        const status = group.status.toLowerCase()
        if (!['completed', 'failed', 'cancelled'].includes(status)) {
          next[group.id] = group
        }
      })
      return next
    })
  }, [])

  useEffect(() => {
    groupsRef.current = groupsById
  }, [groupsById])

  useEffect(() => {
    const unsubscribe = backendApi.onSlideNotification((payload) => {
      if (!payload || typeof payload !== 'object') return
      const data = payload as Record<string, unknown>
      const getValue = (key: string) =>
        data[key] ?? data[key.charAt(0).toLowerCase() + key.slice(1)]

      const groupId = getValue('GroupId') as string | undefined
      const jobId = getValue('JobId') as string | undefined
      const status = getValue('Status') as string | undefined
      const message = getValue('Message') as string | undefined
      const error = getValue('Error') as string | undefined
      const level = getValue('Level') as string | undefined
      const timestamp = getValue('Timestamp') as string | undefined

      if (groupId && typeof getValue('Progress') === 'number') {
        updateGroup(groupId, (group) => ({
          ...group,
          progress: getValue('Progress') as number,
          errorCount: (getValue('ErrorCount') as number) ?? group.errorCount,
        }))
        return
      }

      if (groupId && status) {
        updateGroup(groupId, (group) => ({ ...group, status: status as JobStatus }))
        return
      }

      if (jobId && typeof getValue('CurrentRow') === 'number') {
        updateSheet(jobId, (sheet) => ({
          ...sheet,
          currentRow: getValue('CurrentRow') as number,
          totalRows: (getValue('TotalRows') as number) ?? sheet.totalRows,
          progress: (getValue('Progress') as number) ?? sheet.progress,
          errorCount: (getValue('ErrorCount') as number) ?? sheet.errorCount,
        }))
        return
      }

      if (jobId && status) {
        updateSheet(jobId, (sheet) => ({
          ...sheet,
          status: status as JobStatus,
          errorMessage: message ?? sheet.errorMessage,
        }))
        return
      }

      if (jobId && error) {
        updateSheet(jobId, (sheet) => ({
          ...sheet,
          logs: [...sheet.logs, error].slice(-MAX_LOGS),
        }))
        return
      }

      if (jobId && message) {
        const logLine = level ? `${level}: ${message}` : message
        const withTime = timestamp ? `[${new Date(timestamp).toLocaleTimeString()}] ${logLine}` : logLine
        const targetGroupId = sheetToGroup.current[jobId]

        if (targetGroupId) {
          updateSheet(jobId, (sheet) => ({
            ...sheet,
            logs: [...sheet.logs, withTime].slice(-MAX_LOGS),
          }))
        } else if (groupsRef.current[jobId]) {
          updateGroup(jobId, (group) => ({
            ...group,
            logs: [...group.logs, withTime].slice(-MAX_LOGS),
          }))
        }
      }
    })

    return unsubscribe
  }, [updateGroup, updateSheet])

  useEffect(() => {
    refreshGroups().catch((error) => {
      console.error('Failed to refresh jobs:', error)
    })
  }, [refreshGroups])

  const groups = useMemo(
    () => Object.values(groupsById),
    [groupsById]
  )

  const value = useMemo(
    () => ({
      groups,
      createGroup,
      refreshGroups,
      clearCompleted,
      groupControl,
      jobControl,
      globalControl,
    }),
    [clearCompleted, createGroup, groupControl, groups, jobControl, globalControl, refreshGroups]
  )

  return <JobContext.Provider value={value}>{children}</JobContext.Provider>
}

export const useJobs = () => {
  const context = useContext(JobContext)
  if (!context) {
    throw new Error('useJobs must be used within JobProvider')
  }
  return context
}
