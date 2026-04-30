const SETTINGS_PREFIX = 'slidegen.browser.settings.';
const LEGACY_SHOWCASE_INPUT_STATE_KEY = 'slidegen.ui.inputsideBar.state';

const backendUnavailable = <TResult>(method: string): TResult =>
	({
		type: 'error',
		kind: 'BackendUnavailable',
		message: `Backend is not available in browser runtime: ${method}`,
	}) as TResult;

const createBrowserDesktopApi = (): Window['desktopAPI'] => ({
	async openFile() {
		return undefined;
	},
	async openMultipleFiles() {
		return [];
	},
	async openFolder() {
		return undefined;
	},
	async saveFile() {
		return undefined;
	},
	async openUrl(url) {
		window.open(url, '_blank', 'noopener,noreferrer');
	},
	async openPath(path) {
		console.info(`Browser runtime cannot open local path: ${path}`);
	},
	async readSettings(filename) {
		return localStorage.getItem(`${SETTINGS_PREFIX}${filename}`);
	},
	async writeSettings(filename, data) {
		localStorage.setItem(`${SETTINGS_PREFIX}${filename}`, data);
		return true;
	},
	async windowControl() {},
	async hideToTray() {},
	async setProgressBar() {},
	async backendRequest<TResult = unknown>(method: string) {
		if (method === 'system.health') {
			return { ok: false, message: 'Backend not connected' } as TResult;
		}

		if (method === 'jobs.list') return [] as TResult;
		if (method === 'jobs.get') return null as TResult;
		if (method === 'jobs.logs') return { logs: [] } as TResult;

		return backendUnavailable<TResult>(method);
	},
	onBackendNotification() {
		return () => {};
	},
	async restartBackend() {
		return false;
	},
	logRenderer() {},
	onNavigate() {
		return () => {};
	},
	async setTrayLocale() {},
	async checkForUpdates() {
		return { status: 'unsupported' };
	},
	async downloadUpdate() {
		return false;
	},
	installUpdate() {},
	onUpdateStatus() {
		return () => {};
	},
	async isPortable() {
		return false;
	},
});

const clearLegacyShowcaseState = () => {
	const saved = sessionStorage.getItem(LEGACY_SHOWCASE_INPUT_STATE_KEY);
	if (!saved) return;

	try {
		const state = JSON.parse(saved) as {
			slidePath?: string;
			dataPath?: string;
			savePath?: string;
		};
		const serializedPaths = [state.slidePath, state.dataPath, state.savePath].join('\n');
		if (/showcase|SlideGenerator\\(Templates|Data|Output)/i.test(serializedPaths)) {
			sessionStorage.removeItem(LEGACY_SHOWCASE_INPUT_STATE_KEY);
		}
	} catch {
		sessionStorage.removeItem(LEGACY_SHOWCASE_INPUT_STATE_KEY);
	}
};

export const installBrowserDesktopApi = () => {
	if (window.desktopAPI) return;
	clearLegacyShowcaseState();
	window.desktopAPI = createBrowserDesktopApi();
};
