export interface ResponseBase {
  type?: string
  message?: string
  kind?: string
  filePath?: string
}

export type ControlAction = 'Pause' | 'Resume' | 'Cancel' | 'Stop' | 'Remove'

export interface SlideTextConfig {
  pattern: string
  columns: string[]
}

export interface SlideImageConfig {
  shapeId: number
  columns: string[]
  roiType?: string | null
  cropType?: string | null
}

export interface ShapeDto {
  id: number
  name: string
  data: string
  kind?: string
  isImage?: boolean
}
