import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { translations, Language } from '../locales'

type Theme = 'dark' | 'light'

interface Settings {
  theme: Theme
  language: Language
  enableAnimations: boolean
}

interface AppContextType {
  theme: Theme
  language: Language
  enableAnimations: boolean
  setTheme: (theme: Theme) => void
  setLanguage: (language: Language) => void
  setEnableAnimations: (enable: boolean) => void
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
  const [theme, setThemeState] = useState<Theme>('dark')
  const [language, setLanguageState] = useState<Language>('vi')
  const [enableAnimations, setEnableAnimationsState] = useState<boolean>(true)
  const [isLoaded, setIsLoaded] = useState(false)

  // Load settings on mount
  useEffect(() => {
    const initSettings = async () => {
      const savedSettings = await loadSettings()
      if (savedSettings) {
        setThemeState(savedSettings.theme || 'dark')
        setLanguageState(savedSettings.language || 'vi')
        setEnableAnimationsState(savedSettings.enableAnimations !== false)
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
      }
      saveSettings(settings)
    }
  }, [theme, language, enableAnimations, isLoaded])

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
    if (enableAnimations) {
      document.documentElement.classList.remove('no-animations')
    } else {
      document.documentElement.classList.add('no-animations')
    }
  }, [theme, enableAnimations])

  const setTheme = (newTheme: Theme) => {
    setThemeState(newTheme)
  }

  const setLanguage = (newLanguage: Language) => {
    setLanguageState(newLanguage)
  }

  const setEnableAnimations = (enable: boolean) => {
    setEnableAnimationsState(enable)
  }

  const t = (key: string): string => {
    const langTranslations = translations[language] as Record<string, string>
    return langTranslations[key] || key
  }

  return (
    <AppContext.Provider value={{ theme, language, enableAnimations, setTheme, setLanguage, setEnableAnimations, t }}>
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
