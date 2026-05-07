import type { Meta, StoryObj } from '@storybook/react';
import { SettingsTabs } from './SettingsTabs';
import React, { useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { SettingTab } from '../types';
import '../SettingMenu.css';

const SettingsTabsWrapper = (args: any) => {
  const { t } = useApp();
  const [activeTab, setActiveTab] = useState<SettingTab>(args.activeTab || 'appearance');
  return <SettingsTabs {...args} activeTab={activeTab} onSelectTab={setActiveTab} t={t} />;
};

const meta: Meta<typeof SettingsTabs> = {
  title: 'Features/Settings/SettingsTabs',
  component: SettingsTabs,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof SettingsTabs>;

export const Default: Story = {
  render: (args) => <SettingsTabsWrapper {...args} />,
  args: {
    activeTab: 'appearance',
  },
};
