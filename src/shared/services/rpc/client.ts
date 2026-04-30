import { loggers } from '../logging';

export class RpcChannelClient {
	private readonly channel: string;
	private readonly notificationHandlers = new Set<(payload: unknown) => void>();
	private readonly unsubscribeNotification: () => void;

	constructor(channel: string) {
		this.channel = channel;

		if (window.desktopAPI?.onBackendNotification) {
			this.unsubscribeNotification = window.desktopAPI.onBackendNotification(
				({ method, params }) => {
					if (this.channel !== 'jobs') return;
					if (method !== 'jobs.updated') return;
					this.handleJobUpdated(params);
				},
			);
			return;
		}

		this.unsubscribeNotification = () => {};
		loggers.jobs.warn(
			'Desktop API is unavailable; RPC notifications are disabled in this runtime.',
		);
	}

	async sendRequest<TResponse>(payload: Record<string, unknown>): Promise<TResponse> {
		const type = (payload.type as string | undefined)?.toLowerCase();
		if (!type) throw new Error('Request type is required');

		if (this.channel === 'jobs') {
			return (await this.sendJobRequest(payload, type)) as TResponse;
		}

		if (this.channel === 'sheets') {
			return (await this.sendSheetRequest(payload, type)) as TResponse;
		}

		if (this.channel === 'config') {
			return (await this.sendConfigRequest(payload, type)) as TResponse;
		}

		throw new Error(`Unsupported RPC channel: ${this.channel}`);
	}

	async invoke(methodName: string, ...args: unknown[]): Promise<void> {
		if (methodName === 'SubscribeGroup' || methodName === 'SubscribeSheet') {
			return;
		}
		await this.backendRequest(methodName, args);
	}

	onNotification(handler: (payload: unknown) => void): () => void {
		this.notificationHandlers.add(handler);
		return () => {
			this.notificationHandlers.delete(handler);
		};
	}

	onReconnected(_handler: (connectionId?: string) => void): () => void {
		return () => {};
	}

	onConnected(_handler: (connectionId?: string) => void): () => void {
		return () => {};
	}

	async dispose(): Promise<void> {
		this.notificationHandlers.clear();
		this.unsubscribeNotification();
	}

