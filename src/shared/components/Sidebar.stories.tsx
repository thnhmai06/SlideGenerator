import type { Meta, StoryObj } from '@storybook/react';
import Sidebar, { MenuType } from './Sidebar';
import React, { useState } from 'react';

const meta: Meta<typeof Sidebar> = {
  title: 'Shared/Sidebar',
  component: Sidebar,
  parameters: {
    layout: 'fullscreen',
  },
  decorators: [
    (Story) => (
      <div style={{ height: '100vh', display: 'flex' }}>
        <Story />
        <div style={{ flex: 1, backgroundColor: 'var(--bg-primary)' }} />
      </div>
    ),
  ],
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof Sidebar>;

const SidebarWithState = (args: any) => {
  const [currentMenu, setCurrentMenu] = useState<MenuType>(args.currentMenu || 'input');
  return <Sidebar {...args} currentMenu={currentMenu} onMenuChange={setCurrentMenu} />;
};

export const Default: Story = {
  render: (args) => <SidebarWithState {...args} />,
  args: {
    currentMenu: 'input',
  },
};

export const ProcessActive: Story = {
  render: (args) => <SidebarWithState {...args} />,
  args: {
    currentMenu: 'process',
  },
};

export const ResultsActive: Story = {
  render: (args) => <SidebarWithState {...args} />,
  args: {
    currentMenu: 'download',
  },
};

export const SettingActive: Story = {
  render: (args) => <SidebarWithState {...args} />,
  args: {
    currentMenu: 'setting',
  },
};

export const AboutActive: Story = {
  render: (args) => <SidebarWithState {...args} />,
  args: {
    currentMenu: 'about',
  },
};
