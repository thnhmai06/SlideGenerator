import { checkHealth } from './api';

describe('checkHealth', () => {
	beforeEach(() => {
		window.desktopAPI = {
			...window.desktopAPI,
			backendRequest: vi.fn(),
		} as typeof window.desktopAPI;
	});

	it('returns ok when backend is running', async () => {
		vi.mocked(window.desktopAPI.backendRequest).mockResolvedValue({ ok: true });

		await expect(checkHealth()).resolves.toEqual({
			status: 'ok',
			message: 'Backend is running',
		});
	});

	it('returns unknown when backend reports not running', async () => {
		vi.mocked(window.desktopAPI.backendRequest).mockResolvedValue({ ok: false });

		await expect(checkHealth()).resolves.toEqual({
			status: 'unknown',
			message: 'Backend status unknown',
		});
	});

	it('throws when backend request fails', async () => {
		vi.mocked(window.desktopAPI.backendRequest).mockRejectedValue(new Error('request failed'));

		await expect(checkHealth()).rejects.toThrow('request failed');
	});
});
