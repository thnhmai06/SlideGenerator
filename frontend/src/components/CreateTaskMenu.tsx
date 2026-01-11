import React, { useMemo, useState, useEffect, useRef, useCallback } from 'react';
import { useApp } from '../contexts/useApp';
import { useJobs } from '../contexts/useJobs';
import ShapeSelector from './ShapeSelector';
import TagInput from './TagInput';
import * as backendApi from '../services/backendApi';
import { getAssetPath } from '../utils/paths';
import '../styles/CreateTaskMenu.css';

interface CreateTaskMenuProps {
	onStart: () => void;
}

interface TextReplacement {
	id: number;
	placeholder: string;
	columns: string[];
}

interface ImageReplacement {
	id: number;
	shapeId: string;
	columns: string[];
	roiType: string;
	cropType: string;
}

interface Shape {
	id: string;
	name: string;
	preview: string;
}

interface SavedInputState {
	pptxPath?: string;
	dataPath?: string;
	savePath?: string;
	columns?: string[];
	shapes?: Shape[];
	placeholders?: string[];
	sheetCount?: number;
	totalRows?: number;
	templateLoaded?: boolean;
	dataLoaded?: boolean;
	textReplacements?: Array<{
		id?: number;
		searchText?: string;
		placeholder?: string;
		columns?: string[];
	}>;
	imageReplacements?: Array<{
		id?: number;
		shapeId?: string;
		columns?: string[];
		roiType?: string;
		cropType?: string;
	}>;
}

type NotificationState = {
	type: 'success' | 'error';
	text: string;
};

const STORAGE_KEYS = {
	inputMenuState: 'slidegen.ui.inputsideBar.state',
};

const loadSavedState = (): SavedInputState | null => {
	try {
		const saved = sessionStorage.getItem(STORAGE_KEYS.inputMenuState);
		if (saved) {
			return JSON.parse(saved) as SavedInputState;
		}
	} catch (error) {
		console.error('Error loading saved state:', error);
	}
	return null;
};

const mapTemplateShapes = (template: backendApi.SlideScanTemplateSuccess): Shape[] => {
	return (template.Shapes ?? [])
		.filter((shape) => shape.IsImage === true)
		.map((shape) => ({
			id: String(shape.Id),
			name: shape.Name,
			preview: shape.Data
				? `data:image/png;base64,${shape.Data}`
				: getAssetPath('images', 'app-icon.png'),
		}));
};

const mapTemplatePlaceholders = (template: backendApi.SlideScanTemplateSuccess): string[] => {
	const items = (template.Placeholders ?? [])
		.map((item) => item.trim())
		.filter((item) => item.length > 0);
	return Array.from(new Set(items)).sort((a, b) => a.localeCompare(b));
};

const loadTemplateAssets = async (
	filePath: string,
): Promise<{ shapes: Shape[]; placeholders: string[] }> => {
	const response = await backendApi.scanTemplate(filePath);
	const template = response as backendApi.SlideScanTemplateSuccess;
	return {
		shapes: mapTemplateShapes(template),
		placeholders: mapTemplatePlaceholders(template),
	};
};

const loadDataAssets = async (
	filePath: string,
): Promise<{ columns: string[]; sheetCount: number; totalRows: number }> => {
	await backendApi.loadFile(filePath);
	const columns = await backendApi.getAllColumns([filePath]);
	const workbookInfo = await backendApi.getWorkbookInfo(filePath);
	const workbookData = workbookInfo as backendApi.SheetWorkbookGetInfoSuccess;
	const sheetsInfo = workbookData.Sheets ?? [];
	const rowsSum = sheetsInfo.reduce((acc, sheet) => acc + (sheet.RowCount ?? 0), 0);
	return { columns, sheetCount: sheetsInfo.length, totalRows: rowsSum };
};

const mapTextReplacements = (
	savedState: SavedInputState,
	placeholders: string[],
	columns: string[],
): TextReplacement[] => {
	const placeholderSet = new Set(placeholders);
	const columnSet = new Set(columns);
	const importedText = (savedState.textReplacements || []).map((item) => ({
		id: item.id ?? 1,
		placeholder: item.placeholder || item.searchText || '',
		columns: item.columns || [],
	}));
	return importedText
		.map((item) => ({
			...item,
			placeholder: item.placeholder.trim(),
			columns: item.columns.filter((col) => columnSet.has(col)),
		}))
		.filter(
			(item) =>
				item.placeholder && item.columns.length > 0 && placeholderSet.has(item.placeholder),
		);
};

const mapImageReplacements = (
	savedState: SavedInputState,
	shapes: Shape[],
	columns: string[],
): ImageReplacement[] => {
	const shapeIdSet = new Set(shapes.map((shape) => shape.id));
	const columnSet = new Set(columns);
	const importedImages = (savedState.imageReplacements || []).map((item) => ({
		id: item.id ?? 1,
		shapeId: item.shapeId ?? '',
		columns: item.columns ?? [],
		roiType: item.roiType ?? 'Attention',
		cropType: item.cropType ?? 'Fit',
	}));
	return importedImages
		.map((item) => ({
			...item,
			shapeId: item.shapeId.trim(),
			columns: item.columns.filter((col) => columnSet.has(col)),
		}))
		.filter((item) => item.shapeId && item.columns.length > 0 && shapeIdSet.has(item.shapeId));
};

type ValidationState = {
	canConfigure: boolean;
	canStart: boolean;
	textShapeCount: number;
	imageShapeCount: number;
	uniqueColumnCount: number;
};

const resolvePath = (inputPath: string): string => {
	if (!inputPath) return inputPath;

	if (/^[a-zA-Z]:[/\\]/.test(inputPath) || inputPath.startsWith('/')) {
		return inputPath;
	}

	if (typeof process === 'undefined' || !process.cwd) {
		return inputPath;
	}

	const cwd = process.cwd();
	return `${cwd}\\${inputPath.replace(/\//g, '\\')}`;
};

const buildTextConfigs = (textReplacements: TextReplacement[]): backendApi.SlideTextConfig[] => {
	return textReplacements
		.filter((item) => item.placeholder.trim() && item.columns.length > 0)
		.map((item) => ({
			Pattern: item.placeholder.trim(),
			Columns: item.columns,
		}));
};

const buildImageConfigs = (
	imageReplacements: ImageReplacement[],
): backendApi.SlideImageConfig[] => {
	return imageReplacements
		.filter((item) => item.shapeId && item.columns.length > 0)
		.map((item) => ({
			ShapeId: Number(item.shapeId),
			Columns: item.columns,
			RoiType: item.roiType || 'Center',
			CropType: item.cropType || 'Crop',
		}))
		.filter((item) => Number.isFinite(item.ShapeId));
};

const resolveAvailablePlaceholders = (
	textReplacements: TextReplacement[],
	placeholders: string[],
	current: string,
): string[] => {
	const taken = new Set(
		textReplacements
			.map((item) => item.placeholder.trim())
			.filter((value) => value && value !== current),
	);
	return placeholders.filter((value) => !taken.has(value));
};

const resolveAvailableShapes = (
	imageReplacements: ImageReplacement[],
	shapes: Shape[],
	current: string,
): Shape[] => {
	const taken = new Set(
		imageReplacements
			.map((item) => item.shapeId.trim())
			.filter((value) => value && value !== current),
	);
	return shapes.filter((shape) => !taken.has(shape.id));
};

