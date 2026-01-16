import { useCallback, useRef, useState } from 'react';
import * as backendApi from '@/shared/services/backendApi';
import type { ConfigState } from '../types';
import {
	buildBackendUrl,
	getErrorDetail,
	normalizeBackendUrl,
	parseConfigResponse,
} from '../utils';

const PENDING_BACKEND_URL_KEY = 'slidegen.backend.url.pending';
const PENDING_BACKEND_URL_SESSION_KEY = 'slidegen.backend.url.pending.defer';

export interface UseSettingsOptions {
	t: (key: string) => string;
}

export const useSettings = ({ t }: UseSettingsOptions) => {
	const [config, setConfig] = useState<ConfigState | null>(null);
	const [initialServer, setInitialServer] = useState<ConfigState['server'] | null>(null);
	const [initialJob, setInitialJob] = useState<ConfigState['job'] | null>(null);
	const [loading, setLoading] = useState(false);
	const [saving, setSaving] = useState(false);
	const [message, setMessage] = useState<{
		type: 'success' | 'error' | 'warning';
		text: string;
	} | null>(null);
	const [restartRequired, setRestartRequired] = useState(false);
	const [showRestartNotification, setShowRestartNotification] = useState(false);
	const [isRestartNotificationClosing, setIsRestartNotificationClosing] = useState(false);
	const [showStatusNotification, setShowStatusNotification] = useState(false);
	const [isStatusNotificationClosing, setIsStatusNotificationClosing] = useState(false);
	const statusHideTimeoutRef = useRef<number | null>(null);
	const statusCloseTimeoutRef = useRef<number | null>(null);
	const [faceModelAvailable, setFaceModelAvailable] = useState(false);
	const [modelLoading, setModelLoading] = useState(false);

	const formatErrorMessage = useCallback(
		(key: string, error: unknown) => {
			const detail = getErrorDetail(error);
			return detail ? `${t(key)}: ${detail}` : t(key);
		},
		[t],
	);

	const clearStatusTimeouts = useCallback(() => {
		if (statusHideTimeoutRef.current) {
			window.clearTimeout(statusHideTimeoutRef.current);
			statusHideTimeoutRef.current = null;
		}
		if (statusCloseTimeoutRef.current) {
			window.clearTimeout(statusCloseTimeoutRef.current);
			statusCloseTimeoutRef.current = null;
		}
	}, []);

	const hideStatusNotification = useCallback(() => {
		clearStatusTimeouts();
		setIsStatusNotificationClosing(true);
		statusCloseTimeoutRef.current = window.setTimeout(() => {
			setShowStatusNotification(false);
			setIsStatusNotificationClosing(false);
			setMessage(null);
			statusCloseTimeoutRef.current = null;
		}, 180);
	}, [clearStatusTimeouts]);

	const showMessage = useCallback(
		(type: 'success' | 'error' | 'warning', text: string) => {
			clearStatusTimeouts();
			setMessage({ type, text });
			setShowStatusNotification(true);
			setIsStatusNotificationClosing(false);
			statusHideTimeoutRef.current = window.setTimeout(() => {
				hideStatusNotification();
				statusHideTimeoutRef.current = null;
			}, 5000);
		},
		[clearStatusTimeouts, hideStatusNotification],
	);

	const handleNumberChange = useCallback((value: string, apply: (next: number) => void) => {
		const next = value === '' ? Number.NaN : Number(value);
		apply(next);
	}, []);

	const handleNumberBlur = useCallback((value: string, apply: (next: number) => void) => {
		if (value === '') apply(0);
	}, []);

	const handleNumberFocus = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
		event.currentTarget.select();
	}, []);

	const storeBackendUrl = useCallback((host: string, port: number) => {
		const url = buildBackendUrl(host, port);
		if (!url) return;
		localStorage.setItem('slidegen.backend.url', url);
	}, []);

	const storePendingBackendUrl = useCallback((host: string, port: number) => {
		const url = buildBackendUrl(host, port);
		if (!url) return;
		localStorage.setItem(PENDING_BACKEND_URL_KEY, url);
		sessionStorage.setItem(PENDING_BACKEND_URL_SESSION_KEY, '1');
	}, []);

	const clearPendingBackendUrl = useCallback(() => {
		localStorage.removeItem(PENDING_BACKEND_URL_KEY);
		sessionStorage.removeItem(PENDING_BACKEND_URL_SESSION_KEY);
	}, []);

	const hasPendingBackendUrl = useCallback(() => {
		return Boolean(localStorage.getItem(PENDING_BACKEND_URL_KEY));
	}, []);

	const loadConfig = useCallback(async () => {
		try {
			setLoading(true);
			const response = await backendApi.getConfig();
			const data = response as backendApi.ConfigGetSuccess;
			const { config: nextConfig, server } = parseConfigResponse(data);
			setConfig(nextConfig);
			setInitialServer(server);
			setInitialJob(nextConfig.job);
			const pendingRestart = hasPendingBackendUrl();
			setRestartRequired(pendingRestart);
			if (!pendingRestart) {
				storeBackendUrl(server.host, server.port);
			}
		} catch (error) {
			console.error('Failed to load config:', error);
			showMessage('error', formatErrorMessage('settings.loadError', error));
		} finally {
			setLoading(false);
		}
	}, [formatErrorMessage, hasPendingBackendUrl, showMessage, storeBackendUrl]);

	const loadModelStatus = useCallback(async () => {
		try {
			const response = await backendApi.getModelStatus();
			const available = response.faceModelAvailable;
			setFaceModelAvailable(available);
			return available;
		} catch (error) {
			console.error('Failed to load model status:', error);
			return undefined;
		}
	}, []);

	const handleInitModel = useCallback(async () => {
		try {
			setModelLoading(true);
			const response = await backendApi.controlModel('face', 'init');
			const available = await loadModelStatus();
			const isAvailable = available ?? response.success;
			if (isAvailable) {
				showMessage('success', t('settings.modelInitSuccess'));
			} else {
				showMessage('error', response.message ?? t('settings.modelInitError'));
			}
		} catch (error) {
			console.error('Failed to init model:', error);
			showMessage('error', formatErrorMessage('settings.modelInitError', error));
		} finally {
			setModelLoading(false);
		}
	}, [formatErrorMessage, loadModelStatus, showMessage, t]);

	const handleDeinitModel = useCallback(async () => {
		try {
			setModelLoading(true);
			const response = await backendApi.controlModel('face', 'deinit');
			const available = await loadModelStatus();
			const isUnavailable = available === undefined ? response.success : !available;
			if (isUnavailable) {
				setFaceModelAvailable(false);
				showMessage('success', t('settings.modelDeinitSuccess'));
			} else {
				showMessage('error', response.message ?? t('settings.modelDeinitError'));
			}
		} catch (error) {
			console.error('Failed to deinit model:', error);
			showMessage('error', formatErrorMessage('settings.modelDeinitError', error));
		} finally {
			setModelLoading(false);
		}
	}, [formatErrorMessage, loadModelStatus, showMessage, t]);

	const hasServerChanged = (server: ConfigState['server']) => {
		if (!initialServer) return false;
		return (
			server.host !== initialServer.host ||
			server.port !== initialServer.port ||
			server.debug !== initialServer.debug
		);
	};

	const hasJobChanged = (job: ConfigState['job']) => {
		if (!initialJob) return false;
		return job.maxConcurrentJobs !== initialJob.maxConcurrentJobs;
	};

	const saveConfig = useCallback(async () => {
		if (!config) return;
		try {
			setSaving(true);
			let pendingRestart = hasPendingBackendUrl();
			const serverChanged = hasServerChanged(config.server);
			const jobChanged = hasJobChanged(config.job);
			const desiredUrl = buildBackendUrl(config.server.host, config.server.port) ?? '';
			const currentUrl = normalizeBackendUrl(localStorage.getItem('slidegen.backend.url') ?? '');
			if (pendingRestart && desiredUrl && desiredUrl === currentUrl) {
				clearPendingBackendUrl();
				pendingRestart = false;
			}
			const requiresRestart = pendingRestart || serverChanged || jobChanged;
			const normalizeNumber = (value: number) => (Number.isFinite(value) ? value : 0);
			await backendApi.updateConfig({
				Server: {
					Host: config.server.host,
					Port: normalizeNumber(config.server.port),
					Debug: config.server.debug,
				},
				Download: {
					MaxChunks: normalizeNumber(config.download.maxChunks),
					LimitBytesPerSecond: normalizeNumber(config.download.limitBytesPerSecond),
					SaveFolder: config.download.saveFolder,
					Retry: {
						Timeout: normalizeNumber(config.download.retryTimeout),
						MaxRetries: normalizeNumber(config.download.maxRetries),
					},
				},
				Job: {
					MaxConcurrentJobs: normalizeNumber(config.job.maxConcurrentJobs),
				},
				Image: {
					Face: {
						Confidence: normalizeNumber(config.image.face.confidence),
						UnionAll: config.image.face.unionAll,
					},
					Saliency: {
						PaddingTop: normalizeNumber(config.image.saliency.paddingTop),
						PaddingBottom: normalizeNumber(config.image.saliency.paddingBottom),
						PaddingLeft: normalizeNumber(config.image.saliency.paddingLeft),
						PaddingRight: normalizeNumber(config.image.saliency.paddingRight),
					},
				},
			});
			if (!serverChanged) {
				setInitialServer({ ...config.server });
			}
			if (!jobChanged) {
				setInitialJob({ ...config.job });
			}
			setRestartRequired(requiresRestart);
			if (serverChanged) {
				storePendingBackendUrl(config.server.host, config.server.port);
			} else if (!pendingRestart) {
				storeBackendUrl(config.server.host, config.server.port);
			}
			showMessage('success', t('settings.saveSuccess'));
		} catch (error) {
			console.error('Failed to save config:', error);
			showMessage('error', formatErrorMessage('settings.saveError', error));
		} finally {
			setSaving(false);
		}
	}, [
		clearPendingBackendUrl,
		config,
		formatErrorMessage,
		hasPendingBackendUrl,
		showMessage,
		storeBackendUrl,
		storePendingBackendUrl,
		t,
	]);

	const reloadConfig = useCallback(async () => {
		try {
			setLoading(true);
			await backendApi.reloadConfig();
			await loadConfig();
			showMessage('success', t('settings.reloadSuccess'));
		} catch (error) {
			console.error('Failed to reload config:', error);
			showMessage('error', formatErrorMessage('settings.reloadError', error));
		} finally {
			setLoading(false);
		}
	}, [formatErrorMessage, loadConfig, showMessage, t]);

	const resetConfig = useCallback(async () => {
		if (!window.confirm(t('settings.confirmReset'))) return;
		try {
			setLoading(true);
			await backendApi.resetConfig();
			await loadConfig();
			showMessage('success', t('settings.resetSuccess'));
		} catch (error) {
			console.error('Failed to reset config:', error);
			showMessage('error', formatErrorMessage('settings.resetError', error));
		} finally {
			setLoading(false);
		}
	}, [formatErrorMessage, loadConfig, showMessage, t]);

	const handleRestartServer = useCallback(async () => {
		if (!window.electronAPI?.restartBackend) {
			showMessage('warning', t('settings.restartUnavailable'));
			return;
		}
		try {
			const restarted = await window.electronAPI.restartBackend();
			if (restarted) {
				setRestartRequired(false);
				if (config) {
					storeBackendUrl(config.server.host, config.server.port);
					clearPendingBackendUrl();
					await loadConfig();
				}
				showMessage('success', t('settings.restartSuccess'));
			} else {
				showMessage('error', t('settings.restartError'));
			}
		} catch (error) {
			console.error('Failed to restart server:', error);
			showMessage('error', formatErrorMessage('settings.restartError', error));
		}
	}, [
		clearPendingBackendUrl,
		config,
		formatErrorMessage,
		loadConfig,
		showMessage,
		storeBackendUrl,
		t,
	]);

	const updateServer = useCallback((patch: Partial<ConfigState['server']>) => {
		setConfig((prev) => {
			if (!prev) return prev;
			return {
				...prev,
				server: { ...prev.server, ...patch },
			};
		});
	}, []);

	const updateDownload = useCallback((patch: Partial<ConfigState['download']>) => {
		setConfig((prev) => {
			if (!prev) return prev;
			return {
				...prev,
				download: { ...prev.download, ...patch },
			};
		});
	}, []);

	const updateJob = useCallback((patch: Partial<ConfigState['job']>) => {
		setConfig((prev) => {
			if (!prev) return prev;
			return {
				...prev,
				job: { ...prev.job, ...patch },
			};
		});
	}, []);

	const updateFace = useCallback((patch: Partial<ConfigState['image']['face']>) => {
		setConfig((prev) => {
			if (!prev) return prev;
			return {
				...prev,
				image: {
					...prev.image,
					face: { ...prev.image.face, ...patch },
				},
			};
		});
	}, []);

	const updateSaliency = useCallback((patch: Partial<ConfigState['image']['saliency']>) => {
		setConfig((prev) => {
			if (!prev) return prev;
			return {
				...prev,
				image: {
					...prev.image,
					saliency: { ...prev.image.saliency, ...patch },
				},
			};
		});
	}, []);

	const handleSelectDownloadFolder = useCallback(async () => {
		if (!config || !window.electronAPI) return;
		const folder = await window.electronAPI.openFolder();
		if (folder) {
			updateDownload({ saveFolder: folder });
		}
	}, [config, updateDownload]);

	return {
		// State
		config,
		loading,
		saving,
		message,
		restartRequired,
		showRestartNotification,
		isRestartNotificationClosing,
		showStatusNotification,
		isStatusNotificationClosing,
		faceModelAvailable,
		modelLoading,

		// State setters
		setRestartRequired,
		setShowRestartNotification,
		setIsRestartNotificationClosing,

		// Handlers
		handleNumberChange,
		handleNumberBlur,
		handleNumberFocus,
		hideStatusNotification,

		// Config operations
		loadConfig,
		loadModelStatus,
		saveConfig,
		reloadConfig,
		resetConfig,
		handleRestartServer,
		handleInitModel,
		handleDeinitModel,

		// Update functions
		updateServer,
		updateDownload,
		updateJob,
		updateFace,
		updateSaliency,
		handleSelectDownloadFolder,
	};
};
