import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import * as backendApi from '@/shared/services/backendApi';
import { getAssetPath } from '@/shared/utils/paths';
import type { CropOption, RoiOption, SavedInputState, Shape } from '../types';
import {
	buildImageConfigs,
	buildSheetInfo,
	buildTextConfigs,
	computeValidationState,
	getOptionDescription,
	loadDataAssets,
	loadSavedState,
	loadTemplateAssets,
	mapImageReplacements,
	mapTextReplacements,
	normalizeSheetNames,
	normalizeSheetRowCounts,
	resolveAvailablePlaceholders,
	resolveAvailableShapes,
	resolveRequestedSheets,
	resolvePath,
	STORAGE_KEYS,
} from '../utils';
import { useNotification } from './useNotification';
import { usePreview } from './usePreview';
import { useReplacements } from './useReplacements';

/**
 * Options for the useCreateTask hook.
 */
export interface UseCreateTaskOptions {
	/** Callback invoked when slide generation starts */
	onStart: () => void;
}

/**
 * Hook for managing the slide generation task creation workflow.
 *
 * @remarks
 * This hook orchestrates the entire task creation process including:
 * - Template file selection and scanning
 * - Data file loading and sheet selection
 * - Text and image replacement configuration
 * - Preview generation
 * - Job submission
 *
 * @param options - Hook configuration options
 * @returns State and handlers for the task creation form
 *
 * @example
 * ```tsx
 * const {
 *   pptxPath, setPptxPath,
 *   dataPath, setDataPath,
 *   handleSubmit,
 *   validationState
 * } = useCreateTask({ onStart: () => navigate('/process') });
 * ```
 */