const computeValidationState = (args: {
	pptxPath: string;
	dataPath: string;
	savePath: string;
	isLoadingColumns: boolean;
	isLoadingShapes: boolean;
	isLoadingPlaceholders: boolean;
	placeholders: string[];
	shapes: Shape[];
	columns: string[];
	textReplacements: TextReplacement[];
	imageReplacements: ImageReplacement[];
}): ValidationState => {
	const templateExtPattern = /\.(pptx|potx)$/i;
	const sheetExtPattern = /\.(xlsx|xlsm)$/i;
	const isTemplateValid = Boolean(args.pptxPath && templateExtPattern.test(args.pptxPath));
	const isDataValid = Boolean(args.dataPath && sheetExtPattern.test(args.dataPath));
	const isOutputValid = Boolean(args.savePath && args.savePath.trim().length > 0);

	const canConfigure =
		isTemplateValid &&
		isDataValid &&
		!args.isLoadingColumns &&
		!args.isLoadingShapes &&
		!args.isLoadingPlaceholders;

	const placeholderSet = new Set(args.placeholders);
	const shapeIdSet = new Set(args.shapes.map((shape) => shape.id));

	const normalizedTextPlaceholders = args.textReplacements.map((item) => item.placeholder.trim());
	const usedTextPlaceholders = new Set(
		normalizedTextPlaceholders.filter((value) => value.length > 0),
	);
	const hasDuplicateTextPlaceholders =
		usedTextPlaceholders.size !==
		normalizedTextPlaceholders.filter((value) => value.length > 0).length;

	const normalizedShapeIds = args.imageReplacements.map((item) => item.shapeId.trim());
	const usedShapeIds = new Set(normalizedShapeIds.filter((value) => value.length > 0));
	const hasDuplicateShapeIds =
		usedShapeIds.size !== normalizedShapeIds.filter((value) => value.length > 0).length;

	const invalidTextItems = args.textReplacements.filter((item) => {
		const placeholder = item.placeholder.trim();
		if (!placeholder || item.columns.length === 0) return true;
		return !placeholderSet.has(placeholder);
	});

	const invalidImageItems = args.imageReplacements.filter((item) => {
		const shapeId = item.shapeId.trim();
		if (!shapeId || item.columns.length === 0) return true;
		return !shapeIdSet.has(shapeId);
	});

	const validTextCount = args.textReplacements.length - invalidTextItems.length;
	const validImageCount = args.imageReplacements.length - invalidImageItems.length;
	const hasAnyConfig = validTextCount + validImageCount > 0;

	const hasInvalidConfig =
		invalidTextItems.length > 0 ||
		invalidImageItems.length > 0 ||
		hasDuplicateTextPlaceholders ||
		hasDuplicateShapeIds;

	const canStart =
		isTemplateValid && isDataValid && isOutputValid && hasAnyConfig && !hasInvalidConfig;

	return {
		canConfigure,
		canStart,
		textShapeCount: args.placeholders.length,
		imageShapeCount: args.shapes.length,
		uniqueColumnCount: args.columns.length,
	};
};

const startJob = async (args: {
	pptxPath: string;
	dataPath: string;
	savePath: string;
	canStart: boolean;
	textReplacements: TextReplacement[];
	imageReplacements: ImageReplacement[];
	setPptxPath: React.Dispatch<React.SetStateAction<string>>;
	setDataPath: React.Dispatch<React.SetStateAction<string>>;
	setSavePath: React.Dispatch<React.SetStateAction<string>>;
	setIsStarting: React.Dispatch<React.SetStateAction<boolean>>;
	createGroup: ReturnType<typeof useJobs>['createGroup'];
	onStart: () => void;
	showNotification: (type: 'success' | 'error', text: string) => void;
	t: (key: string) => string;
}) => {
	const resolvedPptxPath = resolvePath(args.pptxPath);
	const resolvedDataPath = resolvePath(args.dataPath);
	const resolvedSavePath = resolvePath(args.savePath);

	if (!resolvedPptxPath || !resolvedDataPath || !resolvedSavePath || !args.canStart) {
		args.showNotification('error', args.t('createTask.error'));
		return;
	}

	const textConfigs = buildTextConfigs(args.textReplacements);
	const imageConfigs = buildImageConfigs(args.imageReplacements);

	args.setPptxPath(resolvedPptxPath);
	args.setDataPath(resolvedDataPath);
	args.setSavePath(resolvedSavePath);

	try {
		args.setIsStarting(true);
		await args.createGroup({
			templatePath: resolvedPptxPath,
			spreadsheetPath: resolvedDataPath,
			outputPath: resolvedSavePath,
			textConfigs,
			imageConfigs,
		});
		args.onStart();
	} catch (error) {
		console.error('Failed to start job:', error);
		const message = error instanceof Error ? error.message : args.t('createTask.error');
		args.showNotification('error', message);
	} finally {
		args.setIsStarting(false);
	}
};

const exportConfigToFile = async (args: {
	pptxPath: string;
	dataPath: string;
	savePath: string;
	columns: string[];
	textReplacements: TextReplacement[];
	imageReplacements: ImageReplacement[];
	showNotification: (type: 'success' | 'error', text: string) => void;
	t: (key: string) => string;
}) => {
	const usedColumns: string[] = [];
	const seen = new Set<string>();
	const addColumns = (values: string[]) => {
		values.forEach((value) => {
			if (!seen.has(value)) {
				seen.add(value);
				usedColumns.push(value);
			}
		});
	};
	args.textReplacements.forEach((item) => addColumns(item.columns));
	args.imageReplacements.forEach((item) => addColumns(item.columns));

	const config = {
		pptxPath: args.pptxPath,
		dataPath: args.dataPath,
		savePath: args.savePath,
		columns: usedColumns,
		textReplacements: args.textReplacements,
		imageReplacements: args.imageReplacements,
	};

	const path = await window.electronAPI.saveFile([
		{ name: 'JSON Files', extensions: ['json'] },
		{ name: 'All Files', extensions: ['*'] },
	]);

	if (!path) return;

	try {
		await window.electronAPI.writeSettings(path, JSON.stringify(config, null, 2));
		args.showNotification('success', args.t('createTask.exportSuccess'));
	} catch (_error) {
		args.showNotification('error', args.t('createTask.exportError'));
	}
};

const importConfigFromFile = async (args: {
	clearReplacements: () => void;
	setPptxPath: React.Dispatch<React.SetStateAction<string>>;
	setDataPath: React.Dispatch<React.SetStateAction<string>>;
	setSavePath: React.Dispatch<React.SetStateAction<string>>;
	setColumns: React.Dispatch<React.SetStateAction<string[]>>;
	setShapes: React.Dispatch<React.SetStateAction<Shape[]>>;
	setPlaceholders: React.Dispatch<React.SetStateAction<string[]>>;
	setSheetCount: React.Dispatch<React.SetStateAction<number>>;
	setTotalRows: React.Dispatch<React.SetStateAction<number>>;
	setTemplateLoaded: React.Dispatch<React.SetStateAction<boolean>>;
	setDataLoaded: React.Dispatch<React.SetStateAction<boolean>>;
	setTextReplacements: React.Dispatch<React.SetStateAction<TextReplacement[]>>;
	setImageReplacements: React.Dispatch<React.SetStateAction<ImageReplacement[]>>;
	setIsLoadingShapes: React.Dispatch<React.SetStateAction<boolean>>;
	setIsLoadingPlaceholders: React.Dispatch<React.SetStateAction<boolean>>;
	setIsLoadingColumns: React.Dispatch<React.SetStateAction<boolean>>;
	showNotification: (type: 'success' | 'error', text: string) => void;
	t: (key: string) => string;
}) => {
	const path = await window.electronAPI.openFile([
		{ name: 'JSON Files', extensions: ['json'] },
		{ name: 'All Files', extensions: ['*'] },
	]);

	if (!path) return;

	args.setIsLoadingShapes(true);
	args.setIsLoadingPlaceholders(true);
	args.setIsLoadingColumns(true);

	try {
		const data = await window.electronAPI.readSettings(path);
		if (!data) return;

		const config = JSON.parse(data) as SavedInputState;
		const nextPptxPath = config.pptxPath || '';
		const nextDataPath = config.dataPath || '';
		const nextSavePath = config.savePath || '';

		args.setPptxPath(nextPptxPath);
		args.setDataPath(nextDataPath);
		args.setSavePath(nextSavePath);
		args.setShapes([]);
		args.setPlaceholders([]);
		args.setColumns([]);
		args.setSheetCount(0);
		args.setTotalRows(0);
		args.setTemplateLoaded(false);
		args.setDataLoaded(false);
		args.clearReplacements();

		const templateAssets = nextPptxPath
			? await loadTemplateAssets(nextPptxPath)
			: { shapes: [], placeholders: [] };
		const dataAssets = nextDataPath
			? await loadDataAssets(nextDataPath)
			: { columns: [], sheetCount: 0, totalRows: 0 };

		if (nextPptxPath) {
			args.setTemplateLoaded(true);
		}
		if (nextDataPath) {
			args.setSheetCount(dataAssets.sheetCount);
			args.setTotalRows(dataAssets.totalRows);
			args.setDataLoaded(true);
		}

		const filteredText = mapTextReplacements(
			{ textReplacements: config.textReplacements },
			templateAssets.placeholders,
			dataAssets.columns,
		);
		const filteredImages = mapImageReplacements(
			{ imageReplacements: config.imageReplacements },
			templateAssets.shapes,
			dataAssets.columns,
		);

		args.setShapes(templateAssets.shapes);
		args.setPlaceholders(templateAssets.placeholders);
		args.setColumns(dataAssets.columns);
		args.setTextReplacements(filteredText);
		args.setImageReplacements(filteredImages);
		args.showNotification('success', args.t('createTask.importSuccess'));
	} catch (_error) {
		args.showNotification('error', args.t('createTask.importError'));
	} finally {
		args.setIsLoadingShapes(false);
		args.setIsLoadingPlaceholders(false);
		args.setIsLoadingColumns(false);
	}
};

