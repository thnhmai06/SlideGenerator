import type { Meta, StoryObj } from '@storybook/react';
import App from './App';

const meta: Meta<typeof App> = {
	title: 'App',
	component: App,
	parameters: {
		layout: 'fullscreen',
	},
	args: {
		enableHealthPolling: false,
		healthCheck: async () => ({ ok: true }),
	},
};

export default meta;
type Story = StoryObj<typeof App>;

export const Default: Story = {
	name: 'Default / Online',
};

export const BackendDisconnected: Story = {
	name: 'Default / Offline',
	args: {
		initialConnectionBanner: 'disconnected',
		healthCheck: async () => {
			throw new Error('Offline');
		},
	},
};

export const ActiveProcess: Story = {
	name: 'Process / Running & Paused',
	args: { initialMenu: 'process' },
	parameters: { appScenario: 'active' },
};

export const ResultsHistory: Story = {
	name: 'Results / Mixed & Failed',
	args: { initialMenu: 'download' },
	parameters: { appScenario: 'results' },
};
