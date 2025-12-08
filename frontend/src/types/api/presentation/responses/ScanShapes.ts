/**
 * Scan Shapes API Responses
 * Endpoint: ws://localhost:{port}/ScanShapes
 */

import { ResponseType } from '../enums'

/**
 * Shape data returned from scan operation
 */
export interface ShapeData {
  Id: number
  Name: string
  Data: string // Base64-encoded image preview
}

/**
 * Acknowledgment response for scan shapes create request
 */
export interface ScanShapesCreateResponse {
  Type: ResponseType.Create
  Path: string
}

/**
 * Success response with scanned shapes
 */
export interface ScanShapesFinishResponse {
  Type: ResponseType.Finish
  Path: string
  Success: boolean
  Shapes?: ShapeData[]
}

/**
 * Error response for scan shapes operation
 */
export interface ScanShapesErrorResponse {
  Type: ResponseType.Error
  Path: string
  Kind: string
  Message: string
}

/**
 * Union type for all Scan Shapes responses
 */
export type ScanShapesResponse =
  | ScanShapesCreateResponse
  | ScanShapesFinishResponse
  | ScanShapesErrorResponse
