/**
 * Scan Shapes API Requests
 * Endpoint: ws://localhost:{port}/ScanShapes
 */

import { RequestType } from '../enums'

/**
 * Request to scan shapes from a PowerPoint template file
 */
export interface ScanShapesCreateRequest {
  Type: RequestType.Create
  FilePath: string
}
