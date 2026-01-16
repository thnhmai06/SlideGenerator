import { createContext } from 'react';
import * as backendApi from '@/shared/services/backendApi';

/** Job execution status. */
export type JobStatus = 'Pending' | 'Running' | 'Paused' | 'Completed' | 'Failed' | 'Cancelled';

/** Single log entry from job execution. */
export interface LogEntry {
	message: string;
	level?: string;
	timestamp?: string;
	/** Row number this log relates to. */
	row?: number;
	/** Row processing status (processing, completed, failed). */
	rowStatus?: string;
}

/** Sheet-level job representing one worksheet being processed. */
export interface SheetJob {
	id: string;
	sheetName: string;
	status: JobStatus;
	currentRow: number;
	totalRows: number;
	/** Progress percentage (0-100). */
	progress: number;
	errorCount: number;
	outputPath?: string;
	errorMessage?: string;
	logs: LogEntry[];
	/** Hangfire background job ID. */
	hangfireJobId?: string;
}

/** Group job representing one template + workbook + output folder. */
export interface GroupJob {
	id: string;
	workbookPath: string;
	outputFolder?: string;
	status: JobStatus;
	/** Aggregate progress percentage. */
	progress: number;
	errorCount: number;
	/** Sheet jobs keyed by sheet ID. */
	sheets: Record<string, SheetJob>;
	logs: LogEntry[];
	createdAt?: string;
	completedAt?: string;
}

/** Payload for creating a new group job. */
export interface CreateGroupPayload {
	templatePath: string;
	spreadsheetPath: string;
	outputPath: string;
	textConfigs: backendApi.SlideTextConfig[];
	imageConfigs: backendApi.SlideImageConfig[];
	/** Specific sheets to process; omit for all sheets. */
	sheetNames?: string[];
}

/** Job context value for managing slide generation jobs. */
export interface JobContextValue {
	/** All group jobs. */
	groups: GroupJob[];
	/** Create a new group job. */
	createGroup: (payload: CreateGroupPayload) => Promise<GroupJob>;
	/** Refresh all groups from backend. */
	refreshGroups: () => Promise<void>;
	/** Remove all completed/failed/cancelled groups. */
	clearCompleted: () => Promise<void>;
	/** Control a group job (Pause, Resume, Stop, Cancel). */
	groupControl: (groupId: string, action: backendApi.ControlAction) => Promise<void>;
	/** Control a sheet job. */
	jobControl: (jobId: string, action: backendApi.ControlAction) => Promise<void>;
	/** Remove a group from UI and backend. */
	removeGroup: (groupId: string) => Promise<boolean>;
	/** Remove a sheet from UI and backend. */
	removeSheet: (jobId: string) => Promise<boolean>;
	/** Load logs for a sheet job. */
	loadSheetLogs: (jobId: string) => Promise<void>;
	/** Control all jobs globally. */
	globalControl: (action: backendApi.ControlAction) => Promise<void>;
	/** Export group config to JSON file. */
	exportGroupConfig: (groupId: string) => Promise<boolean>;
	/** Check if group has stored config. */
	hasGroupConfig: (groupId: string) => boolean;
}

export const JobContext = createContext<JobContextValue | undefined>(undefined);
