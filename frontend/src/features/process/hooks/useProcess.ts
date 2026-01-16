import { useCallback, useMemo, useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import { getBackendBaseUrl } from '@/shared/services/signalrClient';
import type { LogEntry } from '../types';
import { formatLogEntry, formatTime } from '../utils';

/**
 * Hook for managing slide generation process monitoring and control.
 *
 * @remarks
 * Provides state and handlers for:
 * - Viewing active and completed job groups
 * - Expanding/collapsing job details and logs
 * - Pausing, resuming, and stopping jobs
 * - Exporting job configurations
 *
 * @returns Process management state and action handlers
 *
 * @example
 * ```tsx
 * const {
 *   activeGroups,
 *   expandedGroups,
 *   toggleGroup,
 *   handlePauseResumeAll
 * } = useProcess();
 * ```
 */
export const useProcess = () => {
	const { t, language } = useApp();
	const {
		groups,
		groupControl,
		jobControl,
		globalControl,
		loadSheetLogs,
		exportGroupConfig,
		hasGroupConfig,
	} = useJobs();

	const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({});
	const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>({});
	const [collapsedRowGroups, setCollapsedRowGroups] = useState<Record<string, boolean>>({});

	const toggleGroup = useCallback((groupId: string) => {
		setExpandedGroups((prev) => ({ ...prev, [groupId]: !prev[groupId] }));
	}, []);

	const toggleLog = useCallback(
		(sheetId: string) => {
			setExpandedLogs((prev) => {
				const next = !prev[sheetId];
				if (next) {
					void loadSheetLogs(sheetId);
				}
				return { ...prev, [sheetId]: next };
			});
		},
		[loadSheetLogs],
	);

	const toggleRowGroup = useCallback((key: string) => {
		setCollapsedRowGroups((prev) => {
			const current = prev[key] ?? true;
			return { ...prev, [key]: !current };
		});
	}, []);

	const activeGroups = useMemo(
		() =>
			groups.filter(
				(group) => !['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase()),
			),
		[groups],
	);

	const hasProcessing = useMemo(
		() => activeGroups.some((group) => ['running', 'pending'].includes(group.status.toLowerCase())),
		[activeGroups],
	);

	const handlePauseResumeAll = useCallback(async () => {
		const action = hasProcessing ? 'Pause' : 'Resume';
		await globalControl(action);
	}, [hasProcessing, globalControl]);

	const handleStopAll = useCallback(async () => {
		if (confirm(t('process.confirmStopAll'))) {
			await globalControl('Stop');
		}
	}, [t, globalControl]);

	const handleOpenDashboard = useCallback(async () => {
		const url = `${getBackendBaseUrl()}/dashboard`;
		if (window.electronAPI?.openUrl) {
			await window.electronAPI.openUrl(url);
			return;
		}
		window.open(url, '_blank');
	}, []);

	const handleGroupAction = useCallback(
		async (groupId: string, status: string) => {
			const normalized = status.toLowerCase();
			if (normalized === 'paused') {
				await groupControl(groupId, 'Resume');
				return;
			}
			if (normalized === 'running' || normalized === 'pending') {
				await groupControl(groupId, 'Pause');
			}
		},
		[groupControl],
	);

	const handleStopGroup = useCallback(
		async (groupId: string) => {
			if (confirm(t('process.confirmStop'))) {
				await groupControl(groupId, 'Stop');
			}
		},
		[t, groupControl],
	);

	const handleExportGroup = useCallback(
		async (groupId: string) => {
			await exportGroupConfig(groupId);
		},
		[exportGroupConfig],
	);

	const handleStopSheet = useCallback(
		async (sheetId: string) => {
			if (confirm(t('process.confirmStop'))) {
				await jobControl(sheetId, 'Stop');
			}
		},
		[t, jobControl],
	);

	const handleSheetAction = useCallback(
		async (sheetId: string, status: string) => {
			const normalized = status.toLowerCase();
			if (normalized === 'paused') {
				await jobControl(sheetId, 'Resume');
				return;
			}
			if (normalized === 'running' || normalized === 'pending') {
				await jobControl(sheetId, 'Pause');
			}
		},
		[jobControl],
	);

	const formatLogEntryWithLanguage = useCallback(
		(entry: LogEntry, jobLabel?: string) => formatLogEntry(entry, jobLabel, language),
		[language],
	);

	const formatTimeWithLanguage = useCallback(
		(value?: string) => formatTime(value, language),
		[language],
	);

	return {
		t,
		groups,
		activeGroups,
		hasProcessing,
		expandedGroups,
		expandedLogs,
		collapsedRowGroups,
		hasGroupConfig,
		toggleGroup,
		toggleLog,
		toggleRowGroup,
		handlePauseResumeAll,
		handleStopAll,
		handleOpenDashboard,
		handleGroupAction,
		handleStopGroup,
		handleExportGroup,
		handleStopSheet,
		handleSheetAction,
		formatLogEntry: formatLogEntryWithLanguage,
		formatTime: formatTimeWithLanguage,
	};
};
