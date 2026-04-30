import type { Meta, StoryObj } from '@storybook/react';
import { DataInputSection, DataInputSectionProps } from './DataInputSection';
import React, { useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../CreateTaskMenu.css';

const DataInputSectionWrapper = (args: DataInputSectionProps) => {
  const { t } = useApp();
  const [path, setPath] = useState(args.dataPath || '');
  const [selectedSheets, setSelectedSheets] = useState<string[]>(args.selectedSheets || []);

  const toggleSheet = (sheet: string) => {
    setSelectedSheets((prev: string[]) => 
      prev.includes(sheet) ? prev.filter(s => s !== sheet) : [...prev, sheet]
    );
  };

  const toggleAll = () => {
    setSelectedSheets(selectedSheets.length === args.sheetNames.length ? [] : args.sheetNames);
  };

  return (
    <DataInputSection 
      {...args} 
      dataPath={path} 
      onChangePath={setPath} 
      selectedSheets={selectedSheets}
      onToggleSheet={toggleSheet}
      onToggleAllSheets={toggleAll}
      allSheetsSelected={selectedSheets.length === args.sheetNames?.length}
      someSheetsSelected={selectedSheets.length > 0 && selectedSheets.length < args.sheetNames?.length}
      t={t} 
    />
  );
};

const meta: Meta<typeof DataInputSection> = {
  title: 'Features/CreateTask/DataInputSection',
  component: DataInputSection,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof DataInputSection>;

export const Empty: Story = {
  render: (args) => <DataInputSectionWrapper {...args} />,
  args: {
    dataPath: '',
    dataLoaded: false,
    isLoadingColumns: false,
    onBrowse: () => alert('Browse clicked'),
    sheetNames: [],
    selectedSheets: [],
    sheetRowCounts: {},
  },
};

export const Loaded: Story = {
  render: (args) => <DataInputSectionWrapper {...args} />,
  args: {
    dataPath: 'C:\\path\\to\\data.xlsx',
    dataLoaded: true,
    isLoadingColumns: false,
    sheetCount: 3,
    uniqueColumnCount: 15,
    totalRows: 150,
    sheetNames: ['Marketing Plan', 'Quarterly Review', 'Financial Data'],
    selectedSheets: ['Marketing Plan'],
    sheetRowCounts: {
      'Marketing Plan': 50,
      'Quarterly Review': 50,
      'Financial Data': 50,
    },
    onBrowse: () => alert('Browse clicked'),
  },
};
