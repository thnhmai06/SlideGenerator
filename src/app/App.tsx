import React, { lazy, Suspense, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import Sidebar from '@/shared/components/Sidebar';
import TitleBar from '@/shared/components/TitleBar';
import { checkHealth } from '@/shared/services/backendApi';
import { useApp } from '@/shared/contexts/useApp';
import { useJobs } from '@/shared/contexts/useJobs';
import './App.css';

// Lazy load feature components for code splitting
const CreateTaskMenu = lazy(() => import('@/features/create-task'));
const SettingMenu = lazy(() => import('@/features/settings'));
const ProcessMenu = lazy(() => import('@/features/process'));
const ResultMenu = lazy(() => import('@/features/results'));
const AboutMenu = lazy(() => import('@/features/about'));

// Loading fallback component
const MenuLoader: React.FC = () => (
	<div className="menu-loader">
		<div className="menu-loader-spinner" />
	</div>
);

export type MenuType = 'input' | 'setting' | 'download' | 'process' | 'about';

export type ConnectionBannerState = 'hidden' | 'connected' | 'disconnected';

export interface AppProps {
	/** Initial menu used by Storybook/tests to render a specific workflow directly. */
	initialMenu?: MenuType;
	/** Health probe dependency; injectable so App stories do not need a real backend. */
	healthCheck?: () => Promise<unknown>;
	/** Disable polling for deterministic stories and static render states. */
	enableHealthPolling?: boolean;
	/** Optional deterministic banner state for stories. */
	initialConnectionBanner?: ConnectionBannerState;
}

const App: React.FC<AppProps> = ({
	initialMenu = 'input',
	healthCheck = checkHealth,
	enableHealthPolling = true,
	initialConnectionBanner = 'hidden',
}) => {
	const { t } = useApp();
	const { groups } = useJobs();
	const [currentMenu, setCurrentMenu] = useState<MenuType>(initialMenu);
	const [bannerState, setBannerState] = useState<ConnectionBannerState>(initialConnectionBanner);
	const connectionRef = useRef<'connected' | 'disconnected' | 'unknown'>('unknown');

	useEffect(() => {
		setCurrentMenu(initialMenu);
	}, [initialMenu]);

	useEffect(() => {
		const showConnected = () => {
			setBannerState('connected');
			window.setTimeout(() => {
				setBannerState('hidden');
			}, 2000);
		};

		const showDisconnected = () => {
			setBannerState('disconnected');
		};

		const updateStatus = async () => {
			try {
				await healthCheck();
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
		if (!enableHealthPolling) return undefined;
		const intervalId = window.setInterval(updateStatus, 5000);
		return () => {
			window.clearInterval(intervalId);
		};
	}, [enableHealthPolling, healthCheck]);

	useEffect(() => {
		const allowedMenus: MenuType[] = ['input', 'setting', 'download', 'process', 'about'];
		const isMenuType = (value: string): value is MenuType =>
			allowedMenus.includes(value as MenuType);
		const unsubscribe = window.desktopAPI?.onNavigate?.((menu) => {
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

	const handleStart = useCallback(() => {
		setCurrentMenu('process');
	}, []);

	const renderMenu = useMemo(() => {
		switch (currentMenu) {
			case 'input':
				return <CreateTaskMenu onStart={handleStart} />;
			case 'setting':
				return <SettingMenu />;
			case 'download':
				return <ResultMenu />;
			case 'process':
				return <ProcessMenu />;
			case 'about':
				return <AboutMenu />;
			default:
				return <CreateTaskMenu onStart={handleStart} />;
		}
	}, [currentMenu, handleStart]);

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
				<div className="main-content">
					<Suspense fallback={<MenuLoader />}>{renderMenu}</Suspense>
				</div>
			</div>
		</div>
	);
};

export default App;
