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
	| 'error'
	| 'unsupported';

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

export const UpdateChecker: React.FC = () => {
	const { t } = useApp();
	const { groups } = useJobs();

	const [state, setState] = useState<UpdateState>({ status: 'idle' });
	const [portable, setPortable] = useState(false);

	const currentVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : '';

	/** Check if there are any active (non-finished) jobs. */
	const hasActiveJobs = useMemo(() => {
		return groups.some((group) => {
			if (!FINISHED_STATUSES.includes(group.status)) return true;
			return Object.values(group.sheets).some((sheet) => !FINISHED_STATUSES.includes(sheet.status));
		});
	}, [groups]);

	useEffect(() => {
		let mounted = true;

		(async () => {
			if (window.electronAPI?.isPortable) {
				const v = await window.electronAPI.isPortable();
				if (mounted) setPortable(Boolean(v));
			}
		})();

		return () => {
			mounted = false;
		};
	}, []);

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
		if (portable) return;
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
	}, [portable]);

	const handleDownload = useCallback(async () => {
		if (portable) return;
		if (!window.electronAPI?.downloadUpdate) return;

		setState((prev) => ({ ...prev, status: 'downloading', progress: 0 }));
		await window.electronAPI.downloadUpdate();
	}, [portable]);

	const handleInstall = useCallback(() => {
		if (portable) return;
		if (!window.electronAPI?.installUpdate) return;
		if (hasActiveJobs) return;

		window.electronAPI.installUpdate();
	}, [portable, hasActiveJobs]);

	const renderContent = () => {
		if (portable) {
			return (
				<div className="update-status update-not-available">
					<div className="update-info">
						<span className="update-text">{t('update.portableUnsupported')}</span>
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
							<span className="update-badge">{t('update.available')}</span>
							<span className="update-version">
								{t('update.newVersion')} {state.info?.version}
							</span>
						</div>
						<button className="update-btn update-btn-primary" onClick={handleDownload}>
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
							<span className="update-badge update-badge-success">{t('update.downloaded')}</span>
							<span className="update-version">
								{t('update.newVersion')} {state.info?.version}
							</span>
						</div>
						{hasActiveJobs ? (
							<span className="update-hint-disabled">{t('update.activeJobsWarning')}</span>
						) : (
							<button className="update-btn update-btn-primary" onClick={handleInstall}>
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
							<span className="update-text">{t('update.portableUnsupported')}</span>
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
				{showCheckButton ? (
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
