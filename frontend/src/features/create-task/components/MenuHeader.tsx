import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { MenuHeaderProps } from '../types';

export const MenuHeader: React.FC<MenuHeaderProps> = ({ onImport, onExport, onClear, t }) => (
	<div className="menu-header">
		<h1 className="menu-title">{t('createTask.title')}</h1>
		<div className="config-actions">
			<button className="btn btn-secondary" onClick={onImport} title={t('createTask.importConfig')}>
				{t('createTask.import')}
			</button>
			<button className="btn btn-secondary" onClick={onExport} title={t('createTask.exportConfig')}>
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