const scheduleTemplateLoad = (args: {
	pptxPath: string;
	isHydrating: boolean;
	templateLoaded: boolean;
	lastLoadedTemplatePath: string;
	shapesLength: number;
	setShapes: React.Dispatch<React.SetStateAction<Shape[]>>;
	setPlaceholders: React.Dispatch<React.SetStateAction<string[]>>;
	setTemplateLoaded: React.Dispatch<React.SetStateAction<boolean>>;
	loadTemplateFromServer: (path: string) => Promise<void>;
}) => {
	if (args.isHydrating) return undefined;
	if (!args.pptxPath) {
		args.setShapes([]);
		args.setPlaceholders([]);
		args.setTemplateLoaded(false);
		return undefined;
	}

	if (
		args.templateLoaded &&
		args.lastLoadedTemplatePath === args.pptxPath &&
		args.shapesLength > 0
	) {
		return undefined;
	}

	args.setShapes([]);
	args.setPlaceholders([]);
	args.setTemplateLoaded(false);

	const timer = setTimeout(() => {
		args.loadTemplateFromServer(args.pptxPath).catch(() => undefined);
	}, 400);

	return () => clearTimeout(timer);
};

const scheduleDataLoad = (args: {
	dataPath: string;
	isHydrating: boolean;
	isLoadingColumns: boolean;
	dataLoaded: boolean;
	lastLoadedDataPath: string;
	setColumns: React.Dispatch<React.SetStateAction<string[]>>;
	setSheetCount: React.Dispatch<React.SetStateAction<number>>;
	setTotalRows: React.Dispatch<React.SetStateAction<number>>;
	setDataLoaded: React.Dispatch<React.SetStateAction<boolean>>;
	loadDataFromServer: (path: string) => Promise<void>;
}) => {
	if (args.isHydrating) return undefined;
	if (!args.dataPath) {
		args.setColumns([]);
		args.setSheetCount(0);
		args.setTotalRows(0);
		args.setDataLoaded(false);
		return undefined;
	}

	if (args.isLoadingColumns || (args.dataLoaded && args.lastLoadedDataPath === args.dataPath)) {
		return undefined;
	}

	args.setColumns([]);
	args.setSheetCount(0);
	args.setTotalRows(0);
	args.setDataLoaded(false);

	const timer = setTimeout(() => {
		args.loadDataFromServer(args.dataPath).catch(() => undefined);
	}, 400);

	return () => clearTimeout(timer);
};

type InputNotificationProps = {
	notification: NotificationState | null;
	isClosing: boolean;
	onClose: () => void;
	t: (key: string) => string;
};

const splitNotificationText = (text: string) => {
	const idx = text.indexOf(':');
	if (idx <= 0 || idx === text.length - 1) {
		return { title: text.trim(), detail: '' };
	}
	return {
		title: text.slice(0, idx).trim(),
		detail: text.slice(idx + 1).trim(),
	};
};

const InputNotification: React.FC<InputNotificationProps> = ({
	notification,
	isClosing,
	onClose,
	t,
}) => {
	if (!notification) return null;
	return (
		<div
			className={`app-notification message ${
				notification.type === 'error' ? 'message-error' : 'message-success'
			}${isClosing ? ' app-notification--closing' : ''}`}
		>
			{(() => {
				const { title, detail } = splitNotificationText(notification.text);
				return (
					<span className="notification-text">
						<span className="notification-title">{title}</span>
						{detail ? <span className="notification-detail">{detail}</span> : null}
					</span>
				);
			})()}
			<button
				type="button"
				className="notification-close"
				onClick={onClose}
				aria-label={t('common.close')}
			>
				<img
					src={getAssetPath('images', 'close.png')}
					alt=""
					className="notification-close__icon"
				/>
			</button>
		</div>
	);
};

type TextReplacementPanelProps = {
	canConfigure: boolean;
	showTextConfigs: boolean;
	setShowTextConfigs: React.Dispatch<React.SetStateAction<boolean>>;
	addTextReplacement: () => void;
	textReplacements: TextReplacement[];
	getAvailablePlaceholders: (current: string) => string[];
	updateTextReplacement: (
		id: number,
		key: 'placeholder' | 'columns',
		value: string | string[],
	) => void;
	removeTextReplacement: (id: number) => void;
	isLoadingPlaceholders: boolean;
	placeholders: string[];
	columns: string[];
	t: (key: string) => string;
};

const TextReplacementPanel: React.FC<TextReplacementPanelProps> = ({
	canConfigure,
	showTextConfigs,
	setShowTextConfigs,
	addTextReplacement,
	textReplacements,
	getAvailablePlaceholders,
	updateTextReplacement,
	removeTextReplacement,
	isLoadingPlaceholders,
	placeholders,
	columns,
	t,
}) => (
	<div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
		<div className="panel-header">
			<div className="panel-title">
				<button
					type="button"
					className="panel-title-toggle"
					onClick={() => setShowTextConfigs((prev) => !prev)}
					disabled={!canConfigure}
					aria-expanded={showTextConfigs}
				>
					<img
						src={getAssetPath('images', 'chevron-down.png')}
						alt=""
						className={`panel-title-icon ${showTextConfigs ? 'expanded' : ''}`}
					/>
					<h3>
						{t('replacement.textTitle')}{' '}
						<span className="panel-count">({textReplacements.length})</span>
					</h3>
				</button>
			</div>
			<button
				className="btn btn-success"
				onClick={addTextReplacement}
				disabled={!canConfigure || placeholders.length === 0}
			>
				+ {t('replacement.add')}
			</button>
		</div>
		<div className={`panel-content ${showTextConfigs ? 'is-open' : ''}`}>
			<div className="replacement-table replacement-table-text">
				<table className="replacement-table-grid">
					<colgroup>
						<col className="col-main" />
						<col className="col-main" />
						<col className="col-action" />
					</colgroup>
					<thead>
						<tr>
							<th>{t('replacement.searchText')}</th>
							<th>{t('replacement.column')}</th>
							<th className="cell-action">{t('replacement.delete')}</th>
						</tr>
					</thead>
					<tbody>
						{textReplacements.map((item) => {
							const available = getAvailablePlaceholders(item.placeholder);
							return (
								<tr key={item.id}>
									<td>
										<select
											className="table-input"
											value={item.placeholder}
											onChange={(e) =>
												updateTextReplacement(
													item.id,
													'placeholder',
													e.target.value,
												)
											}
											disabled={!canConfigure || isLoadingPlaceholders}
										>
											<option value="">
												{t('replacement.searchPlaceholder')}
											</option>
											{available.map((placeholder) => (
												<option key={placeholder} value={placeholder}>
													{placeholder}
												</option>
											))}
										</select>
									</td>
									<td>
										<TagInput
											value={item.columns}
											onChange={(tags) =>
												updateTextReplacement(item.id, 'columns', tags)
											}
											suggestions={columns}
											placeholder={t('replacement.columnPlaceholder')}
										/>
									</td>
									<td className="cell-action">
										<button
											className="delete-btn"
											onClick={() => removeTextReplacement(item.id)}
											title={t('replacement.delete')}
										>
											<img
												src={getAssetPath('images', 'remove.png')}
												alt="Delete"
												className="delete-icon"
											/>
										</button>
									</td>
								</tr>
							);
						})}
					</tbody>
				</table>
			</div>
		</div>
	</div>
);

type ImageReplacementPanelProps = {
	canConfigure: boolean;
	showImageConfigs: boolean;
	setShowImageConfigs: React.Dispatch<React.SetStateAction<boolean>>;
	addImageReplacement: () => void;
	imageReplacements: ImageReplacement[];
	shapes: Shape[];
	getAvailableShapes: (current: string) => Shape[];
	updateImageReplacement: (
		id: number,
		key: 'shapeId' | 'columns' | 'roiType' | 'cropType',
		value: string | string[],
	) => void;
	removeImageReplacement: (id: number) => void;
	roiOptions: Array<{ value: string; label: string; description: string }>;
	cropOptions: Array<{ value: string; label: string; description: string }>;
	getOptionDescription: (
		options: { value: string; description: string }[],
		value: string,
	) => string;
	columns: string[];
	openPreview: (shape: Shape) => void;
	t: (key: string) => string;
};

