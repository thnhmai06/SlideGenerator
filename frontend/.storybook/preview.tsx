import React, { useEffect, useRef, useState } from 'react';
import type { Preview } from '@storybook/react';
import { installBrowserDesktopApi } from '../src/shared/platform/browserApi';
import AppProviders from '../src/app/providers/AppProviders';

import '../src/shared/styles/theme.css';
import '../src/shared/styles/index.css';

installBrowserDesktopApi();

type MockSheet = {
	sheetName: string;
	status: 'pending' | 'running' | 'paused' | 'completed' | 'failed' | 'cancelled';
	progress: number;
	totalRows: number;
	currentRow: number;
	outputPath: string;
	error?: string | null;
};

type MockJob = {
	jobId: string;
	status: MockSheet['status'];
	progress: number;
	outputFolder: string;
	payloadJson: string;
	sheets: MockSheet[];
	logs: Array<{
		level: string;
		message: string;
		timestamp: string;
		data?: Record<string, unknown>;
	}>;
};

const nowIso = () => new Date().toISOString();

const DEFAULT_TEMPLATE_PATH = 'C:\\SlideGenerator\\Examples\\Template.pptx';
const DEFAULT_SHEET_PATH = 'C:\\SlideGenerator\\Examples\\Personnel_List.xlsx';
const DEFAULT_OUTPUT_PATH = 'C:\\SlideGenerator\\Output';

const createPayloadJson = (sheetNames: string[] = ['Danh sach nhan su']) =>
	JSON.stringify({
		templatePath: DEFAULT_TEMPLATE_PATH,
		spreadsheetPath: DEFAULT_SHEET_PATH,
		outputPath: DEFAULT_OUTPUT_PATH,
		textConfigs: [
			{ pattern: '{{Ho ten}}', columns: ['Ho ten'] },
			{ pattern: '{{Chuc vu}}', columns: ['Chuc vu'] },
		],
		imageConfigs: [{ shapeId: 101, columns: ['Anh dai dien'], roiType: 'RuleOfThirds' }],
		sheetNames,
	});

const createLogs = (status: MockSheet['status']) => [
	{
		level: 'info',
		message: 'Loaded workbook and template',
		timestamp: nowIso(),
		data: { row: 1, rowStatus: 'completed' },
	},
	{
		level: status === 'failed' ? 'error' : 'info',
		message: status === 'failed' ? 'Missing image file for row 12' : 'Rendered slide batch',
		timestamp: nowIso(),
		data: {
			row: status === 'failed' ? 12 : 18,
			rowStatus: status === 'failed' ? 'failed' : 'completed',
		},
	},
];

const makeJob = (
	jobId: string,
	status: MockJob['status'],
	progress: number,
	sheets: MockSheet[],
): MockJob => ({
	jobId,
	status,
	progress,
	outputFolder: DEFAULT_OUTPUT_PATH,
	payloadJson: createPayloadJson(sheets.map((sheet) => sheet.sheetName)),
	sheets,
	logs: createLogs(status),
});

let mockJobs: MockJob[] = [];
let jobInterval: number | null = null;
let notificationCallbacks = new Set<(event: { method: string; params?: unknown }) => void>();
let updateCallbacks = new Set<(state: unknown) => void>();

const emitJobUpdate = (job: MockJob) => {
	notificationCallbacks.forEach((callback) => {
		callback({ method: 'jobs.updated', params: job });
	});
};

const emitUpdateStatus = (state: unknown) => {
	updateCallbacks.forEach((callback) => {
		callback(state);
	});
};

