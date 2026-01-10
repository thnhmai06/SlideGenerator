const getAssetPath = (...p: string[]) => ipcRenderer.sendSync('assets:getPathSync', ...p);
import { contextBridge, ipcRenderer } from 'electron';
import { createElectronAPI } from './preload/api';

contextBridge.exposeInMainWorld('electronAPI', createElectronAPI(ipcRenderer));
contextBridge.exposeInMainWorld('getAssetPath', getAssetPath);
