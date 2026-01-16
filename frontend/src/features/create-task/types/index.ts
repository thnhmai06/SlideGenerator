import type * as backendApi from '@/shared/services/backendApi';

export interface CreateTaskMenuProps {
	onStart: () => void;
}

export interface TextReplacement {
	id: number;
	placeholder: string;
	columns: string[];
}

export interface ImageReplacement {
	id: number;
	shapeId: string;
	columns: string[];
	roiType: string;
	cropType: string;
}

export interface Shape {
	id: string;
	name: string;
	preview: string;
}

export interface SavedInputState {
	pptxPath?: string;
	dataPath?: string;
	savePath?: string;
	columns?: string[];
	shapes?: Shape[];
	placeholders?: string[];
	sheetNames?: string[];
	selectedSheets?: string[];
	sheetRowCounts?: Record<string, number>;
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

export type NotificationState = {
	type: 'success' | 'error';
	text: string;
};

export type TranslationFn = (key: string) => string;

export type ValidationState = {
	canConfigure: boolean;
	canStart: boolean;
	textShapeCount: number;
	imageShapeCount: number;
	uniqueColumnCount: number;
};

export type RoiOption = {
	value: string;
	label: string;
	description: string;
};

export type CropOption = {
	value: string;
	label: string;
	description: string;
};

export interface InputNotificationProps {
	notification: NotificationState | null;
	isClosing: boolean;
	onClose: () => void;
	t: TranslationFn;
}

export interface TextReplacementPanelProps {
	canConfigure: boolean;
	showTextConfigs: boolean;
	setShowTextConfigs: React.Dispatch<React.SetStateAction<boolean>>;
	addTextReplacement: () => void;
	textReplacements: TextReplacement[];
	maxTextConfigs: number;
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
	t: TranslationFn;
}

export interface ImageReplacementPanelProps {
	canConfigure: boolean;
	showImageConfigs: boolean;
	setShowImageConfigs: React.Dispatch<React.SetStateAction<boolean>>;
	addImageReplacement: () => void;
	imageReplacements: ImageReplacement[];
	maxImageConfigs: number;
	shapes: Shape[];
	getAvailableShapes: (current: string) => Shape[];
	updateImageReplacement: (
		id: number,
		key: 'shapeId' | 'columns' | 'roiType' | 'cropType',
		value: string | string[],
	) => void;
	removeImageReplacement: (id: number) => void;
	roiOptions: RoiOption[];
	cropOptions: CropOption[];
	getOptionDescription: (
		options: { value: string; description: string }[],
		value: string,
	) => string;
	columns: string[];
	openPreview: (shape: Shape) => void;
	t: TranslationFn;
}

export interface PreviewModalProps {
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
	t: TranslationFn;
}

export interface MenuHeaderProps {
	onImport: () => void;
	onExport: () => void;
	onClear: () => void;
	t: TranslationFn;
}

export interface TemplateInputSectionProps {
	pptxPath: string;
	onChangePath: (value: string) => void;
	onBrowse: () => void;
	isLoadingShapes: boolean;
	isLoadingPlaceholders: boolean;
	templateLoaded: boolean;
	textShapeCount: number;
	imageShapeCount: number;
	t: TranslationFn;
}

export interface DataInputSectionProps {
	dataPath: string;
	onChangePath: (value: string) => void;
	onBrowse: () => void;
	isLoadingColumns: boolean;
	dataLoaded: boolean;
	sheetCount: number;
	uniqueColumnCount: number;
	totalRows: number;
	sheetNames: string[];
	selectedSheets: string[];
	sheetRowCounts: Record<string, number>;
	allSheetsSelected: boolean;
	someSheetsSelected: boolean;
	onToggleAllSheets: () => void;
	onToggleSheet: (sheetName: string) => void;
	t: TranslationFn;
}

export interface SaveLocationSectionProps {
	savePath: string;
	onChangePath: (value: string) => void;
	onBrowse: () => void;
	t: TranslationFn;
}

export interface StartButtonSectionProps {
	isStarting: boolean;
	canStart: boolean;
	onStart: () => void;
	t: TranslationFn;
}

export type SlideTextConfig = backendApi.SlideTextConfig;
export type SlideImageConfig = backendApi.SlideImageConfig;