export const useCreateTask = ({ onStart }: UseCreateTaskOptions) => {
	const { t } = useApp();
	const { createGroup } = useJobs();

	// Notification and preview hooks
	const notificationHook = useNotification({ t });
	const previewHook = usePreview();
	const replacementsHook = useReplacements();

	// ROI and Crop options
	const roiOptions: RoiOption[] = useMemo(
		() => [
			{
				value: 'RuleOfThirds',
				label: t('replacement.roiRuleOfThirds'),
				description: t('replacement.roiRuleOfThirdsDesc'),
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
		],
		[t],
	);

	const cropOptions: CropOption[] = useMemo(
		() => [
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
		],
		[t],
	);

	// Load saved state
	const savedState = useMemo(() => loadSavedState(), []);

	// Path state
	const [pptxPath, setPptxPath] = useState(savedState?.pptxPath || '');
	const [dataPath, setDataPath] = useState(savedState?.dataPath || '');
	const [savePath, setSavePath] = useState(savedState?.savePath || '');

	// Data state
	const [columns, setColumns] = useState<string[]>(savedState?.columns || []);
	const initialSheetNames = normalizeSheetNames(savedState?.sheetNames);
	const [sheetNames, setSheetNames] = useState<string[]>(initialSheetNames);
	const [selectedSheets, setSelectedSheets] = useState<string[]>(
		normalizeSheetNames(savedState?.selectedSheets ?? initialSheetNames),
	);
	const [sheetRowCounts, setSheetRowCounts] = useState<Record<string, number>>(
		normalizeSheetRowCounts(savedState?.sheetRowCounts),
	);
	const [shapes, setShapes] = useState<Shape[]>(savedState?.shapes || []);
	const [placeholders, setPlaceholders] = useState<string[]>(savedState?.placeholders || []);
	const [sheetCount, setSheetCount] = useState(savedState?.sheetCount || 0);
	const [totalRows, setTotalRows] = useState(savedState?.totalRows || 0);

	// Loading states
	const [isLoadingColumns, setIsLoadingColumns] = useState(false);
	const [isLoadingShapes, setIsLoadingShapes] = useState(false);
	const [isLoadingPlaceholders, setIsLoadingPlaceholders] = useState(false);
	const [isStarting, setIsStarting] = useState(false);

	// Loaded states
	const [templateLoaded, setTemplateLoaded] = useState(Boolean(savedState?.templateLoaded));
	const [dataLoaded, setDataLoaded] = useState(Boolean(savedState?.dataLoaded));

	// Refs
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

	// Initialize replacements from saved state
	useEffect(() => {
		if (savedState?.textReplacements) {
			replacementsHook.setTextReplacements(
				savedState.textReplacements.map((item) => ({
					id: item.id ?? 1,
					placeholder: item.placeholder || item.searchText || '',
					columns: item.columns || [],
				})),
			);
		}
		if (savedState?.imageReplacements) {
			replacementsHook.setImageReplacements(
				savedState.imageReplacements.map((item) => ({
					id: item.id ?? 1,
					shapeId: item.shapeId ?? '',
					columns: item.columns ?? [],
					roiType: item.roiType ?? 'RuleOfThirds',
					cropType: item.cropType ?? 'Fit',
				})),
			);
		}
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	// Clear legacy config
	useEffect(() => {
		localStorage.removeItem('config');
	}, []);

	// Save state to sessionStorage whenever it changes
	useEffect(() => {
		const state = {
			pptxPath,
			dataPath,
			savePath,
			textReplacements: replacementsHook.textReplacements,
			imageReplacements: replacementsHook.imageReplacements,
			shapes,
			placeholders,
			columns,
			sheetNames,
			selectedSheets,
			sheetRowCounts,
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
		replacementsHook.textReplacements,
		replacementsHook.imageReplacements,
		shapes,
		placeholders,
		columns,
		sheetNames,
		selectedSheets,
		sheetRowCounts,
		sheetCount,
		totalRows,
		templateLoaded,
		dataLoaded,
	]);

	// Keep refs in sync
	useEffect(() => {
		pptxPathRef.current = pptxPath;
	}, [pptxPath]);

	useEffect(() => {
		dataPathRef.current = dataPath;
	}, [dataPath]);

	// Notification helpers
	const { showNotification, formatErrorMessage } = notificationHook;
	const { setTextReplacements, setImageReplacements, clearReplacements } = replacementsHook;

	const notifyTemplateError = useCallback(
		(error: unknown) => {
			const now = Date.now();
			if (now - templateErrorAtRef.current < 800) return;
			templateErrorAtRef.current = now;
			showNotification('error', formatErrorMessage('createTask.templateLoadError', error));
		},
		[showNotification, formatErrorMessage],
	);

	const notifyDataError = useCallback(
		(error: unknown) => {
			const now = Date.now();
			if (now - dataErrorAtRef.current < 800) return;
			dataErrorAtRef.current = now;
			showNotification('error', formatErrorMessage('createTask.columnLoadError', error));
		},
		[showNotification, formatErrorMessage],
	);

	// Load template from server
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
				const mappedShapes = (data.shapes ?? [])
					.filter((shape) => shape.isImage === true)
					.map((shape) => ({
						id: String(shape.id),
						name: shape.name,
						preview: shape.data
							? `data:image/png;base64,${shape.data}`
							: getAssetPath('images', 'app-icon.png'),
					}));
				setShapes(mappedShapes);
				lastLoadedTemplatePathRef.current = filePath;

				const items = (data.placeholders ?? [])
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

	// Load data from server
	const loadDataFromServer = useCallback(
		async (filePath: string) => {
			if (!filePath) {
				setColumns([]);
				setSheetNames([]);
				setSelectedSheets([]);
				setSheetRowCounts({});
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
				const sheetInfo = buildSheetInfo(sheetsInfo);

				setColumns(allColumns);
				setSheetNames(sheetInfo.sheetNames);
				setSelectedSheets(sheetInfo.sheetNames);
				setSheetRowCounts(normalizeSheetRowCounts(sheetInfo.sheetRowCounts));
				setSheetCount(sheetsInfo.length);
				setTotalRows(rowsSum);
				setDataLoaded(true);
				lastLoadedDataPathRef.current = filePath;
			} catch (error) {
				if (!isHydratingRef.current && filePath === dataPathRef.current) {
					notifyDataError(error);
					setDataPath('');
					setColumns([]);
					setSheetNames([]);
					setSelectedSheets([]);
					setSheetRowCounts({});
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

	// Hydrate from saved state
	const hydrateFromSavedState = useCallback(async () => {
		if (!savedState) return;
		if (hasHydratedRef.current) return;
		hasHydratedRef.current = true;
		isHydratingRef.current = true;

		const nextPptxPath = savedState.pptxPath || '';
		const nextDataPath = savedState.dataPath || '';
		const nextSavePath = savedState.savePath || '';

		setPptxPath(nextPptxPath);
		setDataPath(nextDataPath);
		setSavePath(nextSavePath);

		const cached = {
			shapes: savedState.shapes || [],
			placeholders: savedState.placeholders || [],
			columns: savedState.columns || [],
			sheetNames: normalizeSheetNames(savedState.sheetNames),
			selectedSheets: normalizeSheetNames(savedState.selectedSheets ?? savedState.sheetNames),
			sheetRowCounts: normalizeSheetRowCounts(savedState.sheetRowCounts),
			sheetCount: savedState.sheetCount || 0,
			totalRows: savedState.totalRows || 0,
			templateLoaded: savedState.templateLoaded || false,
			dataLoaded: savedState.dataLoaded || false,
		};

		setShapes(cached.shapes);
		setPlaceholders(cached.placeholders);
		setColumns(cached.columns);
		setSheetNames(cached.sheetNames);
		setSelectedSheets(cached.selectedSheets);
		setSheetRowCounts(cached.sheetRowCounts);
		setSheetCount(cached.sheetCount);
		setTotalRows(cached.totalRows);
		setTemplateLoaded(cached.templateLoaded);
		setDataLoaded(cached.dataLoaded);
		lastLoadedTemplatePathRef.current = cached.templateLoaded ? nextPptxPath : '';
		lastLoadedDataPathRef.current = cached.dataLoaded ? nextDataPath : '';

		setIsLoadingShapes(true);
		setIsLoadingPlaceholders(true);
		setIsLoadingColumns(true);

		try {
			const templateAssets =
				!cached.templateLoaded && nextPptxPath
					? await loadTemplateAssets(nextPptxPath)
					: { shapes: cached.shapes, placeholders: cached.placeholders };
			const dataAssets =
				!cached.dataLoaded && nextDataPath
					? await loadDataAssets(nextDataPath)
					: {
							columns: cached.columns,
							sheetNames: cached.sheetNames,
							sheetRowCounts: cached.sheetRowCounts,
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

			const explicitSelectedSheets = Array.isArray(savedState.selectedSheets)
				? savedState.selectedSheets
				: undefined;
			const requestedSheets = (explicitSelectedSheets ?? savedState.sheetNames ?? []).filter(
				(name): name is string => typeof name === 'string',
			);
			const availableSheets = dataAssets.sheetNames ?? [];
			const resolvedSelection = resolveRequestedSheets(
				availableSheets,
				requestedSheets,
				!explicitSelectedSheets,
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
			setSheetNames(availableSheets);
			setSelectedSheets(resolvedSelection);
			setSheetRowCounts(normalizeSheetRowCounts(dataAssets.sheetRowCounts));
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
	}, [savedState, showNotification, formatErrorMessage, setTextReplacements, setImageReplacements]);

	// Run hydration on mount
	useEffect(() => {
		hydrateFromSavedState().catch(() => undefined);
	}, [hydrateFromSavedState]);

	// Schedule template load when pptxPath changes
	useEffect(() => {
		if (isHydratingRef.current) return;
		if (!pptxPath) {
			setShapes([]);
			setPlaceholders([]);
			setTemplateLoaded(false);
			return;
		}

		if (templateLoaded && lastLoadedTemplatePathRef.current === pptxPath && shapes.length > 0) {
			return;
		}

		setShapes([]);
		setPlaceholders([]);
		setTemplateLoaded(false);

		const timer = setTimeout(() => {
			loadTemplateFromServer(pptxPath).catch(() => undefined);
		}, 400);

		return () => clearTimeout(timer);
	}, [pptxPath, templateLoaded, shapes.length, loadTemplateFromServer]);

	// Schedule data load when dataPath changes
	useEffect(() => {
		if (isHydratingRef.current) return;
		if (!dataPath) {
			setColumns([]);
			setSheetNames([]);
			setSelectedSheets([]);
			setSheetRowCounts({});
			setSheetCount(0);
			setTotalRows(0);
			setDataLoaded(false);
			return;
		}

		if (isLoadingColumns || (dataLoaded && lastLoadedDataPathRef.current === dataPath)) {
			return;
		}

		setColumns([]);
		setSheetNames([]);
		setSelectedSheets([]);
		setSheetRowCounts({});
		setSheetCount(0);
		setTotalRows(0);
		setDataLoaded(false);

		const timer = setTimeout(() => {
			loadDataFromServer(dataPath).catch(() => undefined);
		}, 400);

		return () => clearTimeout(timer);
	}, [dataPath, dataLoaded, isLoadingColumns, loadDataFromServer]);

	// Browse handlers
	const handleBrowsePptx = useCallback(async () => {
		const path = await window.electronAPI.openFile([
			{ name: 'PowerPoint Files', extensions: ['pptx', 'potx'] },
		]);
		if (path) {
			setPptxPath(path);
			setShapes([]);
			setPlaceholders([]);
		}
	}, []);

	const handleBrowseData = useCallback(async () => {
		const path = await window.electronAPI.openFile([
			{ name: 'Spreadsheets Files', extensions: ['xlsx', 'xlsm'] },
		]);
		if (path) {
			setDataPath(path);
		}
	}, []);

	const handleBrowseSave = useCallback(async () => {
		const path = await window.electronAPI.openFolder();
		if (path) setSavePath(path);
	}, []);

	// Replacement config limits
	const maxTextConfigs = Math.min(placeholders.length, columns.length);
	const maxImageConfigs = Math.min(shapes.length, columns.length);

	// Validation state
	const validationState = useMemo(
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
				textReplacements: replacementsHook.textReplacements,
				imageReplacements: replacementsHook.imageReplacements,
				sheetNames,
				selectedSheets,
				sheetRowCounts,
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
			replacementsHook.textReplacements,
			replacementsHook.imageReplacements,
			sheetNames,
			selectedSheets,
			sheetRowCounts,
		],
	);

	// Sheet selection
	const allSheetsSelected = sheetNames.length > 0 && selectedSheets.length === sheetNames.length;
	const someSheetsSelected = selectedSheets.length > 0 && selectedSheets.length < sheetNames.length;

	const toggleAllSheets = useCallback(() => {
		setSelectedSheets(allSheetsSelected ? [] : sheetNames);
	}, [allSheetsSelected, sheetNames]);

	const toggleSheet = useCallback(
		(sheetName: string) => {
			setSelectedSheets((prev) => {
				const next = new Set(prev);
				if (next.has(sheetName)) {
					next.delete(sheetName);
				} else {
					next.add(sheetName);
				}
				return sheetNames.filter((name) => next.has(name));
			});
		},
		[sheetNames],
	);

	// Placeholder and shape availability helpers
	const getAvailablePlaceholders = useCallback(
		(current: string) =>
			resolveAvailablePlaceholders(replacementsHook.textReplacements, placeholders, current),
		[replacementsHook.textReplacements, placeholders],
	);

	const getAvailableShapes = useCallback(
		(current: string) =>
			resolveAvailableShapes(replacementsHook.imageReplacements, shapes, current),
		[replacementsHook.imageReplacements, shapes],
	);

	// Start job
	const handleStart = useCallback(async () => {
		const resolvedPptxPath = resolvePath(pptxPath);
		const resolvedDataPath = resolvePath(dataPath);
		const resolvedSavePath = resolvePath(savePath);

		if (!resolvedPptxPath || !resolvedDataPath || !resolvedSavePath || !validationState.canStart) {
			showNotification('error', t('createTask.error'));
			return;
		}

		const textConfigs = buildTextConfigs(replacementsHook.textReplacements);
		const imageConfigs = buildImageConfigs(replacementsHook.imageReplacements);

		setPptxPath(resolvedPptxPath);
		setDataPath(resolvedDataPath);
		setSavePath(resolvedSavePath);

		try {
			setIsStarting(true);
			await createGroup({
				templatePath: resolvedPptxPath,
				spreadsheetPath: resolvedDataPath,
				outputPath: resolvedSavePath,
				textConfigs,
				imageConfigs,
				sheetNames: selectedSheets.length > 0 ? selectedSheets : undefined,
			});
			onStart();
		} catch (error) {
			console.error('Failed to start job:', error);
			const message = error instanceof Error ? error.message : t('createTask.error');
			showNotification('error', message);
		} finally {
			setIsStarting(false);
		}
	}, [
		pptxPath,
		dataPath,
		savePath,
		validationState.canStart,
		replacementsHook.textReplacements,
		replacementsHook.imageReplacements,
		selectedSheets,
		createGroup,
		onStart,
		showNotification,
		t,
	]);

	// Export config
	const exportConfig = useCallback(async () => {
		const config = {
			pptxPath,
			dataPath,
			savePath,
			selectedSheets,
			textReplacements: replacementsHook.textReplacements,
			imageReplacements: replacementsHook.imageReplacements,
		};

		const path = await window.electronAPI.saveFile([
			{ name: 'JSON Files', extensions: ['json'] },
			{ name: 'All Files', extensions: ['*'] },
		]);

		if (!path) return;

		try {
			await window.electronAPI.writeSettings(path, JSON.stringify(config, null, 2));
			showNotification('success', t('createTask.exportSuccess'));
		} catch {
			showNotification('error', t('createTask.exportError'));
		}
	}, [
		pptxPath,
		dataPath,
		savePath,
		selectedSheets,
		replacementsHook.textReplacements,
		replacementsHook.imageReplacements,
		showNotification,
		t,
	]);

	// Import config
	const importConfig = useCallback(async () => {
		const path = await window.electronAPI.openFile([
			{ name: 'JSON Files', extensions: ['json'] },
			{ name: 'All Files', extensions: ['*'] },
		]);

		if (!path) return;

		setIsLoadingShapes(true);
		setIsLoadingPlaceholders(true);
		setIsLoadingColumns(true);

		try {
			const data = await window.electronAPI.readSettings(path);
			if (!data) return;

			const config = JSON.parse(data) as SavedInputState;
			const nextPptxPath = config.pptxPath || '';
			const nextDataPath = config.dataPath || '';
			const nextSavePath = config.savePath || '';

			setPptxPath(nextPptxPath);
			setDataPath(nextDataPath);
			setSavePath(nextSavePath);
			setShapes([]);
			setPlaceholders([]);
			setColumns([]);
			setSheetNames([]);
			setSelectedSheets([]);
			setSheetRowCounts({});
			setSheetCount(0);
			setTotalRows(0);
			setTemplateLoaded(false);
			setDataLoaded(false);
			clearReplacements();

			const templateAssets = nextPptxPath
				? await loadTemplateAssets(nextPptxPath)
				: { shapes: [], placeholders: [] };
			const dataAssets = nextDataPath
				? await loadDataAssets(nextDataPath)
				: {
						columns: [],
						sheetCount: 0,
						totalRows: 0,
						sheetNames: [],
						sheetRowCounts: {},
					};

			if (nextPptxPath) {
				setTemplateLoaded(true);
				lastLoadedTemplatePathRef.current = nextPptxPath;
			}
			if (nextDataPath) {
				const explicitSelectedSheets = Array.isArray(config.selectedSheets)
					? config.selectedSheets
					: undefined;
				const requestedSheets = (explicitSelectedSheets ?? config.sheetNames ?? []).filter(
					(name): name is string => typeof name === 'string',
				);
				const availableSheets = dataAssets.sheetNames;
				const resolvedSelection = resolveRequestedSheets(
					availableSheets,
					requestedSheets,
					!explicitSelectedSheets,
				);
				setSheetNames(availableSheets);
				setSelectedSheets(resolvedSelection);
				setSheetRowCounts(normalizeSheetRowCounts(dataAssets.sheetRowCounts));
				setSheetCount(dataAssets.sheetCount);
				setTotalRows(dataAssets.totalRows);
				setDataLoaded(true);
				lastLoadedDataPathRef.current = nextDataPath;
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

			setShapes(templateAssets.shapes);
			setPlaceholders(templateAssets.placeholders);
			setColumns(dataAssets.columns);
			setTextReplacements(filteredText);
			setImageReplacements(filteredImages);
			if (!nextDataPath) {
				setSheetNames([]);
				setSelectedSheets([]);
				setSheetRowCounts({});
			}
			showNotification('success', t('createTask.importSuccess'));
		} catch {
			showNotification('error', t('createTask.importError'));
		} finally {
			setIsLoadingShapes(false);
			setIsLoadingPlaceholders(false);
			setIsLoadingColumns(false);
		}
	}, [showNotification, setTextReplacements, setImageReplacements, clearReplacements, t]);

	// Clear all
	const clearAll = useCallback(() => {
		if (confirm(t('createTask.confirmClear') || 'Are you sure you want to clear all data?')) {
			setPptxPath('');
			setDataPath('');
			setSavePath('');
			setColumns([]);
			setSheetNames([]);
			setSelectedSheets([]);
			setSheetRowCounts({});
			setShapes([]);
			setPlaceholders([]);
			setSheetCount(0);
			setTotalRows(0);
			setTemplateLoaded(false);
			setDataLoaded(false);
			clearReplacements();
			lastLoadedTemplatePathRef.current = '';
			lastLoadedDataPathRef.current = '';
			sessionStorage.removeItem(STORAGE_KEYS.inputMenuState);
		}
	}, [t, clearReplacements]);

	return {
		// Translation
		t,

		// Options
		roiOptions,
		cropOptions,
		getOptionDescription,

		// Path state
		pptxPath,
		dataPath,
		savePath,
		setPptxPath,
		setDataPath,
		setSavePath,

		// Data state
		columns,
		sheetNames,
		selectedSheets,
		sheetRowCounts,
		shapes,
		placeholders,
		sheetCount,
		totalRows,

		// Loading states
		isLoadingColumns,
		isLoadingShapes,
		isLoadingPlaceholders,
		isStarting,

		// Loaded states
		templateLoaded,
		dataLoaded,

		// Validation
		...validationState,

		// Sheet selection
		allSheetsSelected,
		someSheetsSelected,
		toggleAllSheets,
		toggleSheet,

		// Config limits
		maxTextConfigs,
		maxImageConfigs,

		// Placeholder/shape availability
		getAvailablePlaceholders,
		getAvailableShapes,

		// Replacements hook
		textReplacements: replacementsHook.textReplacements,
		imageReplacements: replacementsHook.imageReplacements,
		showTextConfigs: replacementsHook.showTextConfigs,
		showImageConfigs: replacementsHook.showImageConfigs,
		setShowTextConfigs: replacementsHook.setShowTextConfigs,
		setShowImageConfigs: replacementsHook.setShowImageConfigs,
		addTextReplacement: () => replacementsHook.addTextReplacement(maxTextConfigs),
		removeTextReplacement: replacementsHook.removeTextReplacement,
		updateTextReplacement: replacementsHook.updateTextReplacement,
		addImageReplacement: () => replacementsHook.addImageReplacement(maxImageConfigs),
		removeImageReplacement: replacementsHook.removeImageReplacement,
		updateImageReplacement: replacementsHook.updateImageReplacement,

		// Browse handlers
		handleBrowsePptx,
		handleBrowseData,
		handleBrowseSave,

		// Actions
		handleStart,
		exportConfig,
		importConfig,
		clearAll,

		// Notification
		notification: notificationHook.notification,
		isNotificationClosing: notificationHook.isNotificationClosing,
		hideNotification: notificationHook.hideNotification,

		// Preview
		...previewHook,
	};
};
