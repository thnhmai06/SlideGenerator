import React from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/AboutMenu.css'

const AboutMenu: React.FC = () => {
  const { t } = useApp()
  const handleOpenGithub = () => {
    window.electronAPI.openUrl('https://github.com/thnhmai06/SlideGenerator')
  }

  const handleOpenReadme = async () => {
    // Open README.md in parent directory
    const readmePath = '../README.md'
    await window.electronAPI.openPath(readmePath)
  }

  return (
    <div className="about-menu">
      <h1 className="menu-title">{t('menu.about')}</h1>
      
      <div className="about-content">
        <div className="about-section">
          <h2>{t('about.appName')}</h2>
          <p className="version">{t('about.version')}</p>
          <p className="description">
            {t('about.description')}
            <br />
            {t('about.builtWith')}
          </p>
        </div>

        <div className="about-section">
          <h3>{t('about.developer')}</h3>
          <p>thnhmai06</p>
        </div>

        <div className="about-links">
          <button className="link-btn" onClick={handleOpenGithub}>
            <img src="/assets/images/github-logo.png" alt="GitHub" className="link-icon" />
            {t('about.githubRepo')}
          </button>
          <button className="link-btn" onClick={handleOpenReadme}>
            <img src="/assets/images/readme.png" alt="README" className="link-icon" />
            {t('about.readDocs')}
          </button>
        </div>

        <div className="about-footer">
          <p>{t('about.license')}</p>
        </div>
      </div>
    </div>
  )
}

export default AboutMenu
