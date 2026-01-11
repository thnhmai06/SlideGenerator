import React, { useEffect, useMemo, useRef, useState } from 'react';
import Sidebar from '@/shared/components/Sidebar';
import TitleBar from '@/shared/components/TitleBar';
import CreateTaskMenu from '@/features/create-task';
import SettingMenu from '@/features/settings';
import ProcessMenu from '@/features/process';
import ResultMenu from '@/features/results';
import AboutMenu from '@/features/about';
import { checkHealth } from '@/shared/services/backendApi';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import './App.css';

type MenuType = 'input' | 'setting' | 'download' | 'process' | 'about';

const App: React.FC = () => {
	const { t } = useApp();
	const { groups } = useJobs();
	const [currentMenu, setCurrentMenu] = useState<MenuType>('input');
	const [bannerState, setBannerState] = useState<'hidden' | 'connected' | 'disconnected'>(
		'hidden',
	);
	const bannerTimeoutRef = useRef<number | null>(null);
	const connectionRef = useRef<'connected' | 'disconnected' | 'unknown'>('unknown');

	useEffect(() => {
		const clearBannerTimeout = () => {
			if (bannerTimeoutRef.current !== null) {
				window.clearTimeout(bannerTimeoutRef.current);
				bannerTimeoutRef.current = null;
			}
		};

		const showConnected = () => {
			clearBannerTimeout();
			setBannerState('connected');
			bannerTimeoutRef.current = window.setTimeout(() => {
				if (connectionRef.current === 'connected') {
					setBannerState('hidden');
				}
			}, 2000);
		};

		const showDisconnected = () => {
			clearBannerTimeout();
			setBannerState('disconnected');
		};

		const updateStatus = async () => {
			try {
				await checkHealth();
				if (connectionRef.current !== 'connected') {
					connectionRef.current = 'connected';
					console.info('Backend connection restored.');
					showConnected();
				}
			} catch {
				if (connectionRef.current !== 'disconnected') {
					connectionRef.current = 'disconnected';
					showDisconnected();
				}
			}
		};

		updateStatus().catch(() => undefined);
		const intervalId = window.setInterval(updateStatus, 5000);
		return () => {
			clearBannerTimeout();
			window.clearInterval(intervalId);
		};
	}, []);

	useEffect(() => {
		const allowedMenus: MenuType[] = ['input', 'setting', 'download', 'process', 'about'];
		const isMenuType = (value: string): value is MenuType =>
			allowedMenus.includes(value as MenuType);
		const unsubscribe = window.electronAPI?.onNavigate?.((menu) => {
			if (isMenuType(menu)) {
				setCurrentMenu(menu);
			}
		});

		return () => {
			if (typeof unsubscribe === 'function') {
				unsubscribe();
			}
		};
	}, []);

	const appTitle = t('app.title');
	const windowTitle = useMemo(() => {
		const activeGroups = groups.filter((group) =>
			['pending', 'running', 'paused'].includes(group.status.toLowerCase()),
		);
		if (activeGroups.length === 0) return appTitle;

		let totalSlides = 0;
		let completedSlides = 0;
		activeGroups.forEach((group) => {
			Object.values(group.sheets).forEach((sheet) => {
				const total = sheet.totalRows ?? 0;
				totalSlides += total;
				completedSlides += Math.min(sheet.currentRow ?? 0, total);
			});
		});

		const percent = totalSlides > 0 ? Math.round((completedSlides / totalSlides) * 100) : 0;
		return `${appTitle} - ${completedSlides}/${totalSlides} ${t('process.slides')} (${percent}%)`;
	}, [appTitle, groups, t]);

	useEffect(() => {
		document.title = windowTitle;
	}, [windowTitle]);

	const renderMenu = () => {
		switch (currentMenu) {
			case 'input':
				return <CreateTaskMenu onStart={() => setCurrentMenu('process')} />;
			case 'setting':
				return <SettingMenu />;
			case 'download':
				return <ResultMenu />;
			case 'process':
				return <ProcessMenu />;
			case 'about':
				return <AboutMenu />;
			default:
				return <CreateTaskMenu onStart={() => setCurrentMenu('process')} />;
		}
	};

	return (
		<div className="app-shell">
			<TitleBar title={windowTitle} />
			<div className={`connection-banner ${bannerState}`}>
				<div className="connection-banner__content">
					{bannerState === 'disconnected' && t('connection.disconnected')}
					{bannerState === 'connected' && t('connection.connected')}
				</div>
			</div>
			<div className="app-container">
				<Sidebar currentMenu={currentMenu} onMenuChange={setCurrentMenu} />
				<div className="main-content">{renderMenu()}</div>
			</div>
		</div>
	);
};

export default App;
