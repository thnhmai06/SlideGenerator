import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { ProcessHeaderProps } from '../types';

export const ProcessHeader: React.FC<ProcessHeaderProps> = ({
	hasProcessing,
	activeGroupsCount,
	onPauseResumeAll,
	onStopAll,
	onOpenDashboard,
	t,
}) => (
	<div className="menu-header">
		<h1 className="menu-title">{t('process.title')}</h1>
		<div className="header-actions">
			<button
				className="btn btn-secondary"
				onClick={onOpenDashboard}
				title={t('process.viewDetails')}
			>
				<img src={getAssetPath('images', 'open.png')} alt="Dashboard" className="btn-icon" />
				<span>{t('process.viewDetails')}</span>
			</button>
			<button
				className="btn btn-primary"
				onClick={onPauseResumeAll}
				disabled={activeGroupsCount === 0}
				title={hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}
			>
				<img
					src={
						hasProcessing
							? getAssetPath('images', 'pause.png')
							: getAssetPath('images', 'resume.png')
					}
					alt={hasProcessing ? 'Pause All' : 'Resume All'}
					className="btn-icon"
				/>
				<span>{hasProcessing ? t('process.pauseAll') : t('process.resumeAll')}</span>
			</button>
			<button
				className="btn btn-danger"
				onClick={onStopAll}
				disabled={activeGroupsCount === 0}
				title={t('process.stopAll')}
			>
				<img src={getAssetPath('images', 'stop.png')} alt="Stop All" className="btn-icon" />
				<span>{t('process.stopAll')}</span>
			</button>
		</div>
	</div>
);
