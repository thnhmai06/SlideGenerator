import React, { useCallback, useEffect, useMemo, useRef, useState, ReactNode } from 'react';
import * as backendApi from '@/shared/services/backendApi';
import {
	JobContext,
	type CreateGroupPayload,
	type GroupJob,
	type JobStatus,
	type LogEntry,
	type SheetJob,
} from './JobContextType';

const createEmptyGroup = (groupId: string): GroupJob => ({
	id: groupId,
	workbookPath: '',
	status: 'Pending',
	progress: 0,
	errorCount: 0,
	sheets: {},
	logs: [],
});

const createEmptySheet = (sheetId: string): SheetJob => ({
	id: sheetId,
	sheetName: sheetId,
	status: 'Pending',
	currentRow: 0,
	totalRows: 0,
	progress: 0,
	errorCount: 0,
	logs: [],
	hangfireJobId: undefined,
});

const MAX_LOG_ENTRIES = 2000;

const trimLogs = (logs: LogEntry[]) => {
	if (logs.length <= MAX_LOG_ENTRIES) return logs;
	return logs.slice(logs.length - MAX_LOG_ENTRIES);
};

const appendLog = (logs: LogEntry[], entry: LogEntry) => {
	if (logs.length < MAX_LOG_ENTRIES) return [...logs, entry];
	return [...logs.slice(logs.length - MAX_LOG_ENTRIES + 1), entry];
};

const getPayloadValue = (data: Record<string, unknown>, key: string) => {
	return data[key] ?? data[key.charAt(0).toLowerCase() + key.slice(1)];
};

const getPayloadDataValue = (container: Record<string, unknown> | undefined, key: string) => {
	if (!container) return undefined;
	if (key in container) return container[key];
	const lowered = key.toLowerCase();
	for (const [entryKey, value] of Object.entries(container)) {
		if (entryKey.toLowerCase() === lowered) return value;
	}
	return undefined;
};

const handleGroupProgressNotification = (
	groupId: string,
	data: Record<string, unknown>,
	updateGroup: (groupId: string, updater: (group: GroupJob) => GroupJob) => void,
) => {
	if (typeof getPayloadValue(data, 'Progress') !== 'number') return false;
	updateGroup(groupId, (group) => ({
		...group,
		progress: getPayloadValue(data, 'Progress') as number,
		errorCount: (getPayloadValue(data, 'ErrorCount') as number) ?? group.errorCount,
	}));
	return true;
};

const handleGroupStatusNotification = (
	groupId: string,
	status: string,
	updateGroup: (groupId: string, updater: (group: GroupJob) => GroupJob) => void,
) => {
	updateGroup(groupId, (group) => ({ ...group, status: status as JobStatus }));
	return true;
};

const handleSheetProgressNotification = (
	jobId: string,
	data: Record<string, unknown>,
	updateSheet: (sheetId: string, updater: (sheet: SheetJob) => SheetJob) => void,
) => {
	if (typeof getPayloadValue(data, 'CurrentRow') !== 'number') return false;
	updateSheet(jobId, (sheet) => ({
		...sheet,
		currentRow: getPayloadValue(data, 'CurrentRow') as number,
		totalRows: (getPayloadValue(data, 'TotalRows') as number) ?? sheet.totalRows,
		progress: (getPayloadValue(data, 'Progress') as number) ?? sheet.progress,
		errorCount: (getPayloadValue(data, 'ErrorCount') as number) ?? sheet.errorCount,
	}));
	return true;
};

const handleSheetStatusNotification = (
	jobId: string,
	status: string,
	message: string | undefined,
	updateSheet: (sheetId: string, updater: (sheet: SheetJob) => SheetJob) => void,
) => {
	updateSheet(jobId, (sheet) => ({
		...sheet,
		status: status as JobStatus,
		errorMessage: message ?? sheet.errorMessage,
	}));
	return true;
};

const handleSheetErrorNotification = (
	jobId: string,
	error: string,
	timestamp: string | undefined,
	updateSheet: (sheetId: string, updater: (sheet: SheetJob) => SheetJob) => void,
) => {
	const logEntry: LogEntry = {
		message: error,
		level: 'Error',
		timestamp,
	};
	updateSheet(jobId, (sheet) => ({
		...sheet,
		logs: appendLog(sheet.logs, logEntry),
	}));
	return true;
};

