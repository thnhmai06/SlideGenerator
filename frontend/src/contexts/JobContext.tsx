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

interface LogEntry {
  message: string
  level?: string
  timestamp?: string
  row?: number
  rowStatus?: string
}

interface SheetJob {
  id: string
  sheetName: string
  status: JobStatus
  currentRow: number
  totalRows: number
  progress: number
  errorCount: number
  outputPath?: string
  errorMessage?: string
  logs: LogEntry[]
}

interface GroupJob {
  id: string
  workbookPath: string
  outputFolder?: string
  status: JobStatus
  progress: number
  errorCount: number
  sheets: Record<string, SheetJob>
  logs: LogEntry[]
  createdAt?: string
  completedAt?: string
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
  removeGroup: (groupId: string) => Promise<boolean>
  removeSheet: (jobId: string) => Promise<boolean>
  loadSheetLogs: (jobId: string) => Promise<void>
  globalControl: (action: backendApi.ControlAction) => Promise<void>
  exportGroupConfig: (groupId: string) => Promise<boolean>
  hasGroupConfig: (groupId: string) => boolean
}

const JobContext = createContext<JobContextValue | undefined>(undefined)

const MAX_LOGS = 200

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
  const subscribedGroups = useRef(new Set<string>())
  const subscribedSheets = useRef(new Set<string>())
  const sheetToGroup = useRef<Record<string, string>>({})

  const applyGroupTimestamps = (prev: GroupJob, next: GroupJob): GroupJob => {
    const createdAt = next.createdAt ?? prev.createdAt ?? new Date().toISOString()
    const isCompleted = ['completed', 'failed', 'cancelled'].includes(next.status.toLowerCase())
    const completedAt = next.completedAt ?? prev.completedAt ?? (isCompleted ? new Date().toISOString() : undefined)
    return { ...next, createdAt, completedAt }
  }

  const updateGroup = useCallback((groupId: string, updater: (group: GroupJob) => GroupJob) => {
    setGroupsById((prev) => {
      const current = prev[groupId] ?? createEmptyGroup(groupId)
      const updated = applyGroupTimestamps(current, updater(current))
      return { ...prev, [groupId]: updated }
    })
  }, [])

  const GROUP_META_KEY = 'slidegen.group.meta'
  const GROUP_CONFIG_KEY = 'slidegen.group.config'

  const readGroupConfigs = (): Record<string, CreateGroupPayload> => {
    try {
      const raw = sessionStorage.getItem(GROUP_CONFIG_KEY)
      if (!raw) return {}
      return JSON.parse(raw) as Record<string, CreateGroupPayload>
    } catch (error) {
      console.error('Failed to read group configs:', error)
      return {}
    }
  }

  const saveGroupConfig = useCallback((groupId: string, payload: CreateGroupPayload) => {
    try {
      const current = readGroupConfigs()
      current[groupId] = payload
      sessionStorage.setItem(GROUP_CONFIG_KEY, JSON.stringify(current))
    } catch (error) {
      console.error('Failed to save group config:', error)
    }
  }, [])

  const removeGroupConfig = useCallback((groupIds: string[]) => {
    try {
      const current = readGroupConfigs()
      let changed = false
      groupIds.forEach((groupId) => {
        if (groupId in current) {
          delete current[groupId]
          changed = true
        }
      })
      if (!changed) return
      if (Object.keys(current).length === 0) {
        sessionStorage.removeItem(GROUP_CONFIG_KEY)
      } else {
        sessionStorage.setItem(GROUP_CONFIG_KEY, JSON.stringify(current))
      }
    } catch (error) {
      console.error('Failed to remove group configs:', error)
    }
  }, [])

  const getGroupConfig = useCallback((groupId: string): CreateGroupPayload | null => {
    const current = readGroupConfigs()
    return current[groupId] ?? null
  }, [])

  const clearGroupMeta = useCallback((groupIds: string[]) => {
    try {
      const raw = sessionStorage.getItem(GROUP_META_KEY)
      if (!raw) return
      const parsed = JSON.parse(raw) as Record<string, unknown>
      let changed = false
      groupIds.forEach((groupId) => {
        if (groupId in parsed) {
          delete parsed[groupId]
          changed = true
        }
      })
      if (!changed) return
      const nextKeys = Object.keys(parsed)
      if (nextKeys.length === 0) {
        sessionStorage.removeItem(GROUP_META_KEY)
      } else {
        sessionStorage.setItem(GROUP_META_KEY, JSON.stringify(parsed))
      }
    } catch (error) {
      console.error('Failed to clear group meta:', error)
    }
  }, [])

  const saveGroupMeta = useCallback((summaries: backendApi.GroupSummary[]) => {
    try {
      const metaMap: Record<string, unknown> = {}
      summaries.forEach((summary) => {
        metaMap[summary.GroupId] = {
          groupId: summary.GroupId,
          workbookPath: summary.WorkbookPath,
          outputFolder: summary.OutputFolder ?? undefined,
          status: summary.Status,
          progress: summary.Progress,
          sheetCount: summary.SheetCount,
          completedSheets: summary.CompletedSheets,
          errorCount: summary.ErrorCount ?? 0,
          updatedAt: new Date().toISOString(),
        }
      })
      sessionStorage.setItem(GROUP_META_KEY, JSON.stringify(metaMap))
    } catch (error) {
      console.error('Failed to save group meta:', error)
    }
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
        return {
          ...group,
          id: summary.GroupId,
          workbookPath: summary.WorkbookPath ?? group.workbookPath,
          outputFolder: summary.OutputFolder ?? group.outputFolder,
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
            outputPath: job.OutputPath ?? sheets[sheetId]?.outputPath,
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
    })

    saveGroupMeta(summaries)

    await Promise.allSettled(summaries.map((summary) => syncGroupStatus(summary.GroupId)))
  }, [syncGroupStatus, upsertGroupFromSummary, saveGroupMeta])

  const createGroup = useCallback(
    async (payload: CreateGroupPayload) => {
      const response = await backendApi.createGroup({
        TemplatePath: payload.templatePath,
        SpreadsheetPath: payload.spreadsheetPath,
        OutputPath: payload.outputPath,
        Path: payload.outputPath,
        TextConfigs: payload.textConfigs,
        ImageConfigs: payload.imageConfigs,
        SheetNames: payload.sheetNames,
      })

      const data = response as backendApi.SlideGroupCreateSuccess
      const groupId = data.GroupId
      saveGroupConfig(groupId, payload)

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

      await ensureGroupSubscription(groupId)
      await Promise.all(
        Object.values(data.JobIds ?? {}).map((jobId) => ensureSheetSubscription(jobId))
      )

      await syncGroupStatus(groupId)

      return createdGroup
    },
    [ensureGroupSubscription, ensureSheetSubscription, saveGroupConfig, syncGroupStatus, updateGroup]
  )

  const groupControl = useCallback(async (groupId: string, action: backendApi.ControlAction) => {
    await backendApi.groupControl({ GroupId: groupId, Action: action })
    if (action === 'Stop' || action === 'Cancel') {
      clearGroupMeta([groupId])
      removeGroupConfig([groupId])
    }
  }, [clearGroupMeta, removeGroupConfig])

  const removeGroup = useCallback(async (groupId: string) => {
    const response = await backendApi.removeGroup({ GroupId: groupId })
    const data = response as backendApi.SlideGroupRemoveSuccess
    if (!data.Removed) return false

    setGroupsById((prev) => {
      const next = { ...prev }
      delete next[groupId]
      return next
    })

    clearGroupMeta([groupId])
    removeGroupConfig([groupId])
    return true
  }, [clearGroupMeta, removeGroupConfig])

  const jobControl = useCallback(async (jobId: string, action: backendApi.ControlAction) => {
    await backendApi.jobControl({ JobId: jobId, Action: action })
  }, [])

  const loadSheetLogs = useCallback(
    async (jobId: string) => {
      try {
        const response = await backendApi.getJobLogs({ JobId: jobId })
        const data = response as backendApi.SlideJobLogsSuccess
        const logs = data.Logs.map((entry) => {
          const rowValue = entry.Data ? entry.Data.row : undefined
          const row = typeof rowValue === 'number' ? rowValue : Number(rowValue)
          const rowStatusValue = entry.Data ? entry.Data.rowStatus : undefined
          return {
            message: entry.Message,
            level: entry.Level,
            timestamp: entry.Timestamp,
            row: Number.isFinite(row) ? row : undefined,
            rowStatus: typeof rowStatusValue === 'string' ? rowStatusValue : undefined,
          } satisfies LogEntry
        })

        updateSheet(jobId, (sheet) => {
          if (sheet.logs.length > 0) return sheet
          return { ...sheet, logs }
        })
      } catch (error) {
        console.error('Failed to load job logs:', error)
      }
    },
    [updateSheet]
  )

  const removeSheet = useCallback(async (jobId: string) => {
    const response = await backendApi.removeJob({ JobId: jobId })
    const data = response as backendApi.SlideJobRemoveSuccess
    if (!data.Removed) return false

    setGroupsById((prev) => {
      const next: Record<string, GroupJob> = {}
      Object.values(prev).forEach((group) => {
        if (!group.sheets[jobId]) {
          next[group.id] = group
          return
        }

        const sheets = { ...group.sheets }
        delete sheets[jobId]
        if (Object.keys(sheets).length === 0) return

        next[group.id] = { ...group, sheets }
      })

      return next
    })

    return true
  }, [])

  const globalControl = useCallback(async (action: backendApi.ControlAction) => {
    await backendApi.globalControl({ Action: action })
  }, [])

  const clearCompleted = useCallback(() => {
    setGroupsById((prev) => {
      const clearedIds: string[] = []
      const next: Record<string, GroupJob> = {}
      Object.values(prev).forEach((group) => {
        const status = group.status.toLowerCase()
        if (!['completed', 'failed', 'cancelled'].includes(status)) {
          next[group.id] = group
        } else {
          clearedIds.push(group.id)
        }
      })
      if (clearedIds.length > 0) {
        clearGroupMeta(clearedIds)
        removeGroupConfig(clearedIds)
      }
      return next
    })
  }, [clearGroupMeta, removeGroupConfig])

  const exportGroupConfig = useCallback(async (groupId: string) => {
    const config = getGroupConfig(groupId)
    if (!config || !window.electronAPI) return false
    const exportPayload = {
      pptxPath: config.templatePath,
      dataPath: config.spreadsheetPath,
      savePath: config.outputPath,
      textReplacements: (config.textConfigs ?? []).map((item, index) => ({
        id: index + 1,
        placeholder: item.Pattern,
        columns: item.Columns,
      })),
      imageReplacements: (config.imageConfigs ?? []).map((item, index) => ({
        id: index + 1,
        shapeId: String(item.ShapeId),
        columns: item.Columns,
        roiType: item.RoiType ?? 'Attention',
        cropType: item.CropType ?? 'Fit',
      })),
    }

    const path = await window.electronAPI.saveFile([
      { name: 'JSON Files', extensions: ['json'] },
      { name: 'All Files', extensions: ['*'] },
    ])
    if (!path) return false

    await window.electronAPI.writeSettings(path, JSON.stringify(exportPayload, null, 2))
    return true
  }, [getGroupConfig])

  const hasGroupConfig = useCallback((groupId: string) => {
    return Boolean(getGroupConfig(groupId))
  }, [getGroupConfig])

  useEffect(() => {
    groupsRef.current = groupsById
  }, [groupsById])

  useEffect(() => {
    if (!window.electronAPI?.setProgressBar) return
    const activeGroups = Object.values(groupsById).filter((group) =>
      ['pending', 'running', 'paused'].includes(group.status.toLowerCase())
    )

    if (activeGroups.length === 0) {
      window.electronAPI.setProgressBar(-1)
      return
    }

    const avgProgress =
      activeGroups.reduce((sum, group) => sum + (group.progress ?? 0), 0) / activeGroups.length
    const normalized = Math.max(0, Math.min(1, avgProgress / 100))
    window.electronAPI.setProgressBar(normalized)
  }, [groupsById])

  useEffect(() => {
    const unsubscribe = backendApi.onSlideNotification((payload) => {
      if (!payload || typeof payload !== 'object') return
      const data = payload as Record<string, unknown>
      const getValue = (key: string) =>
        data[key] ?? data[key.charAt(0).toLowerCase() + key.slice(1)]
      const getDataValue = (
        container: Record<string, unknown> | undefined,
        key: string
      ) => {
        if (!container) return undefined
        if (key in container) return container[key]
        const lowered = key.toLowerCase()
        for (const [entryKey, value] of Object.entries(container)) {
          if (entryKey.toLowerCase() === lowered) return value
        }
        return undefined
      }

      const groupId = getValue('GroupId') as string | undefined
      const jobId = getValue('JobId') as string | undefined
      const status = getValue('Status') as string | undefined
      const message = getValue('Message') as string | undefined
      const error = getValue('Error') as string | undefined
      const level = getValue('Level') as string | undefined
      const timestamp = getValue('Timestamp') as string | undefined
      const payloadData = getValue('Data') as Record<string, unknown> | undefined

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
        const logEntry: LogEntry = {
          message: error,
          level: 'Error',
          timestamp,
        }
        updateSheet(jobId, (sheet) => ({
          ...sheet,
          logs: [...sheet.logs, logEntry].slice(-MAX_LOGS),
        }))
        return
      }

      if (jobId && message) {
        const rowValue = getDataValue(payloadData, 'row')
        const row = typeof rowValue === 'number' ? rowValue : Number(rowValue)
        const rowStatus = getDataValue(payloadData, 'rowStatus')
        const logEntry: LogEntry = {
          message,
          level,
          timestamp,
          row: Number.isFinite(row) ? row : undefined,
          rowStatus: typeof rowStatus === 'string' ? rowStatus : undefined,
        }
        const targetGroupId = sheetToGroup.current[jobId]

        if (targetGroupId) {
          updateSheet(jobId, (sheet) => ({
            ...sheet,
            logs: [...sheet.logs, logEntry].slice(-MAX_LOGS),
          }))
        } else if (groupsRef.current[jobId]) {
          updateGroup(jobId, (group) => ({
            ...group,
            logs: [...group.logs, logEntry].slice(-MAX_LOGS),
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
      removeGroup,
      removeSheet,
      loadSheetLogs,
      globalControl,
      exportGroupConfig,
      hasGroupConfig,
    }),
    [
      clearCompleted,
      createGroup,
      exportGroupConfig,
      groupControl,
      groups,
      hasGroupConfig,
      jobControl,
      removeGroup,
      removeSheet,
      loadSheetLogs,
      globalControl,
      refreshGroups,
    ]
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
