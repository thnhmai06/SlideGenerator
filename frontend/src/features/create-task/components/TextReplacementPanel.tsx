import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import TagInput from '@/shared/components/TagInput';
import type { TextReplacementPanelProps } from '../types';

export const TextReplacementPanel: React.FC<TextReplacementPanelProps> = ({
	canConfigure,
	showTextConfigs,
	setShowTextConfigs,
	addTextReplacement,
	textReplacements,
	maxTextConfigs,
	getAvailablePlaceholders,
	updateTextReplacement,
	removeTextReplacement,
	isLoadingPlaceholders,
	placeholders,
	columns,
	t,
}) => {
	const isAtLimit = maxTextConfigs > 0 && textReplacements.length >= maxTextConfigs;

	return (
		<div className={`replacement-full-panel ${canConfigure ? '' : 'replacement-disabled'}`}>
			<div className="panel-header">
				<div className="panel-title">
					<button
						type="button"
						className="panel-title-toggle"
						onClick={() => setShowTextConfigs((prev) => !prev)}
						disabled={!canConfigure}
						aria-expanded={showTextConfigs}
					>
						<img
							src={getAssetPath('images', 'chevron-down.png')}
							alt=""
							className={`panel-title-icon ${showTextConfigs ? 'expanded' : ''}`}
						/>
						<h3>
							{t('replacement.textTitle')}{' '}
							<span className="panel-count">
								({textReplacements.length}
								{maxTextConfigs > 0 ? `/${maxTextConfigs}` : ''})
							</span>
						</h3>
					</button>
				</div>
				<button
					className="btn btn-success"
					onClick={addTextReplacement}
					disabled={
						!canConfigure || placeholders.length === 0 || textReplacements.length >= maxTextConfigs
					}
					title={isAtLimit ? `${t('replacement.limitReached')}: ${maxTextConfigs}` : undefined}
				>
					+ {t('replacement.add')}
				</button>
			</div>
			<div className={`panel-content ${showTextConfigs ? 'is-open' : ''}`}>
				<div className="replacement-table replacement-table-text">
					<table className="replacement-table-grid">
						<colgroup>
							<col className="col-main" />
							<col className="col-main" />
							<col className="col-action" />
						</colgroup>
						<thead>
							<tr>
								<th>{t('replacement.searchText')}</th>
								<th>{t('replacement.column')}</th>
								<th className="cell-action">{t('replacement.delete')}</th>
							</tr>
						</thead>
						<tbody>
							{textReplacements.map((item) => {
								const available = getAvailablePlaceholders(item.placeholder);
								return (
									<tr key={item.id}>
										<td>
											<select
												className="table-input"
												value={item.placeholder}
												onChange={(e) =>
													updateTextReplacement(item.id, 'placeholder', e.target.value)
												}
												disabled={!canConfigure || isLoadingPlaceholders}
											>
												<option value="">{t('replacement.searchPlaceholder')}</option>
												{available.map((placeholder) => (
													<option key={placeholder} value={placeholder}>
														{placeholder}
													</option>
												))}
											</select>
										</td>
										<td>
											<TagInput
												value={item.columns}
												onChange={(tags) => updateTextReplacement(item.id, 'columns', tags)}
												suggestions={columns}
												placeholder={t('replacement.columnPlaceholder')}
											/>
										</td>
										<td className="cell-action">
											<button
												className="delete-btn"
												onClick={() => removeTextReplacement(item.id)}
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
								);
							})}
						</tbody>
					</table>
				</div>
			</div>
		</div>
	);
};
