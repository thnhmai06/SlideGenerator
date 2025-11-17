import React, { useState } from 'react'
import { useApp } from '../contexts/AppContext'
import '../styles/SettingMenu.css'

const SettingMenu: React.FC = () => {
  const { theme, language, enableAnimations, setTheme, setLanguage, setEnableAnimations, t } = useApp()
  const [autoSave, setAutoSave] = useState(() => {
    const saved = localStorage.getItem('autoSave')
    return saved === 'true'
  })
  const [showNotifications, setShowNotifications] = useState(() => {
    const saved = localStorage.getItem('showNotifications')
    return saved !== 'false' // default true
  })

  const handleThemeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setTheme(e.target.value as 'dark' | 'light')
  }

  const handleLanguageChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setLanguage(e.target.value as 'vi' | 'en')
  }

  const handleAnimationsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setEnableAnimations(e.target.checked)
  }

  const handleAutoSaveChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.checked
    setAutoSave(value)
    localStorage.setItem('autoSave', value.toString())
  }

  const handleNotificationsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.checked
    setShowNotifications(value)
    localStorage.setItem('showNotifications', value.toString())
  }

  return (
    <div className="setting-menu">
      <h1 className="menu-title">{t('settings.title')}</h1>
      
      <div className="setting-section">
        <h3>{t('settings.appSettings')}</h3>
        <div className="setting-item setting-item-row">
          <label className="setting-label">{t('settings.theme')}</label>
          <select 
            className="setting-select" 
            value={theme}
            onChange={handleThemeChange}
          >
            <option value="dark">{t('settings.themeDark')}</option>
            <option value="light">{t('settings.themeLight')}</option>
          </select>
        </div>
        
        <div className="setting-item setting-item-row">
          <label className="setting-label">{t('settings.language')}</label>
          <select 
            className="setting-select"
            value={language}
            onChange={handleLanguageChange}
          >
            <option value="vi">{t('settings.languageVi')}</option>
            <option value="en">{t('settings.languageEn')}</option>
          </select>
        </div>

        <div className="setting-item setting-item-checkbox">
          <label>
            <input 
              type="checkbox" 
              checked={enableAnimations}
              onChange={handleAnimationsChange}
            />
            <span>{t('settings.enableAnimations')}</span>
          </label>
        </div>
      </div>

      <div className="setting-section">
        <h3>{t('settings.processingOptions')}</h3>
        <div className="setting-item setting-item-checkbox">
          <label>
            <input 
              type="checkbox" 
              checked={autoSave}
              onChange={handleAutoSaveChange}
            />
            <span>{t('settings.autoSave')}</span>
          </label>
        </div>
        
        <div className="setting-item setting-item-checkbox">
          <label>
            <input 
              type="checkbox" 
              checked={showNotifications}
              onChange={handleNotificationsChange}
            />
            <span>{t('settings.showNotifications')}</span>
          </label>
        </div>
      </div>
    </div>
  )
}

export default SettingMenu
