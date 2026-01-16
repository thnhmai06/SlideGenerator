import React from 'react';
import type { JobTabProps } from '../types';

export const JobTab: React.FC<JobTabProps> = ({
	loading,
	config,
	canEditConfig,
	isLocked,
	updateJob,
	handleNumberChange,
	handleNumberBlur,
	handleNumberFocus,
	t,
}) => (
	<div className={`setting-section${isLocked ? ' setting-section--locked' : ''}`}>
		<h3>{t('settings.jobSettings')}</h3>
		{loading || !config ? (
			<div className="loading">{t('settings.loading')}</div>
		) : (
			<div className="setting-item">
				<label className="setting-label">{t('settings.maxConcurrentJobs')}</label>
				<input
					type="number"
					className="setting-input"
					value={Number.isFinite(config.job.maxConcurrentJobs) ? config.job.maxConcurrentJobs : ''}
					disabled={!canEditConfig}
					onChange={(e) =>
						handleNumberChange(e.target.value, (next) => updateJob({ maxConcurrentJobs: next }))
					}
					onBlur={(e) =>
						handleNumberBlur(e.target.value, (next) => updateJob({ maxConcurrentJobs: next }))
					}
					onFocus={handleNumberFocus}
					min="1"
					max="32"
				/>
				<span className="setting-hint">{t('settings.maxConcurrentJobsHint')}</span>
			</div>
		)}
	</div>
);
