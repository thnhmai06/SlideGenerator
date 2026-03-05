import { invoke } from '@tauri-apps/api/core';
import { listen } from '@tauri-apps/api/event';
import { getCurrentWindow, ProgressBarStatus } from '@tauri-apps/api/window';
import { BaseDirectory } from '@tauri-apps/api/path';
import { open, save } from '@tauri-apps/plugin-dialog';
import { readTextFile, writeTextFile } from '@tauri-apps/plugin-fs';
import { openPath, openUrl } from '@tauri-apps/plugin-opener';
import { check, type DownloadEvent, type Update } from '@tauri-apps/plugin-updater';

type FileFilter = { name: string; extensions: string[] };

type UpdateState = {
	status: string;
	info?: { version: string; releaseNotes?: string };
	progress?: number;
	error?: string;
};

const updateListeners = new Set<(state: UpdateState) => void>();
let cachedUpdate: Update | null = null;

const emitUpdateStatus = (state: UpdateState) => {
	for (const listener of updateListeners) {
		listener(state);
	}
};

const isAbsolutePath = (value: string): boolean => {
	return /^[a-zA-Z]:\\/.test(value) || value.startsWith('/') || value.startsWith('\\\\');
};

const toDialogFilters = (filters?: FileFilter[]) => {
	if (!filters || filters.length === 0) return undefined;
	return filters;
};

const currentWindow = getCurrentWindow();

const desktopAPI: Window['desktopAPI'] = {
	async openFile(filters) {
		const selected = await open({
			multiple: false,
			directory: false,
			filters: toDialogFilters(filters),
		});
		return typeof selected === 'string' ? selected : undefined;
	},
	async openMultipleFiles(filters) {
		const selected = await open({
			multiple: true,
			directory: false,
			filters: toDialogFilters(filters),
		});
		return Array.isArray(selected) ? selected.filter((value): value is string => typeof value === 'string') : undefined;
	},
	async openFolder() {
		const selected = await open({ multiple: false, directory: true });
		return typeof selected === 'string' ? selected : undefined;
	},
	async saveFile(filters) {
		const selected = await save({ filters: toDialogFilters(filters) });
		return selected ?? undefined;
	},
	async openUrl(url) {
		await openUrl(url);
	},
	async openPath(path) {
		await openPath(path);
	},
	async readSettings(filename) {
		try {
			if (isAbsolutePath(filename)) {
				return await readTextFile(filename);
			}
			return await readTextFile(filename, { baseDir: BaseDirectory.AppConfig });
		} catch {
			return null;
		}
	},
	async writeSettings(filename, data) {
		try {
			if (isAbsolutePath(filename)) {
				await writeTextFile(filename, data);
			} else {
				await writeTextFile(filename, data, { baseDir: BaseDirectory.AppConfig });
			}
			return true;
		} catch {
			return false;
		}
	},
	async windowControl(action) {
		switch (action) {
			case 'minimize':
				await currentWindow.minimize();
				return;
			case 'maximize':
				await currentWindow.toggleMaximize();
				return;
			default:
				await currentWindow.close();
		}
	},
	async hideToTray() {
		await currentWindow.hide();
	},
	async setProgressBar(value) {
		if (value < 0) {
			await currentWindow.setProgressBar({ status: ProgressBarStatus.None });
			return;
		}
		const progress = Math.max(0, Math.min(100, value <= 1 ? value * 100 : value));
		await currentWindow.setProgressBar({ status: ProgressBarStatus.Normal, progress });
	},
	async backendRequest<TResult = unknown>(method: string, params?: unknown) {
		return await invoke<TResult>('backend_request', { method, params });
	},
	onBackendNotification(handler) {
		let unlisten: (() => void) | undefined;
		let isDisposed = false;
		void listen<{ method: string; params: unknown }>('backend-notification', (event) => {
			handler(event.payload);
		}).then((dispose) => {
			if (isDisposed) {
				dispose();
				return;
			}
			unlisten = dispose;
		});

		return () => {
			isDisposed = true;
			if (unlisten) {
				unlisten();
			}
		};
	},
	async restartBackend() {
		try {
			return await invoke<boolean>('restart_backend');
		} catch {
			return false;
		}
	},
	logRenderer(level, message, source) {
		void invoke('log_renderer', { level, message, source }).catch(() => undefined);
	},
	onNavigate(handler) {
		let unlisten: (() => void) | undefined;
		let isDisposed = false;
		void listen<'input' | 'process' | 'download' | 'setting' | 'about'>('app-navigate', (event) => {
			handler(event.payload);
		}).then((dispose) => {
			if (isDisposed) {
				dispose();
				return;
			}
			unlisten = dispose;
		});

		return () => {
			isDisposed = true;
			if (unlisten) {
				unlisten();
			}
		};
	},
	async setTrayLocale(locale) {
		void invoke('set_tray_locale', { locale }).catch(() => undefined);
	},
	async checkForUpdates() {
		try {
			emitUpdateStatus({ status: 'checking' });
			cachedUpdate = await check();
			if (!cachedUpdate) {
				const state: UpdateState = { status: 'not-available' };
				emitUpdateStatus(state);
				return state;
			}
			const state: UpdateState = {
				status: 'available',
				info: {
					version: cachedUpdate.version,
					releaseNotes: cachedUpdate.body,
				},
			};
			emitUpdateStatus(state);
			return state;
		} catch (error) {
			const state: UpdateState = {
				status: 'error',
				error: error instanceof Error ? error.message : String(error),
			};
			emitUpdateStatus(state);
			return state;
		}
	},
	async downloadUpdate() {
		if (!cachedUpdate) {
			await this.checkForUpdates();
		}
		if (!cachedUpdate) {
			return false;
		}

		let downloaded = 0;
		let total = 0;

		await cachedUpdate.download((event: DownloadEvent) => {
			if (event.event === 'Started') {
				total = event.data.contentLength ?? 0;
				emitUpdateStatus({ status: 'downloading', progress: 0 });
				return;
			}
			if (event.event === 'Progress') {
				downloaded += event.data.chunkLength;
				const progress = total > 0 ? Math.min(100, (downloaded / total) * 100) : undefined;
				emitUpdateStatus({ status: 'downloading', progress });
				return;
			}
			emitUpdateStatus({ status: 'downloaded', progress: 100 });
		});
		return true;
	},
	installUpdate() {
		if (!cachedUpdate) return;
		void cachedUpdate.install();
	},
	onUpdateStatus(handler) {
		updateListeners.add(handler);
		return () => {
			updateListeners.delete(handler);
		};
	},
	async isPortable() {
		try {
			return await invoke<boolean>('is_portable');
		} catch {
			return false;
		}
	},
};

window.desktopAPI = desktopAPI;
