export const isTauriRuntime = (): boolean => {
	if (typeof window === 'undefined') return false;
	const scope = window as unknown as Record<string, unknown>;
	return Boolean(scope.__TAURI_INTERNALS__ || scope.__TAURI__);
};

export const isDesktopRuntime = (): boolean => isTauriRuntime();

export const isBrowserShowcaseRuntime = (): boolean => !isTauriRuntime();
