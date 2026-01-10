import type { IpcRenderer, IpcRendererEvent } from 'electron';

export interface ElectronAPI {
	openFile: (filters?: { name: string; extensions: string[] }[]) => Promise<string | undefined>;
	openMultipleFiles: (
		filters?: { name: string; extensions: string[] }[],
	) => Promise<string[] | undefined>;
	openFolder: () => Promise<string | undefined>;
	saveFile: (filters?: { name: string; extensions: string[] }[]) => Promise<string | undefined>;
	openUrl: (url: string) => Promise<void>;
	openPath: (path: string) => Promise<void>;
	readSettings: (filename: string) => Promise<string | null>;
	writeSettings: (filename: string, data: string) => Promise<boolean>;
	windowControl: (action: 'minimize' | 'maximize' | 'close') => Promise<void>;
	hideToTray: () => Promise<void>;
	setProgressBar: (value: number) => Promise<void>;
	restartBackend: () => Promise<boolean>;
	logRenderer: (level: 'debug' | 'info' | 'warn' | 'error', message: string) => void;
	onNavigate: (
		handler: (menu: 'input' | 'process' | 'download' | 'setting' | 'about') => void,
	) => () => void;
	setTrayLocale: (locale: 'vi' | 'en') => Promise<void>;
}

export const createElectronAPI = (ipcRenderer: IpcRenderer): ElectronAPI => {
	return {
		openFile: (filters) => ipcRenderer.invoke('dialog:openFile', filters),
		openMultipleFiles: (filters) => ipcRenderer.invoke('dialog:openMultipleFiles', filters),
		openFolder: () => ipcRenderer.invoke('dialog:openFolder'),
		saveFile: (filters) => ipcRenderer.invoke('dialog:saveFile', filters),
		openUrl: (url) => ipcRenderer.invoke('dialog:openUrl', url),
		openPath: (path) => ipcRenderer.invoke('dialog:openPath', path),
		readSettings: (filename) => ipcRenderer.invoke('settings:read', filename),
		writeSettings: (filename, data) => ipcRenderer.invoke('settings:write', filename, data),
		windowControl: (action) => ipcRenderer.invoke('window:control', action),
		hideToTray: () => ipcRenderer.invoke('window:hideToTray'),
		setProgressBar: (value) => ipcRenderer.invoke('window:setProgress', value),
		restartBackend: () => ipcRenderer.invoke('backend:restart'),
		logRenderer: (level, message) => ipcRenderer.send('logs:renderer', { level, message }),
		onNavigate: (handler) => {
			const listener = (_: IpcRendererEvent, menu: string) => {
				handler(menu as 'input' | 'process' | 'download' | 'setting' | 'about');
			};
			ipcRenderer.on('app:navigate', listener);
			return () => ipcRenderer.removeListener('app:navigate', listener);
		},
		setTrayLocale: (locale) => ipcRenderer.invoke('tray:setLocale', locale),
	};
};
