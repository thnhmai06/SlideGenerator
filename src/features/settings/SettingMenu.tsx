import React, { useEffect, useState } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import type { SettingTab } from './types';
import { createPadStyles } from './utils';
import { useSettings } from './hooks/useSettings';
import {
	SettingsNotifications,
	SettingsTabs,
	AppearanceTab,
	ServerTab,
	DownloadTab,
	JobTab,
	ImageTab,
	SettingActions,
} from './components';
import './SettingMenu.css';

const SettingMenu: React.FC = () => {
	const {
		theme,
		language,
		enableAnimations,
		closeToTray,
		setTheme,
		setLanguage,
		setEnableAnimations,
		setCloseToTray,
		t,
	} = useApp();
	const { groups } = useJobs();
	const [activeTab, setActiveTab] = useState<SettingTab>('appearance');

	const {
		config,
		loading,
		saving,
		message,
		restartRequired,
		showRestartNotification,
		isRestartNotificationClosing,
		showStatusNotification,
		isStatusNotificationClosing,
		faceModelAvailable,
		modelLoading,

		setShowRestartNotification,
		setIsRestartNotificationClosing,

		handleNumberChange,
		handleNumberBlur,
		handleNumberFocus,
		hideStatusNotification,

		loadConfig,
		loadModelStatus,
		saveConfig,
		reloadConfig,
		resetConfig,
		handleRestartServer,
		handleInitModel,
		handleDeinitModel,

		updateServer,
		updateDownload,
		updateJob,
		updateFace,
		updateSaliency,
		handleSelectDownloadFolder,
	} = useSettings({ t });

	useEffect(() => {
		loadConfig().catch(() => undefined);
		loadModelStatus().catch(() => undefined);
	}, [loadConfig, loadModelStatus]);

	useEffect(() => {
		if (restartRequired) {
			setShowRestartNotification(true);
			setIsRestartNotificationClosing(false);
			return undefined;
		}

		if (!showRestartNotification) return undefined;
		setIsRestartNotificationClosing(true);
		const timeoutId = window.setTimeout(() => {
			setShowRestartNotification(false);
			setIsRestartNotificationClosing(false);
		}, 180);
		return () => window.clearTimeout(timeoutId);
	}, [
		restartRequired,
		setIsRestartNotificationClosing,
		setShowRestartNotification,
		showRestartNotification,
	]);

	const isActiveStatus = (status: string) => ['pending', 'running'].includes(status.toLowerCase());
	const hasActiveJobs = groups.some((group) => {
		if (isActiveStatus(group.status)) return true;
		return Object.values(group.sheets).some((sheet) => isActiveStatus(sheet.status));
	});
	const isLocked = activeTab !== 'appearance' && hasActiveJobs;
	const canEditConfig = !hasActiveJobs;
	const isEditable = !loading && !!config && canEditConfig;

	return (
		<div className="setting-menu">
			<h1 className="menu-title">{t('settings.title')}</h1>

			<SettingsNotifications
				showRestartNotification={showRestartNotification}
				isRestartNotificationClosing={isRestartNotificationClosing}
				onRestart={handleRestartServer}
				message={message}
				showStatusNotification={showStatusNotification}
				isStatusNotificationClosing={isStatusNotificationClosing}
				onCloseStatus={hideStatusNotification}
				showLockedNotification={isLocked}
				t={t}
			/>

			<SettingsTabs activeTab={activeTab} onSelectTab={setActiveTab} t={t} />

			{activeTab === 'appearance' && (
				<AppearanceTab
					theme={theme}
					language={language}
					enableAnimations={enableAnimations}
					closeToTray={closeToTray}
					setTheme={setTheme}
					setLanguage={setLanguage}
					setEnableAnimations={setEnableAnimations}
					setCloseToTray={setCloseToTray}
					t={t}
				/>
			)}

			{activeTab === 'server' && (
				<ServerTab
					loading={loading}
					config={config}
					canEditConfig={canEditConfig}
					isLocked={isLocked}
					updateServer={updateServer}
					handleNumberChange={handleNumberChange}
					handleNumberBlur={handleNumberBlur}
					handleNumberFocus={handleNumberFocus}
					t={t}
				/>
			)}

			{activeTab === 'download' && (
				<DownloadTab
					loading={loading}
					config={config}
					canEditConfig={canEditConfig}
					isLocked={isLocked}
					updateDownload={updateDownload}
					handleNumberChange={handleNumberChange}
					handleNumberBlur={handleNumberBlur}
					handleNumberFocus={handleNumberFocus}
					onSelectFolder={handleSelectDownloadFolder}
					t={t}
				/>
			)}

			{activeTab === 'job' && (
				<JobTab
					loading={loading}
					config={config}
					canEditConfig={canEditConfig}
					isLocked={isLocked}
					updateJob={updateJob}
					handleNumberChange={handleNumberChange}
					handleNumberBlur={handleNumberBlur}
					handleNumberFocus={handleNumberFocus}
					t={t}
				/>
			)}

			{activeTab === 'image' && (
				<ImageTab
					loading={loading}
					config={config}
					canEditConfig={canEditConfig}
					isLocked={isLocked}
					faceModelAvailable={faceModelAvailable}
					modelLoading={modelLoading}
					onInitModel={handleInitModel}
					onDeinitModel={handleDeinitModel}
					updateFace={updateFace}
					updateSaliency={updateSaliency}
					createPadStyles={createPadStyles}
					handleNumberChange={handleNumberChange}
					handleNumberBlur={handleNumberBlur}
					handleNumberFocus={handleNumberFocus}
					t={t}
				/>
			)}

			<SettingActions
				saving={saving}
				isEditable={isEditable}
				showActions={activeTab !== 'appearance'}
				onSave={saveConfig}
				onReload={reloadConfig}
				onReset={resetConfig}
				t={t}
			/>
		</div>
	);
};

export default SettingMenu;
