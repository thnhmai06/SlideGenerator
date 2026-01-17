import React from 'react';
import { AppProvider } from '@/shared/contexts/AppContext';
import { JobProvider } from '@/shared/contexts/JobContext';
import { UpdaterProvider } from '@/shared/contexts/UpdaterContext';

type AppProvidersProps = {
	children: React.ReactNode;
};

const AppProviders: React.FC<AppProvidersProps> = ({ children }) => (
	<AppProvider>
		<JobProvider>
			<UpdaterProvider>{children}</UpdaterProvider>
		</JobProvider>
	</AppProvider>
);

export default AppProviders;
