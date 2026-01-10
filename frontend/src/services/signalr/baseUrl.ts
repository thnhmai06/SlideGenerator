import {
	BACKEND_URL_KEY,
	DEFAULT_BACKEND_URL,
	PENDING_BACKEND_URL_KEY,
	PENDING_BACKEND_URL_SESSION_KEY,
} from './constants';

export function normalizeBaseUrl(url: string): string {
	const trimmed = url.trim();
	if (!trimmed) return '';

	const withScheme = /^https?:\/\//i.test(trimmed) ? trimmed : `http://${trimmed}`;

	const normalizedHost = withScheme.replace(
		/^(https?:\/\/)localhost(?=[:/]|$)/i,
		(_, scheme: string) => `${scheme}127.0.0.1`,
	);

	return normalizedHost.endsWith('/') ? normalizedHost.slice(0, -1) : normalizedHost;
}

export function getBackendBaseUrl(): string {
	const pending = localStorage.getItem(PENDING_BACKEND_URL_KEY) ?? '';
	const canPromote =
		typeof sessionStorage === 'undefined' ||
		!sessionStorage.getItem(PENDING_BACKEND_URL_SESSION_KEY);

	if (pending && canPromote) {
		const normalizedPending = normalizeBaseUrl(pending);
		if (normalizedPending) {
			localStorage.setItem(BACKEND_URL_KEY, normalizedPending);
		}
		localStorage.removeItem(PENDING_BACKEND_URL_KEY);
	}

	const stored = localStorage.getItem(BACKEND_URL_KEY) ?? '';
	const normalized = normalizeBaseUrl(stored);
	return normalized || normalizeBaseUrl(DEFAULT_BACKEND_URL);
}
