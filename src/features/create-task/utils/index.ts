import * as backendApi from '@/shared/services/backendApi';
import { getAssetPath } from '@/shared/utils/paths';
import type {
	ImageReplacement,
	SavedInputState,
	Shape,
	SlideImageConfig,
	SlideTextConfig,
	TextReplacement,
	ValidationState,
} from '../types';

export const STORAGE_KEYS = {
	inputMenuState: 'slidegen.ui.inputsideBar.state',
};

export const loadSavedState = (): SavedInputState | null => {
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

export const normalizeSheetNames = (names?: string[]): string[] => {
	const unique: string[] = [];
	const seen = new Set<string>();
	(names ?? []).forEach((name) => {
		if (typeof name !== 'string') return;
		if (!name || seen.has(name)) return;
		seen.add(name);
		unique.push(name);
	});
	return unique;
};

export const normalizeSheetRowCounts = (
	counts?: Record<string, number>,
): Record<string, number> => {
	const normalized: Record<string, number> = {};
	Object.entries(counts ?? {}).forEach(([key, value]) => {
		if (!key) return;
		normalized[key] = Number.isFinite(value) ? value : 0;
	});
	return normalized;
};

export const buildSheetInfo = (
	sheetsInfo: Array<{ Name?: string | null; RowCount?: number | null }>,
): { sheetNames: string[]; sheetRowCounts: Record<string, number> } => {
	const sheetNames: string[] = [];
	const sheetRowCounts: Record<string, number> = {};
	const seen = new Set<string>();

	for (const sheet of sheetsInfo) {
		const originalName = sheet.Name ?? '';
		if (!originalName) continue;
		if (!seen.has(originalName)) {
			sheetNames.push(originalName);
			seen.add(originalName);
		}
		sheetRowCounts[originalName] = sheet.RowCount ?? 0;
	}

	return { sheetNames, sheetRowCounts };
};

export const resolveRequestedSheets = (
	availableSheets: string[],
	requestedSheets?: string[] | null,
	fallbackToAll = true,
): string[] => {
	if (availableSheets.length === 0) return [];
	if (!requestedSheets) return availableSheets;
	if (requestedSheets.length === 0) return fallbackToAll ? availableSheets : [];
	const requestedSet = new Set(requestedSheets);
	const resolved = availableSheets.filter((name) => requestedSet.has(name));
	if (resolved.length > 0) return resolved;
	return fallbackToAll ? availableSheets : [];
};

export const mapTemplateShapes = (template: backendApi.SlideScanTemplateSuccess): Shape[] => {
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

export const mapTemplatePlaceholders = (
	template: backendApi.SlideScanTemplateSuccess,
): string[] => {
	const items = (template.Placeholders ?? [])
		.map((item) => item.trim())
		.filter((item) => item.length > 0);
	return Array.from(new Set(items)).sort((a, b) => a.localeCompare(b));
};

export const loadTemplateAssets = async (
	filePath: string,
): Promise<{ shapes: Shape[]; placeholders: string[] }> => {
	const response = await backendApi.scanTemplate(filePath);
	const template = response as backendApi.SlideScanTemplateSuccess;
	return {
		shapes: mapTemplateShapes(template),
		placeholders: mapTemplatePlaceholders(template),
	};
};

export const loadDataAssets = async (
	filePath: string,
): Promise<{
	columns: string[];
	sheetCount: number;
	totalRows: number;
	sheetNames: string[];
	sheetRowCounts: Record<string, number>;
}> => {
	await backendApi.loadFile(filePath);
	const columns = await backendApi.getAllColumns([filePath]);
	const workbookInfo = await backendApi.getWorkbookInfo(filePath);
	const workbookData = workbookInfo as backendApi.SheetWorkbookGetInfoSuccess;
	const sheetsInfo = workbookData.Sheets ?? [];
	const rowsSum = sheetsInfo.reduce((acc, sheet) => acc + (sheet.RowCount ?? 0), 0);
	const sheetInfo = buildSheetInfo(sheetsInfo);
	return {
		columns,
		sheetCount: sheetsInfo.length,
		totalRows: rowsSum,
		sheetNames: sheetInfo.sheetNames,
		sheetRowCounts: sheetInfo.sheetRowCounts,
	};
};

export const mapTextReplacements = (
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
			(item) => item.placeholder && item.columns.length > 0 && placeholderSet.has(item.placeholder),
		);
};

export const mapImageReplacements = (
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
		roiType: item.roiType ?? 'RuleOfThirds',
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

export const resolvePath = (inputPath: string): string => {
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

export const buildTextConfigs = (textReplacements: TextReplacement[]): SlideTextConfig[] => {
	return textReplacements
		.filter((item) => item.placeholder.trim() && item.columns.length > 0)
		.map((item) => ({
			Pattern: item.placeholder.trim(),
			Columns: item.columns,
		}));
};

export const buildImageConfigs = (imageReplacements: ImageReplacement[]): SlideImageConfig[] => {
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

export const resolveAvailablePlaceholders = (
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

export const resolveAvailableShapes = (
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

export const computeValidationState = (args: {
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
	sheetNames: string[];
	selectedSheets: string[];
	sheetRowCounts: Record<string, number>;
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

	const hasSelectedSheets = args.sheetNames.length === 0 || args.selectedSheets.length > 0;
	const hasSelectedRows =
		args.sheetNames.length > 0 &&
		args.selectedSheets.reduce((sum, sheet) => sum + (args.sheetRowCounts[sheet] ?? 0), 0) > 0;

	const canStart =
		isTemplateValid &&
		isDataValid &&
		isOutputValid &&
		hasAnyConfig &&
		!hasInvalidConfig &&
		hasSelectedSheets &&
		hasSelectedRows;

	return {
		canConfigure,
		canStart,
		textShapeCount: args.placeholders.length,
		imageShapeCount: args.shapes.length,
		uniqueColumnCount: args.columns.length,
	};
};

export const splitNotificationText = (text: string): { title: string; detail: string } => {
	const idx = text.indexOf(':');
	if (idx <= 0 || idx === text.length - 1) {
		return { title: text.trim(), detail: '' };
	}
	return {
		title: text.slice(0, idx).trim(),
		detail: text.slice(idx + 1).trim(),
	};
};

export const getErrorDetail = (error: unknown): string => {
	if (error instanceof Error && error.message) return error.message;
	if (typeof error === 'string') return error;
	if (error && typeof error === 'object' && 'message' in error) {
		const value = (error as { message?: string }).message;
		if (value) return value;
	}
	return '';
};

export const getOptionDescription = (
	options: { value: string; description: string }[],
	value: string,
): string => {
	return options.find((option) => option.value === value)?.description ?? '';
};
