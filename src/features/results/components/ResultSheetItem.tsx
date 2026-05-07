import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { ResultSheetItemProps } from '../types';
import { getSheetStats, statusKey } from '../utils';

export const ResultSheetItem: React.FC<ResultSheetItemProps> = ({
	sheet,
	showLog,
	logGroups,
	collapsedRowGroups,
	formatLogEntry,
	onToggleLog,
	onToggleRowGroup,
	onOpenFile,
	onRemoveSheet,
	onCopyLogs,
	t,
}) => {
	const { completedSlides, failedSlides } = getSheetStats(sheet);
	const logJobLabel = sheet.hangfireJobId ? `#${sheet.hangfireJobId}` : sheet.id;

	return (
		<div className="file-item">
			<div className="file-header-clickable" onClick={onToggleLog}>
				<img
					src={getAssetPath('images', 'chevron-down.png')}
					alt=""
					aria-hidden="true"
					className={`file-expand-icon ${showLog ? 'expanded' : ''}`}
				/>
				<div className="file-info">
					<div className="file-name">{sheet.sheetName}</div>
					<div className="file-stats">
						<span className="file-stat-badge stat-success" title={t('process.successSlides')}>
							{completedSlides}
						</span>
						<span className="stat-divider">|</span>
						<span className="file-stat-badge stat-failed" title={t('process.failedSlides')}>
							{failedSlides}
						</span>
						<span className="file-progress-text">
							/ {sheet.totalRows} {t('process.slides')} - {Math.round(sheet.progress)}%
						</span>
					</div>
				</div>
				<div className="file-status-and-actions">
					<div className="file-status" data-status={statusKey(sheet.status)}>
						{t(`process.status.${statusKey(sheet.status)}`)}
					</div>
					<div className="file-action-buttons">
						<button
							className="file-action-btn"
							onClick={(e) => {
								e.stopPropagation();
								onOpenFile();
							}}
							disabled={!sheet.outputPath}
							aria-label={t('results.open')}
							title={t('results.open')}
						>
							<img src={getAssetPath('images', 'open.png')} alt="Open" className="file-btn-icon" />
						</button>
						<button
							className="file-action-btn file-action-btn-danger"
							onClick={(e) => {
								e.stopPropagation();
								onRemoveSheet();
							}}
							aria-label={t('results.remove')}
							title={t('results.remove')}
						>
							<img
								src={getAssetPath('images', 'close.png')}
								alt="Clear"
								className="file-btn-icon"
							/>
						</button>
					</div>
				</div>
			</div>

			{showLog && (
				<div className="file-log-content">
					<div className="log-header">
						{t('process.log')}
						<button
							className="copy-log-btn"
							onClick={(e) => {
								e.stopPropagation();
								onCopyLogs();
							}}
							title="Copy log"
						>
							<img src={getAssetPath('images', 'clipboard.png')} alt="Copy" className="log-icon" />
						</button>
					</div>
					<div className="log-content">
						{sheet.logs.length === 0 ? (
							<div className="log-empty">{t('process.noLogs')}</div>
						) : (
							logGroups.map((group) => {
								const rowKey = `${sheet.id}:${group.key}`;
								const isCollapsed = collapsedRowGroups[rowKey] ?? true;
								return (
									<div
										key={group.key}
										className="log-row-group"
										data-status={(group.status ?? 'info').toLowerCase()}
									>
										<div className="log-row-header" onClick={() => onToggleRowGroup(rowKey)}>
											<img
												src={getAssetPath('images', 'chevron-down.png')}
												alt=""
												aria-hidden="true"
												className={`log-row-toggle ${isCollapsed ? '' : 'expanded'}`}
											/>
											<span className="log-row-title">
												{group.row != null ? `Row ${group.row}` : t('process.logGeneral')}
											</span>
											{group.status && (
												<span className="log-row-status">{group.status.toUpperCase()}</span>
											)}
										</div>
										{!isCollapsed && (
											<div className="log-row-entries">
												{group.entries.map((entry, index) => (
													<div
														key={`${group.key}-${index}`}
														className={`log-entry log-${(entry.level ?? 'info').toLowerCase()}`}
													>
														{formatLogEntry(entry, logJobLabel)}
													</div>
												))}
											</div>
										)}
									</div>
								);
							})
						)}
					</div>
				</div>
			)}
		</div>
	);
};
