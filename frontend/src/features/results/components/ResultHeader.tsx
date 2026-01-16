import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { ResultHeaderProps } from '../types';

export const ResultHeader: React.FC<ResultHeaderProps> = ({
	completedGroupsCount,
	onClearAll,
	t,
}) => (
	<div className="menu-header">
		<h1 className="menu-title">{t('results.title')}</h1>
		<div className="header-actions">
			<button
				className="btn btn-danger"
				onClick={onClearAll}
				disabled={completedGroupsCount === 0}
				title={t('results.clearAll')}
			>
				<img src={getAssetPath('images', 'close.png')} alt="Clear" className="btn-icon" />
				{t('results.clearAll')}
			</button>
		</div>
	</div>
);
