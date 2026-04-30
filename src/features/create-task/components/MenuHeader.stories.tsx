import type { Meta, StoryObj } from '@storybook/react';
import { MenuHeader } from './MenuHeader';
import React from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../CreateTaskMenu.css';

const MenuHeaderWrapper = (args: any) => {
  const { t } = useApp();
  return <MenuHeader {...args} t={t} />;
};

const meta: Meta<typeof MenuHeader> = {
  title: 'Features/CreateTask/MenuHeader',
  component: MenuHeader,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof MenuHeader>;

export const Default: Story = {
  render: (args) => <MenuHeaderWrapper {...args} />,
  args: {
    onImport: () => alert('Import clicked'),
    onExport: () => alert('Export clicked'),
    onClear: () => alert('Clear clicked'),
  },
};