const ImageReplacementPanel: React.FC<ImageReplacementPanelProps> = ({
	canConfigure,
	showImageConfigs,
	setShowImageConfigs,
	addImageReplacement,
	imageReplacements,
	shapes,
	getAvailableShapes,
	updateImageReplacement,
	removeImageReplacement,
	roiOptions,
	cropOptions,
	getOptionDescription,
	columns,
	openPreview,
	t,
}) => (
	<div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
		<div className="panel-header">
			<div className="panel-title">
				<button
					type="button"
					className="panel-title-toggle"
					onClick={() => setShowImageConfigs((prev) => !prev)}
					disabled={!canConfigure}
					aria-expanded={showImageConfigs}
				>
					<img
						src={getAssetPath('images', 'chevron-down.png')}
						alt=""
						className={`panel-title-icon ${showImageConfigs ? 'expanded' : ''}`}
					/>
					<h3>
						{t('replacement.imageTitle')}{' '}
						<span className="panel-count">({imageReplacements.length})</span>
					</h3>
				</button>
			</div>
			<button
				className="btn btn-success"
				onClick={addImageReplacement}
				disabled={!canConfigure || shapes.length === 0}
			>
				+ {t('replacement.add')}
			</button>
		</div>
		<div className={`panel-content ${showImageConfigs ? 'is-open' : ''}`}>
			<div className="replacement-table replacement-table-image">
				<div className="shape-gallery">
					<div className="shape-gallery-header">{t('replacement.availableShapes')}</div>
					<div className="shape-gallery-list">
						{shapes.length === 0 ? (
							<div className="shape-gallery-empty">{t('replacement.noShapes')}</div>
						) : (
							shapes.map((shape) => (
								<button
									type="button"
									key={shape.id}
									className="shape-gallery-item"
									onClick={() => openPreview(shape)}
								>
									<img
										src={shape.preview}
										alt={shape.name}
										className="shape-gallery-preview"
									/>
									<div className="shape-gallery-info">
										<span className="shape-gallery-name">{shape.name}</span>
										<span className="shape-gallery-id">{shape.id}</span>
									</div>
								</button>
							))
						)}
					</div>
				</div>
				<table className="replacement-table-grid">
					<colgroup>
						<col className="col-main" />
						<col className="col-main" />
						<col className="col-narrow" />
						<col className="col-narrow" />
						<col className="col-action" />
					</colgroup>
					<thead>
						<tr>
							<th>{t('replacement.shape')}</th>
							<th>{t('replacement.column')}</th>
							<th>{t('replacement.roi')}</th>
							<th>{t('replacement.crop')}</th>
							<th className="cell-action">{t('replacement.delete')}</th>
						</tr>
					</thead>
					<tbody>
						{imageReplacements.map((item) => (
							<tr key={item.id}>
								<td>
									<ShapeSelector
										shapes={getAvailableShapes(item.shapeId)}
										value={item.shapeId}
										onChange={(shapeId) =>
											updateImageReplacement(item.id, 'shapeId', shapeId)
										}
										placeholder={t('replacement.shapePlaceholder')}
									/>
								</td>
								<td>
									<TagInput
										value={item.columns}
										onChange={(tags) =>
											updateImageReplacement(item.id, 'columns', tags)
										}
										suggestions={columns}
										placeholder={t('replacement.columnPlaceholder')}
									/>
								</td>
								<td>
									<div className="select-with-hint">
										<select
											className="table-input"
											value={item.roiType}
											onChange={(e) =>
												updateImageReplacement(
													item.id,
													'roiType',
													e.target.value,
												)
											}
											title={getOptionDescription(roiOptions, item.roiType)}
										>
											{roiOptions.map((option) => (
												<option key={option.value} value={option.value}>
													{option.label}
												</option>
											))}
										</select>
										<span className="select-hint">
											{getOptionDescription(roiOptions, item.roiType)}
										</span>
									</div>
								</td>
								<td>
									<div className="select-with-hint">
										<select
											className="table-input"
											value={item.cropType}
											onChange={(e) =>
												updateImageReplacement(
													item.id,
													'cropType',
													e.target.value,
												)
											}
											title={getOptionDescription(cropOptions, item.cropType)}
										>
											{cropOptions.map((option) => (
												<option key={option.value} value={option.value}>
													{option.label}
												</option>
											))}
										</select>
										<span className="select-hint">
											{getOptionDescription(cropOptions, item.cropType)}
										</span>
									</div>
								</td>
								<td className="cell-action">
									<button
										className="delete-btn"
										onClick={() => removeImageReplacement(item.id)}
										title={t('replacement.delete')}
									>
										<img
											src={getAssetPath('images', 'remove.png')}
											alt="Delete"
											className="delete-icon"
										/>
									</button>
								</td>
							</tr>
						))}
					</tbody>
				</table>
			</div>
		</div>
	</div>
);

type PreviewModalProps = {
	previewShape: Shape;
	previewClosing: boolean;
	closePreview: () => void;
	previewSize: { width: number; height: number } | null;
	previewZoom: number;
	previewOffset: { x: number; y: number };
	adjustPreviewZoom: (delta: number) => void;
	setPreviewZoom: (value: number) => void;
	handleSavePreview: () => void;
	togglePreviewZoom: () => void;
	handlePreviewPointerDown: (event: React.PointerEvent<HTMLImageElement>) => void;
	handlePreviewPointerMove: (event: React.PointerEvent<HTMLImageElement>) => void;
	handlePreviewPointerUp: (event: React.PointerEvent<HTMLImageElement>) => void;
	handlePreviewWheel: (event: React.WheelEvent<HTMLImageElement>) => void;
	setPreviewSize: (size: { width: number; height: number }) => void;
	dragMovedRef: React.MutableRefObject<boolean>;
	t: (key: string) => string;
};

const PreviewModal: React.FC<PreviewModalProps> = ({
	previewShape,
	previewClosing,
	closePreview,
	previewSize,
	previewZoom,
	previewOffset,
	adjustPreviewZoom,
	setPreviewZoom,
	handleSavePreview,
	togglePreviewZoom,
	handlePreviewPointerDown,
	handlePreviewPointerMove,
	handlePreviewPointerUp,
	handlePreviewWheel,
	setPreviewSize,
	dragMovedRef,
	t,
}) => (
	<div
		className={`shape-preview-overlay ${previewClosing ? 'is-closing' : ''}`}
		onClick={closePreview}
	>
		<div
			className={`shape-preview-modal ${previewClosing ? 'is-closing' : ''}`}
			onClick={(event) => event.stopPropagation()}
		>
			<div className="shape-preview-header">
				<div className="shape-preview-title">{t('createTask.previewTitle')}</div>
				<button className="shape-preview-close" onClick={closePreview}>
					{t('common.close')}
				</button>
			</div>
			<div className="shape-preview-meta">
				<span className="shape-preview-name">{previewShape.name}</span>
				<span className="shape-preview-id">ID: {previewShape.id}</span>
				<span className="shape-preview-size">
					{t('createTask.previewSize')}:{' '}
					{previewSize ? `${previewSize.width}x${previewSize.height}px` : '...'}
				</span>
			</div>
			<div className="shape-preview-actions">
				<button className="shape-preview-btn" onClick={() => adjustPreviewZoom(-0.1)}>
					-
				</button>
				<span className="shape-preview-zoom">
					{t('createTask.previewZoom')}: {Math.round(previewZoom * 100)}%
				</span>
				<button className="shape-preview-btn" onClick={() => adjustPreviewZoom(0.1)}>
					+
				</button>
				<button className="shape-preview-btn" onClick={() => setPreviewZoom(1)}>
					{t('createTask.previewReset')}
				</button>
				<button className="shape-preview-btn" onClick={handleSavePreview}>
					<img
						src={getAssetPath('images', 'download.png')}
						alt=""
						className="shape-preview-icon"
					/>
					{t('createTask.previewSave')}
				</button>
			</div>
			<div className="shape-preview-body">
				<div className="shape-preview-frame">
					<img
						src={previewShape.preview}
						alt={previewShape.name}
						className={`shape-preview-image ${previewZoom > 1 ? 'zoomed' : ''}`}
						style={{
							transform: `translate(${previewOffset.x}px, ${previewOffset.y}px) scale(${previewZoom})`,
						}}
						onClick={() => {
							if (!dragMovedRef.current) {
								togglePreviewZoom();
							}
							dragMovedRef.current = false;
						}}
						onPointerDown={handlePreviewPointerDown}
						onPointerMove={handlePreviewPointerMove}
						onPointerUp={handlePreviewPointerUp}
						onPointerLeave={handlePreviewPointerUp}
						onWheel={handlePreviewWheel}
						draggable={false}
						onLoad={(event) => {
							const target = event.currentTarget;
							setPreviewSize({
								width: target.naturalWidth,
								height: target.naturalHeight,
							});
						}}
					/>
				</div>
			</div>
		</div>
	</div>
);

