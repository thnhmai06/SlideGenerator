import { useContext } from 'react';
import { AppContext } from './AppContextType';

/**
 * Hook to access app settings and translation function.
 *
 * @returns App context with theme, language, settings, and `t()` function.
 * @throws Error if used outside AppProvider.
 */
export const useApp = () => {
	const context = useContext(AppContext);
	if (!context) {
		throw new Error('useApp must be used within AppProvider');
	}
	return context;
};
