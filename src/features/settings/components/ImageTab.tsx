import React from 'react';
import type { ImageTabProps } from '../types';

export const ImageTab: React.FC<ImageTabProps> = ({
	loading,
	config,
	canEditConfig,
	isLocked,
	faceModelAvailable,
	modelLoading,
	onInitModel,
	onDeinitModel,
	updateFace,
	updateSaliency,
	createPadStyles,
	handleNumberChange,
	handleNumberBlur,
	handleNumberFocus,
	t,
}) => (
	<div className={`setting-section${isLocked ? ' setting-section--locked' : ''}`}>
		<h3>{t('settings.imageSettings')}</h3>
		{loading || !config ? (
			<div className="loading">{t('settings.loading')}</div>
		) : (
			<>
				<div className="image-config-block">
					<div className="image-config-header">
						<div>
							<h4>{t('settings.imageFace')}</h4>
							<span className="setting-hint">{t('settings.imageFaceHint')}</span>
						</div>
					</div>
					<div className="face-config-row">
						<div className="setting-item">
							<label className="setting-label">{t('settings.imageConfidence')}</label>
							<input
								type="number"
								className="setting-input"
								value={
									Number.isFinite(config.image.face.confidence) ? config.image.face.confidence : ''
								}
								disabled={!canEditConfig}
								onChange={(e) =>
									handleNumberChange(e.target.value, (next) => updateFace({ confidence: next }))
								}
								onBlur={(e) =>
									handleNumberBlur(e.target.value, (next) => updateFace({ confidence: next }))
								}
								onFocus={handleNumberFocus}
								min="0"
								max="1"
								step="0.01"
							/>
							<span className="setting-hint">{t('settings.imagePaddingHint')}</span>
						</div>
						<div className="setting-item setting-item-toggle">
							<div className="toggle-content">
								<div className="toggle-label">
									<div className="label-text">{t('settings.imageUnionAll')}</div>
									<div className="label-description">{t('settings.imageUnionAllDesc')}</div>
								</div>
								<label className="toggle-switch">
									<input
										type="checkbox"
										checked={config.image.face.unionAll}
										disabled={!canEditConfig}
										onChange={(e) => updateFace({ unionAll: e.target.checked })}
									/>
									<span className="toggle-slider"></span>
								</label>
							</div>
						</div>
					</div>

					{/* Model table */}
					<div className="model-table-container">
						<h5 className="model-table-title">{t('settings.imageModel')}</h5>
						<table className="model-table">
							<thead>
								<tr>
									<th>{t('settings.modelName')}</th>
									<th>{t('settings.modelstatus')}</th>
									<th>{t('settings.modelAction')}</th>
								</tr>
							</thead>
							<tbody>
								<tr>
									<td>{t('settings.faceModel')}</td>
									<td>
										<span
											className={`model-status-badge ${faceModelAvailable ? 'model-status-available' : 'model-status-unavailable'}`}
										>
											{faceModelAvailable
												? t('settings.modelAvailable')
												: t('settings.modelUnavailable')}
										</span>
									</td>
									<td>
										{faceModelAvailable ? (
											<button
												className="btn btn-danger"
												onClick={onDeinitModel}
												disabled={modelLoading || !canEditConfig}
											>
												{modelLoading ? t('settings.modelLoading') : t('settings.modelDeinit')}
											</button>
										) : (
											<button
												className="btn btn-primary"
												onClick={onInitModel}
												disabled={modelLoading || !canEditConfig}
											>
												{modelLoading ? t('settings.modelLoading') : t('settings.modelInit')}
											</button>
										)}
									</td>
								</tr>
							</tbody>
						</table>
					</div>
				</div>

				<div className="image-config-block">
					<div className="image-config-header">
						<div>
							<h4>{t('settings.imageSaliency')}</h4>
							<span className="setting-hint">{t('settings.imageSaliencyHint')}</span>
						</div>
					</div>
					<div className="image-config-grid">
						<div className="padding-section">
							<h5 className="padding-title">{t('settings.imagePadding')}</h5>
							<div className="image-padding-layout">
								<div className="pad-item pad-top">
									<label className="setting-label">{t('settings.imagePaddingTop')}</label>
									<input
										type="number"
										className="setting-input"
										value={
											Number.isFinite(config.image.saliency.paddingTop)
												? config.image.saliency.paddingTop
												: ''
										}
										disabled={!canEditConfig}
										onChange={(e) =>
											handleNumberChange(e.target.value, (next) =>
												updateSaliency({ paddingTop: next }),
											)
										}
										onBlur={(e) =>
											handleNumberBlur(e.target.value, (next) =>
												updateSaliency({ paddingTop: next }),
											)
										}
										onFocus={handleNumberFocus}
										min="0"
										max="1"
										step="0.01"
									/>
									<span className="setting-hint">{t('settings.imagePaddingHint')}</span>
								</div>
								<div className="pad-item pad-left">
									<label className="setting-label">{t('settings.imagePaddingLeft')}</label>
									<input
										type="number"
										className="setting-input"
										value={
											Number.isFinite(config.image.saliency.paddingLeft)
												? config.image.saliency.paddingLeft
												: ''
										}
										disabled={!canEditConfig}
										onChange={(e) =>
											handleNumberChange(e.target.value, (next) =>
												updateSaliency({ paddingLeft: next }),
											)
										}
										onBlur={(e) =>
											handleNumberBlur(e.target.value, (next) =>
												updateSaliency({ paddingLeft: next }),
											)
										}
										onFocus={handleNumberFocus}
										min="0"
										max="1"
										step="0.01"
									/>
									<span className="setting-hint">{t('settings.imagePaddingHint')}</span>
								</div>
								<div className="pad-center">
									<div className="pad-diagram">
										<div
											className="pad-box pad-base"
											style={createPadStyles(config.image.saliency).base}
										></div>
										<div
											className="pad-box pad-detect"
											style={createPadStyles(config.image.saliency).detect}
										></div>
										<div
											className="pad-box pad-crop"
											style={createPadStyles(config.image.saliency).crop}
										></div>
									</div>
								</div>
								<div className="pad-item pad-right">
									<label className="setting-label">{t('settings.imagePaddingRight')}</label>
									<input
										type="number"
										className="setting-input"
										value={
											Number.isFinite(config.image.saliency.paddingRight)
												? config.image.saliency.paddingRight
												: ''
										}
										disabled={!canEditConfig}
										onChange={(e) =>
											handleNumberChange(e.target.value, (next) =>
												updateSaliency({ paddingRight: next }),
											)
										}
										onBlur={(e) =>
											handleNumberBlur(e.target.value, (next) =>
												updateSaliency({ paddingRight: next }),
											)
										}
										onFocus={handleNumberFocus}
										min="0"
										max="1"
										step="0.01"
									/>
									<span className="setting-hint">{t('settings.imagePaddingHint')}</span>
								</div>
								<div className="pad-item pad-bottom">
									<label className="setting-label">{t('settings.imagePaddingBottom')}</label>
									<input
										type="number"
										className="setting-input"
										value={
											Number.isFinite(config.image.saliency.paddingBottom)
												? config.image.saliency.paddingBottom
												: ''
										}
										disabled={!canEditConfig}
										onChange={(e) =>
											handleNumberChange(e.target.value, (next) =>
												updateSaliency({ paddingBottom: next }),
											)
										}
										onBlur={(e) =>
											handleNumberBlur(e.target.value, (next) =>
												updateSaliency({ paddingBottom: next }),
											)
										}
										onFocus={handleNumberFocus}
										min="0"
										max="1"
										step="0.01"
									/>
									<span className="setting-hint">{t('settings.imagePaddingHint')}</span>
								</div>
							</div>
						</div>
					</div>
				</div>
			</>
		)}
	</div>
);
