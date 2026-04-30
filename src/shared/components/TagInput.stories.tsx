import type { Meta, StoryObj } from '@storybook/react';
import TagInput from './TagInput';
import React, { useState } from 'react';

const meta: Meta<typeof TagInput> = {
  title: 'Shared/TagInput',
  component: TagInput,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof TagInput>;

const TagInputWithState = (args: any) => {
  const [value, setValue] = useState(args.value || []);
  return <TagInput {...args} value={value} onChange={setValue} />;
};

export const Default: Story = {
  render: (args) => <TagInputWithState {...args} />,
  args: {
    value: ['React', 'TypeScript'],
    suggestions: ['React', 'TypeScript', 'Vite', 'Storybook', 'Tauri', 'CSS', 'HTML'],
    placeholder: 'Add a tag...',
  },
};

export const Empty: Story = {
  render: (args) => <TagInputWithState {...args} />,
  args: {
    value: [],
    suggestions: ['React', 'TypeScript', 'Vite', 'Storybook', 'Tauri'],
    placeholder: 'Search tags...',
  },
};

export const ManySuggestions: Story = {
  render: (args) => <TagInputWithState {...args} />,
  args: {
    value: ['Vite'],
    suggestions: Array.from({ length: 20 }, (_, i) => `Suggestion ${i + 1}`),
    placeholder: 'Type to see more...',
  },
};
