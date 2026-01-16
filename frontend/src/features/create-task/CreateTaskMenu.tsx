import React from 'react';
import type { CreateTaskMenuProps } from './types';
import { getOptionDescription } from './utils';
import { useCreateTask } from './hooks';
import {
	DataInputSection,
	ImageReplacementPanel,
	InputNotification,
	MenuHeader,
	PreviewModal,
	SaveLocationSection,
	StartButtonSection,
	TemplateInputSection,
	TextReplacementPanel,
} from './components';
import './CreateTaskMenu.css';

const CreateTaskMenu: React.FC<CreateTaskMenuProps> = ({ onStart }) => {
	const task = useCreateTask({ onStart });

	return (
		<div className="input-menu">
			<MenuHeader
				onImport={task.importConfig}
				onExport={task.exportConfig}
				onClear={task.clearAll}
				t={task.t}
			/>

			<InputNotification
				notification={task.notification}
				isClosing={task.isNotificationClosing}
				onClose={task.hideNotification}
				t={task.t}
			/>

			{/* File Inputs */}
			<TemplateInputSection
				pptxPath={task.pptxPath}
				onChangePath={task.setPptxPath}
				onBrowse={task.handleBrowsePptx}
				isLoadingShapes={task.isLoadingShapes}
				isLoadingPlaceholders={task.isLoadingPlaceholders}
				templateLoaded={task.templateLoaded}
				textShapeCount={task.textShapeCount}
				imageShapeCount={task.imageShapeCount}
				t={task.t}
			/>

			<DataInputSection
				dataPath={task.dataPath}
				onChangePath={task.setDataPath}
				onBrowse={task.handleBrowseData}
				isLoadingColumns={task.isLoadingColumns}
				dataLoaded={task.dataLoaded}
				sheetCount={task.sheetCount}
				uniqueColumnCount={task.uniqueColumnCount}
				totalRows={task.totalRows}
				sheetNames={task.sheetNames}
				selectedSheets={task.selectedSheets}
				sheetRowCounts={task.sheetRowCounts}
				allSheetsSelected={task.allSheetsSelected}
				someSheetsSelected={task.someSheetsSelected}
				onToggleAllSheets={task.toggleAllSheets}
				onToggleSheet={task.toggleSheet}
				t={task.t}
			/>

			{/* Replacement Tables - Separated */}
			<div className="replacement-section-separated">
				<TextReplacementPanel
					canConfigure={task.canConfigure}
					showTextConfigs={task.showTextConfigs}
					setShowTextConfigs={task.setShowTextConfigs}
					addTextReplacement={task.addTextReplacement}
					textReplacements={task.textReplacements}
					maxTextConfigs={task.maxTextConfigs}
					getAvailablePlaceholders={task.getAvailablePlaceholders}
					updateTextReplacement={task.updateTextReplacement}
					removeTextReplacement={task.removeTextReplacement}
					isLoadingPlaceholders={task.isLoadingPlaceholders}
					placeholders={task.placeholders}
					columns={task.columns}
					t={task.t}
				/>

				<ImageReplacementPanel
					canConfigure={task.canConfigure}
					showImageConfigs={task.showImageConfigs}
					setShowImageConfigs={task.setShowImageConfigs}
					addImageReplacement={task.addImageReplacement}
					imageReplacements={task.imageReplacements}
					maxImageConfigs={task.maxImageConfigs}
					shapes={task.shapes}
					getAvailableShapes={task.getAvailableShapes}
					updateImageReplacement={task.updateImageReplacement}
					removeImageReplacement={task.removeImageReplacement}
					roiOptions={task.roiOptions}
					cropOptions={task.cropOptions}
					getOptionDescription={getOptionDescription}
					columns={task.columns}
					openPreview={task.openPreview}
					t={task.t}
				/>
			</div>

			<SaveLocationSection
				savePath={task.savePath}
				onChangePath={task.setSavePath}
				onBrowse={task.handleBrowseSave}
				t={task.t}
			/>

			<StartButtonSection
				isStarting={task.isStarting}
				canStart={task.canStart}
				onStart={task.handleStart}
				t={task.t}
			/>

			{task.previewShape && (
				<PreviewModal
					previewShape={task.previewShape}
					previewClosing={task.previewClosing}
					closePreview={task.closePreview}
					previewSize={task.previewSize}
					previewZoom={task.previewZoom}
					previewOffset={task.previewOffset}
					adjustPreviewZoom={task.adjustPreviewZoom}
					setPreviewZoom={task.setPreviewZoom}
					handleSavePreview={task.handleSavePreview}
					togglePreviewZoom={task.togglePreviewZoom}
					handlePreviewPointerDown={task.handlePreviewPointerDown}
					handlePreviewPointerMove={task.handlePreviewPointerMove}
					handlePreviewPointerUp={task.handlePreviewPointerUp}
					handlePreviewWheel={task.handlePreviewWheel}
					setPreviewSize={task.setPreviewSize}
					dragMovedRef={task.dragMovedRef}
					t={task.t}
				/>
			)}
		</div>
	);
};

export default CreateTaskMenu;
