import { SignalRHubClient } from '../signalrClient'

/**
 * SignalR hub client for sheet/workbook operations.
 * Connects to `/hubs/sheet` endpoint.
 */
export const sheetHub = new SignalRHubClient('/hubs/sheet')

/**
 * SignalR hub client for job management operations.
 * Connects to `/hubs/job` endpoint.
 */
export const jobHub = new SignalRHubClient('/hubs/job')

/**
 * SignalR hub client for configuration operations.
 * Connects to `/hubs/config` endpoint.
 */
export const configHub = new SignalRHubClient('/hubs/config')
