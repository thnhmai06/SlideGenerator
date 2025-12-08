/**
 * Type Guards for API Responses
 * Utilities to safely check response types at runtime
 */

import { ResponseType } from './enums'
import { ScanShapesResponse, ScanShapesFinishResponse } from './responses/ScanShapes'
import {
  GenerateSlideResponse,
  GenerateSlideCreateResponse,
  GenerateSlideGroupStatusResponse,
  GenerateSlideJobStatusResponse,
  GenerateSlideGroupFinishResponse,
  GenerateSlideJobFinishResponse,
  GenerateSlideGroupErrorResponse,
  GenerateSlideJobErrorResponse,
  GenerateSlideGroupControlResponse,
  GenerateSlideJobControlResponse
} from './responses/GenerateSlide'

/**
 * Union type for all API responses
 */
export type AnyAPIResponse = ScanShapesResponse | GenerateSlideResponse

// ============================================================================
// Base Type Guards
// ============================================================================

export function isErrorResponse(response: AnyAPIResponse): boolean {
  return response.Type === ResponseType.Error
}

export function isFinishResponse(response: AnyAPIResponse): boolean {
  return response.Type === ResponseType.Finish
}

export function isStatusResponse(response: AnyAPIResponse): boolean {
  return response.Type === ResponseType.Status
}

export function isControlResponse(response: AnyAPIResponse): boolean {
  return response.Type === ResponseType.Control
}

export function isCreateResponse(response: AnyAPIResponse): boolean {
  return response.Type === ResponseType.Create
}

// ============================================================================
// Path/Job Based Guards
// ============================================================================

export function isPathBased(obj: any): obj is { Path: string } {
  return 'Path' in obj && typeof obj.Path === 'string'
}

export function isJobBased(obj: any): obj is { JobId: string } {
  return 'JobId' in obj && typeof obj.JobId === 'string'
}

// ============================================================================
// Scan Shapes Specific Guards
// ============================================================================

export function isScanShapesFinishResponse(
  response: AnyAPIResponse
): response is ScanShapesFinishResponse {
  return (
    isFinishResponse(response) &&
    isPathBased(response) &&
    'Shapes' in response
  )
}

// ============================================================================
// Generate Slides Specific Guards
// ============================================================================

export function isGenerateSlideCreateResponse(
  response: AnyAPIResponse
): response is GenerateSlideCreateResponse {
  return (
    isCreateResponse(response) &&
    isPathBased(response) &&
    'JobIds' in response
  )
}

export function isGenerateSlideGroupStatusResponse(
  response: AnyAPIResponse
): response is GenerateSlideGroupStatusResponse {
  return isStatusResponse(response) && isPathBased(response)
}

export function isGenerateSlideJobStatusResponse(
  response: AnyAPIResponse
): response is GenerateSlideJobStatusResponse {
  return isStatusResponse(response) && isJobBased(response)
}

export function isGenerateSlideGroupFinishResponse(
  response: AnyAPIResponse
): response is GenerateSlideGroupFinishResponse {
  return isFinishResponse(response) && isPathBased(response)
}

export function isGenerateSlideJobFinishResponse(
  response: AnyAPIResponse
): response is GenerateSlideJobFinishResponse {
  return isFinishResponse(response) && isJobBased(response)
}

export function isGenerateSlideGroupErrorResponse(
  response: AnyAPIResponse
): response is GenerateSlideGroupErrorResponse {
  return isErrorResponse(response) && isPathBased(response)
}

export function isGenerateSlideJobErrorResponse(
  response: AnyAPIResponse
): response is GenerateSlideJobErrorResponse {
  return isErrorResponse(response) && isJobBased(response)
}

export function isGenerateSlideGroupControlResponse(
  response: AnyAPIResponse
): response is GenerateSlideGroupControlResponse {
  return isControlResponse(response) && isPathBased(response)
}

export function isGenerateSlideJobControlResponse(
  response: AnyAPIResponse
): response is GenerateSlideJobControlResponse {
  return isControlResponse(response) && isJobBased(response)
}
