import React from 'react';
import type { TemplateInputSectionProps } from '../types';

export const TemplateInputSection: React.FC<TemplateInputSectionProps> = ({
	slidePath,
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
		<label className="input-label">{t('createTask.slideFile')}</label>
		<div className="input-group">
			<input
				type="text"
				className="input-field"
				value={slidePath}
				onChange={(e) => onChangePath(e.target.value)}
				placeholder={t('createTask.slidePlaceholder')}
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
