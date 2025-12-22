import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { translations, Language } from '../locales'

type Theme = 'dark' | 'light' | 'system'

interface Settings {
  theme: Theme
  language: Language
  enableAnimations: boolean
  closeToTray: boolean
}

interface AppContextType {
  theme: Theme
  language: Language
  enableAnimations: boolean
  closeToTray: boolean
  setTheme: (theme: Theme) => void
  setLanguage: (language: Language) => void
  setEnableAnimations: (enable: boolean) => void
  setCloseToTray: (enable: boolean) => void
  t: (key: string) => string
}

const AppContext = createContext<AppContextType | undefined>(undefined)

const SETTINGS_FILE = 'app-settings.json'

const loadSettings = async (): Promise<Settings | null> => {
  try {
    const data = await window.electronAPI.readSettings(SETTINGS_FILE)
    return data ? JSON.parse(data) : null
  } catch (error) {
    console.error('Failed to load settings:', error)
    return null
  }
}

const saveSettings = async (settings: Settings) => {
  try {
    await window.electronAPI.writeSettings(SETTINGS_FILE, JSON.stringify(settings, null, 2))
  } catch (error) {
    console.error('Failed to save settings:', error)
  }
}

export const AppProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [theme, setThemeState] = useState<Theme>('system')
  const [resolvedTheme, setResolvedTheme] = useState<'dark' | 'light'>('dark')
  const [language, setLanguageState] = useState<Language>('vi')
  const [enableAnimations, setEnableAnimationsState] = useState<boolean>(true)
  const [closeToTray, setCloseToTrayState] = useState<boolean>(false)
  const [isLoaded, setIsLoaded] = useState(false)

  // Load settings on mount
  useEffect(() => {
    const initSettings = async () => {
      const savedSettings = await loadSettings()
      if (savedSettings) {
        const savedTheme = (savedSettings.theme as Theme) || 'system'
        setThemeState(savedTheme)
        setLanguageState(savedSettings.language || 'vi')
        setEnableAnimationsState(savedSettings.enableAnimations !== false)
        setCloseToTrayState(savedSettings.closeToTray === true)
      }
      setIsLoaded(true)
    }
    initSettings()
  }, [])

  // Auto-save settings whenever they change
  useEffect(() => {
    if (isLoaded) {
      const settings: Settings = {
        theme,
        language,
        enableAnimations,
        closeToTray,
      }
      saveSettings(settings)
    }
  }, [theme, language, enableAnimations, closeToTray, isLoaded])

  useEffect(() => {
    if (typeof window === 'undefined' || !window.matchMedia) {
      setResolvedTheme(theme === 'system' ? 'dark' : theme)
      return
    }

    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const updateResolved = () => {
      if (theme === 'system') {
        setResolvedTheme(media.matches ? 'dark' : 'light')
      } else {
        setResolvedTheme(theme)
      }
    }

    updateResolved()
    media.addEventListener('change', updateResolved)
    return () => media.removeEventListener('change', updateResolved)
  }, [theme])

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', resolvedTheme)
    if (enableAnimations) {
      document.documentElement.classList.remove('no-animations')
    } else {
      document.documentElement.classList.add('no-animations')
    }
  }, [resolvedTheme, enableAnimations])

  const setTheme = (newTheme: Theme) => {
    setThemeState(newTheme)
  }

  const setLanguage = (newLanguage: Language) => {
    setLanguageState(newLanguage)
  }

  const setEnableAnimations = (enable: boolean) => {
    setEnableAnimationsState(enable)
  }

  const setCloseToTray = (enable: boolean) => {
    setCloseToTrayState(enable)
  }

  const t = (key: string): string => {
    const langTranslations = translations[language] as Record<string, string>
    return langTranslations[key] || key
  }

  return (
    <AppContext.Provider
      value={{
        theme,
        language,
        enableAnimations,
        closeToTray,
        setTheme,
        setLanguage,
        setEnableAnimations,
        setCloseToTray,
        t,
      }}
    >
      {children}
    </AppContext.Provider>
  )
}

export const useApp = () => {
  const context = useContext(AppContext)
  if (!context) {
    throw new Error('useApp must be used within AppProvider')
  }
  return context
}
