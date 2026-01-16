import type { Theme } from '@/shared/contexts/AppContextType';
import type { Language } from '@/shared/locales';

export type SettingTab = 'appearance' | 'server' | 'download' | 'job' | 'image';

export interface ConfigState {
	server: {
		host: string;
		port: number;
		debug: boolean;
	};
	download: {
		maxChunks: number;
		limitBytesPerSecond: number;
		saveFolder: string;
		retryTimeout: number;
		maxRetries: number;
		proxy: {
			useProxy: boolean;
			proxyAddress: string;
			username: string;
			password: string;
			domain: string;
		};
	};
	job: {
		maxConcurrentJobs: number;
	};
	image: {
		face: {
			confidence: number;
			unionAll: boolean;
		};
		saliency: {
			paddingTop: number;
			paddingBottom: number;
			paddingLeft: number;
			paddingRight: number;
		};
	};
}

export type TranslationFn = (key: string) => string;

export type NumberChangeHandler = (value: string, apply: (next: number) => void) => void;
export type NumberBlurHandler = (value: string, apply: (next: number) => void) => void;
export type NumberFocusHandler = (event: React.FocusEvent<HTMLInputElement>) => void;

export interface SettingsNotificationsProps {
	showRestartNotification: boolean;
	isRestartNotificationClosing: boolean;
	onRestart: () => void;
	message: { type: 'success' | 'error' | 'warning'; text: string } | null;
	showStatusNotification: boolean;
	isStatusNotificationClosing: boolean;
	onCloseStatus: () => void;
	showLockedNotification: boolean;
	t: TranslationFn;
}

export interface SettingsTabsProps {
	activeTab: SettingTab;
	onSelectTab: (tab: SettingTab) => void;
	t: TranslationFn;
}

export interface AppearanceTabProps {
	theme: Theme;
	language: Language;
	enableAnimations: boolean;
	closeToTray: boolean;
	setTheme: (value: Theme) => void;
	setLanguage: (value: Language) => void;
	setEnableAnimations: (value: boolean) => void;
	setCloseToTray: (value: boolean) => void;
	t: TranslationFn;
}

export interface ServerTabProps {
	loading: boolean;
	config: ConfigState | null;
	canEditConfig: boolean;
	isLocked: boolean;
	updateServer: (patch: Partial<ConfigState['server']>) => void;
	handleNumberChange: NumberChangeHandler;
	handleNumberBlur: NumberBlurHandler;
	handleNumberFocus: NumberFocusHandler;
	t: TranslationFn;
}

export interface DownloadTabProps {
	loading: boolean;
	config: ConfigState | null;
	canEditConfig: boolean;
	isLocked: boolean;
	updateDownload: (patch: Partial<ConfigState['download']>) => void;
	handleNumberChange: NumberChangeHandler;
	handleNumberBlur: NumberBlurHandler;
	handleNumberFocus: NumberFocusHandler;
	onSelectFolder: () => Promise<void>;
	t: TranslationFn;
}

export interface JobTabProps {
	loading: boolean;
	config: ConfigState | null;
	canEditConfig: boolean;
	isLocked: boolean;
	updateJob: (patch: Partial<ConfigState['job']>) => void;
	handleNumberChange: NumberChangeHandler;
	handleNumberBlur: NumberBlurHandler;
	handleNumberFocus: NumberFocusHandler;
	t: TranslationFn;
}

export interface ImageTabProps {
	loading: boolean;
	config: ConfigState | null;
	canEditConfig: boolean;
	isLocked: boolean;
	faceModelAvailable: boolean;
	modelLoading: boolean;
	onInitModel: () => void;
	onDeinitModel: () => void;
	updateFace: (patch: Partial<ConfigState['image']['face']>) => void;
	updateSaliency: (patch: Partial<ConfigState['image']['saliency']>) => void;
	createPadStyles: (padding: {
		paddingTop: number;
		paddingBottom: number;
		paddingLeft: number;
		paddingRight: number;
	}) => {
		base: { inset: string };
		detect: { inset: string };
		crop: { top: string; right: string; bottom: string; left: string };
	};
	handleNumberChange: NumberChangeHandler;
	handleNumberBlur: NumberBlurHandler;
	handleNumberFocus: NumberFocusHandler;
	t: TranslationFn;
}

export interface SettingActionsProps {
	saving: boolean;
	isEditable: boolean;
	showActions: boolean;
	onSave: () => void;
	onReload: () => void;
	onReset: () => void;
	t: TranslationFn;
}
