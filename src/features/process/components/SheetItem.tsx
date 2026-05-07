import React, { memo, useMemo } from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { SheetItemProps } from '../types';
import { getSheetStats } from '../utils';

export const SheetItem: React.FC<SheetItemProps> = memo(
	({
		sheet,
		showLog,
		logGroups,
		collapsedRowGroups,
		statusKey,
		progressColor,
		formatLogEntry,
		onToggleLog,
		onToggleRowGroup,
		onSheetAction,
		onStopSheet,
		onCopyLogs,
		t,
	}) => {
		const stats = useMemo(() => getSheetStats(sheet), [sheet]);
		const { completedSlides, failedSlides, processingSlides } = stats;
		const isPaused = sheet.status === 'Paused';
		const canControl = useMemo(
			() => ['Running', 'Paused', 'Pending'].includes(sheet.status),
			[sheet.status],
		);
		const jobIdLabel = sheet.hangfireJobId ? `#${sheet.hangfireJobId}` : '-';
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
						<div className="file-name-row">
							<div className="file-name">{sheet.sheetName}</div>
							<span className="file-job-id">
								{t('process.jobId')}: {jobIdLabel}
							</span>
						</div>
						<div className="file-stats">
							<span className="file-stat-badge stat-success" title={t('process.successSlides')}>
								{completedSlides}
							</span>
							<span className="stat-divider">|</span>
							<span
								className="file-stat-badge stat-processing"
								title={t('process.processingSlides')}
							>
								{processingSlides}
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
						{canControl && (
							<button
								className="file-action-btn"
								onClick={(e) => {
									e.stopPropagation();
									onSheetAction();
								}}
								title={isPaused ? t('process.resume') : t('process.pause')}
							>
								<img
									src={
										isPaused
											? getAssetPath('images', 'resume.png')
											: getAssetPath('images', 'pause.png')
									}
									alt={isPaused ? 'Resume' : 'Pause'}
									className="btn-icon-small"
								/>
							</button>
						)}
						{canControl && (
							<button
								className="file-action-btn file-action-btn-danger"
								onClick={(e) => {
									e.stopPropagation();
									onStopSheet();
								}}
								title={t('process.stop')}
							>
								<img
									src={getAssetPath('images', 'stop.png')}
									alt="Stop"
									className="btn-icon-small"
								/>
							</button>
						)}
					</div>
				</div>

				<div className="file-progress-bar">
					<div
						className="file-progress-fill"
						style={{
							width: `${Math.round(sheet.progress)}%`,
							backgroundColor: progressColor(sheet.status),
						}}
					/>
				</div>

				{showLog && (
					<div className="file-log-content">
						<div className="log-header">
							{t('process.log')}
							<button className="copy-log-btn" onClick={onCopyLogs} title="Copy log">
								<img
									src={getAssetPath('images', 'clipboard.png')}
									alt="Copy"
									className="log-icon"
								/>
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
	},
);

SheetItem.displayName = 'SheetItem';
