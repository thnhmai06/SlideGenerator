/**
 * Re-exports for SignalR client functionality.
 *
 * @module signalrClient
 * @remarks
 * This module provides the core SignalR client and URL utilities for
 * connecting to the backend.
 */
export { getBackendBaseUrl, normalizeBaseUrl } from './signalr/baseUrl';
export { SignalRHubClient } from './signalr/client';
