import React from 'react';
import type { Theme } from '@/shared/contexts/AppContextType';
import type { Language } from '@/shared/locales';
import type { AppearanceTabProps } from '../types';

export const AppearanceTab: React.FC<AppearanceTabProps> = ({
	theme,
	language,
	enableAnimations,
	closeToTray,
	setTheme,
	setLanguage,
	setEnableAnimations,
	setCloseToTray,
	t,
}) => (
	<div className="setting-section">
		<h3>{t('settings.appearanceSettings')}</h3>

		<div className="settings-grid">
			<div className="setting-item">
				<label className="setting-label">{t('settings.theme')}</label>
				<select
					className="setting-select"
					value={theme}
					onChange={(e) => setTheme(e.target.value as Theme)}
				>
					<option value="dark">{t('settings.themeDark')}</option>
					<option value="light">{t('settings.themeLight')}</option>
					<option value="system">{t('settings.themeSystem')}</option>
				</select>
				<span className="setting-hint">{t('settings.themeHint')}</span>
			</div>

			<div className="setting-item">
				<label className="setting-label">{t('settings.language')}</label>
				<select
					className="setting-select"
					value={language}
					onChange={(e) => setLanguage(e.target.value as Language)}
				>
					<option value="vi">{t('settings.languageVi')}</option>
					<option value="en">{t('settings.languageEn')}</option>
				</select>
				<span className="setting-hint">{t('settings.languageHint')}</span>
			</div>
		</div>

		<div className="setting-item setting-item-toggle">
			<div className="toggle-content">
				<div className="toggle-label">
					<div className="label-text">{t('settings.enableAnimations')}</div>
					<div className="label-description">{t('settings.animationsDesc')}</div>
				</div>
				<label className="toggle-switch">
					<input
						type="checkbox"
						checked={enableAnimations}
						onChange={(e) => setEnableAnimations(e.target.checked)}
					/>
					<span className="toggle-slider"></span>
				</label>
			</div>
		</div>

		<div className="setting-item setting-item-toggle">
			<div className="toggle-content">
				<div className="toggle-label">
					<div className="label-text">{t('settings.closeToTray')}</div>
					<div className="label-description">{t('settings.closeToTrayDesc')}</div>
				</div>
				<label className="toggle-switch">
					<input
						type="checkbox"
						checked={closeToTray}
						onChange={(e) => setCloseToTray(e.target.checked)}
					/>
					<span className="toggle-slider"></span>
				</label>
			</div>
		</div>
	</div>
);
