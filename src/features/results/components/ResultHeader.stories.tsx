import type { Meta, StoryObj } from '@storybook/react';
import { ResultHeader } from './ResultHeader';
import React from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../ResultMenu.css';

const ResultHeaderWrapper = (args: any) => {
  const { t } = useApp();
  return <ResultHeader {...args} t={t} />;
};

const meta: Meta<typeof ResultHeader> = {
  title: 'Features/Results/ResultHeader',
  component: ResultHeader,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof ResultHeader>;

export const Default: Story = {
  render: (args) => <ResultHeaderWrapper {...args} />,
  args: {
    completedGroupsCount: 5,
    onClearAll: () => alert('Clear All clicked'),
  },
};

export const Empty: Story = {
  render: (args) => <ResultHeaderWrapper {...args} />,
  args: {
    completedGroupsCount: 0,
    onClearAll: () => {},
  },
};