const CreateTaskMenu: React.FC<CreateTaskMenuProps> = ({ onStart }) => {
	const { t } = useApp();
	const { createGroup } = useJobs();

	const roiOptions = [
		{
			value: 'Attention',
			label: t('replacement.roiAttention'),
			description: t('replacement.roiAttentionDesc'),
		},
		{
			value: 'Prominent',
			label: t('replacement.roiProminent'),
			description: t('replacement.roiProminentDesc'),
		},
		{
			value: 'Center',
			label: t('replacement.roiCenter'),
			description: t('replacement.roiCenterDesc'),
		},
	];

	const cropOptions = [
		{
			value: 'Crop',
			label: t('replacement.cropCrop'),
			description: t('replacement.cropCropDesc'),
		},
		{
			value: 'Fit',
			label: t('replacement.cropFit'),
			description: t('replacement.cropFitDesc'),
		},
	];

	const getOptionDescription = (
		options: { value: string; description: string }[],
		value: string,
	) => {
		return options.find((option) => option.value === value)?.description ?? '';
	};

	const savedState = useMemo(() => loadSavedState(), []);

	const [pptxPath, setPptxPath] = useState(savedState?.pptxPath || '');
	const [dataPath, setDataPath] = useState(savedState?.dataPath || '');
	const [savePath, setSavePath] = useState(savedState?.savePath || '');
	const [columns, setColumns] = useState<string[]>(savedState?.columns || []);
	const [isLoadingColumns, setIsLoadingColumns] = useState(false);
	const [isLoadingShapes, setIsLoadingShapes] = useState(false);
	const [isLoadingPlaceholders, setIsLoadingPlaceholders] = useState(false);
	const [isStarting, setIsStarting] = useState(false);
	const [showTextConfigs, setShowTextConfigs] = useState(false);
	const [showImageConfigs, setShowImageConfigs] = useState(false);
	const [previewShape, setPreviewShape] = useState<Shape | null>(null);
	const [previewClosing, setPreviewClosing] = useState(false);
	const [previewZoom, setPreviewZoom] = useState(1);
	const [previewSize, setPreviewSize] = useState<{ width: number; height: number } | null>(null);
	const [previewOffset, setPreviewOffset] = useState({ x: 0, y: 0 });
	const isDraggingRef = useRef(false);
	const dragStartRef = useRef({ x: 0, y: 0 });
	const dragMovedRef = useRef(false);
	const [notification, setNotification] = useState<NotificationState | null>(null);
	const [isNotificationClosing, setIsBannerClosing] = useState(false);
	const notificationHideTimeoutRef = useRef<number | null>(null);
	const notificationCloseTimeoutRef = useRef<number | null>(null);
	const isHydratingRef = useRef(false);
	const hasHydratedRef = useRef(false);
	const templateErrorAtRef = useRef(0);
	const pptxPathRef = useRef(pptxPath);
	const dataErrorAtRef = useRef(0);
	const dataPathRef = useRef(dataPath);
	const lastLoadedDataPathRef = useRef(savedState?.dataLoaded ? savedState?.dataPath || '' : '');
	const lastLoadedTemplatePathRef = useRef(
		savedState?.templateLoaded ? savedState?.pptxPath || '' : '',
	);

	const [shapes, setShapes] = useState<Shape[]>(savedState?.shapes || []);
	const [placeholders, setPlaceholders] = useState<string[]>(savedState?.placeholders || []);
	const [sheetCount, setSheetCount] = useState(savedState?.sheetCount || 0);
	const [totalRows, setTotalRows] = useState(savedState?.totalRows || 0);
	const [templateLoaded, setTemplateLoaded] = useState(Boolean(savedState?.templateLoaded));
	const [dataLoaded, setDataLoaded] = useState(Boolean(savedState?.dataLoaded));

	const [textReplacements, setTextReplacements] = useState<TextReplacement[]>(
		savedState?.textReplacements?.map(
			(item: {
				id?: number;
				searchText?: string;
				placeholder?: string;
				columns?: string[];
			}) => ({
				id: item.id ?? 1,
				placeholder: item.placeholder || item.searchText || '',
				columns: item.columns || [],
			}),
		) || [],
	);
	const [imageReplacements, setImageReplacements] = useState<ImageReplacement[]>(
		(savedState?.imageReplacements ?? []).map(
			(item: {
				id?: number;
				shapeId?: string;
				columns?: string[];
				roiType?: string;
				cropType?: string;
			}) => ({
				id: item.id ?? 1,
				shapeId: item.shapeId ?? '',
				columns: item.columns ?? [],
				roiType: item.roiType ?? 'Attention',
				cropType: item.cropType ?? 'Fit',
			}),
		),
	);

	useEffect(() => {
		localStorage.removeItem('config');
	}, []);

	// Save state to sessionStorage whenever it changes
	useEffect(() => {
		const state = {
			pptxPath,
			dataPath,
			savePath,
			textReplacements,
			imageReplacements,
			shapes,
			placeholders,
			columns,
			sheetCount,
			totalRows,
			templateLoaded,
			dataLoaded,
		};
		sessionStorage.setItem(STORAGE_KEYS.inputMenuState, JSON.stringify(state));
	}, [
		pptxPath,
		dataPath,
		savePath,
		textReplacements,
		imageReplacements,
		shapes,
		placeholders,
		columns,
		sheetCount,
		totalRows,
		templateLoaded,
		dataLoaded,
	]);

	useEffect(() => {
		pptxPathRef.current = pptxPath;
	}, [pptxPath]);

	useEffect(() => {
		dataPathRef.current = dataPath;
	}, [dataPath]);

	const getErrorDetail = useCallback((error: unknown): string => {
		if (error instanceof Error && error.message) return error.message;
		if (typeof error === 'string') return error;
		if (error && typeof error === 'object' && 'message' in error) {
			const value = (error as { message?: string }).message;
			if (value) return value;
		}
		return '';
	}, []);

	const formatErrorMessage = useCallback(
		(key: string, error: unknown): string => {
			const detail = getErrorDetail(error);
			return detail ? `${t(key)}: ${detail}` : t(key);
		},
		[getErrorDetail, t],
	);

	const clearNotificationTimeouts = useCallback(() => {
		if (notificationHideTimeoutRef.current) {
			window.clearTimeout(notificationHideTimeoutRef.current);
			notificationHideTimeoutRef.current = null;
		}
		if (notificationCloseTimeoutRef.current) {
			window.clearTimeout(notificationCloseTimeoutRef.current);
			notificationCloseTimeoutRef.current = null;
		}
	}, []);

	const hideNotification = useCallback(() => {
		clearNotificationTimeouts();
		setIsBannerClosing(true);
		notificationCloseTimeoutRef.current = window.setTimeout(() => {
			setNotification(null);
			setIsBannerClosing(false);
			notificationCloseTimeoutRef.current = null;
		}, 180);
	}, [clearNotificationTimeouts]);

	const showNotification = useCallback(
		(type: 'success' | 'error', text: string) => {
			clearNotificationTimeouts();
			setNotification({ type, text });
			setIsBannerClosing(false);
			notificationHideTimeoutRef.current = window.setTimeout(() => {
				hideNotification();
				notificationHideTimeoutRef.current = null;
			}, 4000);
		},
		[clearNotificationTimeouts, hideNotification],
	);

	const notifyTemplateError = useCallback(
		(error: unknown) => {
			const now = Date.now();
			if (now - templateErrorAtRef.current < 800) return;
			templateErrorAtRef.current = now;
			showNotification('error', formatErrorMessage('createTask.templateLoadError', error));
		},
		[formatErrorMessage, showNotification],
	);

	const notifyDataError = useCallback(
		(error: unknown) => {
			const now = Date.now();
			if (now - dataErrorAtRef.current < 800) return;
			dataErrorAtRef.current = now;
			showNotification('error', formatErrorMessage('createTask.columnLoadError', error));
		},
		[formatErrorMessage, showNotification],
	);

	const applySavedStateBasics = (state: SavedInputState) => {
		const nextPptxPath = state.pptxPath || '';
		const nextDataPath = state.dataPath || '';
		const nextSavePath = state.savePath || '';

		setPptxPath(nextPptxPath);
		setDataPath(nextDataPath);
		setSavePath(nextSavePath);

		const cached = {
			shapes: state.shapes || [],
			placeholders: state.placeholders || [],
			columns: state.columns || [],
			sheetCount: state.sheetCount || 0,
			totalRows: state.totalRows || 0,
			templateLoaded: state.templateLoaded || false,
			dataLoaded: state.dataLoaded || false,
		};

		setShapes(cached.shapes);
		setPlaceholders(cached.placeholders);
		setColumns(cached.columns);
		setSheetCount(cached.sheetCount);
		setTotalRows(cached.totalRows);
		setTemplateLoaded(cached.templateLoaded);
		setDataLoaded(cached.dataLoaded);
		lastLoadedTemplatePathRef.current = cached.templateLoaded ? nextPptxPath : '';
		lastLoadedDataPathRef.current = cached.dataLoaded ? nextDataPath : '';

		return { nextPptxPath, nextDataPath, cached };
	};

	const resolveHydrationAssets = async (
		nextPptxPath: string,
		nextDataPath: string,
		cached: {
			shapes: Shape[];
			placeholders: string[];
			columns: string[];
			sheetCount: number;
			totalRows: number;
			templateLoaded: boolean;
			dataLoaded: boolean;
		},
	) => {
		const templateAssets =
			!cached.templateLoaded && nextPptxPath
				? await loadTemplateAssets(nextPptxPath)
				: { shapes: cached.shapes, placeholders: cached.placeholders };
		const dataAssets =
			!cached.dataLoaded && nextDataPath
				? await loadDataAssets(nextDataPath)
				: {
						columns: cached.columns,
						sheetCount: cached.sheetCount,
						totalRows: cached.totalRows,
					};

		if (!cached.templateLoaded && nextPptxPath) {
			setTemplateLoaded(true);
			lastLoadedTemplatePathRef.current = nextPptxPath;
		}
		if (!cached.dataLoaded && nextDataPath) {
			setSheetCount(dataAssets.sheetCount);
			setTotalRows(dataAssets.totalRows);
			setDataLoaded(true);
			lastLoadedDataPathRef.current = nextDataPath;
		}

		return { templateAssets, dataAssets };
	};

	const hydrateFromSavedState = useCallback(async () => {
		if (!savedState) return;
		if (hasHydratedRef.current) return;
		hasHydratedRef.current = true;
		isHydratingRef.current = true;
		const { nextPptxPath, nextDataPath, cached } = applySavedStateBasics(savedState);

		setIsLoadingShapes(true);
		setIsLoadingPlaceholders(true);
		setIsLoadingColumns(true);

		try {
			const { templateAssets, dataAssets } = await resolveHydrationAssets(
				nextPptxPath,
				nextDataPath,
				cached,
			);
			const filteredText = mapTextReplacements(
				savedState,
				templateAssets.placeholders,
				dataAssets.columns,
			);
			const filteredImages = mapImageReplacements(
				savedState,
				templateAssets.shapes,
				dataAssets.columns,
			);

			setShapes(templateAssets.shapes);
			setPlaceholders(templateAssets.placeholders);
			setColumns(dataAssets.columns);
			setTextReplacements(filteredText);
			setImageReplacements(filteredImages);
		} catch (error) {
			showNotification('error', formatErrorMessage('createTask.restoreError', error));
		} finally {
			setIsLoadingShapes(false);
			setIsLoadingPlaceholders(false);
			setIsLoadingColumns(false);
			isHydratingRef.current = false;
		}
	}, [
		applySavedStateBasics,
		formatErrorMessage,
		resolveHydrationAssets,
		savedState,
		showNotification,
	]);

	useEffect(() => {
		hydrateFromSavedState().catch(() => undefined);
	}, [hydrateFromSavedState]);

	const loadTemplateFromServer = useCallback(
		async (filePath: string) => {
			if (!filePath) {
				setShapes([]);
				setPlaceholders([]);
				setTemplateLoaded(false);
				return;
			}
			setIsLoadingShapes(true);
			setIsLoadingPlaceholders(true);
			try {
				const response = await backendApi.scanTemplate(filePath);
				const data = response as backendApi.SlideScanTemplateSuccess;
				const mappedShapes = (data.Shapes ?? [])
					.filter((shape) => shape.IsImage === true)
					.map((shape) => ({
						id: String(shape.Id),
						name: shape.Name,
						preview: shape.Data
							? `data:image/png;base64,${shape.Data}`
							: getAssetPath('images', 'app-icon.png'),
					}));
				setShapes(mappedShapes);
				lastLoadedTemplatePathRef.current = filePath;

				const items = (data.Placeholders ?? [])
					.map((item) => item.trim())
					.filter((item) => item.length > 0);
				const unique = Array.from(new Set(items));
				unique.sort((a, b) => a.localeCompare(b));
				setPlaceholders(unique);
				setTemplateLoaded(true);
			} catch (error) {
				if (!isHydratingRef.current && filePath === pptxPathRef.current) {
					notifyTemplateError(error);
					setPptxPath('');
					setShapes([]);
					setPlaceholders([]);
					setTemplateLoaded(false);
				}
			} finally {
				setIsLoadingShapes(false);
				setIsLoadingPlaceholders(false);
			}
		},
		[notifyTemplateError],
	);

	const loadDataFromServer = useCallback(
		async (filePath: string) => {
			if (!filePath) {
				setColumns([]);
				setSheetCount(0);
				setTotalRows(0);
				setDataLoaded(false);
				return;
			}

			setIsLoadingColumns(true);
			try {
				await backendApi.loadFile(filePath);
				const allColumns = await backendApi.getAllColumns([filePath]);
				const workbookInfo = await backendApi.getWorkbookInfo(filePath);
				const workbookData = workbookInfo as backendApi.SheetWorkbookGetInfoSuccess;
				const sheetsInfo = workbookData.Sheets ?? [];
				const rowsSum = sheetsInfo.reduce((acc, sheet) => acc + (sheet.RowCount ?? 0), 0);

				setColumns(allColumns);
				setSheetCount(sheetsInfo.length);
				setTotalRows(rowsSum);
				setDataLoaded(true);
				lastLoadedDataPathRef.current = filePath;
			} catch (error) {
				if (!isHydratingRef.current && filePath === dataPathRef.current) {
					notifyDataError(error);
					setDataPath('');
					setColumns([]);
					setSheetCount(0);
					setTotalRows(0);
					setDataLoaded(false);
				}
			} finally {
				setIsLoadingColumns(false);
			}
		},
		[notifyDataError],
	);

	useEffect(
		() =>
			scheduleTemplateLoad({
				pptxPath,
				isHydrating: isHydratingRef.current,
				templateLoaded,
				lastLoadedTemplatePath: lastLoadedTemplatePathRef.current,
				shapesLength: shapes.length,
				setShapes,
				setPlaceholders,
				setTemplateLoaded,
				loadTemplateFromServer,
			}),
		[pptxPath, templateLoaded, shapes.length, loadTemplateFromServer],
	);

	useEffect(
		() =>
			scheduleDataLoad({
				dataPath,
				isHydrating: isHydratingRef.current,
				isLoadingColumns,
				dataLoaded,
				lastLoadedDataPath: lastLoadedDataPathRef.current,
				setColumns,
				setSheetCount,
				setTotalRows,
				setDataLoaded,
				loadDataFromServer,
			}),
		[dataPath, dataLoaded, isLoadingColumns, loadDataFromServer],
	);

	const handleBrowsePptx = async () => {
		const path = await window.electronAPI.openFile([
			{ name: 'PowerPoint Files', extensions: ['pptx', 'potx'] },
		]);
		if (path) {
			setPptxPath(path);
			setShapes([]);
			setPlaceholders([]);
		}
	};

	const handleBrowseData = async () => {
		const path = await window.electronAPI.openFile([
			{ name: 'Spreadsheets Files', extensions: ['xlsx', 'xlsm'] },
		]);

		if (path) {
			setDataPath(path);
		}
	};

	const handleBrowseSave = async () => {
		const path = await window.electronAPI.openFolder();
		if (path) setSavePath(path);
	};

	const addTextReplacement = () => {
		setTextReplacements([
			...textReplacements,
			{
				id: textReplacements.length + 1,
				placeholder: '',
				columns: [],
			},
		]);
	};

	const removeTextReplacement = (id: number) => {
		setTextReplacements(textReplacements.filter((item) => item.id !== id));
	};

	const updateTextReplacement = (
		id: number,
		field: 'placeholder' | 'columns',
		value: string | string[],
	) => {
		setTextReplacements(
			textReplacements.map((item) => (item.id === id ? { ...item, [field]: value } : item)),
		);
	};

	const addImageReplacement = () => {
		setImageReplacements([
			...imageReplacements,
			{
				id: imageReplacements.length + 1,
				shapeId: '',
				columns: [],
				roiType: 'Attention',
				cropType: 'Fit',
			},
		]);
	};

	const removeImageReplacement = (id: number) => {
		setImageReplacements(imageReplacements.filter((item) => item.id !== id));
	};

	const updateImageReplacement = (
		id: number,
		field: 'shapeId' | 'columns' | 'roiType' | 'cropType',
		value: string | string[],
	) => {
		setImageReplacements(
			imageReplacements.map((item) => (item.id === id ? { ...item, [field]: value } : item)),
		);
	};

	const clearReplacements = () => {
		setTextReplacements([]);
		setImageReplacements([]);
	};

	const exportConfig = useCallback(
		() =>
			exportConfigToFile({
				pptxPath,
				dataPath,
				savePath,
				columns,
				textReplacements,
				imageReplacements,
				showNotification,
				t,
			}),
		[
			pptxPath,
			dataPath,
			savePath,
			columns,
			textReplacements,
			imageReplacements,
			showNotification,
			t,
		],
	);

	const importConfig = useCallback(
		() =>
			importConfigFromFile({
				clearReplacements,
				setPptxPath,
				setDataPath,
				setSavePath,
				setColumns,
				setShapes,
				setPlaceholders,
				setSheetCount,
				setTotalRows,
				setTemplateLoaded,
				setDataLoaded,
				setTextReplacements,
				setImageReplacements,
				setIsLoadingShapes,
				setIsLoadingPlaceholders,
				setIsLoadingColumns,
				showNotification,
				t,
			}),
		[
			clearReplacements,
			setPptxPath,
			setDataPath,
			setSavePath,
			setColumns,
			setShapes,
			setPlaceholders,
			setSheetCount,
			setTotalRows,
			setTemplateLoaded,
			setDataLoaded,
			setTextReplacements,
			setImageReplacements,
			setIsLoadingShapes,
			setIsLoadingPlaceholders,
			setIsLoadingColumns,
			showNotification,
			t,
		],
	);

	const clearAll = () => {
		if (confirm(t('createTask.confirmClear') || 'Are you sure you want to clear all data?')) {
			setPptxPath('');
			setDataPath('');
			setSavePath('');
			setColumns([]);
			setShapes([]);
			setPlaceholders([]);
			clearReplacements();
			lastLoadedTemplatePathRef.current = '';
			lastLoadedDataPathRef.current = '';
			sessionStorage.removeItem(STORAGE_KEYS.inputMenuState);
		}
	};

	const openPreview = (shape: Shape) => {
		setPreviewShape(shape);
		setPreviewClosing(false);
		setPreviewZoom(1);
		setPreviewSize(null);
		setPreviewOffset({ x: 0, y: 0 });
	};

	const closePreview = () => {
		setPreviewClosing(true);
		setTimeout(() => {
			setPreviewShape(null);
			setPreviewClosing(false);
		}, 180);
	};

	const adjustPreviewZoom = (delta: number) => {
		setPreviewZoom((prev) => {
			const next = Math.min(3, Math.max(0.5, Number((prev + delta).toFixed(2))));
			if (next === 1) {
				setPreviewOffset({ x: 0, y: 0 });
			}
			return next;
		});
	};

	const togglePreviewZoom = () => {
		setPreviewZoom((prev) => {
			const next = prev === 1 ? 2 : 1;
			if (next === 1) {
				setPreviewOffset({ x: 0, y: 0 });
			}
			return next;
		});
	};

	const handlePreviewPointerDown = (event: React.PointerEvent<HTMLImageElement>) => {
		if (previewZoom <= 1) return;
		if (event.button !== 0) return;
		isDraggingRef.current = true;
		dragMovedRef.current = false;
		dragStartRef.current = {
			x: event.clientX - previewOffset.x,
			y: event.clientY - previewOffset.y,
		};
		event.currentTarget.setPointerCapture(event.pointerId);
	};

	const handlePreviewPointerMove = (event: React.PointerEvent<HTMLImageElement>) => {
		if (!isDraggingRef.current) return;
		const nextX = event.clientX - dragStartRef.current.x;
		const nextY = event.clientY - dragStartRef.current.y;
		if (!dragMovedRef.current) {
			const dx = Math.abs(nextX - previewOffset.x);
			const dy = Math.abs(nextY - previewOffset.y);
			if (dx > 2 || dy > 2) {
				dragMovedRef.current = true;
			}
		}
		setPreviewOffset({ x: nextX, y: nextY });
	};

	const handlePreviewPointerUp = (event: React.PointerEvent<HTMLImageElement>) => {
		isDraggingRef.current = false;
		event.currentTarget.releasePointerCapture(event.pointerId);
	};

	const handlePreviewWheel = (event: React.WheelEvent<HTMLImageElement>) => {
		event.preventDefault();
		const delta = event.deltaY > 0 ? -0.1 : 0.1;
		adjustPreviewZoom(delta);
	};

	const handleSavePreview = async () => {
		if (!previewShape) return;
		try {
			const response = await fetch(previewShape.preview);
			const blob = await response.blob();
			const url = URL.createObjectURL(blob);
			const link = document.createElement('a');
			link.href = url;
			link.download = `${previewShape.name || 'shape'}.png`;
			document.body.appendChild(link);
			link.click();
			link.remove();
			URL.revokeObjectURL(url);
		} catch (error) {
			console.error('Failed to save preview image:', error);
		}
	};

	const { canConfigure, canStart, textShapeCount, imageShapeCount, uniqueColumnCount } = useMemo(
		() =>
			computeValidationState({
				pptxPath,
				dataPath,
				savePath,
				isLoadingColumns,
				isLoadingShapes,
				isLoadingPlaceholders,
				placeholders,
				shapes,
				columns,
				textReplacements,
				imageReplacements,
			}),
		[
			pptxPath,
			dataPath,
			savePath,
			isLoadingColumns,
			isLoadingShapes,
			isLoadingPlaceholders,
			placeholders,
			shapes,
			columns,
			textReplacements,
			imageReplacements,
		],
	);

	const getAvailablePlaceholders = useCallback(
		(current: string) => resolveAvailablePlaceholders(textReplacements, placeholders, current),
		[textReplacements, placeholders],
	);

	const getAvailableShapes = useCallback(
		(current: string) => resolveAvailableShapes(imageReplacements, shapes, current),
		[imageReplacements, shapes],
	);

	const handleStart = useCallback(
		() =>
			startJob({
				pptxPath,
				dataPath,
				savePath,
				canStart,
				textReplacements,
				imageReplacements,
				setPptxPath,
				setDataPath,
				setSavePath,
				setIsStarting,
				createGroup,
				onStart,
				showNotification,
				t,
			}),
		[
			pptxPath,
			dataPath,
			savePath,
			canStart,
			textReplacements,
			imageReplacements,
			setPptxPath,
			setDataPath,
			setSavePath,
			setIsStarting,
			createGroup,
			onStart,
			showNotification,
			t,
		],
	);

	return (
		<div className="input-menu">
			<MenuHeader onImport={importConfig} onExport={exportConfig} onClear={clearAll} t={t} />

			<InputNotification
				notification={notification}
				isClosing={isNotificationClosing}
				onClose={hideNotification}
				t={t}
			/>

			{/* File Inputs */}
			<TemplateInputSection
				pptxPath={pptxPath}
				onChangePath={setPptxPath}
				onBrowse={handleBrowsePptx}
				isLoadingShapes={isLoadingShapes}
				isLoadingPlaceholders={isLoadingPlaceholders}
				templateLoaded={templateLoaded}
				textShapeCount={textShapeCount}
				imageShapeCount={imageShapeCount}
				t={t}
			/>

			<DataInputSection
				dataPath={dataPath}
				onChangePath={setDataPath}
				onBrowse={handleBrowseData}
				isLoadingColumns={isLoadingColumns}
				dataLoaded={dataLoaded}
				sheetCount={sheetCount}
				uniqueColumnCount={uniqueColumnCount}
				totalRows={totalRows}
				t={t}
			/>

			{/* Replacement Tables - Separated */}
			<div className="replacement-section-separated">
				<TextReplacementPanel
					canConfigure={canConfigure}
					showTextConfigs={showTextConfigs}
					setShowTextConfigs={setShowTextConfigs}
					addTextReplacement={addTextReplacement}
					textReplacements={textReplacements}
					getAvailablePlaceholders={getAvailablePlaceholders}
					updateTextReplacement={updateTextReplacement}
					removeTextReplacement={removeTextReplacement}
					isLoadingPlaceholders={isLoadingPlaceholders}
					placeholders={placeholders}
					columns={columns}
					t={t}
				/>

				<ImageReplacementPanel
					canConfigure={canConfigure}
					showImageConfigs={showImageConfigs}
					setShowImageConfigs={setShowImageConfigs}
					addImageReplacement={addImageReplacement}
					imageReplacements={imageReplacements}
					shapes={shapes}
					getAvailableShapes={getAvailableShapes}
					updateImageReplacement={updateImageReplacement}
					removeImageReplacement={removeImageReplacement}
					roiOptions={roiOptions}
					cropOptions={cropOptions}
					getOptionDescription={getOptionDescription}
					columns={columns}
					openPreview={openPreview}
					t={t}
				/>
			</div>

			<SaveLocationSection
				savePath={savePath}
				onChangePath={setSavePath}
				onBrowse={handleBrowseSave}
				t={t}
			/>

			<StartButtonSection
				isStarting={isStarting}
				canStart={canStart}
				onStart={handleStart}
				t={t}
			/>

			{previewShape && (
				<PreviewModal
					previewShape={previewShape}
					previewClosing={previewClosing}
					closePreview={closePreview}
					previewSize={previewSize}
					previewZoom={previewZoom}
					previewOffset={previewOffset}
					adjustPreviewZoom={adjustPreviewZoom}
					setPreviewZoom={setPreviewZoom}
					handleSavePreview={handleSavePreview}
					togglePreviewZoom={togglePreviewZoom}
					handlePreviewPointerDown={handlePreviewPointerDown}
					handlePreviewPointerMove={handlePreviewPointerMove}
					handlePreviewPointerUp={handlePreviewPointerUp}
					handlePreviewWheel={handlePreviewWheel}
					setPreviewSize={setPreviewSize}
					dragMovedRef={dragMovedRef}
					t={t}
				/>
			)}
		</div>
	);
};

