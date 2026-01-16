import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import * as backendApi from '@/shared/services/backendApi';
import { loggers } from '@/shared/services/logging';
import type {
	GroupJob,
	SheetJob,
	LogEntry,
	JobStatus,
	CreateGroupPayload,
	JobContextValue,
} from '../JobContextType';
import {
	createEmptyGroup,
	createEmptySheet,
	trimLogs,
	applyGroupTimestamps,
	handleSlideNotification,
	type SlideNotificationContext,
	saveGroupConfigToStorage,
	removeGroupConfigFromStorage,
	getGroupConfigFromStorage,
	clearGroupMetaFromStorage,
	saveGroupMetaToStorage,
} from '../utils';

export const useJobProvider = (): JobContextValue => {
	const [groupsById, setGroupsById] = useState<Record<string, GroupJob>>({});
	const groupsRef = useRef<Record<string, GroupJob>>({});
	const subscribedGroups = useRef(new Set<string>());
	const subscribedSheets = useRef(new Set<string>());
	const sheetToGroup = useRef<Record<string, string>>({});
	const removedGroupIds = useRef(new Set<string>());

	const updateGroup = useCallback((groupId: string, updater: (group: GroupJob) => GroupJob) => {
		setGroupsById((prev) => {
			const current = prev[groupId] ?? createEmptyGroup(groupId);
			const updated = applyGroupTimestamps(current, updater(current));
			return { ...prev, [groupId]: updated };
		});
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

	const saveGroupConfig = useCallback((groupId: string, payload: CreateGroupPayload) => {
		saveGroupConfigToStorage(groupId, payload);
	}, []);

	const removeGroupConfig = useCallback((groupIds: string[]) => {
		removeGroupConfigFromStorage(groupIds);
	}, []);

	const getGroupConfig = useCallback((groupId: string): CreateGroupPayload | null => {
		return getGroupConfigFromStorage(groupId);
	}, []);

	const clearGroupMeta = useCallback((groupIds: string[]) => {
		clearGroupMetaFromStorage(groupIds);
	}, []);

	const saveGroupMeta = useCallback((summaries: backendApi.GroupSummary[]) => {
		saveGroupMetaToStorage(summaries);
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
			updateGroup(summary.groupId, (group) => {
				return {
					...group,
					id: summary.groupId,
					workbookPath: summary.workbookPath ?? group.workbookPath,
					outputFolder: summary.outputFolder ?? group.outputFolder,
					status: summary.status as JobStatus,
					progress: summary.progress ?? group.progress,
					errorCount: summary.errorCount ?? group.errorCount,
				};
			});
		},
		[updateGroup],
	);

	const syncGroupStatus = useCallback(
		async (groupId: string) => {
			const response = await backendApi.groupStatus({ groupId: groupId });
			const status = response as backendApi.SlideGroupStatusSuccess;
			const jobs = status.jobs ?? {};

			updateGroup(groupId, (group) => {
				const sheets: Record<string, SheetJob> = { ...group.sheets };
				Object.values(jobs).forEach((job) => {
					const sheetId = job.jobId;
					sheetToGroup.current[sheetId] = groupId;
					sheets[sheetId] = {
						id: sheetId,
						sheetName: job.sheetName,
						status: job.status as JobStatus,
						currentRow: job.currentRow ?? 0,
						totalRows: job.totalRows ?? 0,
						progress: job.progress ?? 0,
						errorCount: job.errorCount ?? 0,
						outputPath: job.outputPath ?? sheets[sheetId]?.outputPath,
						errorMessage: job.errorMessage ?? undefined,
						logs: sheets[sheetId]?.logs ?? [],
						hangfireJobId: job.hangfireJobId ?? sheets[sheetId]?.hangfireJobId,
					};
				});

				return {
					...group,
					status: status.status as JobStatus,
					progress: status.progress ?? group.progress,
					errorCount: status.errorCount ?? group.errorCount,
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
		const summaries = data.groups ?? [];

		summaries.forEach((summary) => {
			if (!removedGroupIds.current.has(summary.groupId)) {
				upsertGroupFromSummary(summary);
			}
		});

		saveGroupMeta(summaries);

		await Promise.allSettled(
			summaries.map((summary) => {
				if (!removedGroupIds.current.has(summary.groupId)) {
					return syncGroupStatus(summary.groupId);
				}
				return Promise.resolve();
			}),
		);
	}, [syncGroupStatus, upsertGroupFromSummary, saveGroupMeta]);

	const createGroup = useCallback(
		async (payload: CreateGroupPayload) => {
			const response = await backendApi.createGroup({
				templatePath: payload.templatePath,
				spreadsheetPath: payload.spreadsheetPath,
				outputPath: payload.outputPath,
				textConfigs: payload.textConfigs,
				imageConfigs: payload.imageConfigs,
				sheetNames: payload.sheetNames,
			});

			const data = response as backendApi.SlideGroupCreateSuccess;
			const groupId = data.groupId;
			removedGroupIds.current.delete(groupId);
			saveGroupConfig(groupId, payload);

			let createdGroup: GroupJob = createEmptyGroup(groupId);
			updateGroup(groupId, (group) => {
				const sheets: Record<string, SheetJob> = { ...group.sheets };
				Object.entries(data.jobIds ?? {}).forEach(([sheetName, jobId]) => {
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
					outputFolder: data.outputFolder,
					status: 'Running',
					progress: 0,
					errorCount: 0,
					sheets,
				};
				return createdGroup;
			});

			await ensureGroupSubscription(groupId);
			await Promise.all(
				Object.values(data.jobIds ?? {}).map((jobId) => ensureSheetSubscription(jobId)),
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
			await backendApi.groupControl({ groupId: groupId, action: action });
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
				await backendApi.removeGroup({ groupId: groupId });
			} catch (error) {
				loggers.jobs.error(`Failed to remove group ${groupId}:`, error);
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
			await backendApi.jobControl({ jobId: jobId, action: action });
			await refreshGroups();
		},
		[refreshGroups],
	);

	const loadSheetLogs = useCallback(
		async (jobId: string) => {
			try {
				const response = await backendApi.getJobLogs({ jobId: jobId });
				const data = response as backendApi.SlideJobLogsSuccess;
				const logs = data.logs.map((entry) => {
					const rowValue = entry.data ? entry.data.row : undefined;
					const row = typeof rowValue === 'number' ? rowValue : Number(rowValue);
					const rowStatusValue = entry.data ? entry.data.rowStatus : undefined;
					return {
						message: entry.message,
						level: entry.level,
						timestamp: entry.timestamp,
						row: Number.isFinite(row) ? row : undefined,
						rowStatus: typeof rowStatusValue === 'string' ? rowStatusValue : undefined,
					} satisfies LogEntry;
				});

				updateSheet(jobId, (sheet) => {
					if (sheet.logs.length > 0) return sheet;
					return { ...sheet, logs: trimLogs(logs) };
				});
			} catch (error) {
				loggers.jobs.error('Failed to load job logs:', error);
			}
		},
		[updateSheet],
	);

	const removeSheet = useCallback(async (jobId: string) => {
		const response = await backendApi.removeJob({ jobId: jobId });
		const data = response as backendApi.SlideJobRemoveSuccess;
		if (!data.removed) return false;

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
		await backendApi.globalControl({ action: action });
	}, []);

	const clearCompleted = useCallback(async () => {
		const current = groupsRef.current;
		const completedIds = Object.values(current)
			.filter((group) => ['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase()))
			.map((group) => group.id);

		if (completedIds.length === 0) return;

		const removedIds: string[] = [];
		for (const groupId of completedIds) {
			try {
				await backendApi.removeGroup({ groupId: groupId });
				removedIds.push(groupId);
			} catch (error) {
				loggers.jobs.error(`Failed to remove group ${groupId}:`, error);
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
					placeholder: item.pattern,
					columns: item.columns,
				})),
				imageReplacements: (config.imageConfigs ?? []).map((item, index) => ({
					id: index + 1,
					shapeId: String(item.shapeId),
					columns: item.columns,
					roiType: item.roiType ?? 'RuleOfThirds',
					cropType: item.cropType ?? 'Fit',
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
			activeGroups.reduce((sum, group) => sum + (group.progress ?? 0), 0) / activeGroups.length;
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
			loggers.jobs.error('Failed to refresh jobs after reconnect:', error);
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
			loggers.jobs.error('Failed to refresh jobs:', error);
		});
	}, [refreshGroups]);

	const groups = useMemo(() => Object.values(groupsById), [groupsById]);

	return useMemo(
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
};
