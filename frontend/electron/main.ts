import { app, BrowserWindow, ipcMain } from 'electron';
import path from 'path';
import { fileURLToPath } from 'url';
import { getAssetPath, registerAssetHandlers } from './main/assets';
import { createBackendController } from './main/backend';
import { registerDialogHandlers } from './main/dialogs';
import { attachProcessOutputCapture, initLogging, registerRendererLogIpc } from './main/logging';
import { registerSettingsHandlers } from './main/settings';
import { registerUpdaterHandlers } from './main/updater';
import { createMainWindow, registerWindowHandlers, setIsQuitting } from './main/window';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const isDev = process.env.NODE_ENV === 'development';
const preloadPath = path.join(__dirname, 'preload.js');
const indexPath = path.join(__dirname, '../dist/index.html');
const devUrl = 'http://localhost:65000';

const logPaths = initLogging();
attachProcessOutputCapture(logPaths.processLogPath);
registerRendererLogIpc(logPaths.rendererLogPath);

registerAssetHandlers();
registerDialogHandlers();
registerSettingsHandlers();
registerWindowHandlers(getAssetPath);
registerUpdaterHandlers();

const backendController = createBackendController(logPaths.backendLogPath);

app.commandLine.appendSwitch('remote-debugging-port', '9222');

app.whenReady().then(() => {
	backendController.startBackend();
	createMainWindow({
		preloadPath,
		getAssetPath,
		isDev,
		devUrl,
		indexPath,
	});

	app.on('activate', () => {
		if (BrowserWindow.getAllWindows().length === 0) {
			createMainWindow({
				preloadPath,
				getAssetPath,
				isDev,
				devUrl,
				indexPath,
			});
		}
	});
});

app.on('window-all-closed', () => {
	if (process.platform !== 'darwin') {
		app.quit();
	}
});

let isAppQuitting = false;

app.on('before-quit', async (event) => {
	if (isAppQuitting) return;

	event.preventDefault();
	isAppQuitting = true;
	setIsQuitting(true);
	await backendController.stopBackend();
	app.quit();
});

ipcMain.handle('backend:restart', async () => {
	return backendController.restartBackend();
});
