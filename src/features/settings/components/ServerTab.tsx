import React from 'react';
import type { ServerTabProps } from '../types';

export const ServerTab: React.FC<ServerTabProps> = ({
	loading,
	config,
	canEditConfig,
	isLocked,
	updateServer,
	handleNumberChange,
	handleNumberBlur,
	handleNumberFocus,
	t,
}) => (
	<div className={`setting-section${isLocked ? ' setting-section--locked' : ''}`}>
		<h3>{t('settings.serverSettings')}</h3>
		{loading || !config ? (
			<div className="loading">{t('settings.loading')}</div>
		) : (
			<>
				<div className="settings-grid">
					<div className="setting-item">
						<label className="setting-label">{t('settings.host')}</label>
						<input
							type="text"
							className="setting-input"
							value={config.server.host}
							disabled={!canEditConfig}
							onChange={(e) => updateServer({ host: e.target.value })}
							placeholder="127.0.0.1"
						/>
						<span className="setting-hint">{t('settings.hostHint')}</span>
					</div>

					<div className="setting-item">
						<label className="setting-label">{t('settings.port')}</label>
						<input
							type="number"
							className="setting-input"
							value={Number.isFinite(config.server.port) ? config.server.port : ''}
							disabled={!canEditConfig}
							onChange={(e) =>
								handleNumberChange(e.target.value, (next) => updateServer({ port: next }))
							}
							onBlur={(e) =>
								handleNumberBlur(e.target.value, (next) => updateServer({ port: next }))
							}
							onFocus={handleNumberFocus}
							min="1"
							max="65535"
						/>
						<span className="setting-hint">{t('settings.portHint')}</span>
					</div>
				</div>

				<div className="setting-item setting-item-toggle">
					<div className="toggle-content">
						<div className="toggle-label">
							<div className="label-text">{t('settings.debugMode')}</div>
							<div className="label-description">{t('settings.debugModeDesc')}</div>
						</div>
						<label className="toggle-switch">
							<input
								type="checkbox"
								checked={config.server.debug}
								disabled={!canEditConfig}
								onChange={(e) => updateServer({ debug: e.target.checked })}
							/>
							<span className="toggle-slider"></span>
						</label>
					</div>
				</div>
			</>
		)}
	</div>
);
