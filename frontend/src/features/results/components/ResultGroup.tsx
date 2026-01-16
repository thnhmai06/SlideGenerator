import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { SheetJob } from '@/shared/contexts/JobContextType';
import type { LogEntry, RowLogGroup, TranslationFn } from '../types';
import { deriveGroupName, groupLogsByRow, statusKey, summarizeSheets } from '../utils';
import { ResultSheetItem } from './ResultSheetItem';

interface ResultGroupProps {
	group: {
		id: string;
		status: string;
		progress: number;
		workbookPath: string;
		completedAt?: string;
		outputFolder?: string;
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
	onOpenFolder: () => void;
	onRemoveGroup: () => void;
	onExportGroup: () => void;
	onOpenFile: (filePath?: string) => void;
	onRemoveSheet: (sheetId: string) => void;
	onCopyLogs: (sheet: SheetJob) => void;
	t: TranslationFn;
}

export const ResultGroup: React.FC<ResultGroupProps> = ({
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
	onOpenFolder,
	onRemoveGroup,
	onExportGroup,
	onOpenFile,
	onRemoveSheet,
	onCopyLogs,
	t,
}) => {
	const sheets = Object.values(group.sheets);
	const { completedSlides, failedSlides, totalSlides } = summarizeSheets(sheets);
	const groupProgress = group.progress;
	const groupName = deriveGroupName(group.workbookPath, group.id);

	return (
		<div className="output-group">
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
							{group.completedAt && (
								<span className="group-time">
									{t('results.completedAt')}: {formatTime(group.completedAt)}
								</span>
							)}
						</div>
						<div className="group-stats-line">
							<span>
								{completedSlides}/{totalSlides} {t('process.slides')} - {Math.round(groupProgress)}%
							</span>
							<span className="stat-badge stat-success" title={t('process.successSlides')}>
								{completedSlides}
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
						className="output-btn output-btn-icon-only"
						onClick={onExportGroup}
						disabled={!hasGroupConfig(group.id)}
						aria-label={t('results.exportConfig')}
						title={t('results.exportConfig')}
					>
						<img src={getAssetPath('images', 'export-settings.png')} alt="" className="btn-icon" />
					</button>
					<button className="output-btn" onClick={onOpenFolder} disabled={!group.outputFolder}>
						<img
							src={getAssetPath('images', 'folder.png')}
							alt="Open Folder"
							className="btn-icon"
						/>
						<span>{t('results.openFolder')}</span>
					</button>
					<button className="output-btn-danger" onClick={onRemoveGroup}>
						<img src={getAssetPath('images', 'close.png')} alt="Clear Group" className="btn-icon" />
						<span>{t('results.removeGroup')}</span>
					</button>
				</div>
			</div>

			{showDetails && (
				<div className="files-list">
					{sheets.map((sheet) => {
						const showLog = expandedLogs[sheet.id] ?? false;
						const logGroups: RowLogGroup[] = showLog
							? groupLogsByRow(sheet.logs as LogEntry[])
							: [];

						return (
							<ResultSheetItem
								key={sheet.id}
								sheet={sheet}
								showLog={showLog}
								logGroups={logGroups}
								collapsedRowGroups={collapsedRowGroups}
								statusKey={statusKey}
								formatLogEntry={formatLogEntry}
								onToggleLog={() => onToggleLog(sheet.id)}
								onToggleRowGroup={onToggleRowGroup}
								onOpenFile={() => onOpenFile(sheet.outputPath)}
								onRemoveSheet={() => onRemoveSheet(sheet.id)}
								onCopyLogs={() => onCopyLogs(sheet)}
								t={t}
							/>
						);
					})}
				</div>
			)}
		</div>
	);
};
