import { http, HttpResponse } from 'msw';
import { server } from '../../../../../test/mocks/server';
import { DEFAULT_BACKEND_URL } from '@/shared/services/signalr/constants';
import { checkHealth } from './api';

describe('checkHealth', () => {
	beforeEach(() => {
		localStorage.clear();
	});

	it('returns ok when backend is running', async () => {
		await expect(checkHealth()).resolves.toEqual({
			status: 'ok',
			message: 'Backend is running',
		});
	});

	it('returns unknown when backend reports not running', async () => {
		server.use(
			http.get(`${DEFAULT_BACKEND_URL}/health`, () => {
				return HttpResponse.json({ IsRunning: false });
			}),
		);

		await expect(checkHealth()).resolves.toEqual({
			status: 'unknown',
			message: 'Backend status unknown',
		});
	});

	it('throws when backend responds with error', async () => {
		server.use(
			http.get(`${DEFAULT_BACKEND_URL}/health`, () => {
				return new HttpResponse(null, { status: 500 });
			}),
		);

		await expect(checkHealth()).rejects.toThrow('Backend server is not responding');
	});
});
