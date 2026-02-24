import { checkHealth } from './api';

describe('checkHealth', () => {
	beforeEach(() => {
		window.electronAPI = {
			...window.electronAPI,
			backendRequest: vi.fn(),
		} as typeof window.electronAPI;
	});

	it('returns ok when backend is running', async () => {
		vi.mocked(window.electronAPI.backendRequest).mockResolvedValue({ ok: true });

		await expect(checkHealth()).resolves.toEqual({
			status: 'ok',
			message: 'Backend is running',
		});
	});

	it('returns unknown when backend reports not running', async () => {
		vi.mocked(window.electronAPI.backendRequest).mockResolvedValue({ ok: false });

		await expect(checkHealth()).resolves.toEqual({
			status: 'unknown',
			message: 'Backend status unknown',
		});
	});

	it('throws when backend request fails', async () => {
		vi.mocked(window.electronAPI.backendRequest).mockRejectedValue(new Error('request failed'));

		await expect(checkHealth()).rejects.toThrow('request failed');
	});
});
