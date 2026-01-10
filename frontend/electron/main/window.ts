import { app, BrowserWindow, Menu, Tray, ipcMain } from 'electron';
import { readFileSync } from 'fs';
import path from 'path';
import { translations } from '../../src/locales';

let mainWindow: BrowserWindow | null = null;
let tray: Tray | null = null;
let isQuitting = false;
type MenuTarget = 'input' | 'process' | 'download' | 'setting' | 'about';
type TrayLocale = 'vi' | 'en';
type TrayLabels = {
	createTask: string;
	processing: string;
	results: string;
	settings: string;
	about: string;
	appTitle: string;
	show: string;
	hideToTray: string;
	quit: string;
};
const DEFAULT_TRAY_LOCALE: TrayLocale = 'vi';
let trayLocale: TrayLocale | null = null;
let trayEventsBound = false;

export const getMainWindow = () => mainWindow;

export const setIsQuitting = (value: boolean) => {
	isQuitting = value;
};

export const createMainWindow = (options: {
	preloadPath: string;
	getAssetPath: (...parts: string[]) => string;
	isDev: boolean;
	devUrl: string;
	indexPath: string;
}) => {
	mainWindow = new BrowserWindow({
		width: 1200,
		height: 800,
		minWidth: 800,
		minHeight: 600,
		icon: options.getAssetPath('images', 'app-icon.png'),
		frame: false,
		autoHideMenuBar: true,
		webPreferences: {
			preload: options.preloadPath,
			contextIsolation: true,
			nodeIntegration: false,
		},
	});
	mainWindow.maximize();

	if (options.isDev) {
		mainWindow.loadURL(options.devUrl);
		mainWindow.webContents.openDevTools();
	} else {
		mainWindow.loadFile(options.indexPath);
	}

	mainWindow.on('closed', () => {
		mainWindow = null;
	});

	ensureTray(options.getAssetPath);
};

const normalizeTrayLocale = (value?: string | null): TrayLocale => {
	return value === 'en' ? 'en' : 'vi';
};

const loadTrayLocaleFromSettings = (): TrayLocale => {
	try {
		const settingsPath = path.join(app.getPath('userData'), 'app-settings.json');
		const data = readFileSync(settingsPath, 'utf-8');
		const parsed = JSON.parse(data) as { language?: string };
		return normalizeTrayLocale(parsed?.language);
	} catch (_error) {
		return DEFAULT_TRAY_LOCALE;
	}
};

const resolveTrayLocale = (): TrayLocale => {
	if (trayLocale) return trayLocale;
	trayLocale = loadTrayLocaleFromSettings();
	return trayLocale;
};

const getTrayLabels = (locale: TrayLocale): TrayLabels => {
	const dictionary = (translations[locale] ?? translations[DEFAULT_TRAY_LOCALE]) as Record<
		string,
		string
	>;
	const t = (key: string, fallback: string) => dictionary[key] ?? fallback;
	return {
		createTask: t('sideBar.createTask', 'Create Task'),
		processing: t('sideBar.process', 'Processing'),
		results: t('sideBar.result', 'Results'),
		settings: t('sideBar.setting', 'Settings'),
		about: t('sideBar.about', 'About'),
		appTitle: t('app.title', 'Slide Generator'),
		show: t('tray.show', 'Show'),
		hideToTray: t('tray.hideToTray', 'Minimize to tray'),
		quit: t('tray.quit', 'Quit'),
	};
};

const navigateTo = (menu: MenuTarget) => {
	if (mainWindow?.isMinimized()) {
		mainWindow.restore();
	}
	mainWindow?.show();
	mainWindow?.focus();
	mainWindow?.webContents.send('app:navigate', menu);
};

const getWindowVisibilityState = () => {
	if (!mainWindow) {
		return { canShow: false, canHide: false };
	}

	const isMinimized = mainWindow.isMinimized();
	const isVisible = mainWindow.isVisible();
	return {
		canShow: !isVisible || isMinimized,
		canHide: isVisible && !isMinimized,
	};
};

const buildTrayMenu = (labels: TrayLabels) => {
	const { canShow, canHide } = getWindowVisibilityState();
	return Menu.buildFromTemplate([
		{
			label: labels.createTask,
			click: () => navigateTo('input'),
		},
		{
			label: labels.processing,
			click: () => navigateTo('process'),
		},
		{
			label: labels.results,
			click: () => navigateTo('download'),
		},
		{ type: 'separator' },
		{
			label: labels.settings,
			click: () => navigateTo('setting'),
		},
		{
			label: labels.about,
			click: () => navigateTo('about'),
		},
		{ type: 'separator' },
		{
			label: canShow ? `${labels.show} ${labels.appTitle}` : labels.hideToTray,
			enabled: canShow || canHide,
			click: () => {
				if (canShow) {
					if (mainWindow?.isMinimized()) {
						mainWindow.restore();
					}
					mainWindow?.show();
					mainWindow?.focus();
				} else if (canHide) {
					mainWindow?.hide();
				}
			},
		},
		{ type: 'separator' },
		{
			label: labels.quit,
			click: () => {
				isQuitting = true;
				mainWindow?.close();
			},
		},
	]);
};

const refreshTrayMenu = () => {
	if (!tray) return;
	const locale = resolveTrayLocale();
	const labels = getTrayLabels(locale);
	tray.setToolTip(labels.appTitle);
	tray.setContextMenu(buildTrayMenu(labels));
};

const attachTrayWindowEvents = () => {
	if (!mainWindow || trayEventsBound) return;
	trayEventsBound = true;
	mainWindow.on('show', refreshTrayMenu);
	mainWindow.on('hide', refreshTrayMenu);
	mainWindow.on('minimize', refreshTrayMenu);
	mainWindow.on('restore', refreshTrayMenu);
};

const ensureTray = (getAssetPath: (...parts: string[]) => string) => {
	if (tray || !mainWindow) return;

	tray = new Tray(getAssetPath('images', 'app-icon.png'));
	refreshTrayMenu();
	attachTrayWindowEvents();
	tray.on('click', () => {
		mainWindow?.show();
		mainWindow?.focus();
	});
};

export const registerWindowHandlers = (getAssetPath: (...parts: string[]) => string) => {
	ipcMain.handle('window:control', async (_, action: string) => {
		if (!mainWindow) return;

		switch (action) {
			case 'minimize':
				mainWindow.minimize();
				break;
			case 'maximize':
				if (mainWindow.isMaximized()) {
					mainWindow.unmaximize();
				} else {
					mainWindow.maximize();
				}
				break;
			case 'close':
				if (isQuitting) {
					mainWindow.close();
				} else {
					mainWindow.close();
				}
				break;
		}
	});

	ipcMain.handle('window:hideToTray', async () => {
		if (!mainWindow) return;
		ensureTray(getAssetPath);
		mainWindow.hide();
	});

	ipcMain.handle('window:setProgress', async (_, value: number) => {
		if (!mainWindow) return;
		mainWindow.setProgressBar(value);
	});

	ipcMain.handle('tray:setLocale', async (_, value: string) => {
		trayLocale = normalizeTrayLocale(value);
		refreshTrayMenu();
	});
};
