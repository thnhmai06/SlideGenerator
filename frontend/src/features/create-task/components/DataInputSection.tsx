import React, { useEffect, useRef, useState } from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { DataInputSectionProps } from '../types';

export const DataInputSection: React.FC<DataInputSectionProps> = ({
	dataPath,
	onChangePath,
	onBrowse,
	isLoadingColumns,
	dataLoaded,
	sheetCount,
	uniqueColumnCount,
	totalRows,
	sheetNames,
	selectedSheets,
	sheetRowCounts,
	allSheetsSelected,
	someSheetsSelected,
	onToggleAllSheets,
	onToggleSheet,
	t,
}) => {
	const selectAllRef = useRef<HTMLInputElement>(null);
	const [isSheetListOpen, setIsSheetListOpen] = useState(true);
	const selectedRowCount = selectedSheets.reduce(
		(sum, sheet) => sum + (sheetRowCounts[sheet] ?? 0),
		0,
	);

	useEffect(() => {
		if (selectAllRef.current) {
			selectAllRef.current.indeterminate = someSheetsSelected;
		}
	}, [someSheetsSelected]);

	return (
		<div className="input-section">
			<label className="input-label">{t('createTask.dataFile')}</label>
			<div className="input-group">
				<input
					type="text"
					className="input-field"
					value={dataPath}
					onChange={(e) => onChangePath(e.target.value)}
					placeholder={t('createTask.dataPlaceholder')}
				/>
				<button className="browse-btn" onClick={onBrowse} disabled={isLoadingColumns}>
					{isLoadingColumns ? t('createTask.loadingColumns') : t('createTask.browse')}
				</button>
			</div>
			{dataLoaded && !isLoadingColumns && (
				<div className="input-meta">
					<span className="input-meta-title">{t('createTask.dataInfoLabel')}</span>
					<span>
						{t('createTask.sheetCount')}: {sheetCount}
					</span>
					<span>
						{t('createTask.columnCount')}: {uniqueColumnCount}
					</span>
					<span>
						{t('createTask.rowCount')}: {totalRows}
					</span>
				</div>
			)}
			{dataLoaded && !isLoadingColumns && sheetNames.length > 0 && (
				<div className="sheet-selector">
					<div className="sheet-selector-header">
						<button
							type="button"
							className="sheet-selector-toggle"
							onClick={() => setIsSheetListOpen((prev) => !prev)}
							aria-label={
								isSheetListOpen
									? t('createTask.sheetToggleCollapse')
									: t('createTask.sheetToggleExpand')
							}
							title={
								isSheetListOpen
									? t('createTask.sheetToggleCollapse')
									: t('createTask.sheetToggleExpand')
							}
						>
							<img
								src={getAssetPath('images', 'chevron-down.png')}
								alt=""
								className={`sheet-toggle-icon${isSheetListOpen ? ' is-open' : ''}`}
							/>
							<span className="sheet-selector-title">{t('createTask.sheetSelectTitle')}</span>
							<span className="sheet-selector-total">
								{t('createTask.selectedRowCount')}: {selectedRowCount}
							</span>
						</button>
						<label className="sheet-selector-all">
							<input
								ref={selectAllRef}
								type="checkbox"
								checked={allSheetsSelected}
								onChange={onToggleAllSheets}
							/>
							<span>{t('createTask.sheetSelectAll')}</span>
						</label>
						<span className="sheet-selector-count">
							{t('createTask.sheetSelected')}: {selectedSheets.length}/{sheetNames.length}
						</span>
					</div>
					<div className={`sheet-selector-list${isSheetListOpen ? ' is-open' : ' is-collapsed'}`}>
						{sheetNames.map((sheet) => (
							<label key={sheet} className="sheet-selector-item" title={sheet}>
								<input
									type="checkbox"
									checked={selectedSheets.includes(sheet)}
									onChange={() => onToggleSheet(sheet)}
								/>
								<span className="sheet-selector-name">{sheet}</span>
							</label>
						))}
					</div>
				</div>
			)}
		</div>
	);
};