const createLogEntryFromPayload = (
	message: string,
	level: string | undefined,
	timestamp: string | undefined,
	payloadData: Record<string, unknown> | undefined,
): LogEntry => {
	const rowValue = getPayloadDataValue(payloadData, 'row');
	const row = typeof rowValue === 'number' ? rowValue : Number(rowValue);
	const rowStatusValue = getPayloadDataValue(payloadData, 'rowStatus');
	return {
		message,
		level: level ?? 'Info',
		timestamp,
		row: Number.isFinite(row) ? row : undefined,
		rowStatus: typeof rowStatusValue === 'string' ? rowStatusValue : undefined,
	};
};

type RefLike<T> = { current: T };

type SlideNotificationContext = {
	updateGroup: (groupId: string, updater: (group: GroupJob) => GroupJob) => void;
	updateSheet: (sheetId: string, updater: (sheet: SheetJob) => SheetJob) => void;
	removedGroupIds: RefLike<Set<string>>;
	sheetToGroup: RefLike<Record<string, string>>;
	groupsRef: RefLike<Record<string, GroupJob>>;
};

type SlideNotificationPayload = {
	data: Record<string, unknown>;
	groupId?: string;
	jobId?: string;
	status?: string;
	message?: string;
	error?: string;
	level?: string;
	timestamp?: string;
	payloadData?: Record<string, unknown>;
};

const parseSlideNotificationPayload = (payload: unknown): SlideNotificationPayload | null => {
	if (!payload || typeof payload !== 'object') return null;
	const data = payload as Record<string, unknown>;
	return {
		data,
		groupId: getPayloadValue(data, 'GroupId') as string | undefined,
		jobId: getPayloadValue(data, 'JobId') as string | undefined,
		status: getPayloadValue(data, 'Status') as string | undefined,
		message: getPayloadValue(data, 'Message') as string | undefined,
		error: getPayloadValue(data, 'Error') as string | undefined,
		level: getPayloadValue(data, 'Level') as string | undefined,
		timestamp: getPayloadValue(data, 'Timestamp') as string | undefined,
		payloadData: getPayloadValue(data, 'Data') as Record<string, unknown> | undefined,
	};
};

const shouldIgnoreNotification = (
	payload: SlideNotificationPayload,
	context: SlideNotificationContext,
) => {
	if (payload.groupId && context.removedGroupIds.current.has(payload.groupId)) return true;
	if (!payload.jobId) return false;
	const parentGroupId = context.sheetToGroup.current[payload.jobId];
	return Boolean(parentGroupId && context.removedGroupIds.current.has(parentGroupId));
};

const handleGroupNotifications = (
	payload: SlideNotificationPayload,
	context: SlideNotificationContext,
) => {
	if (!payload.groupId) return false;
	if (handleGroupProgressNotification(payload.groupId, payload.data, context.updateGroup)) {
		return true;
	}
	if (!payload.status) return false;
	handleGroupStatusNotification(payload.groupId, payload.status, context.updateGroup);
	return true;
};

const handleSheetNotifications = (
	payload: SlideNotificationPayload,
	context: SlideNotificationContext,
) => {
	if (!payload.jobId) return false;
	if (handleSheetProgressNotification(payload.jobId, payload.data, context.updateSheet))
		return true;
	if (payload.status) {
		handleSheetStatusNotification(
			payload.jobId,
			payload.status,
			payload.message,
			context.updateSheet,
		);
		return true;
	}
	if (payload.error) {
		handleSheetErrorNotification(
			payload.jobId,
			payload.error,
			payload.timestamp,
			context.updateSheet,
		);
		return true;
	}
	return false;
};

const handleLogNotification = (
	payload: SlideNotificationPayload,
	context: SlideNotificationContext,
) => {
	if (!payload.jobId || !payload.message) return;
	const logEntry = createLogEntryFromPayload(
		payload.message,
		payload.level,
		payload.timestamp,
		payload.payloadData,
	);
	const targetGroupId = context.sheetToGroup.current[payload.jobId];
	if (targetGroupId) {
		context.updateSheet(payload.jobId, (sheet) => ({
			...sheet,
			logs: appendLog(sheet.logs, logEntry),
		}));
		return;
	}
	if (context.groupsRef.current[payload.jobId]) {
		context.updateGroup(payload.jobId, (group) => ({
			...group,
			logs: appendLog(group.logs, logEntry),
		}));
	}
};

