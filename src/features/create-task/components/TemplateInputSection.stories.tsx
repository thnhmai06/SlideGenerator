import type { Meta, StoryObj } from '@storybook/react';
import { TemplateInputSection, TemplateInputSectionProps } from './TemplateInputSection';
import React, { useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../CreateTaskMenu.css';

const TemplateInputSectionWrapper = (args: TemplateInputSectionProps) => {

  const { t } = useApp();
  const [path, setPath] = useState(args.slidePath || '');
  return <TemplateInputSection {...args} slidePath={path} onChangePath={setPath} t={t} />;
};

const meta: Meta<typeof TemplateInputSection> = {
  title: 'Features/CreateTask/TemplateInputSection',
  component: TemplateInputSection,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof TemplateInputSection>;

export const Empty: Story = {
  render: (args) => <TemplateInputSectionWrapper {...args} />,
  args: {
    slidePath: '',
    templateLoaded: false,
    isLoadingShapes: false,
    isLoadingPlaceholders: false,
    onBrowse: () => alert('Browse clicked'),
  },
};

export const Loading: Story = {
  render: (args) => <TemplateInputSectionWrapper {...args} />,
  args: {
    slidePath: 'C:\\path\\to\\template.pptx',
    templateLoaded: false,
    isLoadingShapes: true,
    isLoadingPlaceholders: false,
    onBrowse: () => {},
  },
};

export const Loaded: Story = {
  render: (args) => <TemplateInputSectionWrapper {...args} />,
  args: {
    slidePath: 'C:\\path\\to\\template.pptx',
    templateLoaded: true,
    isLoadingShapes: false,
    isLoadingPlaceholders: false,
    textShapeCount: 12,
    imageShapeCount: 5,
    onBrowse: () => alert('Browse clicked'),
  },
};
