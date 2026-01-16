import React, { memo, useMemo } from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { SheetJob } from '@/shared/contexts/JobContextType';
import type { LogEntry, RowLogGroup, TranslationFn } from '../types';
import {
	deriveGroupName,
	groupLogsByRow,
	progressColor,
	statusKey,
	summarizeSheets,
} from '../utils';
import { SheetItem } from './SheetItem';

interface ProcessGroupProps {
	group: {
		id: string;
		status: string;
		progress: number;
		workbookPath: string;
		createdAt?: string;
		sheets: Record<string, SheetJob>;
	};
	showDetails: boolean;
	expandedLogs: Record<string, boolean>;
	collapsedRowGroups: Record<string, boolean>;
	hasGroupConfig: (groupId: string) => boolean;
	formatLogEntry: (entry: LogEntry, jobLabel?: string) => string;
	formatTime: (value?: string) => string;
	onToggleGroup: () => void;
	onToggleLog: (sheetId: string) => void;
	onToggleRowGroup: (key: string) => void;
	onGroupAction: () => void;
	onStopGroup: () => void;
	onExportGroup: () => void;
	onSheetAction: (sheetId: string, status: string) => void;
	onStopSheet: (sheetId: string) => void;
	onCopyLogs: (sheet: SheetJob) => void;
	t: TranslationFn;
}

export const ProcessGroup: React.FC<ProcessGroupProps> = memo(
	({
		group,
		showDetails,
		expandedLogs,
		collapsedRowGroups,
		hasGroupConfig,
		formatLogEntry,
		formatTime,
		onToggleGroup,
		onToggleLog,
		onToggleRowGroup,
		onGroupAction,
		onStopGroup,
		onExportGroup,
		onSheetAction,
		onStopSheet,
		onCopyLogs,
		t,
	}) => {
		const sheets = useMemo(() => Object.values(group.sheets), [group.sheets]);
		const summary = useMemo(() => summarizeSheets(sheets), [sheets]);
		const { completedJobs, totalSlides, completedSlides, processingSlides, failedSlides } = summary;
		const totalSheets = sheets.length;
		const groupProgress = group.progress;
		const groupName = useMemo(
			() => deriveGroupName(group.workbookPath, group.id),
			[group.workbookPath, group.id],
		);

		return (
			<div className="process-group">
				<div className="group-header" onClick={onToggleGroup}>
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
								{group.createdAt && (
									<span className="group-time">
										{t('process.createdAt')}: {formatTime(group.createdAt)}
									</span>
								)}
							</div>
							<div className="group-stats-line">
								<span>
									{completedSlides}/{totalSlides} {t('process.slides')} ({completedJobs}/
									{totalSheets}) - {Math.round(groupProgress)}%
								</span>
								<span className="stat-badge stat-success" title={t('process.successSlides')}>
									{completedSlides}
								</span>
								<span className="stat-divider">|</span>
								<span className="stat-badge stat-processing" title={t('process.processingSlides')}>
									{processingSlides}
								</span>
								<span className="stat-divider">|</span>
								<span className="stat-badge stat-failed" title={t('process.failedSlides')}>
									{failedSlides}
								</span>
							</div>
						</div>
					</div>
					<div className="group-actions" onClick={(e) => e.stopPropagation()}>
						<button
							className="process-btn process-btn-icon-only"
							onClick={onExportGroup}
							disabled={!hasGroupConfig(group.id)}
							aria-label={t('results.exportConfig')}
							title={t('results.exportConfig')}
						>
							<img
								src={getAssetPath('images', 'export-settings.png')}
								alt=""
								className="btn-icon"
							/>
						</button>
						<button
							className="process-btn process-btn-icon"
							onClick={onGroupAction}
							title={group.status === 'Paused' ? t('process.resume') : t('process.pause')}
						>
							<img
								src={
									group.status === 'Paused'
										? getAssetPath('images', 'resume.png')
										: getAssetPath('images', 'pause.png')
								}
								alt={group.status === 'Paused' ? 'Resume' : 'Pause'}
								className="btn-icon"
							/>
						</button>
						<button
							className="process-btn process-btn-danger process-btn-icon"
							onClick={onStopGroup}
							title={t('process.stop')}
						>
							<img src={getAssetPath('images', 'stop.png')} alt="Stop" className="btn-icon" />
						</button>
					</div>
				</div>

				<div className="progress-bar-container">
					<div
						className="progress-bar-fill"
						style={{
							width: `${Math.round(groupProgress)}%`,
							backgroundColor: progressColor(group.status),
						}}
					/>
				</div>

				{showDetails && (
					<div className="files-list">
						{sheets.map((sheet) => {
							const showLog = expandedLogs[sheet.id] ?? false;
							const logGroups: RowLogGroup[] = showLog
								? groupLogsByRow(sheet.logs as LogEntry[])
								: [];

							return (
								<SheetItem
									key={sheet.id}
									sheet={sheet}
									showLog={showLog}
									logGroups={logGroups}
									collapsedRowGroups={collapsedRowGroups}
									statusKey={statusKey}
									progressColor={progressColor}
									formatLogEntry={formatLogEntry}
									onToggleLog={() => onToggleLog(sheet.id)}
									onToggleRowGroup={onToggleRowGroup}
									onSheetAction={() => onSheetAction(sheet.id, sheet.status)}
									onStopSheet={() => onStopSheet(sheet.id)}
									onCopyLogs={() => onCopyLogs(sheet)}
									t={t}
								/>
							);
						})}
					</div>
				)}
			</div>
		);
	},
);

ProcessGroup.displayName = 'ProcessGroup';
