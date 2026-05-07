import type { Meta, StoryObj } from '@storybook/react';
import { InputNotification } from './InputNotification';
import React from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../CreateTaskMenu.css';

const InputNotificationWrapper = (args: any) => {
  const { t } = useApp();
  return <InputNotification {...args} t={t} />;
};

const meta: Meta<typeof InputNotification> = {
  title: 'Features/CreateTask/InputNotification',
  component: InputNotification,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof InputNotification>;

export const Success: Story = {
  render: (args) => <InputNotificationWrapper {...args} />,
  args: {
    notification: { type: 'success', text: 'Cấu hình đã được xuất thành công!' },
    isClosing: false,
    onClose: () => alert('Close clicked'),
  },
};

export const Error: Story = {
  render: (args) => <InputNotificationWrapper {...args} />,
  args: {
    notification: { type: 'error', text: 'Không thể tải file PowerPoint. Vui lòng kiểm tra lại đường dẫn.' },
    isClosing: false,
    onClose: () => alert('Close clicked'),
  },
};

export const Closing: Story = {
  render: (args) => <InputNotificationWrapper {...args} />,
  args: {
    notification: { type: 'success', text: 'Thông báo đang đóng...' },
    isClosing: true,
    onClose: () => {},
  },
};
