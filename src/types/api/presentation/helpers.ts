/**
 * API Helper Functions
 * Utilities for creating API requests
 */

import { RequestType, ControlState } from './enums'
import {
  ScanShapesCreateRequest
} from './requests/ScanShapes'
import {
  GenerateSlideCreateRequest,
  GenerateSlideTextConfig,
  GenerateSlideImageConfig,
  GenerateSlideGroupControlRequest,
  GenerateSlideJobControlRequest,
  GenerateSlideGroupStatusRequest,
  GenerateSlideJobStatusRequest
} from './requests/GenerateSlide'

// ============================================================================
// Scan Shapes Helpers
// ============================================================================

/**
 * Create a scan shapes request
 */
export function createScanShapesRequest(filePath: string): ScanShapesCreateRequest {
  return {
    Type: RequestType.Create,
    FilePath: filePath
  }
}

// ============================================================================
// Generate Slides Helpers
// ============================================================================

/**
 * Create a generate slides request
 */
export function createGenerateSlideRequest(
  templatePath: string,
  spreadsheetPath: string,
  outputPath: string,
  textConfigs: GenerateSlideTextConfig[],
  imageConfigs: GenerateSlideImageConfig[],
  customSheet?: string[]
): GenerateSlideCreateRequest {
  return {
    Type: RequestType.Create,
    TemplatePath: templatePath,
    SpreadsheetPath: spreadsheetPath,
    Path: outputPath,
    TextConfigs: textConfigs,
    ImageConfigs: imageConfigs,
    CustomSheet: customSheet || null
  }
}

/**
 * Create a group control request
 */
export function createGroupControlRequest(
  path: string,
  state?: ControlState
): GenerateSlideGroupControlRequest {
  return {
    Type: RequestType.Control,
    Path: path,
    State: state
  }
}

/**
 * Create a job control request
 */
export function createJobControlRequest(
  jobId: string,
  state?: ControlState
): GenerateSlideJobControlRequest {
  return {
    Type: RequestType.Control,
    JobId: jobId,
    State: state
  }
}

/**
 * Create a group status request
 */
export function createGroupStatusRequest(path: string): GenerateSlideGroupStatusRequest {
  return {
    Type: RequestType.Status,
    Path: path
  }
}

/**
 * Create a job status request
 */
export function createJobStatusRequest(jobId: string): GenerateSlideJobStatusRequest {
  return {
    Type: RequestType.Status,
    JobId: jobId
  }
}
