export interface ResponseBase {
  Type?: string
  type?: string
  Message?: string
  message?: string
  Kind?: string
  FilePath?: string
  filePath?: string
}

export type ControlAction = 'Pause' | 'Resume' | 'Cancel' | 'Stop' | 'Remove'

export interface SlideTextConfig {
  Pattern: string
  Columns: string[]
}

export interface SlideImageConfig {
  ShapeId: number
  Columns: string[]
  RoiType?: string | null
  CropType?: string | null
}

export interface ShapeDto {
  Id: number
  Name: string
  Data: string
  Kind?: string
  IsImage?: boolean
}
