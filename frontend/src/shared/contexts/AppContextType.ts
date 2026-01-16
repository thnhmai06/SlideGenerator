import { createContext } from 'react';
import type { Language } from '@/shared/locales';

/** Available theme options. */
export type Theme = 'dark' | 'light' | 'system';

/** Persisted application settings. */
export interface Settings {
	theme: Theme;
	language: Language;
	enableAnimations: boolean;
	closeToTray: boolean;
}

/** App context value with settings and translation function. */
export interface AppContextType {
	/** Current theme setting. */
	theme: Theme;
	/** Current language. */
	language: Language;
	/** Whether animations are enabled. */
	enableAnimations: boolean;
	/** Whether to minimize to tray on close. */
	closeToTray: boolean;
	/** Update theme setting. */
	setTheme: (theme: Theme) => void;
	/** Update language setting. */
	setLanguage: (language: Language) => void;
	/** Toggle animations. */
	setEnableAnimations: (enable: boolean) => void;
	/** Toggle close-to-tray behavior. */
	setCloseToTray: (enable: boolean) => void;
	/** Translate a key to current language. */
	t: (key: string) => string;
}

export const AppContext = createContext<AppContextType | undefined>(undefined);