	private async sendJobRequest(payload: Record<string, unknown>, type: string): Promise<unknown> {
		switch (type) {
			case 'scanshapes': {
				const filePath = payload.filePath as string;
				const result = await this.backendRequest<{
					filePath: string;
					slides: Array<{ imageShapeIds: number[] }>;
				}>('slide.scan', { filePath });

				const shapeSet = new Set<number>();
				(result?.slides ?? []).forEach((slide) =>
					(slide.imageShapeIds ?? []).forEach((shapeId) => shapeSet.add(shapeId)),
				);

				return {
					type: 'scanshapes',
					filePath: result?.filePath ?? filePath,
					shapes: Array.from(shapeSet).map((id) => ({
						id,
						name: `Shape ${id}`,
						data: '',
						kind: 'Image',
						isImage: true,
					})),
				};
			}
			case 'scanplaceholders': {
				const filePath = payload.filePath as string;
				const result = await this.backendRequest<{
					filePath: string;
					slides: Array<{ placeholders: string[] }>;
				}>('slide.scan', { filePath });

				const placeholderSet = new Set<string>();
				(result?.slides ?? []).forEach((slide) =>
					(slide.placeholders ?? []).forEach((placeholder) => placeholderSet.add(placeholder)),
				);

				return {
					type: 'scanplaceholders',
					filePath: result?.filePath ?? filePath,
					placeholders: Array.from(placeholderSet),
				};
			}
			case 'scantemplate': {
				const filePath = payload.filePath as string;
				const result = await this.backendRequest<{
					filePath: string;
					slides: Array<{ imageShapeIds: number[]; placeholders: string[] }>;
				}>('slide.scan', { filePath });

				const shapeSet = new Set<number>();
				const placeholderSet = new Set<string>();
				(result?.slides ?? []).forEach((slide) => {
					(slide.imageShapeIds ?? []).forEach((shapeId) => shapeSet.add(shapeId));
					(slide.placeholders ?? []).forEach((placeholder) => placeholderSet.add(placeholder));
				});

				return {
					type: 'scantemplate',
					filePath: result?.filePath ?? filePath,
					shapes: Array.from(shapeSet).map((id) => ({
						id,
						name: `Shape ${id}`,
						data: '',
						kind: 'Image',
						isImage: true,
					})),
					placeholders: Array.from(placeholderSet),
				};
			}
			case 'jobcreate': {
				const templatePath = payload.templatePath as string;
				const spreadsheetPath = payload.spreadsheetPath as string;
				const outputPath = payload.outputPath as string;
				const sheetNames = (payload.sheetNames as string[] | undefined) ?? [];
				const textConfigs =
					(payload.textConfigs as Array<{ pattern: string; columns: string[] }> | undefined) ?? [];
				const imageConfigs =
					(payload.imageConfigs as
						| Array<{ shapeId: number; columns: string[]; roiType?: string | null }>
						| undefined) ?? [];

				const templateKey = 'default';
				const sheetTemplateMap = Object.fromEntries(
					sheetNames.map((sheetName) => [sheetName, templateKey]),
				);

				const job = await this.backendRequest<{
					jobId?: string;
					sheetJobIds?: Record<string, string>;
				}>('jobs.create', {
					templates: [{ templateKey, filePath: templatePath, templateSlideIndex: 1 }],
					sheetPath: spreadsheetPath,
					sheetTemplateMap,
					selectedSheets: sheetNames.length > 0 ? sheetNames : null,
					textConfig: textConfigs.map((config) => ({
						placeholder: config.pattern,
						columns: config.columns,
					})),
					imageConfig: imageConfigs.map((config) => ({
						shapeId: config.shapeId,
						columns: config.columns,
						roiMode: config.roiType ?? 'center',
					})),
					outputFolder: outputPath,
				});

				const jobId = job?.jobId ?? '';

				return {
					type: 'jobcreate',
					job: {
						jobId,
						jobType: 'Group',
						status: 'Pending',
						progress: 0,
						outputPath,
						errorCount: 0,
					},
					sheetJobIds: job?.sheetJobIds ?? {},
				};
			}
			case 'jobquery': {
				const jobId = payload.jobId as string | undefined;
				if (jobId) {
					const requestedJobType = payload.jobType as string | undefined;
					const jobIdParts = jobId.split(':');
					const sheetIndex =
						requestedJobType === 'Sheet' && jobId.includes(':')
							? Number(jobIdParts[jobIdParts.length - 1])
							: undefined;
					const targetJobId =
						requestedJobType === 'Sheet' && jobId.includes(':') ? jobId.split(':')[0] : jobId;
					const snapshot = await this.backendRequest<{
						jobId: string;
						status: string;
						progress: number;
						sheets?: Array<{
							sheetName: string;
							outputPath: string;
							currentRow: number;
							totalRows: number;
							status: string;
							error?: string | null;
						}>;
						outputFolder?: string;
						payloadJson?: string | null;
					} | null>('jobs.get', { jobId: targetJobId });

					if (!snapshot) return { type: 'jobquery', job: null, jobs: [] };

					const snapshotSheets = snapshot.sheets ?? [];
					const isGroup = !requestedJobType || requestedJobType === 'Group';
					if (isGroup) {
						const sheets = Object.fromEntries(
							snapshotSheets.map((sheet, index) => [
								`${snapshot.jobId}:${index}`,
								{
									jobType: 'Sheet',
									status: this.toLegacyStatus(sheet.status),
									progress: sheet.totalRows > 0 ? (sheet.currentRow * 100) / sheet.totalRows : 0,
									sheetName: sheet.sheetName,
									outputPath: sheet.outputPath,
									errorCount: sheet.error ? 1 : 0,
								},
							]),
						);

						return {
							type: 'jobquery',
							job: {
								jobId: snapshot.jobId,
								jobType: 'Group',
								status: this.toLegacyStatus(snapshot.status),
								progress: snapshot.progress,
								outputPath: snapshot.outputFolder ?? snapshotSheets[0]?.outputPath,
								outputFolder: snapshot.outputFolder,
								errorCount: snapshotSheets.filter((sheet) => !!sheet.error).length,
								sheets,
								payloadJson: snapshot.payloadJson,
							},
							jobs: null,
						};
					}

					const sheet = snapshotSheets[Number.isFinite(sheetIndex) ? (sheetIndex as number) : 0];
					return {
						type: 'jobquery',
						job: {
							jobId,
							jobType: 'Sheet',
							status: this.toLegacyStatus(sheet?.status ?? snapshot.status),
							progress: sheet?.totalRows
								? (sheet.currentRow * 100) / sheet.totalRows
								: snapshot.progress,
							sheetName: sheet?.sheetName,
							currentRow: sheet?.currentRow,
							totalRows: sheet?.totalRows,
							outputPath: sheet?.outputPath,
							errorMessage: sheet?.error ?? undefined,
							errorCount: sheet?.error ? 1 : 0,
						},
						jobs: null,
					};
				}

				const snapshots = await this.backendRequest<
					Array<{
						jobId: string;
						status: string;
						progress: number;
						outputFolder?: string;
						sheets?: Array<{ outputPath: string; error?: string | null }>;
					}>
				>('jobs.list');

				return {
					type: 'jobquery',
					job: null,
					jobs: (snapshots ?? []).map((snapshot) => {
						const sheets = snapshot.sheets ?? [];
						return {
							jobId: snapshot.jobId,
							jobType: 'Group',
							status: this.toLegacyStatus(snapshot.status),
							progress: snapshot.progress,
							outputPath: snapshot.outputFolder ?? sheets[0]?.outputPath,
							errorCount: sheets.filter((sheet) => !!sheet.error).length,
						};
					}),
				};
			}
			case 'jobcontrol': {
				const action = (payload.action as string | undefined)?.toLowerCase();
				const jobId = payload.jobId as string;
				const jobType = (payload.jobType as string | undefined)?.toLowerCase();
				const targetJobId =
					jobType === 'sheet' && jobId.includes(':') ? jobId.split(':')[0] : jobId;
				const method =
					action === 'pause' ? 'jobs.pause' : action === 'resume' ? 'jobs.resume' : 'jobs.cancel';
				await this.backendRequest(method, { jobId: targetJobId });
				return {
					type: 'jobcontrol',
					jobId,
					action: payload.action,
				};
			}
			case 'joblogs': {
				const jobId = payload.jobId as string;
				const result = await this.backendRequest<{ logs?: unknown[] }>('jobs.logs', { jobId });
				return {
					type: 'joblogs',
					jobId,
					logs: result.logs ?? [],
				};
			}
			default:
				loggers.jobs.warn(`Unsupported JSON-RPC job request type: ${type}`);
				return {
					type: 'error',
					kind: 'NotSupported',
					message: `Unsupported request type: ${type}`,
				};
		}
	}

