import {
	BACKEND_URL_KEY,
	DEFAULT_BACKEND_URL,
	PENDING_BACKEND_URL_KEY,
	PENDING_BACKEND_URL_SESSION_KEY,
} from './constants';

/**
 * Normalizes a backend URL by applying the following transformations:
 * - Trims whitespace
 * - Adds `http://` scheme if missing
 * - Converts `localhost` to `127.0.0.1` for consistency
 * - Removes trailing slash
 *
 * @param url - The raw URL string to normalize
 * @returns The normalized URL, or empty string if input is empty/whitespace
 *
 * @example
 * normalizeBaseUrl('localhost:8080/') // => 'http://127.0.0.1:8080'
 * normalizeBaseUrl('https://api.example.com') // => 'https://api.example.com'
 */
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

/**
 * Resolves the backend base URL for SignalR connection.
 *
 * This function implements a "pending URL" mechanism that allows URL changes
 * to take effect only once per browser session, preventing connection issues
 * during active sessions.
 *
 * @returns The normalized backend base URL to use for connections
 *
 * @remarks
 * URL resolution priority:
 * 1. If a pending URL exists and hasn't been promoted this session, it becomes active
 * 2. Otherwise, uses the stored URL from localStorage
 * 3. Falls back to DEFAULT_BACKEND_URL if no URL is configured
 */
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
