import React from 'react';
import type { SheetJob } from '@/shared/contexts/JobContextType';
import type { LogEntry } from './types';
import { useResults } from './hooks';
import { ResultGroup, ResultHeader } from './components';
import './ResultMenu.css';

const ResultMenu: React.FC = () => {
	const results = useResults();

	const handleCopyLogs = (sheet: SheetJob) => {
		const logJobLabel = sheet.hangfireJobId ? `#${sheet.hangfireJobId}` : sheet.id;
		navigator.clipboard.writeText(
			sheet.logs.map((entry) => results.formatLogEntry(entry as LogEntry, logJobLabel)).join('\n'),
		);
	};

	return (
		<div className="output-menu">
			<ResultHeader
				completedGroupsCount={results.completedGroups.length}
				onClearAll={results.handleClearAll}
				t={results.t}
			/>

			<div className="output-section">
				{results.completedGroups.length === 0 ? (
					<div className="empty-state">{results.t('results.empty')}</div>
				) : (
					<div className="output-list">
						{results.completedGroups.map((group) => (
							<ResultGroup
								key={group.id}
								group={group}
								showDetails={results.expandedGroups[group.id] ?? false}
								expandedLogs={results.expandedLogs}
								collapsedRowGroups={results.collapsedRowGroups}
								hasGroupConfig={results.hasGroupConfig}
								formatLogEntry={results.formatLogEntry}
								formatTime={results.formatTime}
								onToggleGroup={() => results.toggleGroup(group.id)}
								onToggleLog={results.toggleLog}
								onToggleRowGroup={results.toggleRowGroup}
								onOpenFolder={() => results.handleOpenFolder(group.outputFolder)}
								onRemoveGroup={() => results.handleRemoveGroup(group.id)}
								onExportGroup={() => results.handleExportGroup(group.id)}
								onOpenFile={results.handleOpenFile}
								onRemoveSheet={results.handleRemoveSheet}
								onCopyLogs={handleCopyLogs}
								t={results.t}
							/>
						))}
					</div>
				)}
			</div>
		</div>
	);
};

export default ResultMenu;
