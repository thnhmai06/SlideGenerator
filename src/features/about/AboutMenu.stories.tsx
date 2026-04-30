import type { Meta, StoryObj } from '@storybook/react';
import AboutMenu from './AboutMenu';
import React from 'react';
import './AboutMenu.css';

const meta: Meta<typeof AboutMenu> = {
  title: 'Features/About/AboutMenu',
  component: AboutMenu,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;
type Story = StoryObj<typeof AboutMenu>;

export const Default: Story = {};

export const UpdateAvailable: Story = {
  name: 'Available',
  parameters: {
    updateScenario: 'available',
  },
};

export const Downloading: Story = {
  name: 'Downloading',
  parameters: {
    updateScenario: 'downloading',
  },
};

export const Downloaded: Story = {
  name: 'Downloaded',
  parameters: {
    updateScenario: 'downloaded',
  },
};

export const UpdateError: Story = {
  name: 'Error',
  parameters: {
    updateScenario: 'error',
  },
};
