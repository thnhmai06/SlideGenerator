import { createContext } from 'react';
import type { Language } from '@/shared/locales';

export type Theme = 'dark' | 'light' | 'system';

export interface Settings {
	theme: Theme;
	language: Language;
	enableAnimations: boolean;
	closeToTray: boolean;
}

export interface AppContextType {
	theme: Theme;
	language: Language;
	enableAnimations: boolean;
	closeToTray: boolean;
	setTheme: (theme: Theme) => void;
	setLanguage: (language: Language) => void;
	setEnableAnimations: (enable: boolean) => void;
	setCloseToTray: (enable: boolean) => void;
	t: (key: string) => string;
}

export const AppContext = createContext<AppContextType | undefined>(undefined);