type MenuHeaderProps = {
	onImport: () => void;
	onExport: () => void;
	onClear: () => void;
	t: (key: string) => string;
};

const MenuHeader: React.FC<MenuHeaderProps> = ({ onImport, onExport, onClear, t }) => (
	<div className="menu-header">
		<h1 className="menu-title">{t('createTask.title')}</h1>
		<div className="config-actions">
			<button
				className="btn btn-secondary"
				onClick={onImport}
				title={t('createTask.importConfig')}
			>
				{t('createTask.import')}
			</button>
			<button
				className="btn btn-secondary"
				onClick={onExport}
				title={t('createTask.exportConfig')}
			>
				{t('createTask.export')}
			</button>
			<button className="btn btn-danger" onClick={onClear} title={t('createTask.clearAll')}>
				<img
					src={getAssetPath('images', 'remove.png')}
					alt={t('createTask.clearAll')}
					className="btn-icon"
				/>{' '}
				<span>{t('createTask.clearAll')}</span>
			</button>
		</div>
	</div>
);

type TemplateInputSectionProps = {
	pptxPath: string;
	onChangePath: (value: string) => void;
	onBrowse: () => void;
	isLoadingShapes: boolean;
	isLoadingPlaceholders: boolean;
	templateLoaded: boolean;
	textShapeCount: number;
	imageShapeCount: number;
	t: (key: string) => string;
};

