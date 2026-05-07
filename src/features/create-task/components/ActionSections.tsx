import React from 'react';
import type { SaveLocationSectionProps, StartButtonSectionProps } from '../types';

export const SaveLocationSection: React.FC<SaveLocationSectionProps> = ({
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

export const StartButtonSection: React.FC<StartButtonSectionProps> = ({
	isStarting,
	canStart,
	onStart,
	t,
}) => (
	<button className="start-btn" onClick={onStart} disabled={isStarting || !canStart}>
		{isStarting ? t('process.processing') : t('createTask.start')}
	</button>
);
