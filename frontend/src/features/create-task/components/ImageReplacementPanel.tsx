import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import ShapeSelector from '@/shared/components/ShapeSelector';
import TagInput from '@/shared/components/TagInput';
import type { ImageReplacementPanelProps } from '../types';

export const ImageReplacementPanel: React.FC<ImageReplacementPanelProps> = ({
	canConfigure,
	showImageConfigs,
	setShowImageConfigs,
	addImageReplacement,
	imageReplacements,
	maxImageConfigs,
	shapes,
	getAvailableShapes,
	updateImageReplacement,
	removeImageReplacement,
	roiOptions,
	cropOptions,
	getOptionDescription,
	columns,
	openPreview,
	t,
}) => {
	const isAtLimit = maxImageConfigs > 0 && imageReplacements.length >= maxImageConfigs;

	return (
		<div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
			<div className="panel-header">
				<div className="panel-title">
					<button
						type="button"
						className="panel-title-toggle"
						onClick={() => setShowImageConfigs((prev) => !prev)}
						disabled={!canConfigure}
						aria-expanded={showImageConfigs}
					>
						<img
							src={getAssetPath('images', 'chevron-down.png')}
							alt=""
							className={`panel-title-icon ${showImageConfigs ? 'expanded' : ''}`}
						/>
						<h3>
							{t('replacement.imageTitle')}{' '}
							<span className="panel-count">
								({imageReplacements.length}
								{maxImageConfigs > 0 ? `/${maxImageConfigs}` : ''})
							</span>
						</h3>
					</button>
				</div>
				<button
					className="btn btn-success"
					onClick={addImageReplacement}
					disabled={
						!canConfigure || shapes.length === 0 || imageReplacements.length >= maxImageConfigs
					}
					title={isAtLimit ? `${t('replacement.limitReached')}: ${maxImageConfigs}` : undefined}
				>
					+ {t('replacement.add')}
				</button>
			</div>
			<div className={`panel-content ${showImageConfigs ? 'is-open' : ''}`}>
				<div className="replacement-table replacement-table-image">
					<div className="shape-gallery">
						<div className="shape-gallery-header">{t('replacement.availableShapes')}</div>
						<div className="shape-gallery-list">
							{shapes.length === 0 ? (
								<div className="shape-gallery-empty">{t('replacement.noShapes')}</div>
							) : (
								shapes.map((shape) => (
									<button
										type="button"
										key={shape.id}
										className="shape-gallery-item"
										onClick={() => openPreview(shape)}
									>
										<img src={shape.preview} alt={shape.name} className="shape-gallery-preview" />
										<div className="shape-gallery-info">
											<span className="shape-gallery-name">{shape.name}</span>
											<span className="shape-gallery-id">{shape.id}</span>
										</div>
									</button>
								))
							)}
						</div>
					</div>
					<table className="replacement-table-grid">
						<colgroup>
							<col className="col-main" />
							<col className="col-main" />
							<col className="col-narrow" />
							<col className="col-narrow" />
							<col className="col-action" />
						</colgroup>
						<thead>
							<tr>
								<th>{t('replacement.shape')}</th>
								<th>{t('replacement.column')}</th>
								<th>{t('replacement.roi')}</th>
								<th>{t('replacement.crop')}</th>
								<th className="cell-action">{t('replacement.delete')}</th>
							</tr>
						</thead>
						<tbody>
							{imageReplacements.map((item) => (
								<tr key={item.id}>
									<td>
										<ShapeSelector
											shapes={getAvailableShapes(item.shapeId)}
											value={item.shapeId}
											onChange={(shapeId) => updateImageReplacement(item.id, 'shapeId', shapeId)}
											placeholder={t('replacement.shapePlaceholder')}
										/>
									</td>
									<td>
										<TagInput
											value={item.columns}
											onChange={(tags) => updateImageReplacement(item.id, 'columns', tags)}
											suggestions={columns}
											placeholder={t('replacement.columnPlaceholder')}
										/>
									</td>
									<td>
										<div className="select-with-hint">
											<select
												className="table-input"
												value={item.roiType}
												onChange={(e) => updateImageReplacement(item.id, 'roiType', e.target.value)}
												title={getOptionDescription(roiOptions, item.roiType)}
											>
												{roiOptions.map((option) => (
													<option key={option.value} value={option.value}>
														{option.label}
													</option>
												))}
											</select>
											<span className="select-hint">
												{getOptionDescription(roiOptions, item.roiType)}
											</span>
										</div>
									</td>
									<td>
										<div className="select-with-hint">
											<select
												className="table-input"
												value={item.cropType}
												onChange={(e) =>
													updateImageReplacement(item.id, 'cropType', e.target.value)
												}
												title={getOptionDescription(cropOptions, item.cropType)}
											>
												{cropOptions.map((option) => (
													<option key={option.value} value={option.value}>
														{option.label}
													</option>
												))}
											</select>
											<span className="select-hint">
												{getOptionDescription(cropOptions, item.cropType)}
											</span>
										</div>
									</td>
									<td className="cell-action">
										<button
											className="delete-btn"
											onClick={() => removeImageReplacement(item.id)}
											title={t('replacement.delete')}
										>
											<img
												src={getAssetPath('images', 'remove.png')}
												alt="Delete"
												className="delete-icon"
											/>
										</button>
									</td>
								</tr>
							))}
						</tbody>
					</table>
				</div>
			</div>
		</div>
	);
};
