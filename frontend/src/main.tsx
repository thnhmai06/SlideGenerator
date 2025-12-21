import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import { AppProvider } from './contexts/AppContext'
import { JobProvider } from './contexts/JobContext'
import './styles/theme.css'
import './styles/index.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppProvider>
      <JobProvider>
        <App />
      </JobProvider>
    </AppProvider>
  </React.StrictMode>,
)
