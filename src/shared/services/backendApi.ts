/**
 * Backend API types and functions barrel export.
 *
 * @module backendApi
 * @remarks
 * This module re-exports all backend API types and functions from their
 * respective domain modules (jobs, sheets, config, health).
 */

// Common types
export type {
	ControlAction,
	ShapeDto,
	SlideImageConfig,
	SlideTextConfig,
} from './backend/common/types';

// Job management types
export type {
	GroupSummary,
	JobDetail,
	JobExportPayload,
	JobLogEntry,
	JobState,
	JobStatusInfo,
	JobSummary,
	JobType,
	SlideGlobalGetGroupsSuccess,
	SlideGroupCreateSuccess,
	SlideGroupRemoveSuccess,
	SlideGroupStatusSuccess,
	SlideJobLogsSuccess,
	SlideJobRemoveSuccess,
	SlideJobStatusSuccess,
	SlideScanPlaceholdersSuccess,
	SlideScanShapesSuccess,
	SlideScanTemplateSuccess,
} from './backend/jobs/types';

// Configuration types
export type {
	ConfigGetSuccess,
	ConfigReloadSuccess,
	ConfigResetSuccess,
	ConfigUpdateSuccess,
	ModelStatusSuccess,
	ModelControlSuccess,
} from './backend/config/types';

// Sheet/workbook types
export type {
	ColumnListResponse,
	FileListResponse,
	LoadFileResponse,
	LoadedFile,
	SheetDataResponse,
	SheetDetailInfo,
	SheetInfo,
	SheetListResponse,
	SheetWorkbookGetInfoSuccess,
} from './backend/sheets/types';

// Job management APIs
export {
	createGroup,
	getAllGroups,
	getGroupPayload,
	getJobLogs,
	globalControl,
	groupControl,
	groupStatus,
	jobControl,
	jobStatus,
	onSlideConnected,
	onSlideNotification,
	onSlideReconnected,
	removeGroup,
	removeJob,
	scanPlaceholders,
	scanShapes,
	scanTemplate,
	subscribeGroup,
	subscribeSheet,
} from './backend/jobs/api';

// Sheet/workbook APIs
export {
	getAllColumns,
	getColumns,
	getLoadedFiles,
	getSheetData,
	getSheetInfo,
	getSheetRow,
	getSheets,
	getWorkbookInfo,
	loadFile,
	unloadFile,
} from './backend/sheets/api';

// Health check API
export { checkHealth } from './backend/health/api';

// Configuration APIs
export {
	getConfig,
	reloadConfig,
	resetConfig,
	updateConfig,
	getModelStatus,
	controlModel,
} from './backend/config/api';
