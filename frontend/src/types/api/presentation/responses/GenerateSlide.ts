/**
 * Generate Slides API Responses
 * Endpoint: ws://localhost:{port}/GenerateSlide
 */

import { ResponseType, ControlState } from '../enums'

/**
 * Acknowledgment response with job IDs
 */
export interface GenerateSlideCreateResponse {
  Type: ResponseType.Create
  Path: string
  JobIds: Record<string, string> // Map of sheet name to job UUID
}

/**
 * Progress update for all jobs in a group
 */
export interface GenerateSlideGroupStatusResponse {
  Type: ResponseType.Status
  Path: string
  Percent: number
  Current?: number
  Total?: number
  Message?: string
}

/**
 * Progress update for a specific job
 */
export interface GenerateSlideJobStatusResponse {
  Type: ResponseType.Status
  JobId: string
  Percent: number
  Current: number
  Total: number
  Message: string
}

/**
 * Completion response for all jobs in a group
 */
export interface GenerateSlideGroupFinishResponse {
  Type: ResponseType.Finish
  Path: string
  Success: boolean
}

/**
 * Completion response for a specific job
 */
export interface GenerateSlideJobFinishResponse {
  Type: ResponseType.Finish
  JobId: string
  Success: boolean
}

/**
 * Error response for group-level operations
 */
export interface GenerateSlideGroupErrorResponse {
  Type: ResponseType.Error
  Path: string
  Kind: string
  Message: string
}

/**
 * Error response for job-level operations
 */
export interface GenerateSlideJobErrorResponse {
  Type: ResponseType.Error
  JobId: string
  Kind: string
  Message: string
}

/**
 * Control confirmation response for group operations
 */
export interface GenerateSlideGroupControlResponse {
  Type: ResponseType.Control
  Path: string
  State: ControlState
}

/**
 * Control confirmation response for job operations
 */
export interface GenerateSlideJobControlResponse {
  Type: ResponseType.Control
  JobId: string
  State: ControlState
}

/**
 * Union type for all Generate Slides responses
 */
export type GenerateSlideResponse =
  | GenerateSlideCreateResponse
  | GenerateSlideGroupStatusResponse
  | GenerateSlideJobStatusResponse
  | GenerateSlideGroupFinishResponse
  | GenerateSlideJobFinishResponse
  | GenerateSlideGroupErrorResponse
  | GenerateSlideJobErrorResponse
  | GenerateSlideGroupControlResponse
  | GenerateSlideJobControlResponse
