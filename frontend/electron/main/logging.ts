import { app, ipcMain } from 'electron';
import path from 'path';
import fsSync from 'fs';
import { promises as fs } from 'fs';
import log from 'electron-log';

export interface LogPaths {
	sessionLogFolder: string;
	processLogPath: string;
	rendererLogPath: string;
	backendLogPath: string;
}

/** Maximum age of log folders in days before cleanup */
const LOG_RETENTION_DAYS = 30;

const padNumber = (value: number, length = 2) => String(value).padStart(length, '0');
const formatTimestamp = (time = new Date()) => {
	return (
		`${time.getFullYear()}-${padNumber(time.getMonth() + 1)}-${padNumber(time.getDate())} ` +
		`${padNumber(time.getHours())}:${padNumber(time.getMinutes())}:${padNumber(time.getSeconds())}.` +
		padNumber(time.getMilliseconds(), 3)
	);
};

const formatFolderTimestamp = (time = new Date()) => {
	return (
		`${time.getFullYear()}-${padNumber(time.getMonth() + 1)}-${padNumber(time.getDate())}_` +
		`${padNumber(time.getHours())}-${padNumber(time.getMinutes())}-${padNumber(time.getSeconds())}-` +
		padNumber(time.getMilliseconds(), 3)
	);
};

export const initLogging = (): LogPaths => {
	const runTimestamp = formatFolderTimestamp();
	const appFolder = app.isPackaged ? path.dirname(app.getPath('exe')) : process.cwd();
	const logFolder = path.join(appFolder, 'logs');
	const sessionLogFolder = path.join(logFolder, runTimestamp);

	if (!fsSync.existsSync(sessionLogFolder)) {
		fsSync.mkdirSync(sessionLogFolder, { recursive: true });
	}

	const processLogPath = path.join(sessionLogFolder, 'process.log');
	const rendererLogPath = path.join(sessionLogFolder, 'renderer.log');
	const backendLogPath = path.join(sessionLogFolder, 'backend.log');

	log.initialize({ preload: true });
	const fileTransport = log.transports?.file as unknown as
		| {
				resolvePathFn?: (variables: unknown, message?: unknown) => string;
				format?: string;
		  }
		| undefined;
	if (fileTransport) {
		fileTransport.resolvePathFn = () => processLogPath;
		// Format matching backend: timestamp [LEVEL] [Source] message
		fileTransport.format = '{y}-{m}-{d} {h}:{i}:{s}.{ms} [{level}] [Main] {text}';
	}

	// Also update console format for consistency
	const consoleTransport = log.transports?.console as unknown as { format?: string } | undefined;
	if (consoleTransport) {
		consoleTransport.format = '{y}-{m}-{d} {h}:{i}:{s}.{ms} [{level}] [Main] {text}';
	}

	Object.assign(console, log.functions);
	log.info('Process logger initialized');

	// Cleanup old log folders asynchronously
	cleanupOldLogs(logFolder).catch((error) => {
		log.warn('[Logging] Failed to cleanup old logs:', error);
	});

	return { sessionLogFolder, processLogPath, rendererLogPath, backendLogPath };
};

export const attachProcessOutputCapture = (processLogPath: string) => {
	const appendProcessOutput = (chunk: unknown) => {
		if (typeof chunk !== 'string' && !Buffer.isBuffer(chunk)) return;
		const text = typeof chunk === 'string' ? chunk : chunk.toString('utf-8');
		if (!text) return;
		fs.appendFile(processLogPath, text).catch(() => undefined);
	};

	const stdoutWrite = process.stdout.write.bind(process.stdout) as (...args: unknown[]) => boolean;
	const stderrWrite = process.stderr.write.bind(process.stderr) as (...args: unknown[]) => boolean;
	process.stdout.write = ((chunk: unknown, ...args: unknown[]) => {
		appendProcessOutput(chunk);
		return stdoutWrite(chunk, ...args);
	}) as typeof process.stdout.write;
	process.stderr.write = ((chunk: unknown, ...args: unknown[]) => {
		appendProcessOutput(chunk);
		return stderrWrite(chunk, ...args);
	}) as typeof process.stderr.write;
};

export const registerRendererLogIpc = (rendererLogPath: string) => {
	ipcMain.on('logs:renderer', (_, payload: { level?: string; message?: string; source?: string }) => {
		const level = payload?.level ?? 'info';
		const source = payload?.source ?? 'Renderer';
		const message = payload?.message ?? '';
		const line = `${formatTimestamp()} [${level}] [${source}] ${message}\n`;
		fs.appendFile(rendererLogPath, line).catch((error) => {
			log.warn('Failed to append renderer log:', error);
		});
	});
};

/**
 * Removes log folders older than LOG_RETENTION_DAYS.
 * Folders are expected to be named with timestamp format: YYYY-MM-DD_HH-MM-SS-mmm
 */
const cleanupOldLogs = async (logFolder: string): Promise<void> => {
	const now = Date.now();
	const maxAge = LOG_RETENTION_DAYS * 24 * 60 * 60 * 1000;

	let entries: fsSync.Dirent[];
	try {
		entries = await fs.readdir(logFolder, { withFileTypes: true });
	} catch {
		return; // Log folder doesn't exist yet
	}

	const folderPattern = /^(\d{4})-(\d{2})-(\d{2})_(\d{2})-(\d{2})-(\d{2})-(\d{3})$/;

	for (const entry of entries) {
		if (!entry.isDirectory()) continue;

		const match = folderPattern.exec(entry.name);
		if (!match) continue;

		const [, year, month, day, hour, minute, second] = match.map(Number);
		const folderDate = new Date(year, month - 1, day, hour, minute, second);
		const age = now - folderDate.getTime();

		if (age > maxAge) {
			const folderPath = path.join(logFolder, entry.name);
			try {
				await fs.rm(folderPath, { recursive: true, force: true });
				log.info(`[Logging] Removed old log folder: ${entry.name}`);
			} catch (error) {
				log.warn(`[Logging] Failed to remove old log folder ${entry.name}:`, error);
			}
		}
	}
};
