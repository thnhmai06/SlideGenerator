import { BrowserWindow, ipcMain } from 'electron';
import { autoUpdater, UpdateInfo } from 'electron-updater';
import log from 'electron-log';
import * as path from 'path';
import * as fs from 'fs';

export type UpdateStatus =
	| 'checking'
	| 'available'
	| 'not-available'
	| 'downloading'
	| 'downloaded'
	| 'error'
	| 'unsupported'; // portable

export interface UpdateState {
	status: UpdateStatus;
	info?: UpdateInfo;
	progress?: number;
	error?: string;
}

/** Detect portable mode via env set by portable launcher/build. */
function isPortableMode(): boolean {
	return !!process.env.PORTABLE_EXECUTABLE_DIR;
}

/** Read updater publish config from package.json (custom field). */
function readUpdateConfigFromPackageJson(): {
	provider: string;
	owner: string;
	repo: string;
	releaseType?: string;
} {
	const pkgPath = path.join(process.resourcesPath, 'app.asar', 'package.json');
	const fallbackPkgPath = path.join(require('electron').app.getAppPath(), 'package.json');

	const read = (p: string) => JSON.parse(fs.readFileSync(p, 'utf-8'));
	const pkg = fs.existsSync(pkgPath) ? read(pkgPath) : read(fallbackPkgPath);
	const release = pkg.build.publish;

	return release;
}

/** Persist downloaded version to make "downloaded" version-aware. */
function getMetaPath(): string {
	const { app } = require('electron');
	return path.join(app.getPath('userData'), 'pending', 'update.json');
}

function getDownloadedVersion(): string | null {
	try {
		const p = getMetaPath();
		if (!fs.existsSync(p)) return null;
		const meta = JSON.parse(fs.readFileSync(p, 'utf-8')) as { version?: string };
		return meta.version ?? null;
	} catch {
		return null;
	}
}

function setDownloadedVersion(version: string): void {
	const dir = path.dirname(getMetaPath());
	if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
	fs.writeFileSync(
		getMetaPath(),
		JSON.stringify({ version, downloadedAt: new Date().toISOString() }, null, 2),
	);
}

function isDownloadedFor(remoteVersion: string): boolean {
	return getDownloadedVersion() === remoteVersion;
}

/** Broadcast state to all renderer windows. */
function sendUpdateStatus(state: UpdateState): void {
	for (const win of BrowserWindow.getAllWindows()) {
		win.webContents.send('updater:status', state);
	}
}

export function registerUpdaterHandlers(): void {
	const portable = isPortableMode();

	autoUpdater.logger = log;
	autoUpdater.autoDownload = false;
	autoUpdater.autoInstallOnAppQuit = false;
	autoUpdater.allowPrerelease = false;

	try {
		const cfg = readUpdateConfigFromPackageJson();
		autoUpdater.setFeedURL({
			provider: cfg.provider,
			owner: cfg.owner,
			repo: cfg.repo,
			releaseType: cfg.releaseType ?? 'release',
		} as any);
	} catch (e) {
		log.error('Updater config error:', e);
	}

	autoUpdater.on('checking-for-update', () => sendUpdateStatus({ status: 'checking' }));

	autoUpdater.on('update-available', (info: UpdateInfo) => {
		sendUpdateStatus({ status: isDownloadedFor(info.version) ? 'downloaded' : 'available', info });
	});

	autoUpdater.on('update-not-available', (info: UpdateInfo) => {
		sendUpdateStatus({ status: 'not-available', info });
	});

	autoUpdater.on('download-progress', (progress) => {
		sendUpdateStatus({ status: 'downloading', progress: Math.round(progress.percent) });
	});

	autoUpdater.on('update-downloaded', (info: UpdateInfo) => {
		setDownloadedVersion(info.version);
		sendUpdateStatus({ status: 'downloaded', info });
	});

	autoUpdater.on('error', (err: Error) => {
		sendUpdateStatus({ status: 'error', error: err.message });
	});

	ipcMain.handle('updater:isPortable', () => isPortableMode());

	ipcMain.handle('updater:check', async (): Promise<UpdateState> => {
		try {
			const result = await autoUpdater.checkForUpdates();
			const info = result?.updateInfo;

			if (!info) return { status: 'not-available' };

			const available = autoUpdater.currentVersion.compare(info.version) < 0;
			if (!available) return { status: 'not-available', info };

			return { status: isDownloadedFor(info.version) ? 'downloaded' : 'available', info };
		} catch (e) {
			const msg = e instanceof Error ? e.message : 'Unknown error';
			return { status: 'error', error: msg };
		}
	});

	ipcMain.handle('updater:download', async (): Promise<boolean> => {
		if (portable) {
			sendUpdateStatus({
				status: 'unsupported',
				error: 'Portable build does not support updater.',
			});
			return false;
		}
		try {
			await autoUpdater.downloadUpdate();
			return true;
		} catch (e) {
			log.error('Download failed:', e);
			return false;
		}
	});

	ipcMain.handle('updater:install', (): void => {
		if (portable) {
			sendUpdateStatus({
				status: 'unsupported',
				error: 'Portable build does not support updater.',
			});
			return;
		}
		autoUpdater.quitAndInstall(false, true);
	});

	ipcMain.handle('updater:getVersion', (): string => autoUpdater.currentVersion.version);
}
