import { useCallback, useMemo, useState } from 'react';
import type { ImageReplacement, Shape, TextReplacement } from '../types';
import { resolveAvailablePlaceholders, resolveAvailableShapes } from '../utils';

/**
 * Hook for managing text and image replacement configurations.
 *
 * @remarks
 * Provides state management for configuring which placeholders and shapes
 * in the PowerPoint template should be replaced with data from the spreadsheet.
 *
 * @returns Replacement state and CRUD handlers
 *
 * @example
 * ```tsx
 * const {
 *   textReplacements,
 *   addTextReplacement,
 *   updateTextReplacement,
 *   removeTextReplacement
 * } = useReplacements();
 * ```
 */
export const useReplacements = () => {
	const [textReplacements, setTextReplacements] = useState<TextReplacement[]>([]);
	const [imageReplacements, setImageReplacements] = useState<ImageReplacement[]>([]);
	const [showTextConfigs, setShowTextConfigs] = useState(false);
	const [showImageConfigs, setShowImageConfigs] = useState(false);

	const addTextReplacement = useCallback((maxTextConfigs: number) => {
		setTextReplacements((prev) => {
			if (prev.length >= maxTextConfigs) return prev;
			return [
				...prev,
				{
					id: prev.length + 1,
					placeholder: '',
					columns: [],
				},
			];
		});
	}, []);

	const removeTextReplacement = useCallback((id: number) => {
		setTextReplacements((prev) => prev.filter((item) => item.id !== id));
	}, []);

	const updateTextReplacement = useCallback(
		(id: number, field: 'placeholder' | 'columns', value: string | string[]) => {
			setTextReplacements((prev) =>
				prev.map((item) => (item.id === id ? { ...item, [field]: value } : item)),
			);
		},
		[],
	);

	const addImageReplacement = useCallback((maxImageConfigs: number) => {
		setImageReplacements((prev) => {
			if (prev.length >= maxImageConfigs) return prev;
			return [
				...prev,
				{
					id: prev.length + 1,
					shapeId: '',
					columns: [],
					roiType: 'RuleOfThirds',
					cropType: 'Fit',
				},
			];
		});
	}, []);

	const removeImageReplacement = useCallback((id: number) => {
		setImageReplacements((prev) => prev.filter((item) => item.id !== id));
	}, []);

	const updateImageReplacement = useCallback(
		(
			id: number,
			field: 'shapeId' | 'columns' | 'roiType' | 'cropType',
			value: string | string[],
		) => {
			setImageReplacements((prev) =>
				prev.map((item) => (item.id === id ? { ...item, [field]: value } : item)),
			);
		},
		[],
	);

	const clearReplacements = useCallback(() => {
		setTextReplacements([]);
		setImageReplacements([]);
	}, []);

	const getAvailablePlaceholders = useCallback(
		(placeholders: string[], current: string) =>
			resolveAvailablePlaceholders(textReplacements, placeholders, current),
		[textReplacements],
	);

	const getAvailableShapes = useCallback(
		(shapes: Shape[], current: string) =>
			resolveAvailableShapes(imageReplacements, shapes, current),
		[imageReplacements],
	);

	return useMemo(
		() => ({
			textReplacements,
			imageReplacements,
			showTextConfigs,
			showImageConfigs,
			setTextReplacements,
			setImageReplacements,
			setShowTextConfigs,
			setShowImageConfigs,
			addTextReplacement,
			removeTextReplacement,
			updateTextReplacement,
			addImageReplacement,
			removeImageReplacement,
			updateImageReplacement,
			clearReplacements,
			getAvailablePlaceholders,
			getAvailableShapes,
		}),
		[
			textReplacements,
			imageReplacements,
			showTextConfigs,
			showImageConfigs,
			addTextReplacement,
			removeTextReplacement,
			updateTextReplacement,
			addImageReplacement,
			removeImageReplacement,
			updateImageReplacement,
			clearReplacements,
			getAvailablePlaceholders,
			getAvailableShapes,
		],
	);
};