const handleSlideNotification = (payload: unknown, context: SlideNotificationContext) => {
	const parsed = parseSlideNotificationPayload(payload);
	if (!parsed) return;
	if (shouldIgnoreNotification(parsed, context)) return;
	if (handleGroupNotifications(parsed, context)) return;
	if (handleSheetNotifications(parsed, context)) return;
	handleLogNotification(parsed, context);
};

export const JobProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
	const [groupsById, setGroupsById] = useState<Record<string, GroupJob>>({});
	const groupsRef = useRef<Record<string, GroupJob>>({});
	const subscribedGroups = useRef(new Set<string>());
	const subscribedSheets = useRef(new Set<string>());
	const sheetToGroup = useRef<Record<string, string>>({});
	const removedGroupIds = useRef(new Set<string>());

	const applyGroupTimestamps = (prev: GroupJob, next: GroupJob): GroupJob => {
		const createdAt = next.createdAt ?? prev.createdAt ?? new Date().toISOString();
		const isCompleted = ['completed', 'failed', 'cancelled'].includes(
			next.status.toLowerCase(),
		);
		const completedAt =
			next.completedAt ??
			prev.completedAt ??
			(isCompleted ? new Date().toISOString() : undefined);
		return { ...next, createdAt, completedAt };
	};

	const updateGroup = useCallback((groupId: string, updater: (group: GroupJob) => GroupJob) => {
		setGroupsById((prev) => {
			const current = prev[groupId] ?? createEmptyGroup(groupId);
			const updated = applyGroupTimestamps(current, updater(current));
			return { ...prev, [groupId]: updated };
		});
	}, []);

	const GROUP_META_KEY = 'slidegen.group.meta';
	const GROUP_CONFIG_KEY = 'slidegen.group.config';

	const readGroupConfigs = (): Record<string, CreateGroupPayload> => {
		try {
			const raw = sessionStorage.getItem(GROUP_CONFIG_KEY);
			if (!raw) return {};
			return JSON.parse(raw) as Record<string, CreateGroupPayload>;
		} catch (error) {
			console.error('Failed to read group configs:', error);
			return {};
		}
	};

	const saveGroupConfig = useCallback((groupId: string, payload: CreateGroupPayload) => {
		try {
			const current = readGroupConfigs();
			current[groupId] = payload;
			sessionStorage.setItem(GROUP_CONFIG_KEY, JSON.stringify(current));
		} catch (error) {
			console.error('Failed to save group config:', error);
		}
	}, []);

	const removeGroupConfig = useCallback((groupIds: string[]) => {
		try {
			const current = readGroupConfigs();
			let changed = false;
			groupIds.forEach((groupId) => {
				if (groupId in current) {
					delete current[groupId];
					changed = true;
				}
			});
			if (!changed) return;
			if (Object.keys(current).length === 0) {
				sessionStorage.removeItem(GROUP_CONFIG_KEY);
			} else {
				sessionStorage.setItem(GROUP_CONFIG_KEY, JSON.stringify(current));
			}
		} catch (error) {
			console.error('Failed to remove group configs:', error);
		}
	}, []);

	const getGroupConfig = useCallback((groupId: string): CreateGroupPayload | null => {
		const current = readGroupConfigs();
		return current[groupId] ?? null;
	}, []);

	const resolveGroupConfig = useCallback(
		async (groupId: string): Promise<CreateGroupPayload | null> => {
			const stored = getGroupConfig(groupId);
			if (stored) return stored;

			const payload = await backendApi.getGroupPayload(groupId);
			if (!payload) return null;

			const resolved: CreateGroupPayload = {
				templatePath: payload.templatePath,
				spreadsheetPath: payload.spreadsheetPath,
				outputPath: payload.outputPath,
				textConfigs: payload.textConfigs ?? [],
				imageConfigs: payload.imageConfigs ?? [],
				sheetNames: payload.sheetNames,
			};
			saveGroupConfig(groupId, resolved);
			return resolved;
		},
		[getGroupConfig, saveGroupConfig],
	);

	const clearGroupMeta = useCallback((groupIds: string[]) => {
		try {
			const raw = sessionStorage.getItem(GROUP_META_KEY);
			if (!raw) return;
			const parsed = JSON.parse(raw) as Record<string, unknown>;
			let changed = false;
			groupIds.forEach((groupId) => {
				if (groupId in parsed) {
					delete parsed[groupId];
					changed = true;
				}
			});
			if (!changed) return;
			const nextKeys = Object.keys(parsed);
			if (nextKeys.length === 0) {
				sessionStorage.removeItem(GROUP_META_KEY);
			} else {
				sessionStorage.setItem(GROUP_META_KEY, JSON.stringify(parsed));
			}
		} catch (error) {
			console.error('Failed to clear group meta:', error);
		}
	}, []);

	const saveGroupMeta = useCallback((summaries: backendApi.GroupSummary[]) => {
		try {
			const metaMap: Record<string, unknown> = {};
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
				};
			});
			sessionStorage.setItem(GROUP_META_KEY, JSON.stringify(metaMap));
		} catch (error) {
			console.error('Failed to save group meta:', error);
		}
	}, []);

	const updateSheet = useCallback((sheetId: string, updater: (sheet: SheetJob) => SheetJob) => {
		const groupId = sheetToGroup.current[sheetId];
		if (!groupId) return;

		setGroupsById((prev) => {
			const group = prev[groupId];
			if (!group) return prev;

			const currentSheet = group.sheets[sheetId] ?? createEmptySheet(sheetId);
			const updatedSheet = updater(currentSheet);
			const updatedGroup: GroupJob = {
				...group,
				sheets: { ...group.sheets, [sheetId]: updatedSheet },
			};
			return { ...prev, [groupId]: updatedGroup };
		});
	}, []);

	const ensureGroupSubscription = useCallback(async (groupId: string) => {
		if (subscribedGroups.current.has(groupId)) return;
		await backendApi.subscribeGroup(groupId);
		subscribedGroups.current.add(groupId);
	}, []);

	const ensureSheetSubscription = useCallback(async (sheetId: string) => {
		if (subscribedSheets.current.has(sheetId)) return;
		await backendApi.subscribeSheet(sheetId);
		subscribedSheets.current.add(sheetId);
	}, []);

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
				};
			});
		},
		[updateGroup],
	);

	const syncGroupStatus = useCallback(
		async (groupId: string) => {
			const response = await backendApi.groupStatus({ GroupId: groupId });
			const status = response as backendApi.SlideGroupStatusSuccess;
			const jobs = status.Jobs ?? {};

			updateGroup(groupId, (group) => {
				const sheets: Record<string, SheetJob> = { ...group.sheets };
				Object.values(jobs).forEach((job) => {
					const sheetId = job.JobId;
					sheetToGroup.current[sheetId] = groupId;
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
						hangfireJobId: job.HangfireJobId ?? sheets[sheetId]?.hangfireJobId,
					};
				});

				return {
					...group,
					status: status.Status as JobStatus,
					progress: status.Progress ?? group.progress,
					errorCount: status.ErrorCount ?? group.errorCount,
					sheets,
				};
			});

			await ensureGroupSubscription(groupId);
			await Promise.all(Object.keys(jobs).map((jobId) => ensureSheetSubscription(jobId)));
		},
		[ensureGroupSubscription, ensureSheetSubscription, updateGroup],
	);

	const refreshGroups = useCallback(async () => {
		const response = await backendApi.getAllGroups();
		const data = response as backendApi.SlideGlobalGetGroupsSuccess;
		const summaries = data.Groups ?? [];

		summaries.forEach((summary) => {
			if (!removedGroupIds.current.has(summary.GroupId)) {
				upsertGroupFromSummary(summary);
			}
		});

		saveGroupMeta(summaries);

		await Promise.allSettled(
			summaries.map((summary) => {
				if (!removedGroupIds.current.has(summary.GroupId)) {
					return syncGroupStatus(summary.GroupId);
				}
				return Promise.resolve();
			}),
		);
	}, [syncGroupStatus, upsertGroupFromSummary, saveGroupMeta]);

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
			});

			const data = response as backendApi.SlideGroupCreateSuccess;
			const groupId = data.GroupId;
			removedGroupIds.current.delete(groupId);
			saveGroupConfig(groupId, payload);

			let createdGroup: GroupJob = createEmptyGroup(groupId);
			updateGroup(groupId, (group) => {
				const sheets: Record<string, SheetJob> = { ...group.sheets };
				Object.entries(data.JobIds ?? {}).forEach(([sheetName, jobId]) => {
					sheetToGroup.current[jobId] = groupId;
					sheets[jobId] = {
						...(sheets[jobId] ?? createEmptySheet(jobId)),
						id: jobId,
						sheetName,
					};
				});

				createdGroup = {
					...group,
					id: groupId,
					workbookPath: payload.spreadsheetPath,
					outputFolder: data.OutputFolder,
					status: 'Running',
					progress: 0,
					errorCount: 0,
					sheets,
				};
				return createdGroup;
			});

			await ensureGroupSubscription(groupId);
			await Promise.all(
				Object.values(data.JobIds ?? {}).map((jobId) => ensureSheetSubscription(jobId)),
			);

			await syncGroupStatus(groupId);

			return createdGroup;
		},
		[
			ensureGroupSubscription,
			ensureSheetSubscription,
			saveGroupConfig,
			syncGroupStatus,
			updateGroup,
		],
	);

	const groupControl = useCallback(
		async (groupId: string, action: backendApi.ControlAction) => {
			await backendApi.groupControl({ GroupId: groupId, Action: action });
			if (action === 'Stop' || action === 'Cancel') {
				clearGroupMeta([groupId]);
			}
			await refreshGroups();
		},
		[clearGroupMeta, refreshGroups],
	);

	const removeGroup = useCallback(
		async (groupId: string) => {
			try {
				await backendApi.removeGroup({ GroupId: groupId });
			} catch (error) {
				console.error(`Failed to remove group ${groupId}:`, error);
			}

			setGroupsById((prev) => {
				const next = { ...prev };
				delete next[groupId];
				return next;
			});

			removedGroupIds.current.add(groupId);
			clearGroupMeta([groupId]);
			removeGroupConfig([groupId]);
			return true;
		},
		[clearGroupMeta, removeGroupConfig],
	);

	const jobControl = useCallback(
		async (jobId: string, action: backendApi.ControlAction) => {
			await backendApi.jobControl({ JobId: jobId, Action: action });
			await refreshGroups();
		},
		[refreshGroups],
	);

	const loadSheetLogs = useCallback(
		async (jobId: string) => {
			try {
				const response = await backendApi.getJobLogs({ JobId: jobId });
				const data = response as backendApi.SlideJobLogsSuccess;
				const logs = data.Logs.map((entry) => {
					const rowValue = entry.Data ? entry.Data.row : undefined;
					const row = typeof rowValue === 'number' ? rowValue : Number(rowValue);
					const rowStatusValue = entry.Data ? entry.Data.rowStatus : undefined;
					return {
						message: entry.Message,
						level: entry.Level,
						timestamp: entry.Timestamp,
						row: Number.isFinite(row) ? row : undefined,
						rowStatus: typeof rowStatusValue === 'string' ? rowStatusValue : undefined,
					} satisfies LogEntry;
				});

				updateSheet(jobId, (sheet) => {
					if (sheet.logs.length > 0) return sheet;
					return { ...sheet, logs: trimLogs(logs) };
				});
			} catch (error) {
				console.error('Failed to load job logs:', error);
			}
		},
		[updateSheet],
	);

	const removeSheet = useCallback(async (jobId: string) => {
		const response = await backendApi.removeJob({ JobId: jobId });
		const data = response as backendApi.SlideJobRemoveSuccess;
		if (!data.Removed) return false;

		setGroupsById((prev) => {
			const next: Record<string, GroupJob> = {};
			Object.values(prev).forEach((group) => {
				if (!group.sheets[jobId]) {
					next[group.id] = group;
					return;
				}

				const sheets = { ...group.sheets };
				delete sheets[jobId];
				if (Object.keys(sheets).length === 0) return;

				next[group.id] = { ...group, sheets };
			});

			return next;
		});

		return true;
	}, []);

	const globalControl = useCallback(async (action: backendApi.ControlAction) => {
		await backendApi.globalControl({ Action: action });
	}, []);

	const clearCompleted = useCallback(async () => {
		const current = groupsRef.current;
		const completedIds = Object.values(current)
			.filter((group) =>
				['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase()),
			)
			.map((group) => group.id);

		if (completedIds.length === 0) return;

		const removedIds: string[] = [];
		for (const groupId of completedIds) {
			try {
				await backendApi.removeGroup({ GroupId: groupId });
				// Always add to removedIds even if backend says "false" (maybe already gone)
				removedIds.push(groupId);
			} catch (error) {
				console.error(`Failed to remove group ${groupId}:`, error);
				// Optionally add to removedIds here too if we want to force clear even on error
				removedIds.push(groupId);
			}
		}

		if (removedIds.length === 0) return;

		setGroupsById((prev) => {
			const next = { ...prev };
			removedIds.forEach((groupId) => {
				delete next[groupId];
				removedGroupIds.current.add(groupId);
			});
			return next;
		});

		clearGroupMeta(removedIds);
		removeGroupConfig(removedIds);
	}, [clearGroupMeta, removeGroupConfig]);

	const exportGroupConfig = useCallback(
		async (groupId: string) => {
			if (!window.electronAPI) return false;
			const config = await resolveGroupConfig(groupId);
			if (!config) return false;
			const exportPayload = {
				pptxPath: config.templatePath,
				dataPath: config.spreadsheetPath,
				savePath: config.outputPath,
				selectedSheets: config.sheetNames,
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
			};

			const path = await window.electronAPI.saveFile([
				{ name: 'JSON Files', extensions: ['json'] },
				{ name: 'All Files', extensions: ['*'] },
			]);
			if (!path) return false;

			await window.electronAPI.writeSettings(path, JSON.stringify(exportPayload, null, 2));
			return true;
		},
		[resolveGroupConfig],
	);

	const hasGroupConfig = useCallback(
		(groupId: string) => {
			if (getGroupConfig(groupId)) return true;
			return Boolean(groupsRef.current[groupId]);
		},
		[getGroupConfig],
	);

	useEffect(() => {
		groupsRef.current = groupsById;
	}, [groupsById]);

	useEffect(() => {
		if (!window.electronAPI?.setProgressBar) return;
		const activeGroups = Object.values(groupsById).filter((group) =>
			['pending', 'running', 'paused'].includes(group.status.toLowerCase()),
		);

		if (activeGroups.length === 0) {
			window.electronAPI.setProgressBar(-1);
			return;
		}

		const avgProgress =
			activeGroups.reduce((sum, group) => sum + (group.progress ?? 0), 0) /
			activeGroups.length;
		const normalized = Math.max(0, Math.min(1, avgProgress / 100));
		window.electronAPI.setProgressBar(normalized);
	}, [groupsById]);

	useEffect(() => {
		const context: SlideNotificationContext = {
			updateGroup,
			updateSheet,
			removedGroupIds,
			sheetToGroup,
			groupsRef,
		};
		const unsubscribe = backendApi.onSlideNotification((payload) =>
			handleSlideNotification(payload, context),
		);

		return unsubscribe;
	}, [updateGroup, updateSheet]);

	const handleSlideConnected = useCallback(() => {
		subscribedGroups.current.clear();
		subscribedSheets.current.clear();
		refreshGroups().catch((error) => {
			console.error('Failed to refresh jobs after reconnect:', error);
		});
	}, [refreshGroups]);

	useEffect(() => {
		const unsubscribeConnected = backendApi.onSlideConnected(handleSlideConnected);
		const unsubscribeReconnected = backendApi.onSlideReconnected(handleSlideConnected);

		return () => {
			unsubscribeConnected();
			unsubscribeReconnected();
		};
	}, [handleSlideConnected]);

	useEffect(() => {
		refreshGroups().catch((error) => {
			console.error('Failed to refresh jobs:', error);
		});
	}, [refreshGroups]);

	const groups = useMemo(() => Object.values(groupsById), [groupsById]);

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
		],
	);

	return <JobContext.Provider value={value}>{children}</JobContext.Provider>;
};
