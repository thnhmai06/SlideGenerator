import React, { createContext, useContext, useEffect, useMemo, useState, ReactNode } from 'react';
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

interface UpdaterContextType {
	state: UpdateState;
	portable: boolean;
	checkForUpdates: () => Promise<void>;
	downloadUpdate: () => Promise<void>;
	installUpdate: () => void;
	hasActiveJobs: boolean;
}

const UpdaterContext = createContext<UpdaterContextType | null>(null);

const FINISHED_STATUSES: JobStatus[] = ['Completed', 'Failed', 'Cancelled'];

export const UpdaterProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
	const { groups } = useJobs();
	const [state, setState] = useState<UpdateState>({ status: 'idle' });
	const [portable, setPortable] = useState(false);

	const hasActiveJobs = useMemo(() => {
		return groups.some((group) => {
			if (!FINISHED_STATUSES.includes(group.status)) return true;
			return Object.values(group.sheets).some((sheet) => !FINISHED_STATUSES.includes(sheet.status));
		});
	}, [groups]);

	useEffect(() => {
		let mounted = true;
		(async () => {
			if (window.desktopAPI?.isPortable) {
				const v = await window.desktopAPI.isPortable();
				if (mounted) setPortable(Boolean(v));
			}
		})();
		return () => {
			mounted = false;
		};
	}, []);

	useEffect(() => {
		if (!window.desktopAPI?.onUpdateStatus) return;
		const unsubscribe = window.desktopAPI.onUpdateStatus((newState) => {
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

	const checkForUpdates = async () => {
		if (portable) return;
		if (!window.desktopAPI?.checkForUpdates) return;

		setState((prev) => ({ ...prev, status: 'checking' }));
		try {
			const result = await window.desktopAPI.checkForUpdates();
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
	};

	const downloadUpdate = async () => {
		if (portable) return;
		if (!window.desktopAPI?.downloadUpdate) return;

		setState((prev) => ({ ...prev, status: 'downloading', progress: 0 }));
		await window.desktopAPI.downloadUpdate();
	};

	const installUpdate = () => {
		if (portable) return;
		if (!window.desktopAPI?.installUpdate) return;
		if (hasActiveJobs) return;

		window.desktopAPI.installUpdate();
	};

	const value = useMemo(
		() => ({
			state,
			portable,
			checkForUpdates,
			downloadUpdate,
			installUpdate,
			hasActiveJobs,
		}),
		[state, portable, hasActiveJobs],
	);

	return <UpdaterContext.Provider value={value}>{children}</UpdaterContext.Provider>;
};

export const useUpdater = (): UpdaterContextType => {
	const context = useContext(UpdaterContext);
	if (!context) {
		throw new Error('useUpdater must be used within an UpdaterProvider');
	}
	return context;
};
