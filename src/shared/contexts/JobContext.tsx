import React, { ReactNode } from 'react';
import { JobContext } from './JobContextType';
import { useJobProvider } from './hooks';

export const JobProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
	const value = useJobProvider();

	return <JobContext.Provider value={value}>{children}</JobContext.Provider>;
};
