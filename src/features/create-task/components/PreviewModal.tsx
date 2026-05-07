import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { PreviewModalProps } from '../types';

export const PreviewModal: React.FC<PreviewModalProps> = ({
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
					<img src={getAssetPath('images', 'download.png')} alt="" className="shape-preview-icon" />
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
