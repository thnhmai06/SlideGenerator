import { BrowserWindow, ipcMain } from 'electron';
import { autoUpdater, UpdateInfo } from 'electron-updater';
import log from 'electron-log';

/**
 * Update status types for IPC communication
 */
export type UpdateStatus =
	| 'checking'
	| 'available'
	| 'not-available'
	| 'downloading'
	| 'downloaded'
	| 'error';

export interface UpdateState {
	status: UpdateStatus;
	info?: UpdateInfo;
	progress?: number;
	error?: string;
}

/**
 * Configures and registers the auto-updater with IPC handlers.
 *
 * @remarks
 * Uses electron-updater to check for updates from GitHub releases.
 * The updater is configured to NOT auto-download or auto-install,
 * giving users control via the About menu.
 * Only stable releases are considered (pre-releases like -dev are ignored).
 */
export function registerUpdaterHandlers(): void {
	// Configure updater
	autoUpdater.logger = log;
	autoUpdater.autoDownload = false;
	autoUpdater.autoInstallOnAppQuit = true;
	autoUpdater.allowPrerelease = false; // Only check for stable releases

	// Set up event handlers
	autoUpdater.on('checking-for-update', () => {
		sendUpdateStatus({ status: 'checking' });
	});

	autoUpdater.on('update-available', (info: UpdateInfo) => {
		sendUpdateStatus({ status: 'available', info });
	});

	autoUpdater.on('update-not-available', (info: UpdateInfo) => {
		sendUpdateStatus({ status: 'not-available', info });
	});

	autoUpdater.on('download-progress', (progress) => {
		sendUpdateStatus({
			status: 'downloading',
			progress: Math.round(progress.percent),
		});
	});

	autoUpdater.on('update-downloaded', (info: UpdateInfo) => {
		sendUpdateStatus({ status: 'downloaded', info });
	});

	autoUpdater.on('error', (error: Error) => {
		sendUpdateStatus({ status: 'error', error: error.message });
	});

	// Register IPC handlers
	ipcMain.handle('updater:check', async (): Promise<UpdateState> => {
		try {
			const result = await autoUpdater.checkForUpdates();
			if (result?.updateInfo) {
				const isAvailable = autoUpdater.currentVersion.compare(result.updateInfo.version) < 0;
				return {
					status: isAvailable ? 'available' : 'not-available',
					info: result.updateInfo,
				};
			}
			return { status: 'not-available' };
		} catch (error) {
			const message = error instanceof Error ? error.message : 'Unknown error';
			return { status: 'error', error: message };
		}
	});

	ipcMain.handle('updater:download', async (): Promise<boolean> => {
		try {
			await autoUpdater.downloadUpdate();
			return true;
		} catch (error) {
			log.error('Download update failed:', error);
			return false;
		}
	});

	ipcMain.handle('updater:install', (): void => {
		autoUpdater.quitAndInstall(false, true);
	});

	ipcMain.handle('updater:getVersion', (): string => {
		return autoUpdater.currentVersion.version;
	});
}

/**
 * Sends update status to all renderer windows.
 */
function sendUpdateStatus(state: UpdateState): void {
	const windows = BrowserWindow.getAllWindows();
	windows.forEach((win) => {
		win.webContents.send('updater:status', state);
	});
}
