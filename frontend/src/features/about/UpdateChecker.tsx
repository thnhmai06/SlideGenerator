import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import type { JobStatus } from '@/shared/contexts/JobContextType';

type UpdateStatus =
	| 'idle'
	| 'checking'
	| 'available'
	| 'not-available'
	| 'downloading'
	| 'downloaded'
	| 'error';

interface UpdateInfo {
	version: string;
	releaseNotes?: string;
}

interface UpdateState {
	status: UpdateStatus;
	info?: UpdateInfo;
	progress?: number;
	error?: string;
}

/** Finished job statuses that don't block update installation. */
const FINISHED_STATUSES: JobStatus[] = ['Completed', 'Failed', 'Cancelled'];

/**
 * Component for checking and installing application updates.
 *
 * @remarks
 * Provides UI for:
 * - Manually checking for updates
 * - Displaying update availability and version info
 * - Downloading and installing updates
 * - Blocking installation when active jobs exist
 */
export const UpdateChecker: React.FC = () => {
	const { t } = useApp();
	const { groups } = useJobs();
	const [state, setState] = useState<UpdateState>({ status: 'idle' });
	const currentVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : '';

	/** Check if there are any active (non-finished) jobs. */
	const hasActiveJobs = useMemo(() => {
		return groups.some((group) => {
			// Check group status
			if (!FINISHED_STATUSES.includes(group.status)) {
				return true;
			}
			// Check all sheet statuses
			return Object.values(group.sheets).some(
				(sheet) => !FINISHED_STATUSES.includes(sheet.status),
			);
		});
	}, [groups]);

	useEffect(() => {
		if (!window.electronAPI?.onUpdateStatus) return;

		const unsubscribe = window.electronAPI.onUpdateStatus((newState) => {
			setState((prev) => ({
				...prev,
				status: newState.status as UpdateStatus,
				info: newState.info as UpdateInfo | undefined,
				progress: newState.progress,
				error: newState.error,
			}));
		});

		return unsubscribe;
	}, []);

	const handleCheckForUpdates = useCallback(async () => {
		if (!window.electronAPI?.checkForUpdates) return;

		setState({ status: 'checking' });
		try {
			const result = await window.electronAPI.checkForUpdates();
			setState({
				status: result.status as UpdateStatus,
				info: result.info as UpdateInfo | undefined,
				error: result.error,
			});
		} catch (error) {
			setState({
				status: 'error',
				error: error instanceof Error ? error.message : 'Unknown error',
			});
		}
	}, []);

	const handleDownload = useCallback(async () => {
		if (!window.electronAPI?.downloadUpdate) return;

		setState((prev) => ({ ...prev, status: 'downloading', progress: 0 }));
		await window.electronAPI.downloadUpdate();
	}, []);

	const handleInstall = useCallback(() => {
		if (!window.electronAPI?.installUpdate) return;
		if (hasActiveJobs) {
			// Don't install if there are active jobs
			return;
		}
		window.electronAPI.installUpdate();
	}, [hasActiveJobs]);

	const renderContent = () => {
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
							<span className="update-badge">{t('update.available')}</span>
							<span className="update-version">
								{t('update.newVersion')} {state.info?.version}
							</span>
						</div>
						<button className="update-btn update-btn-primary" onClick={handleDownload}>
							{t('update.downloadAndInstall')}
						</button>
					</div>
				);

			case 'downloading':
				return (
					<div className="update-status update-downloading">
						<span>{t('update.downloading')}</span>
						<div className="update-progress-bar">
							<div className="update-progress-fill" style={{ width: `${state.progress ?? 0}%` }} />
						</div>
						<span className="update-progress-text">{state.progress ?? 0}%</span>
					</div>
				);

			case 'downloaded':
				return (
					<div className="update-status update-downloaded">
						<span className="update-badge update-badge-success">{t('update.downloaded')}</span>
						{hasActiveJobs ? (
							<div className="update-warning">
								<span className="update-warning-icon">⚠</span>
								<span>{t('update.activeJobsWarning')}</span>
							</div>
						) : (
							<div className="update-actions">
								<button className="update-btn update-btn-primary" onClick={handleInstall}>
									{t('update.installNow')}
								</button>
								<span className="update-hint">{t('update.installLater')}</span>
							</div>
						)}
					</div>
				);

			case 'not-available':
				return (
					<div className="update-status update-not-available">
						<span className="update-check-icon">✓</span>
						<span>{t('update.notAvailable')}</span>
					</div>
				);

			case 'error':
				return (
					<div className="update-status update-error">
						<span className="update-error-icon">⚠</span>
						<span>{t('update.error')}</span>
						{state.error && <span className="update-error-detail">{state.error}</span>}
					</div>
				);

			default:
				return null;
		}
	};

	return (
		<div className="update-checker">
			<div className="update-header">
				<span className="update-current-version">
					{t('update.currentVersion')} {currentVersion}
				</span>
				{state.status === 'idle' || state.status === 'not-available' || state.status === 'error' ? (
					<button className="update-btn update-btn-check" onClick={handleCheckForUpdates}>
						{t('update.checkForUpdates')}
					</button>
				) : null}
			</div>
			{renderContent()}
		</div>
	);
};

export default UpdateChecker;
