import { getAssetPath } from './paths';

describe('getAssetPath', () => {
	const original = window.getAssetPath;

	afterEach(() => {
		if (original) {
			window.getAssetPath = original;
		} else {
			delete (window as { getAssetPath?: (...parts: string[]) => string }).getAssetPath;
		}
	});

	it('uses window.getAssetPath when available', () => {
		const spy = vi.fn((...parts: string[]) => `custom/${parts.join('/')}`);
		window.getAssetPath = spy;
		const result = getAssetPath('images', 'app.png');
		expect(result).toBe('custom/images/app.png');
		expect(spy).toHaveBeenCalledWith('images', 'app.png');
	});

	it('falls back to assets path when window helper missing', () => {
		delete (window as { getAssetPath?: (...parts: string[]) => string }).getAssetPath;
		const result = getAssetPath('images', 'app.png');
		expect(result).toBe('assets/images/app.png');
	});
});
