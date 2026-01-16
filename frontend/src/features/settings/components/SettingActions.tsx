import React from 'react';
import type { SettingActionsProps } from '../types';

export const SettingActions: React.FC<SettingActionsProps> = ({
	saving,
	isEditable,
	showActions,
	onSave,
	onReload,
	onReset,
	t,
}) => {
	if (!showActions) return null;
	return (
		<div className="setting-actions">
			<button className="btn btn-primary" onClick={onSave} disabled={!isEditable || saving}>
				{saving ? t('settings.saving') : t('settings.save')}
			</button>
			<button className="btn btn-secondary" onClick={onReload} disabled={!isEditable}>
				{t('settings.reload')}
			</button>
			<button className="btn btn-danger" onClick={onReset} disabled={!isEditable}>
				{t('settings.resetToDefaults')}
			</button>
		</div>
	);
};
