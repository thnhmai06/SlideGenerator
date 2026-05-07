import type { Meta, StoryObj } from '@storybook/react';
import { ResultGroup, ResultGroupProps } from './ResultGroup';
import React, { useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../ResultMenu.css';

const ResultGroupWrapper = (args: ResultGroupProps) => {
  const { t } = useApp();
  const [showDetails, setShowDetails] = useState(args.showDetails || false);
  const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>(args.expandedLogs || {});
  const [collapsedRowGroups, setCollapsedRowGroups] = useState<Record<string, boolean>>(args.collapsedRowGroups || {});

  return (
    <ResultGroup 
      {...args} 
      showDetails={showDetails}
      expandedLogs={expandedLogs}
      collapsedRowGroups={collapsedRowGroups}
      onToggleGroup={() => setShowDetails(!showDetails)}
      onToggleLog={(id) => setExpandedLogs((prev: Record<string, boolean>) => ({ ...prev, [id]: !prev[id] }))}
      onToggleRowGroup={(key) => setCollapsedRowGroups((prev: Record<string, boolean>) => ({ ...prev, [key]: !prev[key] }))}
      formatLogEntry={(entry) => `[${entry.timestamp}] ${entry.message}`}
      formatTime={(val) => val || '2024-05-01 10:30'}
      hasGroupConfig={() => true}
      onOpenFolder={() => alert('Open Folder clicked')}
      onRemoveGroup={() => alert('Remove Group clicked')}
      onExportGroup={() => alert('Export Config clicked')}
      onOpenFile={(path) => alert(`Open File: ${path}`)}
      onRemoveSheet={(id) => alert(`Remove Sheet: ${id}`)}
      onCopyLogs={(sheet) => alert(`Logs copied for: ${sheet.sheetName}`)}
      t={t} 
    />
  );
};

const meta: Meta<typeof ResultGroup> = {
  title: 'Features/Results/ResultGroup',
  component: ResultGroup,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof ResultGroup>;

const mockLogs = [
  { level: 'info', message: 'Starting sheet processing', timestamp: '10:30:01', row: 0 },
  { level: 'info', message: 'Processing row 1', timestamp: '10:30:05', row: 1 },
  { level: 'info', message: 'Row 1 completed successfully', timestamp: '10:30:12', row: 1 },
  { level: 'info', message: 'Processing row 2', timestamp: '10:30:15', row: 2 },
  { level: 'error', message: 'Row 2 failed: Image not found at C:\\Data\\img2.jpg', timestamp: '10:30:20', row: 2 },
  { level: 'info', message: 'Processing row 3', timestamp: '10:30:25', row: 3 },
  { level: 'info', message: 'Row 3 completed successfully', timestamp: '10:30:35', row: 3 },
  { level: 'info', message: 'Sheet processing finished with 1 error', timestamp: '10:30:40', row: 0 },
];

const completedGroup = {
  id: 'group-1',
  status: 'Completed',
  progress: 100,
  workbookPath: 'C:\\Projects\\Slides\\Marketing_Campaign.xlsx',
  completedAt: '2024-05-01T10:30:00Z',
  outputFolder: 'C:\\Projects\\Slides\\Output',
  sheets: {
    'sheet-1': {
      id: 'sheet-1',
      sheetName: 'Personnel List',
      status: 'Completed' as const,
      progress: 100,
      totalRows: 45,
      currentRow: 45,
      errorCount: 0,
      outputPath: 'C:\\Projects\\Slides\\Output\\Personnel_List.pptx',
      logs: mockLogs.filter(l => l.row !== 2),
    }
  },
};

const mixedGroup = {
  id: 'group-2',
  status: 'Completed',
  progress: 100,
  workbookPath: 'C:\\Projects\\Slides\\Employee_Reviews.xlsx',
  completedAt: '2024-05-01T11:45:00Z',
  outputFolder: 'C:\\Projects\\Slides\\Output',
  sheets: {
    'sheet-1': {
      id: 'sheet-1',
      sheetName: 'Q1 Reviews',
      status: 'Completed' as const,
      progress: 100,
      totalRows: 20,
      currentRow: 20,
      errorCount: 0,
      outputPath: 'C:\\Projects\\Slides\\Output\\Q1_Reviews.pptx',
      logs: mockLogs.slice(0, 3),
    },
    'sheet-2': {
      id: 'sheet-2',
      sheetName: 'Q2 Reviews',
      status: 'Failed' as const,
      progress: 85,
      totalRows: 20,
      currentRow: 17,
      errorCount: 1,
      outputPath: 'C:\\Projects\\Slides\\Output\\Q2_Reviews.pptx',
      logs: mockLogs,
    }
  },
};

export const Completed: Story = {
  render: (args) => <ResultGroupWrapper {...args} />,
  args: {
    group: completedGroup,
  },
};

export const MixedResults: Story = {
  render: (args) => <ResultGroupWrapper {...args} />,
  args: {
    group: mixedGroup,
    showDetails: true,
    expandedLogs: { 'sheet-2': true },
  },
};

export const MultipleGroups: Story = {
  render: (args) => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      <ResultGroupWrapper 
        {...args} 
        group={{
          ...completedGroup,
          id: 'group-1',
          workbookPath: 'C:\\Data\\Personnel_Q1.xlsx',
          completedAt: '2024-05-01T09:00:00Z',
        }} 
      />
      <ResultGroupWrapper 
        {...args} 
        group={{
          ...mixedGroup,
          id: 'group-2',
          workbookPath: 'C:\\Data\\Personnel_Q2.xlsx',
          completedAt: '2024-05-01T14:30:00Z',
        }} 
      />
    </div>
  ),
};
