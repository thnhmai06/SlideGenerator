import React from 'react';
import type { SheetJob } from '@/shared/contexts/JobContextType';
import type { LogEntry } from './types';
import { useProcess } from './hooks';
import { ProcessGroup, ProcessHeader } from './components';
import './ProcessMenu.css';

const ProcessMenu: React.FC = () => {
	const process = useProcess();

	const handleCopyLogs = (sheet: SheetJob) => {
		const logJobLabel = sheet.hangfireJobId ? `#${sheet.hangfireJobId}` : sheet.id;
		navigator.clipboard.writeText(
			sheet.logs.map((entry) => process.formatLogEntry(entry as LogEntry, logJobLabel)).join('\n'),
		);
	};

	return (
		<div className="process-menu">
			<ProcessHeader
				hasProcessing={process.hasProcessing}
				activeGroupsCount={process.activeGroups.length}
				onPauseResumeAll={process.handlePauseResumeAll}
				onStopAll={process.handleStopAll}
				onOpenDashboard={process.handleOpenDashboard}
				t={process.t}
			/>

			<div className="process-section">
				{process.activeGroups.length === 0 ? (
					<div className="empty-state">{process.t('process.empty')}</div>
				) : (
					<div className="process-list">
						{process.activeGroups.map((group) => (
							<ProcessGroup
								key={group.id}
								group={group}
								showDetails={process.expandedGroups[group.id] ?? false}
								expandedLogs={process.expandedLogs}
								collapsedRowGroups={process.collapsedRowGroups}
								hasGroupConfig={process.hasGroupConfig}
								formatLogEntry={process.formatLogEntry}
								formatTime={process.formatTime}
								onToggleGroup={() => process.toggleGroup(group.id)}
								onToggleLog={process.toggleLog}
								onToggleRowGroup={process.toggleRowGroup}
								onGroupAction={() => process.handleGroupAction(group.id, group.status)}
								onStopGroup={() => process.handleStopGroup(group.id)}
								onExportGroup={() => process.handleExportGroup(group.id)}
								onSheetAction={process.handleSheetAction}
								onStopSheet={process.handleStopSheet}
								onCopyLogs={handleCopyLogs}
								t={process.t}
							/>
						))}
					</div>
				)}
			</div>
		</div>
	);
};

export default ProcessMenu;
