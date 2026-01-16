import type { ConfigGetSuccess } from '@/shared/services/backend/config/types';
import type { ConfigState } from '../types';

export const getErrorDetail = (error: unknown): string => {
	if (error instanceof Error && error.message) return error.message;
	if (typeof error === 'string') return error;
	if (error && typeof error === 'object' && 'message' in error) {
		const value = (error as { message?: string }).message;
		if (value) return value;
	}
	return '';
};

export const parseConfigResponse = (data: ConfigGetSuccess) => {
	const config: ConfigState = {
		server: {
			host: data.server.host ?? '',
			port: data.server.port ?? 0,
			debug: data.server.debug ?? false,
		},
		download: {
			maxChunks: data.download.maxChunks ?? 0,
			limitBytesPerSecond: data.download.limitBytesPerSecond ?? 0,
			saveFolder: data.download.saveFolder ?? '',
			retryTimeout: data.download.retry.timeout ?? 0,
			maxRetries: data.download.retry.maxRetries ?? 0,
		},
		job: {
			maxConcurrentJobs: data.job.maxConcurrentJobs ?? 0,
		},
		image: {
			face: {
				confidence: data.image.face.confidence ?? 0,
				unionAll: data.image.face.unionAll ?? false,
			},
			saliency: {
				paddingTop: data.image.saliency.paddingTop ?? 0,
				paddingBottom: data.image.saliency.paddingBottom ?? 0,
				paddingLeft: data.image.saliency.paddingLeft ?? 0,
				paddingRight: data.image.saliency.paddingRight ?? 0,
			},
		},
	};

	return {
		config,
		server: config.server,
	};
};

export const splitNotificationText = (text: string) => {
	const idx = text.indexOf(':');
	if (idx <= 0 || idx === text.length - 1) {
		return { title: text.trim(), detail: '' };
	}
	return {
		title: text.slice(0, idx).trim(),
		detail: text.slice(idx + 1).trim(),
	};
};

export const buildBackendUrl = (host: string, port: number): string | undefined => {
	if (!host || !port) return;
	const trimmedHost = host.trim();
	if (!trimmedHost) return;

	const hasScheme = /^https?:\/\//i.test(trimmedHost);
	const base = hasScheme ? trimmedHost : `http://${trimmedHost}`;
	const normalizedHost = base.replace(/^(https?:\/\/)localhost(?=[:/]|$)/i, '$1127.0.0.1');
	const normalizedBase = normalizedHost.endsWith('/')
		? normalizedHost.slice(0, -1)
		: normalizedHost;
	const hasPort = /:\d+$/.test(normalizedBase);
	return hasPort ? normalizedBase : `${normalizedBase}:${port}`;
};

export const normalizeBackendUrl = (url: string): string => {
	const trimmed = url.trim();
	if (!trimmed) return '';
	const withScheme = /^https?:\/\//i.test(trimmed) ? trimmed : `http://${trimmed}`;
	const normalizedHost = withScheme.replace(/^(https?:\/\/)localhost(?=[:/]|$)/i, '$1127.0.0.1');
	return normalizedHost.endsWith('/') ? normalizedHost.slice(0, -1) : normalizedHost;
};

export const createPadStyles = (padding: {
	paddingTop: number;
	paddingBottom: number;
	paddingLeft: number;
	paddingRight: number;
}) => {
	const baseInset = 10;
	const detectInset = 25;
	const range = detectInset - baseInset;
	const clamp01 = (value: number) => Math.min(1, Math.max(0, Number.isFinite(value) ? value : 0));
	const resolveInset = (value: number) => `${detectInset - range * clamp01(value)}%`;
	return {
		base: { inset: `${baseInset}%` },
		detect: { inset: `${detectInset}%` },
		crop: {
			top: resolveInset(padding.paddingTop),
			right: resolveInset(padding.paddingRight),
			bottom: resolveInset(padding.paddingBottom),
			left: resolveInset(padding.paddingLeft),
		},
	};
};
