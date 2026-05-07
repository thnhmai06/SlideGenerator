import type { SheetJob } from '@/shared/contexts/JobContextType';
import { formatUserDateTime, formatUserTime } from './time';

export interface LogEntry {
	message: string;
	level?: string;
	timestamp?: string;
	row?: number;
	rowStatus?: string;
}

export interface RowLogGroup {
	key: string;
	row?: number;
	status?: string;
	entries: LogEntry[];
}

export const statusKey = (status: string): string => {
	const normalized = status.toLowerCase();
	if (normalized === 'running') return 'processing';
	if (normalized === 'failed') return 'error';
	return normalized;
};

export const progressColor = (status: string): string => {
	switch (status.toLowerCase()) {
		case 'pending':
			return '#9ca3af';
		case 'running':
			return '#3b82f6';
		case 'paused':
			return '#f59e0b';
		case 'completed':
			return '#10b981';
		case 'error':
		case 'failed':
		case 'cancelled':
			return '#ef4444';
		default:
			return 'var(--accent-primary)';
	}
};

export const deriveGroupName = (workbookPath: string, fallback: string): string => {
	if (!workbookPath) return fallback;
	const parts = workbookPath.split(/[/\\]/);
	return parts[parts.length - 1] || fallback;
};

export const formatLogEntry = (
	entry: LogEntry,
	jobLabel: string | undefined,
	language: string,
): string => {
	const timeValue = formatUserTime(entry.timestamp, language);
	const time = timeValue ? `[${timeValue}] ` : '';
	const level = entry.level ? `${entry.level}: ` : '';
	const job = jobLabel ? `${jobLabel}: ` : '';
	return `${time}${level}${job}${entry.message}`;
};

export const groupLogsByRow = (logs: LogEntry[]): RowLogGroup[] => {
	const groups: RowLogGroup[] = [];
	const map = new Map<string, RowLogGroup>();
	for (const entry of logs) {
		const key = entry.row != null ? `row:${entry.row}` : 'general';
		let group = map.get(key);
		if (!group) {
			group = { key, row: entry.row, status: entry.rowStatus, entries: [] };
			map.set(key, group);
			groups.push(group);
		}
		group.entries.push(entry);
		if (entry.rowStatus) group.status = entry.rowStatus;
	}
	return groups;
};

export const getSheetStats = (sheet: SheetJob) => {
	const completedSlides = Math.min(sheet.currentRow, sheet.totalRows);
	const isFailed = sheet.status === 'Failed' || sheet.status === 'Cancelled';
	const failedSlides = isFailed ? Math.max(sheet.totalRows - completedSlides, 0) : 0;
	const processingSlides =
		sheet.status === 'Running'
			? Math.max(sheet.totalRows - completedSlides, 0)
			: sheet.status === 'Pending'
				? sheet.totalRows
				: 0;
	return { completedSlides, failedSlides, processingSlides };
};

export const summarizeSheets = (sheets: SheetJob[]) => {
	let completedJobs = 0,
		processingJobs = 0,
		failedJobs = 0;
	let totalSlides = 0,
		completedSlides = 0,
		processingSlides = 0,
		failedSlides = 0;

	for (const sheet of sheets) {
		if (sheet.status === 'Completed') completedJobs++;
		else if (sheet.status === 'Running' || sheet.status === 'Pending') processingJobs++;
		else if (sheet.status === 'Failed' || sheet.status === 'Cancelled') failedJobs++;

		const stats = getSheetStats(sheet);
		completedSlides += stats.completedSlides;
		processingSlides += stats.processingSlides;
		failedSlides += stats.failedSlides;
		totalSlides += sheet.totalRows ?? 0;
	}

	return {
		completedJobs,
		processingJobs,
		failedJobs,
		totalSlides,
		completedSlides,
		processingSlides,
		failedSlides,
	};
};

export const summarizeSheetsSimple = (
	sheets: Array<{ status: string; currentRow: number; totalRows: number }>,
) => {
	let completedSlides = 0,
		failedSlides = 0,
		totalSlides = 0;

	for (const sheet of sheets) {
		const total = sheet.totalRows ?? 0;
		const done = Math.min(sheet.currentRow ?? 0, total);
		totalSlides += total;
		completedSlides += done;
		if (sheet.status === 'Failed' || sheet.status === 'Cancelled') {
			failedSlides += Math.max(total - done, 0);
		}
	}

	return { completedSlides, failedSlides, totalSlides };
};

export const formatTime = (value: string | undefined, language: string): string =>
	value ? formatUserDateTime(value, language) : '';
