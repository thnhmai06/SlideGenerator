import React from 'react';
import type { DownloadTabProps } from '../types';

export const DownloadTab: React.FC<DownloadTabProps> = ({
	loading,
	config,
	canEditConfig,
	isLocked,
	updateDownload,
	handleNumberChange,
	handleNumberBlur,
	handleNumberFocus,
	onSelectFolder,
	t,
}) => (
	<div className={`setting-section${isLocked ? ' setting-section--locked' : ''}`}>
		<h3>{t('settings.downloadSettings')}</h3>
		{loading || !config ? (
			<div className="loading">{t('settings.loading')}</div>
		) : (
			<>
				<div className="setting-item">
					<label className="setting-label">{t('settings.saveFolder')}</label>
					<div className="input-group">
						<input
							type="text"
							className="setting-input"
							value={config.download.saveFolder}
							disabled={!canEditConfig}
							onChange={(e) => updateDownload({ saveFolder: e.target.value })}
							placeholder="./downloads"
						/>
						<button className="browse-btn" disabled={!canEditConfig} onClick={onSelectFolder}>
							{t('createTask.browse')}
						</button>
					</div>
					<span className="setting-hint">{t('settings.saveFolderHint')}</span>
				</div>

				<div className="settings-grid">
					<div className="setting-item">
						<label className="setting-label">{t('settings.maxChunks')}</label>
						<input
							type="number"
							className="setting-input"
							value={Number.isFinite(config.download.maxChunks) ? config.download.maxChunks : ''}
							disabled={!canEditConfig}
							onChange={(e) =>
								handleNumberChange(e.target.value, (next) => updateDownload({ maxChunks: next }))
							}
							onBlur={(e) =>
								handleNumberBlur(e.target.value, (next) => updateDownload({ maxChunks: next }))
							}
							onFocus={handleNumberFocus}
							min="1"
							max="128"
						/>
						<span className="setting-hint">{t('settings.maxChunksHint')}</span>
					</div>

					<div className="setting-item">
						<label className="setting-label">{t('settings.speedLimit')}</label>
						<input
							type="number"
							className="setting-input"
							value={
								Number.isFinite(config.download.limitBytesPerSecond)
									? config.download.limitBytesPerSecond
									: ''
							}
							disabled={!canEditConfig}
							onChange={(e) =>
								handleNumberChange(e.target.value, (next) =>
									updateDownload({ limitBytesPerSecond: next }),
								)
							}
							onBlur={(e) =>
								handleNumberBlur(e.target.value, (next) =>
									updateDownload({ limitBytesPerSecond: next }),
								)
							}
							onFocus={handleNumberFocus}
							min="0"
						/>
						<span className="setting-hint">{t('settings.speedLimitHint')}</span>
					</div>
				</div>

				<div className="settings-grid">
					<div className="setting-item">
						<label className="setting-label">{t('settings.retryTimeout')}</label>
						<input
							type="number"
							className="setting-input"
							value={
								Number.isFinite(config.download.retryTimeout) ? config.download.retryTimeout : ''
							}
							disabled={!canEditConfig}
							onChange={(e) =>
								handleNumberChange(e.target.value, (next) => updateDownload({ retryTimeout: next }))
							}
							onBlur={(e) =>
								handleNumberBlur(e.target.value, (next) => updateDownload({ retryTimeout: next }))
							}
							onFocus={handleNumberFocus}
							min="1"
						/>
						<span className="setting-hint">{t('settings.retryTimeoutHint')}</span>
					</div>

					<div className="setting-item">
						<label className="setting-label">{t('settings.maxRetries')}</label>
						<input
							type="number"
							className="setting-input"
							value={Number.isFinite(config.download.maxRetries) ? config.download.maxRetries : ''}
							disabled={!canEditConfig}
							onChange={(e) =>
								handleNumberChange(e.target.value, (next) => updateDownload({ maxRetries: next }))
							}
							onBlur={(e) =>
								handleNumberBlur(e.target.value, (next) => updateDownload({ maxRetries: next }))
							}
							onFocus={handleNumberFocus}
							min="0"
							max="10"
						/>
						<span className="setting-hint">{t('settings.maxRetriesHint')}</span>
					</div>
				</div>

				<h4 className="setting-subsection-title">{t('settings.proxySettings')}</h4>

				<div className="setting-item">
					<label className="setting-label setting-label-inline">
						<input
							type="checkbox"
							className="setting-checkbox"
							checked={config.download.proxy.useProxy}
							disabled={!canEditConfig}
							onChange={(e) =>
								updateDownload({
									proxy: { ...config.download.proxy, useProxy: e.target.checked },
								})
							}
						/>
						{t('settings.useProxy')}
					</label>
					<span className="setting-hint">{t('settings.useProxyHint')}</span>
				</div>

				{config.download.proxy.useProxy && (
					<>
						<div className="setting-item">
							<label className="setting-label">{t('settings.proxyAddress')}</label>
							<input
								type="text"
								className="setting-input"
								value={config.download.proxy.proxyAddress}
								disabled={!canEditConfig}
								onChange={(e) =>
									updateDownload({
										proxy: { ...config.download.proxy, proxyAddress: e.target.value },
									})
								}
								placeholder="http://proxy.example.com:8080"
							/>
							<span className="setting-hint">{t('settings.proxyAddressHint')}</span>
						</div>

						<div className="settings-grid">
							<div className="setting-item">
								<label className="setting-label">{t('settings.proxyUsername')}</label>
								<input
									type="text"
									className="setting-input"
									value={config.download.proxy.username}
									disabled={!canEditConfig}
									onChange={(e) =>
										updateDownload({
											proxy: { ...config.download.proxy, username: e.target.value },
										})
									}
									placeholder={t('settings.optional')}
								/>
							</div>

							<div className="setting-item">
								<label className="setting-label">{t('settings.proxyPassword')}</label>
								<input
									type="password"
									className="setting-input"
									value={config.download.proxy.password}
									disabled={!canEditConfig}
									onChange={(e) =>
										updateDownload({
											proxy: { ...config.download.proxy, password: e.target.value },
										})
									}
									placeholder={t('settings.optional')}
								/>
							</div>
						</div>

						<div className="setting-item">
							<label className="setting-label">{t('settings.proxyDomain')}</label>
							<input
								type="text"
								className="setting-input"
								value={config.download.proxy.domain}
								disabled={!canEditConfig}
								onChange={(e) =>
									updateDownload({
										proxy: { ...config.download.proxy, domain: e.target.value },
									})
								}
								placeholder={t('settings.optional')}
							/>
							<span className="setting-hint">{t('settings.proxyDomainHint')}</span>
						</div>
					</>
				)}
			</>
		)}
	</div>
);
