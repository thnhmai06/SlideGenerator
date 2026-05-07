import type { Meta, StoryObj } from '@storybook/react';
import TitleBar from './TitleBar';

const meta: Meta<typeof TitleBar> = {
  title: 'Shared/TitleBar',
  component: TitleBar,
  parameters: {
    layout: 'fullscreen',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof TitleBar>;

export const Default: Story = {
  args: {
    title: 'Slide Generator',
  },
};

export const LongTitle: Story = {
  args: {
    title: 'Slide Generator - My Awesome Presentation - Version 2.0.0 (Production Build)',
  },
};