	private async sendConfigRequest(
		payload: Record<string, unknown>,
		type: string,
	): Promise<unknown> {
		switch (type) {
			case 'get':
				return await this.backendRequest('config.get');
			case 'update':
				return await this.backendRequest('config.update', payload);
			case 'reload':
				return await this.backendRequest('config.reload');
			case 'reset':
				return await this.backendRequest('config.reset');
			case 'modelstatus':
				return await this.backendRequest('config.modelStatus');
			case 'modelcontrol':
				return await this.backendRequest('config.modelControl', {
					model: payload.Model,
					action: payload.Action,
				});
			default:
				return {
					type: 'error',
					kind: 'NotSupported',
					message: `Unsupported config request type: ${type}`,
				};
		}
	}

	private async sendSheetRequest(payload: Record<string, unknown>, type: string): Promise<unknown> {
		if (type === 'scan') {
			return await this.backendRequest('sheet.scan', payload);
		}

		return {
			type: 'error',
			kind: 'NotSupported',
			message: `Unsupported sheet request type: ${type}`,
		};
	}

	private handleJobUpdated(params: unknown): void {
		if (!params || typeof params !== 'object') return;

		const snapshot = params as Record<string, unknown>;
		const groupId = (snapshot.jobId as string | undefined) ?? '';
		if (!groupId) return;

		const groupStatus = this.toLegacyStatus((snapshot.status as string | undefined) ?? '');
		const groupProgress =
			typeof snapshot.progress === 'number'
				? (snapshot.progress as number)
				: Number(snapshot.progress ?? 0) || 0;

		const sheets = ((snapshot.sheets as Array<Record<string, unknown>>) ?? []) as Array<
			Record<string, unknown>
		>;
		const groupErrorCount = sheets.filter((sheet) => Boolean(sheet.error)).length;

		this.notificationHandlers.forEach((handler) =>
			handler({
				groupId,
				status: groupStatus,
				progress: groupProgress,
				errorCount: groupErrorCount,
			}),
		);

		sheets.forEach((sheet, index) => {
			const currentRow =
				typeof sheet.currentRow === 'number'
					? (sheet.currentRow as number)
					: Number(sheet.currentRow ?? 0) || 0;
			const totalRows =
				typeof sheet.totalRows === 'number'
					? (sheet.totalRows as number)
					: Number(sheet.totalRows ?? 0) || 0;
			const progress = totalRows > 0 ? (currentRow * 100) / totalRows : 0;
			const sheetStatus = this.toLegacyStatus((sheet.status as string | undefined) ?? '');
			const error = (sheet.error as string | null | undefined) ?? undefined;

			this.notificationHandlers.forEach((handler) =>
				handler({
					groupId,
					jobId: `${groupId}:${index}`,
					status: sheetStatus,
					currentRow,
					totalRows,
					progress,
					errorCount: error ? 1 : 0,
					error,
					message: error,
				}),
			);
		});
	}

	private toLegacyStatus(status: string): string {
		switch ((status ?? '').toLowerCase()) {
			case 'running':
				return 'Processing';
			case 'completed':
				return 'Done';
			case 'failed':
				return 'Error';
			case 'cancelled':
				return 'Cancelled';
			case 'paused':
				return 'Paused';
			default:
				return 'Pending';
		}
	}

	private async backendRequest<TResult = unknown>(
		method: string,
		params?: unknown,
	): Promise<TResult> {
		if (!window.desktopAPI?.backendRequest) {
			throw new Error(
				'Desktop API is unavailable. Run desktop mode (`task dev`) to access backend RPC.',
			);
		}
		return await window.desktopAPI.backendRequest<TResult>(method, params);
	}
}
