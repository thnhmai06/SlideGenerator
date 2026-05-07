import type { Meta, StoryObj } from '@storybook/react';
import { PreviewModal, PreviewModalProps } from './PreviewModal';
import React, { useState, useRef } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../CreateTaskMenu.css';
import appIcon from '../../../../assets/images/app-icon.png';

const PreviewModalWrapper = (args: PreviewModalProps) => {
  const { t } = useApp();
  const [zoom, setZoom] = useState(args.previewZoom || 1);
  const [offset] = useState(args.previewOffset || { x: 0, y: 0 });
  const [size, setSize] = useState(args.previewSize || { width: 800, height: 600 });
  const dragMovedRef = useRef(false);

  const adjustZoom = (delta: number) => setZoom((prev: number) => Math.max(0.1, prev + delta));
  const toggleZoom = () => setZoom((prev: number) => (prev > 1 ? 1 : 2));

  return (
    <PreviewModal 
      {...args} 
      previewZoom={zoom}
      previewOffset={offset}
      previewSize={size}
      adjustPreviewZoom={adjustZoom}
      setPreviewZoom={setZoom}
      togglePreviewZoom={toggleZoom}
      setPreviewSize={setSize}
      dragMovedRef={dragMovedRef}
      t={t} 
    />
  );
};

const meta: Meta<typeof PreviewModal> = {
  title: 'Features/CreateTask/PreviewModal',
  component: PreviewModal,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof PreviewModal>;

export const Default: Story = {
  render: (args) => <PreviewModalWrapper {...args} />,
  args: {
    previewShape: { id: '3', name: 'Main Hero Slide', preview: appIcon },
    previewClosing: false,
    previewOffset: { x: 0, y: 0 },
    handleSavePreview: () => alert('Save clicked'),
    closePreview: () => alert('Close clicked'),
  },
};

export const Zoomed: Story = {
  render: (args) => <PreviewModalWrapper {...args} />,
  args: {
    previewShape: { id: '3', name: 'Main Hero Slide', preview: appIcon },
    previewZoom: 2.5,
    previewOffset: { x: 50, y: -30 },
  },
};
