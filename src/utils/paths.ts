export const getAssetPath = (...parts: string[]): string => {
  if (typeof window !== 'undefined' && typeof window.getAssetPath === 'function') {
    return window.getAssetPath(...parts)
  }

  return `assets/${parts.join('/')}`
}
