import type { Meta, StoryObj } from '@storybook/react';
import { TextReplacementPanel, TextReplacementPanelProps } from './TextReplacementPanel';
import React, { useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import '../CreateTaskMenu.css';

const TextReplacementPanelWrapper = (args: TextReplacementPanelProps) => {
	const { t } = useApp();
	const [show, setShow] = useState(args.showTextConfigs || true);
	const [replacements, setReplacements] = useState(args.textReplacements || []);

	const updateReplacement = (id: number, field: string, value: any) => {
		setReplacements((prev: any[]) =>
			prev.map((item) => (item.id === id ? { ...item, [field]: value } : item)),
		);
		args.updateTextReplacement?.(id, field, value);
	};

	const removeReplacement = (id: number) => {
		setReplacements((prev: any[]) => prev.filter((item) => item.id !== id));
		args.removeTextReplacement?.(id);
	};

	const addReplacement = () => {
		const newItem = { id: Date.now(), placeholder: '', columns: [] };
		setReplacements((prev: any[]) => [...prev, newItem]);
		args.addTextReplacement?.();
	};

	return (
		<TextReplacementPanel
			{...args}
			showTextConfigs={show}
			setShowTextConfigs={setShow}
			textReplacements={replacements}
			updateTextReplacement={updateReplacement}
			removeTextReplacement={removeReplacement}
			addTextReplacement={addReplacement}
			getAvailablePlaceholders={(current: string) => {
				const used = replacements
					.map((r: any) => r.placeholder)
					.filter((p: string) => p !== current && p !== '');
				return args.placeholders.filter((p: string) => !used.includes(p));
			}}
			t={t}
		/>
	);
};

const meta: Meta<typeof TextReplacementPanel> = {
	title: 'Features/CreateTask/TextReplacementPanel',
	component: TextReplacementPanel,
	tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof TextReplacementPanel>;

export const Default: Story = {
	render: (args) => <TextReplacementPanelWrapper {...args} />,
	args: {
		canConfigure: true,
		maxTextConfigs: 10,
		placeholders: ['{{title}}', '{{subtitle}}', '{{metric}}', '{{notes}}', '{{presenter}}'],
		columns: ['Title', 'Subtitle', 'Revenue', 'Presenter Name', 'Date'],
		textReplacements: [],
	},
};

export const Configured: Story = {
	render: (args) => <TextReplacementPanelWrapper {...args} />,
	args: {
		...Default.args,
		textReplacements: [{ id: 1, placeholder: '{{title}}', columns: ['Title'] }],
	},
};
