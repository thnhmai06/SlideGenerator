import React from 'react';
import { AppProvider } from '@/shared/contexts/AppContext';
import { JobProvider } from '@/shared/contexts/JobContext';

type AppProvidersProps = {
	children: React.ReactNode;
};

const AppProviders: React.FC<AppProvidersProps> = ({ children }) => (
	<AppProvider>
		<JobProvider>{children}</JobProvider>
	</AppProvider>
);

export default AppProviders;
