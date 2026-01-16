import React, { memo, useCallback } from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { getAssetPath } from '@/shared/utils/paths';
import './TitleBar.css';

/** Props for {@link TitleBar}. */
interface TitleBarProps {
	/** Window title text. */
	title: string;
}

/**
 * Custom window title bar with minimize, maximize, and close buttons.
 *
 * @remarks
 * Supports close-to-tray behavior based on app settings.
 * Replaces the native Electron title bar for a custom look.
 */
const TitleBar: React.FC<TitleBarProps> = memo(({ title }) => {
	const { closeToTray, t } = useApp();

	const handleMinimize = useCallback(() => {
		window.electronAPI?.windowControl('minimize');
	}, []);

	const handleMaximize = useCallback(() => {
		window.electronAPI?.windowControl('maximize');
	}, []);

	const handleClose = useCallback(() => {
		if (closeToTray) {
			window.electronAPI?.hideToTray();
			return;
		}
		window.electronAPI?.windowControl('close');
	}, [closeToTray]);

	return (
		<div className="title-bar">
			<div className="title-bar-left">
				<img
					src={getAssetPath('images', 'app-icon.png')}
					alt={t('app.title')}
					className="title-bar-icon"
				/>
				<span className="title-bar-title">{title}</span>
			</div>
			<div className="title-bar-controls">
				<button className="title-bar-btn" onClick={handleMinimize} aria-label="Minimize">
					<img src={getAssetPath('images', 'window-minimize.png')} alt="" />
				</button>
				<button className="title-bar-btn" onClick={handleMaximize} aria-label="Maximize">
					<img src={getAssetPath('images', 'window-maximize.png')} alt="" />
				</button>
				<button
					className="title-bar-btn title-bar-btn-danger"
					onClick={handleClose}
					aria-label="Close"
				>
					<img src={getAssetPath('images', 'window-close.png')} alt="" />
				</button>
			</div>
		</div>
	);
});

TitleBar.displayName = 'TitleBar';

export default TitleBar;
