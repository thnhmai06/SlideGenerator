import type { Meta, StoryObj } from '@storybook/react';
import { ImageReplacementPanel } from './ImageReplacementPanel';
import React, { useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../CreateTaskMenu.css';
import appIcon from '../../../../assets/images/app-icon.png';
import processIcon from '../../../../assets/images/process.png';
import settingIcon from '../../../../assets/images/setting.png';

const ImageReplacementPanelWrapper = (args: any) => {
  const { t } = useApp();
  const [show, setShow] = useState(args.showImageConfigs || true);
  const [replacements, setReplacements] = useState(args.imageReplacements || []);

  const updateReplacement = (id: number, field: string, value: any) => {
    setReplacements((prev: any[]) => 
      prev.map(item => item.id === id ? { ...item, [field]: value } : item)
    );
  };

  const removeReplacement = (id: number) => {
    setReplacements((prev: any[]) => prev.filter(item => item.id !== id));
  };

  const addReplacement = () => {
    setReplacements((prev: any[]) => [...prev, { 
      id: Date.now(), 
      shapeId: '', 
      columns: [], 
      roiType: 'RuleOfThirds', 
      cropType: 'Fit' 
    }]);
  };

  return (
    <ImageReplacementPanel 
      {...args} 
      showImageConfigs={show}
      setShowImageConfigs={setShow}
      imageReplacements={replacements}
      updateImageReplacement={updateReplacement}
      removeImageReplacement={removeReplacement}
      addImageReplacement={addReplacement}
      getAvailableShapes={(current: string) => {
        const used = replacements.map((r: any) => r.shapeId).filter((s: string) => s !== current && s !== '');
        return args.shapes.filter((s: any) => !used.includes(s.id));
      }}
      t={t} 
    />
  );
};

const meta: Meta<typeof ImageReplacementPanel> = {
  title: 'Features/CreateTask/ImageReplacementPanel',
  component: ImageReplacementPanel,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof ImageReplacementPanel>;

export const Default: Story = {
  render: (args) => <ImageReplacementPanelWrapper {...args} />,
  args: {
    canConfigure: true,
    maxImageConfigs: 5,
    shapes: [
      { id: '3', name: 'Hero Image', preview: appIcon },
      { id: '7', name: 'Author Photo', preview: processIcon },
      { id: '12', name: 'Background', preview: settingIcon },
    ],
    columns: ['HeroURL', 'AuthorURL', 'BackgroundURL'],
    imageReplacements: [],
    roiOptions: [
      { value: 'RuleOfThirds', label: 'Rule of Thirds', description: 'Align focal points with the rule of thirds grid.' },
      { value: 'Center', label: 'Center', description: 'Keep the subject in the exact center of the crop.' },
    ],
    cropOptions: [
      { value: 'Fit', label: 'Fit', description: 'Resize image to fit within the shape without cropping.' },
      { value: 'Fill', label: 'Fill', description: 'Fill the entire shape with the image, cropping if necessary.' },
    ],
    getOptionDescription: (options: any[], value: string) => options.find(o => o.value === value)?.label || '',
    openPreview: (shape: any) => alert(`Preview shape: ${shape.name}`),
  },
};

export const Configured: Story = {
  render: (args) => <ImageReplacementPanelWrapper {...args} />,
  args: {
    ...Default.args,
    imageReplacements: [
      { id: 1, shapeId: '3', columns: ['HeroURL'], roiType: 'RuleOfThirds', cropType: 'Fit' },
    ],
  },
};
