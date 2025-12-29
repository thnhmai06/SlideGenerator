import { useContext } from 'react'
import { JobContext } from './JobContextBase'

export const useJobs = () => {
  const context = useContext(JobContext)
  if (!context) {
    throw new Error('useJobs must be used within JobProvider')
  }
  return context
}
