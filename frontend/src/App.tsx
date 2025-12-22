import React, { useState } from 'react'
import Sidebar from './components/Sidebar'
import CreateTaskMenu from './components/CreateTaskMenu'
import SettingMenu from './components/SettingMenu'
import ProcessMenu from './components/ProcessMenu'
import ResultMenu from './components/ResultMenu'
import AboutMenu from './components/AboutMenu'
import TitleBar from './components/TitleBar'
import './styles/App.css'

type MenuType = 'input' | 'setting' | 'download' | 'process' | 'about'

const App: React.FC = () => {
  const [currentMenu, setCurrentMenu] = useState<MenuType>('input')

  const renderMenu = () => {
    switch (currentMenu) {
      case 'input':
        return <CreateTaskMenu onStart={() => setCurrentMenu('process')} />
      case 'setting':
        return <SettingMenu />
      case 'download':
        return <ResultMenu />
      case 'process':
        return <ProcessMenu />
      case 'about':
        return <AboutMenu />
      default:
        return <CreateTaskMenu onStart={() => setCurrentMenu('process')} />
    }
  }

  return (
    <div className="app-shell">
      <TitleBar />
      <div className="app-container">
        <Sidebar currentMenu={currentMenu} onMenuChange={setCurrentMenu} />
        <div className="main-content">
          {renderMenu()}
        </div>
      </div>
    </div>
  )
}

export default App
