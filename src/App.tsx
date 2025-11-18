import React, { useState } from 'react'
import Sidebar from './components/Sidebar'
import InputMenu from './components/InputMenu'
import SettingMenu from './components/SettingMenu'
import ProcessMenu from './components/ProcessMenu'
import OutputMenu from './components/OutputMenu'
import AboutMenu from './components/AboutMenu'
import './styles/App.css'

type MenuType = 'input' | 'setting' | 'download' | 'process' | 'about'

const App: React.FC = () => {
  const [currentMenu, setCurrentMenu] = useState<MenuType>('input')

  const renderMenu = () => {
    switch (currentMenu) {
      case 'input':
        return <InputMenu onStart={() => setCurrentMenu('process')} />
      case 'setting':
        return <SettingMenu />
      case 'download':
        return <OutputMenu />
      case 'process':
        return <ProcessMenu />
      case 'about':
        return <AboutMenu />
      default:
        return <InputMenu onStart={() => setCurrentMenu('process')} />
    }
  }

  return (
    <div className="app-container">
      <Sidebar currentMenu={currentMenu} onMenuChange={setCurrentMenu} />
      <div className="main-content">
        {renderMenu()}
      </div>
    </div>
  )
}

export default App
