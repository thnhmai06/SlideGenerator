import { app } from 'electron';
import { spawn, ChildProcess } from 'child_process';
import path from 'path';
import fsSync from 'fs';
import log from 'electron-log';

interface BackendLaunch {
	command: string;
	args: string[];
	cwd: string;
}

const shouldStartBackend = () => {
	if (process.env.NODE_ENV === 'development') return false;
	return process.env.SLIDEGEN_DISABLE_BACKEND !== '1';
};

const resolveBackendCommand = (): BackendLaunch | null => {
	const override = process.env.SLIDEGEN_BACKEND_PATH;

	if (override && fsSync.existsSync(override)) {
		const ext = path.extname(override).toLowerCase();

		if (ext === '.dll') {
			return {
				command: 'dotnet',
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
		const backendRoot = path.join(process.resourcesPath, 'backend');
		const exePath = path.join(backendRoot, 'SlideGenerator.Presentation.exe');

		if (fsSync.existsSync(exePath)) {
			return { command: exePath, args: [], cwd: backendRoot };
		}

		const dllPath = path.join(backendRoot, 'SlideGenerator.Presentation.dll');
		if (fsSync.existsSync(dllPath)) {
			return { command: 'dotnet', args: [dllPath], cwd: backendRoot };
		}
	}

	return null;
};

export const createBackendController = (backendLogPath: string) => {
	let backendProcess: ChildProcess | null = null;

	const startBackend = () => {
		if (!shouldStartBackend() || backendProcess) return;
		const launch = resolveBackendCommand();
		if (!launch) return;

		backendProcess = spawn(launch.command, launch.args, {
			cwd: launch.cwd,
			windowsHide: true,
			stdio: 'ignore',
			detached: false,
			env: {
				...process.env,
				SLIDEGEN_LOG_PATH: backendLogPath,
			},
		});

		backendProcess.on('exit', (code) => {
			log.info(`Backend process exited with code ${code}`);
			backendProcess = null;
		});
	};

	const stopBackend = async () => {
		if (!backendProcess) return;
		const proc = backendProcess;
		backendProcess = null;

		return new Promise<void>((resolve) => {
			const timeout = setTimeout(() => {
				if (!proc.killed) {
					log.info('Backend timed out, force killing...');
					proc.kill();
				}
				resolve();
			}, 5000);

			proc.once('exit', () => {
				clearTimeout(timeout);
				resolve();
			});

			try {
				proc.kill('SIGINT');
				setTimeout(() => {
					if (!proc.killed) proc.kill('SIGTERM');
				}, 1000);
			} catch (error) {
				log.error('Error stopping backend:', error);
				proc.kill();
				resolve();
			}
		});
	};

	const restartBackend = async () => {
		await stopBackend();
		startBackend();
		return Boolean(backendProcess);
	};

	return {
		startBackend,
		stopBackend,
		restartBackend,
	};
};
