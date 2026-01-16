/**
 * Frontend logging service that sends logs to main process via IPC.
 * Log format matches backend: [timestamp] [LEVEL] [Source] message
 */

type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LoggerOptions {
	source: string;
}

interface Logger {
	debug: (message: string, ...args: unknown[]) => void;
	info: (message: string, ...args: unknown[]) => void;
	warn: (message: string, ...args: unknown[]) => void;
	error: (message: string, ...args: unknown[]) => void;
}

const formatMessage = (message: string, args: unknown[]): string => {
	if (args.length === 0) return message;
	// Simple template replacement for objects
	const formatted = args
		.map((arg) => (typeof arg === 'object' ? JSON.stringify(arg) : String(arg)))
		.join(' ');
	return `${message} ${formatted}`;
};

const log = (level: LogLevel, source: string, message: string, args: unknown[]): void => {
	const formattedMessage = formatMessage(message, args);

	// Send to main process for file logging
	if (window.electronAPI?.logRenderer) {
		window.electronAPI.logRenderer(level, formattedMessage, source);
	}

	// Also log to console for development
	const consoleMethod = level === 'debug' ? 'log' : level;
	// eslint-disable-next-line no-console
	console[consoleMethod](`[${source}]`, message, ...args);
};

/**
 * Creates a logger instance with a specific source context.
 *
 * @example
 * const logger = createLogger({ source: 'SignalR' });
 * logger.info('Connected to hub');
 * logger.error('Connection failed', error);
 */
export const createLogger = (options: LoggerOptions): Logger => {
	const { source } = options;

	return {
		debug: (message: string, ...args: unknown[]) => log('debug', source, message, args),
		info: (message: string, ...args: unknown[]) => log('info', source, message, args),
		warn: (message: string, ...args: unknown[]) => log('warn', source, message, args),
		error: (message: string, ...args: unknown[]) => log('error', source, message, args),
	};
};

// Pre-configured loggers for common modules
export const loggers = {
	signalR: createLogger({ source: 'SignalR' }),
	settings: createLogger({ source: 'Settings' }),
	jobs: createLogger({ source: 'Jobs' }),
	config: createLogger({ source: 'Config' }),
	app: createLogger({ source: 'App' }),
};
