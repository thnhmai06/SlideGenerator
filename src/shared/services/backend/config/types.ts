export interface ConfigGetSuccess {
  Type: 'get'
  Server: {
    Host: string
    Port: number
    Debug: boolean
  }
  Download: {
    MaxChunks: number
    LimitBytesPerSecond: number
    SaveFolder: string
    Retry: {
      Timeout: number
      MaxRetries: number
    }
  }
  Job: {
    MaxConcurrentJobs: number
  }
  Image: {
    Face: {
      Confidence: number
      UnionAll: boolean
    }
    Saliency: {
      PaddingTop: number
      PaddingBottom: number
      PaddingLeft: number
      PaddingRight: number
    }
  }
}

export interface ConfigUpdateSuccess {
  Type: 'update'
  Success: boolean
  Message: string
}

export interface ConfigReloadSuccess {
  Type: 'reload'
  Success: boolean
  Message: string
}

export interface ConfigResetSuccess {
  Type: 'reset'
  Success: boolean
  Message: string
}
