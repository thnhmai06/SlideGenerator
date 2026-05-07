import type { Meta, StoryObj } from '@storybook/react';
import { ProcessHeader } from './ProcessHeader';
import React from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../ProcessMenu.css';

const ProcessHeaderWrapper = (args: any) => {
  const { t } = useApp();
  return <ProcessHeader {...args} t={t} />;
};

const meta: Meta<typeof ProcessHeader> = {
  title: 'Features/Process/ProcessHeader',
  component: ProcessHeader,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof ProcessHeader>;

export const Default: Story = {
  render: (args) => <ProcessHeaderWrapper {...args} />,
  args: {
    hasProcessing: true,
    activeGroupsCount: 2,
    onPauseResumeAll: () => alert('Pause/Resume All clicked'),
    onStopAll: () => alert('Stop All clicked'),
    onOpenDashboard: () => alert('Open Dashboard clicked'),
  },
};

export const Idle: Story = {
  render: (args) => <ProcessHeaderWrapper {...args} />,
  args: {
    hasProcessing: false,
    activeGroupsCount: 0,
    onPauseResumeAll: () => {},
    onStopAll: () => {},
    onOpenDashboard: () => {},
  },
};
