import React from 'react';
import { useApp } from '../contexts/useApp';
import { getAssetPath } from '../utils/paths';
import '../styles/Sidebar.css';

type MenuType = 'input' | 'setting' | 'download' | 'process' | 'about';

interface SidebarProps {
	currentMenu: MenuType;
	onMenuChange: (menu: MenuType) => void;
}

const Sidebar: React.FC<SidebarProps> = ({ currentMenu, onMenuChange }) => {
	const { t } = useApp();

	const menuItems = [
		{
			id: 'input' as MenuType,
			label: t('sideBar.createTask'),
			icon: getAssetPath('images', 'createTask.png'),
			activeIcon: getAssetPath('images', 'createTask-selected.png'),
		},
		{
			id: 'process' as MenuType,
			label: t('process.title'),
			icon: getAssetPath('images', 'process.png'),
			activeIcon: getAssetPath('images', 'process-selected.png'),
		},
		{
			id: 'download' as MenuType,
			label: t('results.title'),
			icon: getAssetPath('images', 'result.png'),
			activeIcon: getAssetPath('images', 'result-selected.png'),
		},
	];

	return (
		<div className="sidebar">
			<div className="sidebar-header">
				<img
					src={getAssetPath('images', 'app-logo.png')}
					alt="UET Logo"
					className="sidebar-logo"
				/>
				<h2>{t('app.title')}</h2>
			</div>
			<ul className="sidebar-menu">
				{menuItems.map((item) => (
					<li
						key={item.id}
						className={`sidebar-item ${currentMenu === item.id ? 'active' : ''}`}
						onClick={() => onMenuChange(item.id)}
					>
						<img
							src={currentMenu === item.id ? item.activeIcon : item.icon}
							alt={item.label}
							className="sidebar-icon"
						/>
						<span className="sidebar-label">{item.label}</span>
					</li>
				))}
			</ul>
			<div className="sidebar-footer">
				<button
					className={`sidebar-icon-btn ${currentMenu === 'setting' ? 'active' : ''}`}
					onClick={() => onMenuChange('setting')}
					title={t('sideBar.setting')}
				>
					<img
						src={
							currentMenu === 'setting'
								? getAssetPath('images', 'setting-selected.png')
								: getAssetPath('images', 'setting.png')
						}
						alt="Setting"
						className="footer-icon"
					/>
				</button>
				<div className="footer-spacer"></div>
				<button
					className={`sidebar-icon-btn ${currentMenu === 'about' ? 'active' : ''}`}
					onClick={() => onMenuChange('about')}
					title={t('sideBar.about')}
				>
					<img
						src={
							currentMenu === 'about'
								? getAssetPath('images', 'about-selected.png')
								: getAssetPath('images', 'about.png')
						}
						alt="About"
						className="footer-icon"
					/>
				</button>
			</div>
		</div>
	);
};

export default Sidebar;
