export interface ConfigGetSuccess {
  type: 'get'
  server: {
    host: string
    port: number
    debug: boolean
  }
  download: {
    maxChunks: number
    limitBytesPerSecond: number
    saveFolder: string
    retry: {
      timeout: number
      maxRetries: number
    }
    proxy: {
      useProxy: boolean
      proxyAddress: string
      username: string
      password: string
      domain: string
    }
  }
  job: {
    maxConcurrentJobs: number
  }
  image: {
    face: {
      confidence: number
      unionAll: boolean
    }
    saliency: {
      paddingTop: number
      paddingBottom: number
      paddingLeft: number
      paddingRight: number
    }
  }
}

export interface ConfigUpdateSuccess {
  type: 'update'
  success: boolean
  message: string
}

export interface ConfigReloadSuccess {
  type: 'reload'
  success: boolean
  message: string
}

export interface ConfigResetSuccess {
  type: 'reset'
  success: boolean
  message: string
}

export interface ModelStatusSuccess {
  type: 'modelstatus'
  faceModelAvailable: boolean
}

export interface ModelControlSuccess {
  type: 'modelcontrol'
  model: string
  action: string
  success: boolean
  message?: string
}
