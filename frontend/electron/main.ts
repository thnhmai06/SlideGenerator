import { app, BrowserWindow, ipcMain, dialog, shell } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import fs from "fs/promises";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
app.commandLine.appendSwitch("remote-debugging-port", "9222");

let mainWindow: BrowserWindow | null = null;

function createWindow() {
	mainWindow = new BrowserWindow({
		width: 1200,
		height: 800,
		minWidth: 800,
		minHeight: 600,
		icon: path.join(__dirname, "../assets/images/app-icon.png"),
		autoHideMenuBar: true,
		webPreferences: {
			preload: path.join(__dirname, "preload.js"),
			contextIsolation: true,
			nodeIntegration: false,
		},
	});
	mainWindow.maximize();

	// Load URL based on dev/prod mode
	if (process.env.NODE_ENV === "development") {
		mainWindow.loadURL("http://localhost:5173");
		mainWindow.webContents.openDevTools();
	} else {
		mainWindow.loadFile(path.join(__dirname, "../dist/index.html"));
	}

	mainWindow.on("closed", () => {
		mainWindow = null;
	});
}

app.whenReady().then(() => {
	createWindow();

	app.on("activate", () => {
		if (BrowserWindow.getAllWindows().length === 0) {
			createWindow();
		}
	});
});

app.on("window-all-closed", () => {
	if (process.platform !== "darwin") {
		app.quit();
	}
});

// IPC handlers for file dialogs
ipcMain.handle("dialog:openFile", async (_, filters: Electron.FileFilter[]) => {
	const result = await dialog.showOpenDialog({
		properties: ["openFile"],
		filters: filters || [{ name: "All Files", extensions: ["*"] }],
	});
	return result.filePaths[0];
});

ipcMain.handle(
	"dialog:openMultipleFiles",
	async (_, filters: Electron.FileFilter[]) => {
		const result = await dialog.showOpenDialog({
			properties: ["openFile", "multiSelections"],
			filters: filters || [{ name: "All Files", extensions: ["*"] }],
		});
		return result.filePaths;
	}
);

ipcMain.handle("dialog:openFolder", async () => {
	const result = await dialog.showOpenDialog({
		properties: ["openDirectory"],
	});
	return result.filePaths[0];
});

ipcMain.handle("dialog:saveFile", async (_, filters: Electron.FileFilter[]) => {
	const result = await dialog.showSaveDialog({
		filters: filters || [{ name: "All Files", extensions: ["*"] }],
		defaultPath: "output.pptx",
	});
	return result.filePath;
});

ipcMain.handle("dialog:openUrl", async (_, url: string) => {
	await shell.openExternal(url);
});

ipcMain.handle("dialog:openPath", async (_, filePath: string) => {
	await shell.openPath(filePath);
});

ipcMain.handle("settings:read", async (_, filename: string) => {
	try {
		const settingsPath = path.join(app.getPath("userData"), filename);
		const data = await fs.readFile(settingsPath, "utf-8");
		return data;
	} catch (error) {
		// File doesn't exist or can't be read, return null
		return null;
	}
});

ipcMain.handle("settings:write", async (_, filename: string, data: string) => {
	try {
		const settingsPath = path.join(app.getPath("userData"), filename);
		await fs.writeFile(settingsPath, data, "utf-8");
		return true;
	} catch (error) {
		console.error("Error writing settings:", error);
		return false;
	}
});
