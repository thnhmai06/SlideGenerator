import type { SheetJob } from '@/shared/contexts/JobContextType';
export type { LogEntry, RowLogGroup } from '@/shared/utils/job';
import type { LogEntry, RowLogGroup } from '@/shared/utils/job';

export type TranslationFn = (key: string) => string;

export interface ResultSheetItemProps {
	sheet: SheetJob;
	showLog: boolean;
	logGroups: RowLogGroup[];
	collapsedRowGroups: Record<string, boolean>;
	statusKey: (status: string) => string;
	formatLogEntry: (entry: LogEntry, jobLabel?: string) => string;
	onToggleLog: () => void;
	onToggleRowGroup: (key: string) => void;
	onOpenFile: () => void;
	onRemoveSheet: () => void;
	onCopyLogs: () => void;
	t: TranslationFn;
}

export interface ResultHeaderProps {
	completedGroupsCount: number;
	onClearAll: () => void;
	t: TranslationFn;
}

export interface ResultGroupProps {
	group: {
		id: string;
		status: string;
		progress: number;
		workbookPath: string;
		completedAt?: string;
		outputFolder?: string;
		sheets: Record<string, SheetJob>;
	};
	showDetails: boolean;
	expandedLogs: Record<string, boolean>;
	collapsedRowGroups: Record<string, boolean>;
	hasGroupConfig: (groupId: string) => boolean;
	formatLogEntry: (entry: LogEntry, jobLabel?: string) => string;
	formatTime: (value?: string) => string;
	onToggleGroup: () => void;
	onToggleLog: (sheetId: string) => void;
	onToggleRowGroup: (key: string) => void;
	onOpenFolder: () => void;
	onRemoveGroup: () => void;
	onExportGroup: () => void;
	onOpenFile: (filePath?: string) => void;
	onRemoveSheet: (sheetId: string) => void;
	onCopyLogs: (sheet: SheetJob) => void;
	t: TranslationFn;
}
