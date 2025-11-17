import React from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/Sidebar.css'

type MenuType = 'input' | 'setting' | 'download' | 'process' | 'about'

interface SidebarProps {
  currentMenu: MenuType
  onMenuChange: (menu: MenuType) => void
}

const Sidebar: React.FC<SidebarProps> = ({ currentMenu, onMenuChange }) => {
  const { t } = useApp()
  
  const menuItems = [
    { id: 'input' as MenuType, label: t('menu.input'), icon: '/assets/upload.png', activeIcon: '/assets/upload-selected.png' },
    { id: 'process' as MenuType, label: t('process.title'), icon: '/assets/process.png', activeIcon: '/assets/process-selected.png' },
    { id: 'download' as MenuType, label: t('output.title'), icon: '/assets/download.png', activeIcon: '/assets/download-selected.png' },
  ]

  return (
    <div className="sidebar">
      <div className="sidebar-header">
        <img src="/assets/UET-logo.png" alt="UET Logo" className="sidebar-logo" />
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
          title={t('menu.setting')}
        >
          <img 
            src={currentMenu === 'setting' ? '/assets/setting-selected.png' : '/assets/setting.png'} 
            alt="Setting" 
            className="footer-icon" 
          />
        </button>
        <div className="footer-spacer"></div>
        <button 
          className={`sidebar-icon-btn ${currentMenu === 'about' ? 'active' : ''}`}
          onClick={() => onMenuChange('about')}
          title={t('menu.about')}
        >
          <img 
            src={currentMenu === 'about' ? '/assets/about-selected.png' : '/assets/about.png'} 
            alt="About" 
            className="footer-icon" 
          />
        </button>
      </div>
    </div>
  )
}

export default Sidebar
