import type { SheetJob } from '@/shared/contexts/JobContextType';
export type { LogEntry, RowLogGroup } from '@/shared/utils/job';
import type { LogEntry, RowLogGroup } from '@/shared/utils/job';

export type TranslationFn = (key: string) => string;

export interface SheetItemProps {
	sheet: SheetJob;
	showLog: boolean;
	logGroups: RowLogGroup[];
	collapsedRowGroups: Record<string, boolean>;
	statusKey: (status: string) => string;
	progressColor: (status: string) => string;
	formatLogEntry: (entry: LogEntry, jobLabel?: string) => string;
	onToggleLog: () => void;
	onToggleRowGroup: (key: string) => void;
	onSheetAction: () => void;
	onStopSheet: () => void;
	onCopyLogs: () => void;
	t: TranslationFn;
}

export interface ProcessHeaderProps {
	hasProcessing: boolean;
	activeGroupsCount: number;
	onPauseResumeAll: () => void;
	onStopAll: () => void;
	onOpenDashboard: () => void;
	t: TranslationFn;
}

export interface ProcessGroupProps {
	group: {
		id: string;
		status: string;
		progress: number;
		workbookPath: string;
		createdAt?: string;
		sheets: Record<string, SheetJob>;
	};
	showDetails: boolean;
	expandedLogs: Record<string, boolean>;
	collapsedRowGroups: Record<string, boolean>;
	statusKey: (status: string) => string;
	progressColor: (status: string) => string;
	formatLogEntry: (entry: LogEntry, jobLabel?: string) => string;
	formatTime: (value?: string) => string;
	deriveGroupName: (workbookPath: string, fallback: string) => string;
	hasGroupConfig: (groupId: string) => boolean;
	onToggleGroup: () => void;
	onToggleLog: (sheetId: string) => void;
	onToggleRowGroup: (key: string) => void;
	onGroupAction: () => void;
	onStopGroup: () => void;
	onExportGroup: () => void;
	onSheetAction: (sheetId: string, status: string) => void;
	onStopSheet: (sheetId: string) => void;
	onCopyLogs: (sheet: SheetJob) => void;
	t: TranslationFn;
}
