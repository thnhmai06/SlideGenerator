type BackendJob = {
	jobId: string;
	status: string;
	progress: number;
	sheets: Array<{
		sheetName: string;
		outputPath: string;
		currentRow: number;
		totalRows: number;
		status: string;
		error?: string | null;
	}>;
};

type BackendJobLog = {
	level: string;
	message: string;
	timestamp: string;
	data?: {
		row?: number;
		rowStatus?: string;
	};
};

const SETTINGS_PREFIX = 'slidegen.browser.settings.';
const now = Date.now();

const minutesAgo = (minutes: number): string => new Date(now - minutes * 60_000).toISOString();

const sampleJobs: BackendJob[] = [
	{
		jobId: 'showcase-active',
		status: 'running',
		progress: 62,
		sheets: [
			{
				sheetName: 'Marketing Plan',
				outputPath: 'C:\\SlideGenerator\\Output\\marketing-plan.pptx',
				currentRow: 31,
				totalRows: 50,
				status: 'running',
			},
			{
				sheetName: 'Quarterly Review',
				outputPath: 'C:\\SlideGenerator\\Output\\quarterly-review.pptx',
				currentRow: 50,
				totalRows: 50,
				status: 'completed',
			},
		],
	},
	{
		jobId: 'showcase-completed',
		status: 'completed',
		progress: 100,
		sheets: [
			{
				sheetName: 'Customer Stories',
				outputPath: 'C:\\SlideGenerator\\Output\\customer-stories.pptx',
				currentRow: 24,
				totalRows: 24,
				status: 'completed',
			},
			{
				sheetName: 'Sales Dashboard',
				outputPath: 'C:\\SlideGenerator\\Output\\sales-dashboard.pptx',
				currentRow: 18,
				totalRows: 18,
				status: 'completed',
			},
		],
	},
	{
		jobId: 'showcase-error',
		status: 'failed',
		progress: 88,
		sheets: [
			{
				sheetName: 'Missing Images',
				outputPath: 'C:\\SlideGenerator\\Output\\missing-images.pptx',
				currentRow: 14,
				totalRows: 16,
				status: 'failed',
				error: 'Image URL is empty at row 15.',
			},
		],
	},
];

const sampleConfig = {
	type: 'get',
	server: {
		host: 'localhost',
		port: 5100,
		debug: true,
	},
	download: {
		maxChunks: 4,
		limitBytesPerSecond: 0,
		saveFolder: 'C:\\SlideGenerator\\Downloads',
		retry: {
			timeout: 30,
			maxRetries: 3,
		},
		proxy: {
			useProxy: false,
			proxyAddress: '',
			username: '',
			password: '',
			domain: '',
		},
	},
	job: {
		maxConcurrentJobs: 2,
	},
	image: {
		face: {
			confidence: 0.75,
			unionAll: false,
			maxDimension: 1280,
		},
		saliency: {
			paddingTop: 0.2,
			paddingBottom: 0.2,
			paddingLeft: 0.15,
			paddingRight: 0.15,
		},
	},
};

const workbook = {
	filePath: 'C:\\SlideGenerator\\Data\\showcase.xlsx',
	sheets: [
		{
			sheetName: 'Marketing Plan',
			headers: ['Title', 'Subtitle', 'Presenter', 'HeroImage', 'Metric', 'Notes'],
			recordCount: 50,
		},
		{
			sheetName: 'Quarterly Review',
			headers: ['Title', 'Quarter', 'Revenue', 'Growth', 'ChartImage', 'Summary'],
			recordCount: 50,
		},
	],
};

