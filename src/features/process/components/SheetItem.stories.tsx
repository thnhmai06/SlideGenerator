import type { Meta, StoryObj } from '@storybook/react';
import { SheetItem, SheetItemProps } from './SheetItem';
import React, { useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../ProcessMenu.css';


const SheetItemWrapper = (args: SheetItemProps) => {
  const { t } = useApp();
  const [showLog, setShowLog] = useState(args.showLog || false);
  const [collapsedRowGroups, setCollapsedRowGroups] = useState(args.collapsedRowGroups || {});

  const toggleLog = () => setShowLog(!showLog);
  const toggleRowGroup = (key: string) => {
    setCollapsedRowGroups((prev: Record<string, boolean>) => ({ ...prev, [key]: !prev[key] }));
  };

  return (
    <SheetItem 
      {...args} 
      showLog={showLog}
      collapsedRowGroups={collapsedRowGroups}
      onToggleLog={toggleLog}
      onToggleRowGroup={toggleRowGroup}
      statusKey={(status) => status.toLowerCase()}
      progressColor={(status) => status === 'Running' ? '#4caf50' : '#ffa000'}
      formatLogEntry={(entry) => `[${entry.timestamp}] ${entry.message}`}
      t={t} 
    />
  );
};

const meta: Meta<typeof SheetItem> = {
  title: 'Features/Process/SheetItem',
  component: SheetItem,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof SheetItem>;

const sampleSheet = {
  id: 'sheet-1',
  sheetName: 'Marketing Plan',
  hangfireJobId: '12345',
  status: 'Running',
  progress: 45,
  totalRows: 100,
  currentRow: 45,
  errorCount: 0,
  logs: [
    { level: 'info', message: 'Row 1 processing', timestamp: '10:00:01', row: 1 },
    { level: 'info', message: 'Row 1 completed', timestamp: '10:00:05', row: 1 },
    { level: 'warning', message: 'Row 2 image missing', timestamp: '10:00:10', row: 2 },
  ]
};

const sampleLogGroups = [
  { key: 'row-1', row: 1, status: 'Completed', entries: [
    { level: 'info', message: 'Row 1 processing', timestamp: '10:00:01' },
    { level: 'info', message: 'Row 1 completed', timestamp: '10:00:05' },
  ]},
  { key: 'row-2', row: 2, status: 'Warning', entries: [
    { level: 'warning', message: 'Row 2 image missing', timestamp: '10:00:10' },
  ]}
];

export const Running: Story = {
  render: (args) => <SheetItemWrapper {...args} />,
  args: {
    sheet: sampleSheet,
    logGroups: sampleLogGroups,
  },
};

export const Paused: Story = {
  render: (args) => <SheetItemWrapper {...args} />,
  args: {
    sheet: { ...sampleSheet, status: 'Paused' },
    logGroups: sampleLogGroups,
  },
};

export const WithLogs: Story = {
  render: (args) => <SheetItemWrapper {...args} />,
  args: {
    sheet: sampleSheet,
    logGroups: sampleLogGroups,
    showLog: true,
  },
};
