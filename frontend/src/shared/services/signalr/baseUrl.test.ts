import {
	BACKEND_URL_KEY,
	DEFAULT_BACKEND_URL,
	PENDING_BACKEND_URL_KEY,
	PENDING_BACKEND_URL_SESSION_KEY,
} from '@/shared/services/signalr/constants';
import { getBackendBaseUrl, normalizeBaseUrl } from './baseUrl';

describe('normalizeBaseUrl', () => {
	it('normalizes empty inputs', () => {
		expect(normalizeBaseUrl('')).toBe('');
		expect(normalizeBaseUrl('   ')).toBe('');
	});

	it('adds scheme and normalizes localhost', () => {
		expect(normalizeBaseUrl('localhost:65500/')).toBe('http://127.0.0.1:65500');
	});

	it('keeps scheme and removes trailing slash', () => {
		expect(normalizeBaseUrl('https://example.com/')).toBe('https://example.com');
	});
});

describe('getBackendBaseUrl', () => {
	beforeEach(() => {
		localStorage.clear();
		sessionStorage.clear();
	});

	it('returns default when no stored urls exist', () => {
		expect(getBackendBaseUrl()).toBe(normalizeBaseUrl(DEFAULT_BACKEND_URL));
	});

	it('promotes pending url when allowed', () => {
		localStorage.setItem(PENDING_BACKEND_URL_KEY, 'localhost:65000/');
		const value = getBackendBaseUrl();
		expect(value).toBe('http://127.0.0.1:65000');
		expect(localStorage.getItem(PENDING_BACKEND_URL_KEY)).toBeNull();
		expect(localStorage.getItem(BACKEND_URL_KEY)).toBe('http://127.0.0.1:65000');
	});

	it('keeps pending url when session defers promotion', () => {
		localStorage.setItem(PENDING_BACKEND_URL_KEY, 'localhost:65000/');
		localStorage.setItem(BACKEND_URL_KEY, 'http://stored:65001');
		sessionStorage.setItem(PENDING_BACKEND_URL_SESSION_KEY, '1');

		const value = getBackendBaseUrl();
		expect(value).toBe('http://stored:65001');
		expect(localStorage.getItem(PENDING_BACKEND_URL_KEY)).toBe('localhost:65000/');
	});
});