const sampleLogs: Record<string, BackendJobLog[]> = {
	'showcase-active:0': [
		{
			level: 'info',
			message: 'Loaded template and prepared placeholder bindings.',
			timestamp: minutesAgo(18),
		},
		{
			level: 'info',
			message: 'Rendering slide from row data.',
			timestamp: minutesAgo(16),
			data: { row: 29, rowStatus: 'completed' },
		},
		{
			level: 'info',
			message: 'Downloaded image asset for HeroImage.',
			timestamp: minutesAgo(15),
			data: { row: 30, rowStatus: 'completed' },
		},
		{
			level: 'warning',
			message: 'Presenter image is missing; using fallback crop.',
			timestamp: minutesAgo(13),
			data: { row: 31, rowStatus: 'processing' },
		},
		{
			level: 'info',
			message: 'Writing generated deck to output folder.',
			timestamp: minutesAgo(12),
			data: { row: 31, rowStatus: 'processing' },
		},
	],
	'showcase-active:1': [
		{
			level: 'info',
			message: 'Workbook sheet scanned successfully.',
			timestamp: minutesAgo(30),
		},
		{
			level: 'info',
			message: 'All rows completed.',
			timestamp: minutesAgo(22),
			data: { row: 50, rowStatus: 'completed' },
		},
		{
			level: 'info',
			message: 'Output file saved.',
			timestamp: minutesAgo(21),
		},
	],
	'showcase-completed:0': [
		{
			level: 'info',
			message: 'Started sheet task.',
			timestamp: minutesAgo(90),
		},
		{
			level: 'info',
			message: 'Applied text replacements for customer profile.',
			timestamp: minutesAgo(84),
			data: { row: 8, rowStatus: 'completed' },
		},
		{
			level: 'info',
			message: 'Completed 24 slides.',
			timestamp: minutesAgo(72),
			data: { row: 24, rowStatus: 'completed' },
		},
	],
	'showcase-completed:1': [
		{
			level: 'info',
			message: 'Chart image replacement configured.',
			timestamp: minutesAgo(68),
		},
		{
			level: 'info',
			message: 'Generated dashboard appendix.',
			timestamp: minutesAgo(64),
			data: { row: 18, rowStatus: 'completed' },
		},
	],
	'showcase-error:0': [
		{
			level: 'info',
			message: 'Started image-heavy sheet task.',
			timestamp: minutesAgo(44),
		},
		{
			level: 'error',
			message: 'Image URL is empty at row 15.',
			timestamp: minutesAgo(39),
			data: { row: 15, rowStatus: 'failed' },
		},
		{
			level: 'warning',
			message: 'Task stopped after validation failure.',
			timestamp: minutesAgo(38),
			data: { row: 15, rowStatus: 'failed' },
		},
	],
};

const getSamplePath = (filters?: { extensions: string[] }[]) => {
	const extensions = filters?.flatMap((filter) => filter.extensions) ?? [];
	if (extensions.some((extension) => ['pptx', 'potx'].includes(extension))) {
		return 'C:\\SlideGenerator\\Templates\\showcase-template.pptx';
	}
	if (extensions.some((extension) => ['xlsx', 'xlsm'].includes(extension))) {
		return workbook.filePath;
	}
	if (extensions.includes('json')) {
		return 'showcase-task.json';
	}
	return 'C:\\SlideGenerator\\showcase-file';
};

const seedShowcaseState = () => {
	const inputStateKey = 'slidegen.ui.inputsideBar.state';
	if (sessionStorage.getItem(inputStateKey)) return;

	sessionStorage.setItem(
		inputStateKey,
		JSON.stringify({
			slidePath: 'C:\\SlideGenerator\\Templates\\showcase-template.pptx',
			dataPath: workbook.filePath,
			savePath: 'C:\\SlideGenerator\\Output',
			columns: ['Title', 'Subtitle', 'Presenter', 'HeroImage', 'Metric', 'Notes'],
			shapes: [
				{ id: '3', name: 'Hero image', preview: 'assets/images/app-icon.png' },
				{ id: '7', name: 'Profile image', preview: 'assets/images/app-icon.png' },
				{ id: '12', name: 'Chart image', preview: 'assets/images/app-icon.png' },
			],
			placeholders: ['{{metric}}', '{{notes}}', '{{presenter}}', '{{subtitle}}', '{{title}}'],
			sheetNames: workbook.sheets.map((sheet) => sheet.sheetName),
			selectedSheets: workbook.sheets.map((sheet) => sheet.sheetName),
			sheetRowCounts: Object.fromEntries(
				workbook.sheets.map((sheet) => [sheet.sheetName, sheet.recordCount]),
			),
			sheetCount: workbook.sheets.length,
			totalRows: workbook.sheets.reduce((total, sheet) => total + sheet.recordCount, 0),
			templateLoaded: true,
			dataLoaded: true,
			textReplacements: [
				{ id: 1, placeholder: '{{title}}', columns: ['Title'] },
				{ id: 2, placeholder: '{{subtitle}}', columns: ['Subtitle'] },
			],
			imageReplacements: [
				{
					id: 1,
					shapeId: '3',
					columns: ['HeroImage'],
					roiType: 'RuleOfThirds',
					cropType: 'Fit',
				},
			],
		}),
	);
};

const findJob = (jobId: string): BackendJob | undefined =>
	sampleJobs.find((job) => job.jobId === jobId);

