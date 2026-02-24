/**
 * Calls the backend health JSON-RPC endpoint and normalizes the response.
 */
export async function checkHealth(): Promise<{ status: string; message: string }> {
  const data = await window.electronAPI.backendRequest<{ ok?: boolean }>('system.health')
  return {
    status: data.ok ? 'ok' : 'unknown',
    message: data.ok ? 'Backend is running' : 'Backend status unknown',
  }
}
