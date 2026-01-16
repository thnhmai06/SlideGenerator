import { loggers } from '@/shared/services/logging';
import type {
	GroupJob,
	SheetJob,
	LogEntry,
	JobStatus,
	CreateGroupPayload,
} from '../JobContextType';

/** Maximum log entries to keep per job. */
export const MAX_LOG_ENTRIES = 2000;

/** Threshold for triggering log trim (higher than MAX to batch operations). */
const LOG_TRIM_THRESHOLD = 2500;

/**
 * Creates an empty group job with default values.
 *
 * @param groupId - Unique group identifier.
 * @returns New GroupJob with pending status.
 */
export const createEmptyGroup = (groupId: string): GroupJob => ({
	id: groupId,
	workbookPath: '',
	status: 'Pending',
	progress: 0,
	errorCount: 0,
	sheets: {},
	logs: [],
});

/**
 * Creates an empty sheet job with default values.
 *
 * @param sheetId - Unique sheet identifier.
 * @returns New SheetJob with pending status.
 */
export const createEmptySheet = (sheetId: string): SheetJob => ({
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

/**
 * Trims logs to {@link MAX_LOG_ENTRIES}, keeping the most recent.
 *
 * @param logs - Log entries to trim.
 * @returns Trimmed array (same reference if no trim needed).
 */
export const trimLogs = (logs: LogEntry[]): LogEntry[] => {
	if (logs.length <= MAX_LOG_ENTRIES) return logs;
	return logs.slice(logs.length - MAX_LOG_ENTRIES);
};

/**
 * Appends a log entry, trimming when threshold is exceeded.
 *
 * @param logs - Existing log entries.
 * @param entry - New entry to append.
 * @returns New array with entry appended.
 */
export const appendLog = (logs: LogEntry[], entry: LogEntry): LogEntry[] => {
	if (logs.length < LOG_TRIM_THRESHOLD) {
		return [...logs, entry];
	}
	return [...logs.slice(logs.length - MAX_LOG_ENTRIES + 1), entry];
};

/**
 * Applies createdAt/completedAt timestamps to a group.
 *
 * @param prev - Previous group state.
 * @param next - Next group state.
 * @returns Group with timestamps applied.
 */
export const applyGroupTimestamps = (prev: GroupJob, next: GroupJob): GroupJob => {
	const createdAt = next.createdAt ?? prev.createdAt ?? new Date().toISOString();
	const isCompleted = ['completed', 'failed', 'cancelled'].includes(next.status.toLowerCase());
	const completedAt =
		next.completedAt ?? prev.completedAt ?? (isCompleted ? new Date().toISOString() : undefined);
	return { ...next, createdAt, completedAt };
};

/**
 * Creates a LogEntry from notification payload data.
 *
 * @param message - Log message.
 * @param level - Log level (defaults to 'Info').
 * @param timestamp - ISO timestamp.
 * @param payloadData - Additional data containing row info.
 * @returns Formatted LogEntry.
 */
export const createLogEntryFromPayload = (
	message: string,
	level: string | undefined,
	timestamp: string | undefined,
	payloadData: Record<string, unknown> | undefined,
): LogEntry => {
	const rowValue = payloadData?.row;
	const row = typeof rowValue === 'number' ? rowValue : Number(rowValue);
	const rowStatusValue = payloadData?.rowStatus;
	return {
		message,
		level: level ?? 'Info',
		timestamp,
		row: Number.isFinite(row) ? row : undefined,
		rowStatus: typeof rowStatusValue === 'string' ? rowStatusValue : undefined,
	};
};

/** Ref-like object for mutable values. */
export type RefLike<T> = { current: T };

/** Context for handling SignalR notifications. */
export type SlideNotificationContext = {
	updateGroup: (groupId: string, updater: (group: GroupJob) => GroupJob) => void;
	updateSheet: (sheetId: string, updater: (sheet: SheetJob) => SheetJob) => void;
	removedGroupIds: RefLike<Set<string>>;
	sheetToGroup: RefLike<Record<string, string>>;
	groupsRef: RefLike<Record<string, GroupJob>>;
};

/** Parsed notification payload from SignalR. */
export type SlideNotificationPayload = {
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

export const parseSlideNotificationPayload = (
	payload: unknown,
): SlideNotificationPayload | null => {
	if (!payload || typeof payload !== 'object') return null;
	const data = payload as Record<string, unknown>;
	return {
		data,
		groupId: data.groupId as string | undefined,
		jobId: data.jobId as string | undefined,
		status: data.status as string | undefined,
		message: data.message as string | undefined,
		error: data.error as string | undefined,
		level: data.level as string | undefined,
		timestamp: data.timestamp as string | undefined,
		payloadData: data.data as Record<string, unknown> | undefined,
	};
};

export const handleGroupProgressNotification = (
	groupId: string,
	data: Record<string, unknown>,
	updateGroup: (groupId: string, updater: (group: GroupJob) => GroupJob) => void,
) => {
	if (typeof data.progress !== 'number') return false;
	updateGroup(groupId, (group) => ({
		...group,
		progress: data.progress as number,
		errorCount: (data.errorCount as number) ?? group.errorCount,
	}));
	return true;
};

export const handleGroupStatusNotification = (
	groupId: string,
	status: string,
	updateGroup: (groupId: string, updater: (group: GroupJob) => GroupJob) => void,
) => {
	updateGroup(groupId, (group) => ({ ...group, status: status as JobStatus }));
	return true;
};

export const handleSheetProgressNotification = (
	jobId: string,
	data: Record<string, unknown>,
	updateSheet: (sheetId: string, updater: (sheet: SheetJob) => SheetJob) => void,
) => {
	if (typeof data.currentRow !== 'number') return false;
	updateSheet(jobId, (sheet) => ({
		...sheet,
		currentRow: data.currentRow as number,
		totalRows: (data.totalRows as number) ?? sheet.totalRows,
		progress: (data.progress as number) ?? sheet.progress,
		errorCount: (data.errorCount as number) ?? sheet.errorCount,
	}));
	return true;
};

export const handleSheetStatusNotification = (
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

export const handleSheetErrorNotification = (
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

export const shouldIgnoreNotification = (
	payload: SlideNotificationPayload,
	context: SlideNotificationContext,
) => {
	if (payload.groupId && context.removedGroupIds.current.has(payload.groupId)) return true;
	if (!payload.jobId) return false;
	const parentGroupId = context.sheetToGroup.current[payload.jobId];
	return Boolean(parentGroupId && context.removedGroupIds.current.has(parentGroupId));
};

export const handleGroupNotifications = (
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

export const handleSheetNotifications = (
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

export const handleLogNotification = (
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

export const handleSlideNotification = (payload: unknown, context: SlideNotificationContext) => {
	const parsed = parseSlideNotificationPayload(payload);
	if (!parsed) return;
	if (shouldIgnoreNotification(parsed, context)) return;
	if (handleGroupNotifications(parsed, context)) return;
	if (handleSheetNotifications(parsed, context)) return;
	handleLogNotification(parsed, context);
};

const GROUP_META_KEY = 'slidegen.group.meta';
const GROUP_CONFIG_KEY = 'slidegen.group.config';

export const readGroupConfigs = (): Record<string, CreateGroupPayload> => {
	try {
		const raw = sessionStorage.getItem(GROUP_CONFIG_KEY);
		if (!raw) return {};
		return JSON.parse(raw) as Record<string, CreateGroupPayload>;
	} catch (error) {
		loggers.jobs.error('Failed to read group configs:', error);
		return {};
	}
};

export const saveGroupConfigToStorage = (groupId: string, payload: CreateGroupPayload) => {
	try {
		const current = readGroupConfigs();
		current[groupId] = payload;
		sessionStorage.setItem(GROUP_CONFIG_KEY, JSON.stringify(current));
	} catch (error) {
		loggers.jobs.error('Failed to save group config:', error);
	}
};

export const removeGroupConfigFromStorage = (groupIds: string[]) => {
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
		loggers.jobs.error('Failed to remove group configs:', error);
	}
};

export const getGroupConfigFromStorage = (groupId: string): CreateGroupPayload | null => {
	const current = readGroupConfigs();
	return current[groupId] ?? null;
};

export const clearGroupMetaFromStorage = (groupIds: string[]) => {
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
		loggers.jobs.error('Failed to clear group meta:', error);
	}
};

export const saveGroupMetaToStorage = (
	summaries: {
		GroupId: string;
		WorkbookPath: string;
		OutputFolder?: string;
		Status: string;
		Progress: number;
		SheetCount: number;
		CompletedSheets: number;
		ErrorCount?: number;
	}[],
) => {
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
		loggers.jobs.error('Failed to save group meta:', error);
	}
};