const createBrowserDesktopApi = (): Window['desktopAPI'] => ({
	async openFile(filters) {
		return getSamplePath(filters);
	},
	async openMultipleFiles(filters) {
		return [getSamplePath(filters)];
	},
	async openFolder() {
		return 'C:\\SlideGenerator\\Output';
	},
	async saveFile(filters) {
		return getSamplePath(filters);
	},
	async openUrl(url) {
		window.open(url, '_blank', 'noopener,noreferrer');
	},
	async openPath(path) {
		console.info(`Browser showcase cannot open local path: ${path}`);
	},
	async readSettings(filename) {
		return localStorage.getItem(`${SETTINGS_PREFIX}${filename}`);
	},
	async writeSettings(filename, data) {
		localStorage.setItem(`${SETTINGS_PREFIX}${filename}`, data);
		return true;
	},
	async windowControl() {},
	async hideToTray() {},
	async setProgressBar() {},
	async backendRequest<TResult = unknown>(method: string, params?: unknown) {
		switch (method) {
			case 'system.health':
				return { ok: true, showcase: true } as TResult;
			case 'slide.scan':
				return {
					filePath:
						(params as { filePath?: string } | undefined)?.filePath ??
						'C:\\SlideGenerator\\Templates\\showcase-template.pptx',
					slides: [
						{
							imageShapeIds: [3, 7, 12],
							placeholders: ['{{title}}', '{{subtitle}}', '{{presenter}}', '{{metric}}'],
						},
						{
							imageShapeIds: [18],
							placeholders: ['{{summary}}', '{{notes}}'],
						},
					],
				} as TResult;
			case 'sheet.scan':
				return workbook as TResult;
			case 'jobs.list':
				return sampleJobs as TResult;
			case 'jobs.get': {
				const jobId = (params as { jobId?: string } | undefined)?.jobId ?? '';
				return (findJob(jobId) ?? null) as TResult;
			}
			case 'jobs.logs': {
				const jobId = (params as { jobId?: string } | undefined)?.jobId ?? '';
				return { logs: sampleLogs[jobId] ?? [] } as TResult;
			}
			case 'jobs.create':
				{
					const request = (params ?? {}) as {
						sheetPath?: string;
						outputFolder?: string;
						selectedSheets?: string[] | null;
					};
					const jobId = `showcase-created-${Date.now()}`;
					const selectedSheets =
						request.selectedSheets && request.selectedSheets.length > 0
							? request.selectedSheets
							: workbook.sheets.map((sheet) => sheet.sheetName);
					sampleJobs.unshift({
						jobId,
						status: 'running',
						progress: 8,
						sheets: selectedSheets.map((sheetName) => ({
							sheetName,
							outputPath: `${request.outputFolder ?? 'C:\\SlideGenerator\\Output'}\\${sheetName}.pptx`,
							currentRow: 0,
							totalRows:
								workbook.sheets.find((sheet) => sheet.sheetName === sheetName)?.recordCount ?? 20,
							status: 'running',
						})),
					});
					return { jobId } as TResult;
				}
			case 'jobs.pause':
			case 'jobs.resume':
			case 'jobs.cancel': {
				const jobId = (params as { jobId?: string } | undefined)?.jobId ?? '';
				const job = findJob(jobId);
				if (job) {
					job.status =
						method === 'jobs.pause'
							? 'paused'
							: method === 'jobs.resume'
								? 'running'
								: 'cancelled';
					job.sheets = job.sheets.map((sheet) => ({
						...sheet,
						status: job.status,
					}));
				}
				return { ok: true } as TResult;
			}
			case 'config.get':
				return sampleConfig as TResult;
			case 'config.update':
			case 'config.reload':
			case 'config.reset':
				return { type: method.split('.')[1], success: true, message: 'Browser showcase is read-only.' } as TResult;
			case 'config.modelStatus':
				return { type: 'modelstatus', faceModelAvailable: true } as TResult;
			case 'config.modelControl':
				return {
					type: 'modelcontrol',
					model: (params as { model?: string } | undefined)?.model ?? 'face',
					action: (params as { action?: string } | undefined)?.action ?? 'init',
					success: true,
				} as TResult;
			default:
				throw new Error(`Unsupported browser showcase backend method: ${method}`);
		}
	},
	onBackendNotification() {
		return () => {};
	},
	async restartBackend() {
		return false;
	},
	logRenderer() {},
	onNavigate() {
		return () => {};
	},
	async setTrayLocale() {},
	async checkForUpdates() {
		return { status: 'unsupported' };
	},
	async downloadUpdate() {
		return false;
	},
	installUpdate() {},
	onUpdateStatus() {
		return () => {};
	},
	async isPortable() {
		return false;
	},
});

export const installBrowserDesktopApi = () => {
	if (window.desktopAPI) return;
	seedShowcaseState();
	window.desktopAPI = createBrowserDesktopApi();
};
