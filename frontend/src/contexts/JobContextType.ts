import { createContext } from 'react';
import * as backendApi from '../services/backendApi';

export type JobStatus = 'Pending' | 'Running' | 'Paused' | 'Completed' | 'Failed' | 'Cancelled';

export interface LogEntry {
	message: string;
	level?: string;
	timestamp?: string;
	row?: number;
	rowStatus?: string;
}

export interface SheetJob {
	id: string;
	sheetName: string;
	status: JobStatus;
	currentRow: number;
	totalRows: number;
	progress: number;
	errorCount: number;
	outputPath?: string;
	errorMessage?: string;
	logs: LogEntry[];
	hangfireJobId?: string;
}

export interface GroupJob {
	id: string;
	workbookPath: string;
	outputFolder?: string;
	status: JobStatus;
	progress: number;
	errorCount: number;
	sheets: Record<string, SheetJob>;
	logs: LogEntry[];
	createdAt?: string;
	completedAt?: string;
}

export interface CreateGroupPayload {
	templatePath: string;
	spreadsheetPath: string;
	outputPath: string;
	textConfigs: backendApi.SlideTextConfig[];
	imageConfigs: backendApi.SlideImageConfig[];
	sheetNames?: string[];
}

export interface JobContextValue {
	groups: GroupJob[];
	createGroup: (payload: CreateGroupPayload) => Promise<GroupJob>;
	refreshGroups: () => Promise<void>;
	clearCompleted: () => Promise<void>;
	groupControl: (groupId: string, action: backendApi.ControlAction) => Promise<void>;
	jobControl: (jobId: string, action: backendApi.ControlAction) => Promise<void>;
	removeGroup: (groupId: string) => Promise<boolean>;
	removeSheet: (jobId: string) => Promise<boolean>;
	loadSheetLogs: (jobId: string) => Promise<void>;
	globalControl: (action: backendApi.ControlAction) => Promise<void>;
	exportGroupConfig: (groupId: string) => Promise<boolean>;
	hasGroupConfig: (groupId: string) => boolean;
}

export const JobContext = createContext<JobContextValue | undefined>(undefined);
