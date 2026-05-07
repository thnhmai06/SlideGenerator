import type { ResponseBase } from './types'

export function getResponseType(response: ResponseBase): string {
  return (response.type ?? '').toLowerCase()
}

export function getResponseErrorMessage(response: ResponseBase): string {
  const message = response.message ?? ''
  const kind = response.kind ?? ''
  const filePath = response.filePath ?? ''
  const prefix = filePath ? `[${filePath}] ` : ''
  if (message && kind && !message.includes(kind)) {
    return `${prefix}${kind}: ${message}`
  }
  return `${prefix}${message || kind || 'Backend error'}`
}

export function assertSuccess<T>(response: ResponseBase): T {
  if (getResponseType(response) === 'error') {
    throw new Error(getResponseErrorMessage(response))
  }
  return response as T
}
