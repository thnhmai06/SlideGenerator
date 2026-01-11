import React from 'react';
import { useApp } from '@/shared/contexts/useApp';
import { getAssetPath } from '@/shared/utils/paths';
import './AboutMenu.css';

const AboutMenu: React.FC = () => {
	const { t } = useApp();
	const version = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : '';
	const developers = [
		{ name: 'thnhmai06', url: 'https://github.com/thnhmai06' },
		{ name: 'NAV-adsf23fd', url: 'https://github.com/NAV-adsf23fd' },
		{ name: 'Hair-Nguyeenx', url: 'https://github.com/Hair-Nguyeenx' },
	];
	const handleOpenGithub = () => {
		window.electronAPI.openUrl('https://github.com/thnhmai06/SlideGenerator');
	};

	return (
		<div className="about-menu">
			<h1 className="menu-title">{t('sideBar.about')}</h1>

			<div className="about-content">
				<div className="about-hero-gifs">
					<img
						src={getAssetPath('images', 'march-7th-dance.gif')}
						alt=""
						className="about-hero-gif"
					/>
					<img
						src={getAssetPath('images', 'evernight-dance.gif')}
						alt=""
						className="about-hero-gif"
					/>
				</div>
				<div className="about-section">
					<h2>{t('about.appName')}</h2>
					<p className="version">{`${t('about.version')} ${version}`.trim()}</p>
					<p className="description">
						{t('about.description')}
						<br />
						{t('about.details')}
					</p>
				</div>

				<div className="about-section">
					<h3>{t('about.developer')}</h3>
					<div className="developer-list">
						{developers.map((dev) => (
							<button
								key={dev.name}
								className="developer-link"
								onClick={() => window.electronAPI.openUrl(dev.url)}
							>
								<img
									src={`https://github.com/${dev.name}.png`}
									alt=""
									className="developer-avatar"
									onError={(event) => {
										const target = event.currentTarget;
										target.style.display = 'none';
									}}
								/>
								{dev.name}
							</button>
						))}
					</div>
				</div>

				<div className="about-links">
					<button className="link-btn" onClick={handleOpenGithub}>
						<img
							src={getAssetPath('images', 'github-logo.png')}
							alt="GitHub"
							className="link-icon"
						/>
						{t('about.githubRepo')}
					</button>
				</div>

				<div className="about-footer">
					<p>{t('about.license')}</p>
				</div>
			</div>
		</div>
	);
};

export default AboutMenu;