const TemplateInputSection: React.FC<TemplateInputSectionProps> = ({
	pptxPath,
	onChangePath,
	onBrowse,
	isLoadingShapes,
	isLoadingPlaceholders,
	templateLoaded,
	textShapeCount,
	imageShapeCount,
	t,
}) => (
	<div className="input-section">
		<label className="input-label">{t('createTask.pptxFile')}</label>
		<div className="input-group">
			<input
				type="text"
				className="input-field"
				value={pptxPath}
				onChange={(e) => onChangePath(e.target.value)}
				placeholder={t('createTask.pptxPlaceholder')}
			/>
			<button className="browse-btn" onClick={onBrowse} disabled={isLoadingShapes}>
				{isLoadingShapes ? t('createTask.loadingShapes') : t('createTask.browse')}
			</button>
		</div>
		{templateLoaded && !isLoadingShapes && !isLoadingPlaceholders && (
			<div className="input-meta">
				<span className="input-meta-title">{t('createTask.templateInfoLabel')}</span>
				<span>
					{t('createTask.textShapeCount')}: {textShapeCount}
				</span>
				<span>
					{t('createTask.imageShapeCount')}: {imageShapeCount}
				</span>
			</div>
		)}
	</div>
);

type DataInputSectionProps = {
	dataPath: string;
	onChangePath: (value: string) => void;
	onBrowse: () => void;
	isLoadingColumns: boolean;
	dataLoaded: boolean;
	sheetCount: number;
	uniqueColumnCount: number;
	totalRows: number;
	t: (key: string) => string;
};

