import { getBackendBaseUrl } from '../../signalrClient'

export async function checkHealth(): Promise<{ status: string; message: string }> {
  const baseUrl = getBackendBaseUrl()
  const response = await fetch(`${baseUrl}/health`)

  if (!response.ok) {
    throw new Error('Backend server is not responding')
  }

  const data = (await response.json()) as { IsRunning?: boolean }
  return {
    status: data.IsRunning ? 'ok' : 'unknown',
    message: data.IsRunning ? 'Backend is running' : 'Backend status unknown',
  }
}
