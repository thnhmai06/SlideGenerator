import { useCallback, useMemo, useRef, useState } from 'react';
import type { Shape } from '../types';

/**
 * Hook for managing shape preview modal with zoom and pan support.
 *
 * @remarks
 * Provides preview functionality for template shapes including:
 * - Modal open/close with animations
 * - Zoom in/out with mouse wheel
 * - Pan support when zoomed
 * - Save preview image to file
 *
 * @returns Preview state and interaction handlers
 *
 * @example
 * ```tsx
 * const {
 *   previewShape,
 *   openPreview,
 *   closePreview,
 *   previewZoom
 * } = usePreview();
 * ```
 */
export const usePreview = () => {
	const [previewShape, setPreviewShape] = useState<Shape | null>(null);
	const [previewClosing, setPreviewClosing] = useState(false);
	const [previewZoom, setPreviewZoom] = useState(1);
	const [previewSize, setPreviewSize] = useState<{ width: number; height: number } | null>(null);
	const [previewOffset, setPreviewOffset] = useState({ x: 0, y: 0 });
	const isDraggingRef = useRef(false);
	const dragStartRef = useRef({ x: 0, y: 0 });
	const dragMovedRef = useRef(false);

	const openPreview = useCallback((shape: Shape) => {
		setPreviewShape(shape);
		setPreviewClosing(false);
		setPreviewZoom(1);
		setPreviewSize(null);
		setPreviewOffset({ x: 0, y: 0 });
	}, []);

	const closePreview = useCallback(() => {
		setPreviewClosing(true);
		setTimeout(() => {
			setPreviewShape(null);
			setPreviewClosing(false);
		}, 180);
	}, []);

	const adjustPreviewZoom = useCallback((delta: number) => {
		setPreviewZoom((prev) => {
			const next = Math.min(3, Math.max(0.5, Number((prev + delta).toFixed(2))));
			if (next === 1) {
				setPreviewOffset({ x: 0, y: 0 });
			}
			return next;
		});
	}, []);

	const togglePreviewZoom = useCallback(() => {
		setPreviewZoom((prev) => {
			const next = prev === 1 ? 2 : 1;
			if (next === 1) {
				setPreviewOffset({ x: 0, y: 0 });
			}
			return next;
		});
	}, []);

	const handlePreviewPointerDown = useCallback(
		(event: React.PointerEvent<HTMLImageElement>) => {
			if (previewZoom <= 1) return;
			if (event.button !== 0) return;
			isDraggingRef.current = true;
			dragMovedRef.current = false;
			dragStartRef.current = {
				x: event.clientX - previewOffset.x,
				y: event.clientY - previewOffset.y,
			};
			event.currentTarget.setPointerCapture(event.pointerId);
		},
		[previewZoom, previewOffset],
	);

	const handlePreviewPointerMove = useCallback(
		(event: React.PointerEvent<HTMLImageElement>) => {
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
		},
		[previewOffset],
	);

	const handlePreviewPointerUp = useCallback((event: React.PointerEvent<HTMLImageElement>) => {
		isDraggingRef.current = false;
		event.currentTarget.releasePointerCapture(event.pointerId);
	}, []);

	const handlePreviewWheel = useCallback(
		(event: React.WheelEvent<HTMLImageElement>) => {
			event.preventDefault();
			const delta = event.deltaY > 0 ? -0.1 : 0.1;
			adjustPreviewZoom(delta);
		},
		[adjustPreviewZoom],
	);

	const handleSavePreview = useCallback(async () => {
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
	}, [previewShape]);

	return useMemo(
		() => ({
			previewShape,
			previewClosing,
			previewZoom,
			previewSize,
			previewOffset,
			dragMovedRef,
			openPreview,
			closePreview,
			adjustPreviewZoom,
			setPreviewZoom,
			setPreviewSize,
			togglePreviewZoom,
			handlePreviewPointerDown,
			handlePreviewPointerMove,
			handlePreviewPointerUp,
			handlePreviewWheel,
			handleSavePreview,
		}),
		[
			previewShape,
			previewClosing,
			previewZoom,
			previewSize,
			previewOffset,
			openPreview,
			closePreview,
			adjustPreviewZoom,
			togglePreviewZoom,
			handlePreviewPointerDown,
			handlePreviewPointerMove,
			handlePreviewPointerUp,
			handlePreviewWheel,
			handleSavePreview,
		],
	);
};
