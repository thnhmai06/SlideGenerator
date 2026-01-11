import React, { useMemo, useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import { getAssetPath } from '@/shared/utils/paths';
import { formatUserDateTime, formatUserTime } from '@/shared/utils/time';
import './ResultMenu.css';

type LogEntry = {
	message: string;
	level?: string;
	timestamp?: string;
	row?: number;
	rowStatus?: string;
};

type RowLogGroup = {
	key: string;
	row?: number;
	status?: string;
	entries: LogEntry[];
};

const ResultMenu: React.FC = () => {
	const { t, language } = useApp();
	const {
		groups,
		clearCompleted,
		removeGroup,
		removeSheet,
		loadSheetLogs,
		exportGroupConfig,
		hasGroupConfig,
	} = useJobs();
	const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({});
	const [expandedLogs, setExpandedLogs] = useState<Record<string, boolean>>({});
	const [collapsedRowGroups, setCollapsedRowGroups] = useState<Record<string, boolean>>({});

	const completedGroups = useMemo(
		() =>
			groups.filter((group) =>
				['completed', 'failed', 'cancelled'].includes(group.status.toLowerCase()),
			),
		[groups],
	);

	const toggleGroup = (groupId: string) => {
		setExpandedGroups((prev) => ({ ...prev, [groupId]: !prev[groupId] }));
	};

	const toggleLog = (sheetId: string) => {
		setExpandedLogs((prev) => {
			const next = !prev[sheetId];
			if (next) {
				void loadSheetLogs(sheetId);
			}
			return { ...prev, [sheetId]: next };
		});
	};

	const statusKey = (status: string) => {
		const normalized = status.toLowerCase();
		if (normalized === 'running') return 'processing';
		if (normalized === 'failed') return 'error';
		return normalized;
	};

	const deriveGroupName = (workbookPath: string, fallback: string) => {
		if (!workbookPath) return fallback;
		const parts = workbookPath.split(/[/\\]/);
		return parts[parts.length - 1] || fallback;
	};

	const handleOpenFolder = async (folderPath: string | undefined) => {
		if (!folderPath || !window.electronAPI) return;
		await window.electronAPI.openPath(folderPath);
	};

	const handleExportGroup = async (groupId: string) => {
		await exportGroupConfig(groupId);
	};

	const handleOpenFile = async (filePath: string | undefined) => {
		if (!filePath || !window.electronAPI) return;
		await window.electronAPI.openPath(filePath);
	};

	const handleRemoveSheet = async (sheetId: string) => {
		await removeSheet(sheetId);
	};

	const handleRemoveGroup = async (groupId: string) => {
		if (confirm(t('results.confirmRemoveGroup'))) {
			await removeGroup(groupId);
		}
	};

	const handleClearAll = async () => {
		if (confirm(t('results.confirmClearAll'))) {
			await clearCompleted();
		}
	};

	const formatLogEntry = (entry: LogEntry, jobLabel?: string) => {
		const timeValue = formatUserTime(entry.timestamp, language);
		const time = timeValue ? `[${timeValue}] ` : '';
		const level = entry.level ? `${entry.level}: ` : '';
		const job = jobLabel ? `${jobLabel}: ` : '';
		return `${time}${level}${job}${entry.message}`;
	};

	const groupLogsByRow = (logs: LogEntry[]): RowLogGroup[] => {
		const groups: RowLogGroup[] = [];
		const map = new Map<string, RowLogGroup>();
		logs.forEach((entry) => {
			const key = entry.row != null ? `row:${entry.row}` : 'general';
			let group = map.get(key);
			if (!group) {
				group = {
					key,
					row: entry.row,
					status: entry.rowStatus,
					entries: [],
				};
				map.set(key, group);
				groups.push(group);
			}
			group.entries.push(entry);
			if (entry.rowStatus) group.status = entry.rowStatus;
		});
		return groups;
	};

	const summarizeSheets = (
		sheets: Array<{ status: string; currentRow: number; totalRows: number }>,
	) => {
		let completedSlides = 0;
		let failedSlides = 0;
		let totalSlides = 0;

		sheets.forEach((sheet) => {
			const total = sheet.totalRows ?? 0;
			const done = Math.min(sheet.currentRow ?? 0, total);
			totalSlides += total;
			completedSlides += done;
			if (sheet.status === 'Failed' || sheet.status === 'Cancelled') {
				failedSlides += Math.max(total - done, 0);
			}
		});

		return { completedSlides, failedSlides, totalSlides };
	};

	const toggleRowGroup = (key: string) => {
		setCollapsedRowGroups((prev) => {
			const current = prev[key] ?? true;
			return { ...prev, [key]: !current };
		});
	};

	const formatTime = (value?: string) => {
		if (!value) return '';
		return formatUserDateTime(value, language);
	};

	return (
		<div className="output-menu">
			<div className="menu-header">
				<h1 className="menu-title">{t('results.title')}</h1>
				<div className="header-actions">
					<button
						className="btn btn-danger"
						onClick={handleClearAll}
						disabled={completedGroups.length === 0}
						title={t('results.clearAll')}
					>
						<img
							src={getAssetPath('images', 'close.png')}
							alt="Clear"
							className="btn-icon"
						/>
						{t('results.clearAll')}
					</button>
				</div>
			</div>

			<div className="output-section">
				{completedGroups.length === 0 ? (
					<div className="empty-state">{t('results.empty')}</div>
				) : (
					<div className="output-list">
						{completedGroups.map((group) => {
							const sheets = Object.values(group.sheets);
							const { completedSlides, failedSlides, totalSlides } =
								summarizeSheets(sheets);
							const groupProgress = group.progress;
							const groupName = deriveGroupName(group.workbookPath, group.id);
							const showDetails = expandedGroups[group.id] ?? false;

							return (
								<div key={group.id} className="output-group">
									<div
										className="group-header"
										onClick={() => toggleGroup(group.id)}
									>
										<div className="group-main-info">
											<img
												src={getAssetPath('images', 'chevron-down.png')}
												alt=""
												aria-hidden="true"
												className={`expand-icon ${showDetails ? 'expanded' : ''}`}
											/>
											<div className="group-info">
												<div className="group-name-row">
													<div className="group-name">{groupName}</div>
													{group.completedAt && (
														<span className="group-time">
															{t('results.completedAt')}:{' '}
															{formatTime(group.completedAt)}
														</span>
													)}
												</div>
												<div className="group-stats-line">
													<span>
														{completedSlides}/{totalSlides}{' '}
														{t('process.slides')} -{' '}
														{Math.round(groupProgress)}%
													</span>
													<span
														className="stat-badge stat-success"
														title={t('process.successSlides')}
													>
														{completedSlides}
													</span>
													<span className="stat-divider">|</span>
													<span
														className="stat-badge stat-failed"
														title={t('process.failedSlides')}
													>
														{failedSlides}
													</span>
												</div>
											</div>
										</div>
										<div
											className="group-actions"
											onClick={(e) => e.stopPropagation()}
										>
											<button
												className="output-btn output-btn-icon-only"
												onClick={() => handleExportGroup(group.id)}
												disabled={!hasGroupConfig(group.id)}
												aria-label={t('results.exportConfig')}
												title={t('results.exportConfig')}
											>
												<img
													src={getAssetPath(
														'images',
														'export-settings.png',
													)}
													alt=""
													className="btn-icon"
												/>
											</button>
											<button
												className="output-btn"
												onClick={() => handleOpenFolder(group.outputFolder)}
												disabled={!group.outputFolder}
											>
												<img
													src={getAssetPath('images', 'folder.png')}
													alt="Open Folder"
													className="btn-icon"
												/>
												<span>{t('results.openFolder')}</span>
											</button>
											<button
												className="output-btn-danger"
												onClick={() => handleRemoveGroup(group.id)}
											>
												<img
													src={getAssetPath('images', 'close.png')}
													alt="Clear Group"
													className="btn-icon"
												/>
												<span>{t('results.removeGroup')}</span>
											</button>
										</div>
									</div>

									{showDetails && (
										<div className="files-list">
											{sheets.map((sheet) => {
												const showLog = expandedLogs[sheet.id] ?? false;
												const completedSlides = Math.min(
													sheet.currentRow,
													sheet.totalRows,
												);
												const failedSlides =
													sheet.status === 'Failed' ||
													sheet.status === 'Cancelled'
														? Math.max(
																sheet.totalRows - completedSlides,
																0,
															)
														: 0;
												const logGroups = showLog
													? groupLogsByRow(sheet.logs as LogEntry[])
													: [];
												const logJobLabel = sheet.hangfireJobId
													? `#${sheet.hangfireJobId}`
													: sheet.id;

												return (
													<div key={sheet.id} className="file-item">
														<div
															className="file-header-clickable"
															onClick={() => toggleLog(sheet.id)}
														>
															<img
																src={getAssetPath(
																	'images',
																	'chevron-down.png',
																)}
																alt=""
																aria-hidden="true"
																className={`file-expand-icon ${showLog ? 'expanded' : ''}`}
															/>
															<div className="file-info">
																<div className="file-name">
																	{sheet.sheetName}
																</div>
																<div className="file-stats">
																	<span
																		className="file-stat-badge stat-success"
																		title={t(
																			'process.successSlides',
																		)}
																	>
																		{completedSlides}
																	</span>
																	<span className="stat-divider">
																		|
																	</span>
																	<span
																		className="file-stat-badge stat-failed"
																		title={t(
																			'process.failedSlides',
																		)}
																	>
																		{failedSlides}
																	</span>
																	<span className="file-progress-text">
																		/ {sheet.totalRows}{' '}
																		{t('process.slides')} -{' '}
																		{Math.round(sheet.progress)}
																		%
																	</span>
																</div>
															</div>
															<div className="file-status-and-actions">
																<div
																	className="file-status"
																	data-status={statusKey(
																		sheet.status,
																	)}
																>
																	{t(
																		`process.status.${statusKey(sheet.status)}`,
																	)}
																</div>
																<div className="file-action-buttons">
																	<button
																		className="file-action-btn"
																		onClick={(e) => {
																			e.stopPropagation();
																			handleOpenFile(
																				sheet.outputPath,
																			);
																		}}
																		disabled={!sheet.outputPath}
																		aria-label={t(
																			'results.open',
																		)}
																		title={t('results.open')}
																	>
																		<img
																			src={getAssetPath(
																				'images',
																				'open.png',
																			)}
																			alt="Open"
																			className="file-btn-icon"
																		/>
																	</button>
																	<button
																		className="file-action-btn file-action-btn-danger"
																		onClick={(e) => {
																			e.stopPropagation();
																			handleRemoveSheet(
																				sheet.id,
																			);
																		}}
																		aria-label={t(
																			'results.remove',
																		)}
																		title={t('results.remove')}
																	>
																		<img
																			src={getAssetPath(
																				'images',
																				'close.png',
																			)}
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
																			navigator.clipboard.writeText(
																				sheet.logs
																					.map((entry) =>
																						formatLogEntry(
																							entry as LogEntry,
																							logJobLabel,
																						),
																					)
																					.join('\n'),
																			);
																		}}
																		title="Copy log"
																	>
																		<img
																			src={getAssetPath(
																				'images',
																				'clipboard.png',
																			)}
																			alt="Copy"
																			className="log-icon"
																		/>
																	</button>
																</div>
																<div className="log-content">
																	{sheet.logs.length === 0 ? (
																		<div className="log-empty">
																			{t('process.noLogs')}
																		</div>
																	) : (
																		logGroups.map((group) => {
																			const rowKey = `${sheet.id}:${group.key}`;
																			const isCollapsed =
																				collapsedRowGroups[
																					rowKey
																				] ?? true;
																			return (
																				<div
																					key={group.key}
																					className="log-row-group"
																					data-status={(
																						group.status ??
																						'info'
																					).toLowerCase()}
																				>
																					<div
																						className="log-row-header"
																						onClick={() =>
																							toggleRowGroup(
																								rowKey,
																							)
																						}
																					>
																						<img
																							src={getAssetPath(
																								'images',
																								'chevron-down.png',
																							)}
																							alt=""
																							aria-hidden="true"
																							className={`log-row-toggle ${
																								isCollapsed
																									? ''
																									: 'expanded'
																							}`}
																						/>
																						<span className="log-row-title">
																							{group.row !=
																							null
																								? `Row ${group.row}`
																								: t(
																										'process.logGeneral',
																									)}
																						</span>
																						{group.status && (
																							<span className="log-row-status">
																								{group.status.toUpperCase()}
																							</span>
																						)}
																					</div>
																					{!isCollapsed && (
																						<div className="log-row-entries">
																							{group.entries.map(
																								(
																									entry,
																									index,
																								) => (
																									<div
																										key={`${group.key}-${index}`}
																										className={`log-entry log-${(entry.level ?? 'info').toLowerCase()}`}
																									>
																										{formatLogEntry(
																											entry,
																											logJobLabel,
																										)}
																									</div>
																								),
																							)}
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
											})}
										</div>
									)}
								</div>
							);
						})}
					</div>
				)}
			</div>
		</div>
	);
};

export default ResultMenu;
