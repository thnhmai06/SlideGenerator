import React from 'react';
import type { SettingsTabsProps } from '../types';

export const SettingsTabs: React.FC<SettingsTabsProps> = ({ activeTab, onSelectTab, t }) => (
	<div className="setting-tabs">
		<button
			className={`tab-button ${activeTab === 'appearance' ? 'active' : ''}`}
			onClick={() => onSelectTab('appearance')}
		>
			{t('settings.appearance')}
		</button>
		<button
			className={`tab-button ${activeTab === 'server' ? 'active' : ''}`}
			onClick={() => onSelectTab('server')}
		>
			{t('settings.server')}
		</button>
		<button
			className={`tab-button ${activeTab === 'download' ? 'active' : ''}`}
			onClick={() => onSelectTab('download')}
		>
			{t('settings.download')}
		</button>
		<button
			className={`tab-button ${activeTab === 'job' ? 'active' : ''}`}
			onClick={() => onSelectTab('job')}
		>
			{t('settings.job')}
		</button>
		<button
			className={`tab-button ${activeTab === 'image' ? 'active' : ''}`}
			onClick={() => onSelectTab('image')}
		>
			{t('settings.image')}
		</button>
	</div>
);
