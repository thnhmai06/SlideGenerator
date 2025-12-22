import { app, BrowserWindow, ipcMain, dialog, shell, Menu, Tray } from "electron";
import { spawn, ChildProcess } from "child_process";
import path from "path";
import { fileURLToPath } from "url";
import fs from "fs/promises";
import fsSync from "fs";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
app.commandLine.appendSwitch("remote-debugging-port", "9222");

let mainWindow: BrowserWindow | null = null;
let tray: Tray | null = null;
let isQuitting = false;
let backendProcess: ChildProcess | null = null;
let backendWorkingDir: string | null = null;

const shouldStartBackend = () =>
	process.env.SLIDEGEN_DISABLE_BACKEND !== "1";

const resolveBackendCommand = () => {
	const override = process.env.SLIDEGEN_BACKEND_PATH;
	if (override && fsSync.existsSync(override)) {
		const ext = path.extname(override).toLowerCase();
		if (ext === ".dll") {
			return {
				command: "dotnet",
				args: [override],
				cwd: path.dirname(override),
			};
		}
		return {
			command: override,
			args: [],
			cwd: path.dirname(override),
		};
	}

	if (app.isPackaged) {
		const backendRoot = path.join(process.resourcesPath, "backend");
		const exePath = path.join(backendRoot, "SlideGenerator.Presentation.exe");
		if (fsSync.existsSync(exePath)) {
			return { command: exePath, args: [], cwd: backendRoot };
		}
		const dllPath = path.join(backendRoot, "SlideGenerator.Presentation.dll");
		if (fsSync.existsSync(dllPath)) {
			return { command: "dotnet", args: [dllPath], cwd: backendRoot };
		}
		return null;
	}

	const projectPath = path.join(
		process.cwd(),
		"..",
		"backend",
		"src",
		"SlideGenerator.Presentation",
		"SlideGenerator.Presentation.csproj"
	);
	if (!fsSync.existsSync(projectPath)) return null;
	return {
		command: "dotnet",
		args: ["run", "--project", projectPath],
		cwd: path.dirname(projectPath),
	};
};

const startBackend = () => {
	if (!shouldStartBackend() || backendProcess) return;
	const launch = resolveBackendCommand();
	if (!launch) return;

	backendWorkingDir = launch.cwd;
	backendProcess = spawn(launch.command, launch.args, {
		cwd: launch.cwd,
		windowsHide: true,
		stdio: "ignore",
	});

	backendProcess.on("exit", () => {
		backendProcess = null;
	});
};

const stopBackend = () => {
	if (!backendProcess) return;
	const proc = backendProcess;
	backendProcess = null;
	const timeout = setTimeout(() => {
		if (!proc.killed) {
			proc.kill();
		}
	}, 5000);
	proc.once("exit", () => {
		clearTimeout(timeout);
	});
	try {
		proc.kill("SIGINT");
	} catch {
		proc.kill();
	}
};

const restartBackend = () => {
	stopBackend();
	startBackend();
	return Boolean(backendProcess);
};

const resolveBackendConfigPath = () => {
	const cwd = backendWorkingDir ?? process.cwd();
	const candidate = path.join(cwd, "backend.config.yaml");
	if (fsSync.existsSync(candidate)) return candidate;
	const fallback = path.join(process.resourcesPath, "backend", "backend.config.yaml");
	return fsSync.existsSync(fallback) ? fallback : candidate;
};

function createWindow() {
	mainWindow = new BrowserWindow({
		width: 1200,
		height: 800,
		minWidth: 800,
		minHeight: 600,
		icon: path.join(__dirname, "../assets/images/app-icon.png"),
		frame: false,
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

function ensureTray() {
	if (tray || !mainWindow) return;

	tray = new Tray(path.join(__dirname, "../assets/images/app-icon.png"));
	tray.setToolTip("Slide Generator");
	tray.setContextMenu(
		Menu.buildFromTemplate([
			{
				label: "Show",
				click: () => {
					mainWindow?.show();
					mainWindow?.focus();
				},
			},
			{
				label: "Hide",
				click: () => {
					mainWindow?.hide();
				},
			},
			{ type: "separator" },
			{
				label: "Quit",
				click: () => {
					isQuitting = true;
					app.quit();
				},
			},
		])
	);
	tray.on("click", () => {
		mainWindow?.show();
		mainWindow?.focus();
	});
}

app.whenReady().then(() => {
	startBackend();
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

app.on("before-quit", () => {
	isQuitting = true;
	stopBackend();
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
		defaultPath: "task-config.json",
	});
	return result.filePath;
});

ipcMain.handle("dialog:openUrl", async (_, url: string) => {
	await shell.openExternal(url);
});

ipcMain.handle("dialog:openPath", async (_, filePath: string) => {
	await shell.openPath(filePath);
});

ipcMain.handle("window:control", async (_, action: string) => {
	if (!mainWindow) return;

	switch (action) {
		case "minimize":
			mainWindow.minimize();
			break;
		case "maximize":
			if (mainWindow.isMaximized()) {
				mainWindow.unmaximize();
			} else {
				mainWindow.maximize();
			}
			break;
		case "close":
			if (isQuitting) {
				mainWindow.close();
			} else {
				mainWindow.close();
			}
			break;
	}
});

ipcMain.handle("window:hideToTray", async () => {
	if (!mainWindow) return;
	ensureTray();
	mainWindow.hide();
});

ipcMain.handle("window:setProgress", async (_, value: number) => {
	if (!mainWindow) return;
	mainWindow.setProgressBar(value);
});

ipcMain.handle("backend:restart", async () => {
	return restartBackend();
});

ipcMain.handle("backend:getConfig", async () => {
	try {
		const configPath = resolveBackendConfigPath();
		const data = await fs.readFile(configPath, "utf-8");
		return data;
	} catch (error) {
		console.error("Error reading backend config:", error);
		return null;
	}
});

ipcMain.handle("settings:read", async (_, filename: string) => {
	try {
		const settingsPath = path.isAbsolute(filename)
			? filename
			: path.join(app.getPath("userData"), filename);
		const data = await fs.readFile(settingsPath, "utf-8");
		return data;
	} catch (error) {
		// File doesn't exist or can't be read, return null
		return null;
	}
});

ipcMain.handle("settings:write", async (_, filename: string, data: string) => {
	try {
		const settingsPath = path.isAbsolute(filename)
			? filename
			: path.join(app.getPath("userData"), filename);
		await fs.writeFile(settingsPath, data, "utf-8");
		return true;
	} catch (error) {
		console.error("Error writing settings:", error);
		return false;
	}
});
