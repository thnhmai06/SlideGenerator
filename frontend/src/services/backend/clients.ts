import { SignalRHubClient } from '../signalrClient'

export const sheetHub = new SignalRHubClient('/hubs/sheet')
export const jobHub = new SignalRHubClient('/hubs/job')
export const configHub = new SignalRHubClient('/hubs/config')
