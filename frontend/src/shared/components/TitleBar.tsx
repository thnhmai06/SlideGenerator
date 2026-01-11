import React from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { getAssetPath } from '@/shared/utils/paths';
import './TitleBar.css';

type TitleBarProps = {
	title: string;
};

const TitleBar: React.FC<TitleBarProps> = ({ title }) => {
	const { closeToTray, t } = useApp();
	const handleAction = (action: 'minimize' | 'maximize' | 'close') => {
		if (action === 'close' && closeToTray) {
			window.electronAPI?.hideToTray();
			return;
		}
		window.electronAPI?.windowControl(action);
	};

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
				<button
					className="title-bar-btn"
					onClick={() => handleAction('minimize')}
					aria-label="Minimize"
				>
					<img src={getAssetPath('images', 'window-minimize.png')} alt="" />
				</button>
				<button
					className="title-bar-btn"
					onClick={() => handleAction('maximize')}
					aria-label="Maximize"
				>
					<img src={getAssetPath('images', 'window-maximize.png')} alt="" />
				</button>
				<button
					className="title-bar-btn title-bar-btn-danger"
					onClick={() => handleAction('close')}
					aria-label="Close"
				>
					<img src={getAssetPath('images', 'window-close.png')} alt="" />
				</button>
			</div>
		</div>
	);
};

export default TitleBar;
