import { useContext } from 'react';
import { JobContext } from './JobContextType';

/**
 * Hook to access job management functions and state.
 *
 * @returns Job context with groups, control functions, and utilities.
 * @throws Error if used outside JobProvider.
 */
export const useJobs = () => {
	const context = useContext(JobContext);
	if (!context) {
		throw new Error('useJobs must be used within JobProvider');
	}
	return context;
};
