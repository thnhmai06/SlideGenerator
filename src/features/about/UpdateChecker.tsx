import React from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { useUpdater } from '@/shared/contexts/UpdaterContext';

export const UpdateChecker: React.FC = () => {
	const { t } = useApp();
	const { state, portable, checkForUpdates, downloadUpdate, installUpdate, hasActiveJobs } =
		useUpdater();

	const currentVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : '';

	const renderContent = () => {
		if (portable) {
			return (
				<div className="update-status update-not-available">
					<div className="update-info">
						<span className="update-hint-disabled">{t('update.portableUnsupported')}</span>
					</div>
				</div>
			);
		}

		switch (state.status) {
			case 'checking':
				return (
					<div className="update-status update-checking">
						<span className="update-spinner" />
						<span>{t('update.checking')}</span>
					</div>
				);

			case 'available':
				return (
					<div className="update-status update-available">
						<div className="update-info">
							<span className="update-text-highlight">{t('update.available')}</span>
							<span className="update-version">
								{t('update.newVersion')} {state.info?.version}
							</span>
						</div>
						<button className="update-btn update-btn-primary" onClick={downloadUpdate}>
							{t('update.download')}
						</button>
					</div>
				);

			case 'downloading':
				return (
					<div className="update-status update-downloading">
						<div className="update-info">
							<span className="update-text">{t('update.downloading')}</span>
							<span className="update-progress-text">{state.progress ?? 0}%</span>
						</div>
						<div className="update-progress-bar">
							<div className="update-progress-fill" style={{ width: `${state.progress ?? 0}%` }} />
						</div>
					</div>
				);

			case 'downloaded':
				return (
					<div className="update-status update-downloaded">
						<div className="update-info">
							<span className="update-text-highlight update-text-highlight-success">
								{t('update.downloaded')}
							</span>
							<span className="update-version">
								{t('update.newVersion')} {state.info?.version}
							</span>
						</div>
						{hasActiveJobs ? (
							<span className="update-hint-disabled">{t('update.activeJobsWarning')}</span>
						) : (
							<button className="update-btn update-btn-primary" onClick={installUpdate}>
								{t('update.installNow')}
							</button>
						)}
					</div>
				);

			case 'not-available':
				return (
					<div className="update-status update-not-available">
						<div className="update-info">
							<span className="update-check-icon">✓</span>
							<span className="update-text">{t('update.notAvailable')}</span>
						</div>
					</div>
				);

			case 'error':
				return (
					<div className="update-status update-error">
						<div className="update-info">
							<span className="update-error-icon">⚠</span>
							<div className="update-error-content">
								<span className="update-text">{t('update.error')}</span>
								{state.error && <span className="update-error-detail">{state.error}</span>}
							</div>
						</div>
					</div>
				);

			case 'unsupported':
				return (
					<div className="update-status update-not-available">
						<div className="update-info">
							<span className="update-hint-disabled">{t('update.portableUnsupported')}</span>
						</div>
					</div>
				);

			default:
				return null;
		}
	};

	const showCheckButton =
		!portable &&
		(state.status === 'idle' || state.status === 'not-available' || state.status === 'error');

	return (
		<div className="update-checker">
			<div className="update-header">
				<span className="update-current-version">
					{t('update.currentVersion')} {currentVersion}
				</span>
				{showCheckButton && (
					<button className="update-btn update-btn-check" onClick={checkForUpdates}>
						{t('update.checkForUpdates')}
					</button>
				)}
			</div>
			{renderContent()}
		</div>
	);
};

export default UpdateChecker;