const DataInputSection: React.FC<DataInputSectionProps> = ({
	dataPath,
	onChangePath,
	onBrowse,
	isLoadingColumns,
	dataLoaded,
	sheetCount,
	uniqueColumnCount,
	totalRows,
	t,
}) => (
	<div className="input-section">
		<label className="input-label">{t('createTask.dataFile')}</label>
		<div className="input-group">
			<input
				type="text"
				className="input-field"
				value={dataPath}
				onChange={(e) => onChangePath(e.target.value)}
				placeholder={t('createTask.dataPlaceholder')}
			/>
			<button className="browse-btn" onClick={onBrowse} disabled={isLoadingColumns}>
				{isLoadingColumns ? t('createTask.loadingColumns') : t('createTask.browse')}
			</button>
		</div>
		{dataLoaded && !isLoadingColumns && (
			<div className="input-meta">
				<span className="input-meta-title">{t('createTask.dataInfoLabel')}</span>
				<span>
					{t('createTask.sheetCount')}: {sheetCount}
				</span>
				<span>
					{t('createTask.columnCount')}: {uniqueColumnCount}
				</span>
				<span>
					{t('createTask.rowCount')}: {totalRows}
				</span>
			</div>
		)}
	</div>
);

type SaveLocationSectionProps = {
	savePath: string;
	onChangePath: (value: string) => void;
	onBrowse: () => void;
	t: (key: string) => string;
};

const SaveLocationSection: React.FC<SaveLocationSectionProps> = ({
	savePath,
	onChangePath,
	onBrowse,
	t,
}) => (
	<div className="input-section">
		<label className="input-label">{t('createTask.saveLocation')}</label>
		<div className="input-group">
			<input
				type="text"
				className="input-field"
				value={savePath}
				onChange={(e) => onChangePath(e.target.value)}
				placeholder={t('createTask.savePlaceholder')}
			/>
			<button className="browse-btn" onClick={onBrowse}>
				{t('createTask.browse')}
			</button>
		</div>
	</div>
);

type StartButtonSectionProps = {
	isStarting: boolean;
	canStart: boolean;
	onStart: () => void;
	t: (key: string) => string;
};

const StartButtonSection: React.FC<StartButtonSectionProps> = ({
	isStarting,
	canStart,
	onStart,
	t,
}) => (
	<button className="start-btn" onClick={onStart} disabled={isStarting || !canStart}>
		{isStarting ? t('process.processing') : t('createTask.start')}
	</button>
);

export default CreateTaskMenu;
