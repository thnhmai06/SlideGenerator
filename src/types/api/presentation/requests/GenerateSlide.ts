/**
 * Generate Slides API Requests
 * Endpoint: ws://localhost:{port}/GenerateSlide
 */

import { RequestType, ControlState } from '../enums'

/**
 * Configuration for text replacement in slides
 */
export interface GenerateSlideTextConfig {
  Pattern: string
  Columns: string[]
}

/**
 * Configuration for image replacement in slides
 */
export interface GenerateSlideImageConfig {
  ShapeId: number
  Columns: string[]
}

/**
 * Request to create a new slide generation job
 */
export interface GenerateSlideCreateRequest {
  Type: RequestType.Create
  TemplatePath: string
  SpreadsheetPath: string
  TextConfigs: GenerateSlideTextConfig[]
  ImageConfigs: GenerateSlideImageConfig[]
  Path: string
  CustomSheet?: string[] | null
}

/**
 * Request to control all jobs in a generation group
 */
export interface GenerateSlideGroupControlRequest {
  Type: RequestType.Control
  Path: string
  State?: ControlState
}

/**
 * Request to control a specific job
 */
export interface GenerateSlideJobControlRequest {
  Type: RequestType.Control
  JobId: string
  State?: ControlState
}

/**
 * Request to query status of all jobs in a group
 */
export interface GenerateSlideGroupStatusRequest {
  Type: RequestType.Status
  Path: string
}

/**
 * Request to query status of a specific job
 */
export interface GenerateSlideJobStatusRequest {
  Type: RequestType.Status
  JobId: string
}
