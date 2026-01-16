import { useCallback, useMemo, useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import type { LogEntry } from '../types';
import { formatLogEntry, formatTime } from '../utils';

export const useResults = () => {
	const { t, language } = useApp();
	const {
		groups,
		clearCompleted,
		removeGroup,
		removeSheet,
		loadSheetLogs,
		exportGroupConfig,
		hasGroupConfig,
	} = useJobs();

	const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({});
	const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>({});
	const [collapsedRowGroups, setCollapsedRowGroups] = useState<Record<string, boolean>>({});

	const completedGroups = useMemo(
		() =>
			groups.filter((group) =>
				['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase()),
			),
		[groups],
	);

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

	const handleOpenFolder = useCallback(async (folderPath: string | undefined) => {
		if (!folderPath || !window.electronAPI) return;
		await window.electronAPI.openPath(folderPath);
	}, []);

	const handleExportGroup = useCallback(
		async (groupId: string) => {
			await exportGroupConfig(groupId);
		},
		[exportGroupConfig],
	);

	const handleOpenFile = useCallback(async (filePath: string | undefined) => {
		if (!filePath || !window.electronAPI) return;
		await window.electronAPI.openPath(filePath);
	}, []);

	const handleRemoveSheet = useCallback(
		async (sheetId: string) => {
			await removeSheet(sheetId);
		},
		[removeSheet],
	);

	const handleRemoveGroup = useCallback(
		async (groupId: string) => {
			if (confirm(t('results.confirmRemoveGroup'))) {
				await removeGroup(groupId);
			}
		},
		[t, removeGroup],
	);

	const handleClearAll = useCallback(async () => {
		if (confirm(t('results.confirmClearAll'))) {
			await clearCompleted();
		}
	}, [t, clearCompleted]);

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
		completedGroups,
		expandedGroups,
		expandedLogs,
		collapsedRowGroups,
		hasGroupConfig,
		toggleGroup,
		toggleLog,
		toggleRowGroup,
		handleOpenFolder,
		handleExportGroup,
		handleOpenFile,
		handleRemoveSheet,
		handleRemoveGroup,
		handleClearAll,
		formatLogEntry: formatLogEntryWithLanguage,
		formatTime: formatTimeWithLanguage,
	};
};
