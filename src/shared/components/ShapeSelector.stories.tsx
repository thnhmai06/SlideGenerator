import type { Meta, StoryObj } from '@storybook/react';
import ShapeSelector from './ShapeSelector';
import React, { useState } from 'react';

// Import images to be used in stories
import appIcon from '../../../assets/images/app-icon.png';
import processIcon from '../../../assets/images/process.png';
import settingIcon from '../../../assets/images/setting.png';

const meta: Meta<typeof ShapeSelector> = {
  title: 'Shared/ShapeSelector',
  component: ShapeSelector,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof ShapeSelector>;

const ShapeSelectorWithState = (args: any) => {
  const [value, setValue] = useState(args.value || '');
  return <ShapeSelector {...args} value={value} onChange={setValue} />;
};

const sampleShapes = [
  { id: '1', name: 'Main Title', preview: appIcon },
  { id: '2', name: 'Process Flow', preview: processIcon },
  { id: '3', name: 'Settings Gear', preview: settingIcon },
];

export const Default: Story = {
  render: (args) => <ShapeSelectorWithState {...args} />,
  args: {
    shapes: sampleShapes,
    value: '1',
  },
};

export const Empty: Story = {
  render: (args) => <ShapeSelectorWithState {...args} />,
  args: {
    shapes: [],
    value: '',
    placeholder: 'No shapes available',
  },
};

export const ManyShapes: Story = {
  render: (args) => <ShapeSelectorWithState {...args} />,
  args: {
    shapes: [
      ...sampleShapes,
      { id: '4', name: 'Duplicate 1', preview: appIcon },
      { id: '5', name: 'Duplicate 2', preview: processIcon },
      { id: '6', name: 'Duplicate 3', preview: settingIcon },
    ],
    value: '2',
  },
};
