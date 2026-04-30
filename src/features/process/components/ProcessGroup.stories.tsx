import type { Meta, StoryObj } from '@storybook/react';
import { ProcessGroup } from './ProcessGroup';
import React, { useState, useEffect } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { SheetJob } from '@/shared/contexts/JobContextType';
import '../ProcessMenu.css';
import { ProcessGroupProps } from './ProcessGroup';

interface ProcessGroupWrapperProps extends ProcessGroupProps {
  simulateProgress?: boolean;
}

const ProcessGroupWrapper = (args: ProcessGroupWrapperProps) => {
  const { t } = useApp();
  const [group, setGroup] = useState(args.group);
  const [showDetails, setShowDetails] = useState(args.showDetails || false);
  const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>(args.expandedLogs || {});
  const [collapsedRowGroups, setCollapsedRowGroups] = useState<Record<string, boolean>>(args.collapsedRowGroups || {});

  // Simulate progress
  useEffect(() => {
    if (args.simulateProgress && group.status === 'Running') {
      const interval = setInterval(() => {
        setGroup((prev: any) => {
          const newProgress = Math.min(100, prev.progress + 1);
          const newSheets = { ...prev.sheets };
          
          Object.keys(newSheets).forEach(id => {
            const sheet = newSheets[id];
            if (sheet.status === 'Running') {
              const sheetProgress = Math.min(100, sheet.progress + 2);
              newSheets[id] = { 
                ...sheet, 
                progress: sheetProgress,
                currentRow: Math.floor(sheet.totalRows * (sheetProgress / 100)),
                status: sheetProgress === 100 ? 'Completed' : 'Running'
              };
            }
          });

          return { 
            ...prev, 
            progress: newProgress, 
            sheets: newSheets,
            status: newProgress === 100 ? 'Completed' : 'Running'
          };
        });
      }, 500);
      return () => clearInterval(interval);
    }
  }, [args.simulateProgress, group.status]);

  return (
    <ProcessGroup 
      {...args} 
      group={group}
      showDetails={showDetails}
      expandedLogs={expandedLogs}
      collapsedRowGroups={collapsedRowGroups}
      onToggleGroup={() => setShowDetails(!showDetails)}
      onToggleLog={(id) => setExpandedLogs(prev => ({ ...prev, [id]: !prev[id] }))}
      onToggleRowGroup={(key) => setCollapsedRowGroups(prev => ({ ...prev, [key]: !prev[key] }))}
      formatLogEntry={(entry) => `[${entry.timestamp}] ${entry.message}`}
      formatTime={(val) => val || 'Just now'}
      hasGroupConfig={() => true}
      onGroupAction={() => {
        setGroup((prev: any) => ({ ...prev, status: prev.status === 'Paused' ? 'Running' : 'Paused' }));
      }}
      onStopGroup={() => setGroup((prev: any) => ({ ...prev, status: 'Cancelled' }))}
      onExportGroup={() => alert('Export Config clicked')}
      onSheetAction={(id) => {
        setGroup((prev: any) => {
          const newSheets = { ...prev.sheets };
          const sheet = newSheets[id];
          newSheets[id] = { ...sheet, status: sheet.status === 'Paused' ? 'Running' : 'Paused' };
          return { ...prev, sheets: newSheets };
        });
      }}
      onStopSheet={(id) => {
        setGroup((prev: any) => {
          const newSheets = { ...prev.sheets };
          newSheets[id] = { ...newSheets[id], status: 'Cancelled' };
          return { ...prev, sheets: newSheets };
        });
      }}
      onCopyLogs={() => alert('Logs copied to clipboard')}
      t={t} 
    />
  );
};

const meta: Meta<typeof ProcessGroup> = {
  title: 'Features/Process/ProcessGroup',
  component: ProcessGroup,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<ProcessGroupWrapperProps>;

const sampleSheets: Record<string, SheetJob> = {
  'sheet-1': {
    id: 'sheet-1',
    sheetName: 'Marketing Plan',
    hangfireJobId: '101',
    status: 'Running',
    progress: 30,
    totalRows: 50,
    currentRow: 15,
    errorCount: 0,
    logs: [],
  },
  'sheet-2': {
    id: 'sheet-2',
    sheetName: 'Budget 2024',
    hangfireJobId: '102',
    status: 'Running',
    progress: 10,
    totalRows: 100,
    currentRow: 10,
    errorCount: 0,
    logs: [],
  }
};

export const Running: Story = {
  render: (args) => <ProcessGroupWrapper {...args} />,
  args: {
    group: {
      id: 'group-1',
      status: 'Running',
      progress: 20,
      workbookPath: 'C:\\Projects\\Slides\\Campaign.xlsx',
      createdAt: '2024-04-30T08:00:00Z',
      sheets: sampleSheets,
    },
    simulateProgress: true,
  } as any,
};

export const MixedStatus: Story = {
  render: (args) => <ProcessGroupWrapper {...args} />,
  args: {
    group: {
      id: 'group-2',
      status: 'Running',
      progress: 60,
      workbookPath: 'C:\\Projects\\Slides\\Financials.xlsx',
      sheets: {
        'sheet-1': { ...sampleSheets['sheet-1'], status: 'Completed', progress: 100, currentRow: 50 },
        'sheet-2': { ...sampleSheets['sheet-2'], status: 'Failed', progress: 40, currentRow: 40, errorMessage: 'Network error' },
      },
    },
    showDetails: true,
  } as any,
};