const seedJobs = (scenario?: string): MockJob[] => {
	if (scenario === 'active') {
		return [
			makeJob('group-running-1', 'running', 45, [
				{
					sheetName: 'Danh sách nhân sự',
					status: 'running',
					progress: 60,
					totalRows: 50,
					currentRow: 30,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Danh_sach_nhan_su.pptx`,
				},
				{
					sheetName: 'Phòng ban',
					status: 'pending',
					progress: 0,
					totalRows: 20,
					currentRow: 0,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Phong_ban.pptx`,
				},
			]),
			makeJob('group-paused-1', 'paused', 25, [
				{
					sheetName: 'Hợp đồng',
					status: 'paused',
					progress: 25,
					totalRows: 40,
					currentRow: 10,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Hop_dong.pptx`,
				},
			]),
			makeJob('group-cancelled-1', 'cancelled', 10, [
				{
					sheetName: 'Dữ liệu cũ',
					status: 'cancelled',
					progress: 10,
					totalRows: 100,
					currentRow: 10,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Du_lieu_cu.pptx`,
				},
			]),
		];
	}

	if (scenario === 'results') {
		return [
			makeJob('group-success-1', 'completed', 100, [
				{
					sheetName: 'Kế hoạch Marketing',
					status: 'completed',
					progress: 100,
					totalRows: 30,
					currentRow: 30,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Marketing.pptx`,
				},
			]),
			makeJob('group-mixed-1', 'completed', 85, [
				{
					sheetName: 'Ứng viên tiềm năng',
					status: 'completed',
					progress: 100,
					totalRows: 50,
					currentRow: 50,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Ung_vien.pptx`,
				},
				{
					sheetName: 'Báo cáo kỹ thuật',
					status: 'failed',
					progress: 70,
					totalRows: 30,
					currentRow: 21,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Technical.pptx`,
					error: 'Lỗi định dạng ảnh tại dòng 22: File không tồn tại.',
				},
			]),
			makeJob('group-failed-all', 'failed', 0, [
				{
					sheetName: 'Dữ liệu lỗi',
					status: 'failed',
					progress: 0,
					totalRows: 10,
					currentRow: 0,
					outputPath: `${DEFAULT_OUTPUT_PATH}\\Error.pptx`,
					error: 'Không thể truy cập tệp Excel nguồn.',
				},
			]),
		];
	}

	return [];
};

const startJobSimulation = () => {
	if (jobInterval) window.clearInterval(jobInterval);
	jobInterval = window.setInterval(() => {
		mockJobs = mockJobs.map((job) => {
			if (job.status !== 'running') return job;
			const progress = Math.min(100, (job.progress || 0) + 12);
			const status = progress >= 100 ? 'completed' : 'running';
			const next = {
				...job,
				progress,
				status,
				sheets: job.sheets.map((sheet) => ({
					...sheet,
					progress,
					status,
					currentRow: Math.min(
						sheet.totalRows,
						Math.max(sheet.currentRow, Math.floor((progress / 100) * sheet.totalRows)),
					),
				})),
			};
			emitJobUpdate(next);
			return next;
		});
	}, 1500);
};

const DEFAULT_CONFIG = {
	type: 'configget',
	server: { host: 'localhost', port: 5100, debug: true },
	download: {
		maxChunks: 4,
		limitBytesPerSecond: 0,
		saveFolder: 'C:\\SlideGenerator\\Downloads',
		retry: { timeout: 30, maxRetries: 3 },
		proxy: { useProxy: false, proxyAddress: '', username: '', password: '', domain: '' },
	},
	job: { maxConcurrentJobs: 2 },
	image: {
		face: { confidence: 0.75, unionAll: false, maxDimension: 1280 },
		saliency: { paddingTop: 0.2, paddingBottom: 0.2, paddingLeft: 0.15, paddingRight: 0.15 },
	},
};

const findJob = (jobId?: string) => {
	if (!jobId) return null;
	const groupId = jobId.includes(':') ? jobId.split(':')[0] : jobId;
	return mockJobs.find((job) => job.jobId === groupId) ?? null;
};

const preview: Preview = {
	parameters: {
		controls: {
			matchers: {
				color: /(background|color)$/i,
				date: /Date$/i,
			},
		},
	},
	globalTypes: {
		connection: {
			description: 'Mock backend connection state',
			defaultValue: 'connected',
			toolbar: {
				title: 'Connection',
				icon: 'transfer',
				items: [
					{ value: 'connected', title: 'Connected' },
					{ value: 'disconnected', title: 'Disconnected' },
					{ value: 'startup', title: 'Startup delay' },
				],
				dynamicTitle: true,
			},
		},
		portable: {
			description: 'Portable update mode',
			defaultValue: 'no',
			toolbar: {
				title: 'Portable',
				icon: 'mobile',
				items: [
					{ value: 'no', title: 'Installed' },
					{ value: 'yes', title: 'Portable' },
				],
				dynamicTitle: true,
			},
		},
	},
	decorators: [
		(Story, context) => {
			const connectionMode = context.globals.connection;
			const isPortableMode = context.globals.portable === 'yes';
			const scenario = context.parameters.appScenario as string | undefined;
			const updateScenario = context.parameters.updateScenario as string | undefined;
			const [isStartupDone, setIsStartupDone] = useState(false);
			const startupTimerRef = useRef<number | null>(null);

			useEffect(() => {
				mockJobs = seedJobs(scenario);
				startJobSimulation();

				if (connectionMode === 'startup') {
					setIsStartupDone(false);
					if (startupTimerRef.current) window.clearTimeout(startupTimerRef.current);
					startupTimerRef.current = window.setTimeout(() => setIsStartupDone(true), 3000);
				} else {
					setIsStartupDone(connectionMode === 'connected');
				}

				return () => {
					if (startupTimerRef.current) window.clearTimeout(startupTimerRef.current);
					if (jobInterval) window.clearInterval(jobInterval);
					notificationCallbacks = new Set();
					updateCallbacks = new Set();
				};
			}, [connectionMode, context.id, scenario]);

			useEffect(() => {
				if (updateScenario && isStartupDone) {
					const timer = window.setTimeout(() => {
						const info = {
							version: '2.1.0',
							releaseNotes: 'Mocked release notes for Storybook simulation.',
						};
						if (updateScenario === 'available') {
							emitUpdateStatus({ status: 'available', info });
						} else if (updateScenario === 'downloading') {
							emitUpdateStatus({ status: 'downloading', progress: 45, info });
						} else if (updateScenario === 'downloaded') {
							emitUpdateStatus({ status: 'downloaded', progress: 100, info });
						} else if (updateScenario === 'error') {
							emitUpdateStatus({ status: 'error', error: 'Simulated download error' });
						}
					}, 300);
					return () => window.clearTimeout(timer);
				}
				return undefined;
			}, [updateScenario, isStartupDone, context.id]);

			useEffect(() => {
				if (!window.desktopAPI) return undefined;

				const original = {
					backendRequest: window.desktopAPI.backendRequest,
					isPortable: window.desktopAPI.isPortable,
					checkForUpdates: window.desktopAPI.checkForUpdates,
					openFile: window.desktopAPI.openFile,
					openFolder: window.desktopAPI.openFolder,
					onBackendNotification: window.desktopAPI.onBackendNotification,
					confirm: window.confirm,
				};

				window.confirm = () => true;
				window.desktopAPI.isPortable = async () => isPortableMode;
				window.desktopAPI.openFile = async (filters?: Array<{ extensions?: string[] }>) => {
					const isSpreadsheet = filters?.some((filter) =>
						filter.extensions?.some((ext) => ['xlsx', 'xlsm'].includes(ext.toLowerCase())),
					);
					return isSpreadsheet ? DEFAULT_SHEET_PATH : DEFAULT_TEMPLATE_PATH;
				};
				window.desktopAPI.openFolder = async () => DEFAULT_OUTPUT_PATH;
				window.desktopAPI.onBackendNotification = (callback) => {
					notificationCallbacks.add(callback);
					return () => notificationCallbacks.delete(callback);
				};

				window.desktopAPI.checkForUpdates = async () => {
					if (isPortableMode) return { status: 'unsupported' };
					return {
						status: 'available',
						info: { version: '2.1.0', releaseNotes: 'Improved batch generation and update flow.' },
					};
				};

				window.desktopAPI.onUpdateStatus = (callback: (state: unknown) => void) => {
					updateCallbacks.add(callback);
					return () => updateCallbacks.delete(callback);
				};

				window.desktopAPI.downloadUpdate = async () => {
					let progress = 0;
					const interval = window.setInterval(() => {
						progress += Math.floor(Math.random() * 10) + 5;
						if (progress > 100) progress = 100;

						emitUpdateStatus({
							status: progress >= 100 ? 'downloaded' : 'downloading',
							progress,
							info: { version: '2.1.0' },
						});

						if (progress >= 100) window.clearInterval(interval);
					}, 400);
					return true;
				};

				window.desktopAPI.installUpdate = () => {
					alert('Mock installation triggered. In real app, this would restart.');
					emitUpdateStatus({ status: 'idle' });
				};

				window.desktopAPI.backendRequest = async <TResult,>(method: string, params?: unknown) => {
					const isOffline =
						connectionMode === 'disconnected' || (connectionMode === 'startup' && !isStartupDone);

					if (method === 'system.health') {
						if (isOffline) return { ok: false, message: 'Connection failed' } as TResult;
						return { ok: true, is_mock: true } as TResult;
					}

					if (isOffline) {
						if (method === 'jobs.list') return [] as TResult;
						if (method === 'config.get') return DEFAULT_CONFIG as TResult;
						return {
							type: 'error',
							kind: 'Unavailable',
							message: 'Backend not available',
						} as TResult;
					}

					if (method === 'slide.scan') {
						return {
							filePath:
								(params as { filePath?: string } | undefined)?.filePath ?? DEFAULT_TEMPLATE_PATH,
							slides: [
								{
									placeholders: ['{{Ho ten}}', '{{Chuc vu}}', '{{Phong ban}}', '{{Ma NV}}'],
									imageShapeIds: [101, 102],
								},
								{ placeholders: ['{{Ngay sinh}}', '{{Que quan}}'], imageShapeIds: [201] },
							],
						} as TResult;
					}

					if (method === 'sheet.scan') {
						return {
							filePath:
								(params as { filePath?: string } | undefined)?.filePath ?? DEFAULT_SHEET_PATH,
							sheets: [
								{
									sheetName: 'Danh sach nhan su',
									headers: [
										'Ho ten',
										'Chuc vu',
										'Phong ban',
										'Ma NV',
										'Ngay sinh',
										'Que quan',
										'Anh dai dien',
										'Chu ky',
									],
									recordCount: 45,
								},
								{ sheetName: 'Cau hinh', headers: ['Key', 'Value'], recordCount: 10 },
							],
						} as TResult;
					}

					if (method === 'jobs.create') {
						const payload = params as {
							outputFolder?: string;
							selectedSheets?: string[] | null;
						};
						const jobId = `group-${Date.now()}`;
						const sheetNames = payload.selectedSheets?.length
							? payload.selectedSheets
							: ['Danh sach nhan su', 'Cau hinh'];
						const sheets = sheetNames.map((sheetName, index) => ({
							sheetName,
							status: 'running' as const,
							progress: index === 0 ? 8 : 0,
							totalRows: sheetName === 'Cau hinh' ? 10 : 45,
							currentRow: index === 0 ? 3 : 0,
							outputPath: `${payload.outputFolder ?? DEFAULT_OUTPUT_PATH}\\${sheetName.replace(/\s+/g, '_')}.pptx`,
						}));
						const job = makeJob(jobId, 'running', 5, sheets);
						job.outputFolder = payload.outputFolder ?? DEFAULT_OUTPUT_PATH;
						mockJobs = [job, ...mockJobs];
						emitJobUpdate(job);
						return {
							jobId,
							sheetJobIds: Object.fromEntries(
								sheetNames.map((sheetName, index) => [sheetName, `${jobId}:${index}`]),
							),
						} as TResult;
					}

					if (method === 'jobs.list') return mockJobs as TResult;
					if (method === 'jobs.get') {
						const job = findJob((params as { jobId?: string } | undefined)?.jobId);
						return job as TResult;
					}
					if (method === 'jobs.logs') {
						const job = findJob((params as { jobId?: string } | undefined)?.jobId);
						return { logs: job?.logs ?? [] } as TResult;
					}
					if (['jobs.pause', 'jobs.resume', 'jobs.cancel'].includes(method)) {
						const jobId = (params as { jobId?: string } | undefined)?.jobId;
						const status =
							method === 'jobs.pause'
								? 'paused'
								: method === 'jobs.resume'
									? 'running'
									: 'cancelled';
						mockJobs = mockJobs.map((job) => {
							if (job.jobId !== jobId) return job;
							const next = {
								...job,
								status,
								sheets: job.sheets.map((sheet) => ({ ...sheet, status })),
							};
							emitJobUpdate(next);
							return next;
						});
						return { ok: true } as TResult;
					}

					if (method === 'config.get') return DEFAULT_CONFIG as TResult;
					if (method === 'config.update') return { type: 'configupdate', success: true } as TResult;
					if (method === 'config.reload') return { type: 'configreload', success: true } as TResult;
					if (method === 'config.reset') return { type: 'configreset', success: true } as TResult;
					if (method === 'config.modelStatus') {
						return { type: 'modelstatus', faceModelAvailable: true } as TResult;
					}
					if (method === 'config.modelControl') {
						return { type: 'modelcontrol', success: true, message: 'OK' } as TResult;
					}

					return { ok: true } as TResult;
				};

				return () => {
					window.desktopAPI.backendRequest = original.backendRequest;
					window.desktopAPI.isPortable = original.isPortable;
					window.desktopAPI.checkForUpdates = original.checkForUpdates;
					window.desktopAPI.openFile = original.openFile;
					window.desktopAPI.openFolder = original.openFolder;
					window.desktopAPI.onBackendNotification = original.onBackendNotification;
					window.confirm = original.confirm;
				};
			}, [connectionMode, context.id, isPortableMode, isStartupDone]);

			return (
				<AppProviders>
					<Story />
				</AppProviders>
			);
		},
	],
};

export default preview;
