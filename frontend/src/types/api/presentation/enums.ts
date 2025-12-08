/**
 * API Enums
 */

/**
 * Control actions for job execution
 */
export enum ControlState {
  Pause = 'Pause',
  Resume = 'Resume',
  Stop = 'Stop'
}

/**
 * Types of requests that can be sent
 */
export enum RequestType {
  Create = 'Create',
  Control = 'Control',
  Status = 'Status'
}

/**
 * Types of responses from the server
 */
export enum ResponseType {
  Create = 'Create',
  Control = 'Control',
  Status = 'Status',
  Finish = 'Finish',
  Error = 'Error'
}
