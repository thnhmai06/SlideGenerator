import type { ResponseBase } from './types'

export function getResponseType(response: ResponseBase): string {
  return (response.Type ?? response.type ?? '').toLowerCase()
}

export function getCaseInsensitive<TValue = unknown>(obj: unknown, key: string): TValue | undefined {
  if (!obj || typeof obj !== 'object') return undefined
  const record = obj as Record<string, unknown>
  if (key in record) return record[key] as TValue
  const lowered = key.toLowerCase()
  for (const [entryKey, value] of Object.entries(record)) {
    if (entryKey.toLowerCase() === lowered) {
      return value as TValue
    }
  }
  return undefined
}

export function getResponseErrorMessage(response: ResponseBase): string {
  const message = response.Message ?? response.message ?? ''
  const kind = response.Kind ?? ''
  const filePath = response.FilePath ?? response.filePath ?? ''
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
