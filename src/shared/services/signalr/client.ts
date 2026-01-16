import {
	HubConnection,
	HubConnectionBuilder,
	HubConnectionState,
	HttpTransportType,
	LogLevel,
} from '@microsoft/signalr';

import { getBackendBaseUrl } from './baseUrl';
import { NOTIFICATION_METHOD, RESPONSE_METHOD } from './constants';

/**
 * A SignalR hub client wrapper that provides automatic connection management,
 * request queuing, and reconnection handling.
 *
 * @remarks
 * This client wraps the Microsoft SignalR HubConnection and adds:
 * - Automatic connection establishment before sending requests
 * - Request queuing to prevent concurrent connection attempts
 * - Automatic reconnection when backend URL changes
 * - Event handlers for notifications and connection state changes
 *
 * @example
 * ```typescript
 * const client = new SignalRHubClient('/hubs/slides');
 *
 * // Subscribe to notifications
 * const unsubscribe = client.onNotification((payload) => {
 *   console.log('Received:', payload);
 * });
 *
 * // Send a request
 * const result = await client.sendRequest<MyResponse>({ action: 'generate' });
 *
 * // Cleanup
 * unsubscribe();
 * await client.dispose();
 * ```
 */
export class SignalRHubClient {
	/** The hub endpoint path (e.g., '/hubs/slides') */
	private readonly hubPath: string;
	/** The underlying SignalR connection */
	private connection: HubConnection;
	/** Current backend base URL */
	private baseUrl: string;
	/** Set of notification handlers to invoke on ReceiveNotification events */
	private notificationHandlers = new Set<(payload: unknown) => void>();
	/** Set of handlers to invoke when connection is re-established after disconnect */
	private reconnectHandlers = new Set<(connectionId?: string) => void>();
	/** Set of handlers to invoke when initial connection is established */
	private connectedHandlers = new Set<(connectionId?: string) => void>();
	/** Queue to serialize connection and request operations */
	private queue: Promise<void> = Promise.resolve();

	/**
	 * Creates a new SignalR hub client.
	 *
	 * @param hubPath - The hub endpoint path (e.g., '/hubs/slides')
	 */
	constructor(hubPath: string) {
		this.hubPath = hubPath;
		this.baseUrl = getBackendBaseUrl();
		this.connection = this.buildConnection(this.baseUrl);
	}

	/**
	 * Sends a request to the backend hub and waits for a typed response.
	 *
	 * @typeParam TResponse - The expected response type
	 * @param payload - The request payload to send
	 * @param timeoutMs - Maximum time to wait for response (default: 15000ms)
	 * @returns Promise resolving to the backend response
	 * @throws Error if timeout expires or connection fails
	 */
	async sendRequest<TResponse>(
		payload: Record<string, unknown>,
		timeoutMs = 15000,
	): Promise<TResponse> {
		return this.enqueue(async () => {
			await this.ensureConnected();
			return await this.sendRequestInternal<TResponse>(payload, timeoutMs);
		});
	}

	/**
	 * Invokes a hub method without waiting for a specific response.
	 *
	 * @param methodName - The hub method name to invoke
	 * @param args - Arguments to pass to the hub method
	 */
	async invoke(methodName: string, ...args: unknown[]): Promise<void> {
		await this.enqueue(async () => {
			await this.ensureConnected();
			await this.connection.invoke(methodName, ...args);
		});
	}

	/**
	 * Registers a handler for backend notification events.
	 *
	 * @param handler - Callback function invoked when a notification is received
	 * @returns Cleanup function to unsubscribe the handler
	 */
	onNotification(handler: (payload: unknown) => void): () => void {
		this.notificationHandlers.add(handler);
		this.connection.on(NOTIFICATION_METHOD, handler);
		return () => {
			this.notificationHandlers.delete(handler);
			this.connection.off(NOTIFICATION_METHOD, handler);
		};
	}

	/**
	 * Registers a handler for reconnection events.
	 *
	 * @param handler - Callback function invoked when connection is re-established
	 * @returns Cleanup function to unsubscribe the handler
	 */
	onReconnected(handler: (connectionId?: string) => void): () => void {
		this.reconnectHandlers.add(handler);
		return () => {
			this.reconnectHandlers.delete(handler);
		};
	}

	/**
	 * Registers a handler for initial connection events.
	 *
	 * @param handler - Callback function invoked when connection is first established
	 * @returns Cleanup function to unsubscribe the handler
	 */
	onConnected(handler: (connectionId?: string) => void): () => void {
		this.connectedHandlers.add(handler);
		return () => {
			this.connectedHandlers.delete(handler);
		};
	}

	/**
	 * Builds a new SignalR HubConnection with WebSocket transport.
	 *
	 * @param baseUrl - The backend base URL
	 * @returns Configured HubConnection instance
	 */
	private buildConnection(baseUrl: string): HubConnection {
		const connection = new HubConnectionBuilder()
			.withUrl(`${baseUrl}${this.hubPath}`, {
				withCredentials: false,
				skipNegotiation: true,
				transport: HttpTransportType.WebSockets,
			})
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Warning)
			.build();

		connection.onreconnected((connectionId) => {
			this.reconnectHandlers.forEach((handler) => handler(connectionId ?? undefined));
		});

		return connection;
	}

	/**
	 * Refreshes the connection if the backend URL has changed.
	 * Stops the old connection and creates a new one with updated URL.
	 */
	private async refreshConnectionIfNeeded(): Promise<void> {
		const currentBaseUrl = getBackendBaseUrl();
		if (currentBaseUrl === this.baseUrl) return;

		this.baseUrl = currentBaseUrl;
		const previous = this.connection;
		if (previous.state !== HubConnectionState.Disconnected) {
			try {
				await previous.stop();
			} catch (error) {
				console.warn('Failed to stop SignalR connection before reconnect:', error);
			}
		}

		this.connection = this.buildConnection(this.baseUrl);
		this.notificationHandlers.forEach((handler) => {
			this.connection.on(NOTIFICATION_METHOD, handler);
		});
	}

	/**
	 * Ensures the connection is in Connected state before operations.
	 * Handles reconnection and waits for pending connection attempts.
	 */
	private async ensureConnected(): Promise<void> {
		await this.refreshConnectionIfNeeded();
		const state = this.connection.state;
		if (state === HubConnectionState.Connected) return;

		if (state === HubConnectionState.Connecting || state === HubConnectionState.Reconnecting) {
			await this.waitForConnected();
			return;
		}

		await this.connection.start();
		this.connectedHandlers.forEach((handler) => handler(this.connection.connectionId ?? undefined));
	}

	/**
	 * Waits for a pending connection to complete with polling.
	 *
	 * @returns Promise that resolves when connected or rejects if disconnected
	 */
	private waitForConnected(): Promise<void> {
		return new Promise((resolve, reject) => {
			const check = () => {
				if (this.connection.state === HubConnectionState.Connected) {
					resolve();
					return;
				}
				if (this.connection.state === HubConnectionState.Disconnected) {
					reject(new Error('SignalR disconnected while connecting.'));
					return;
				}
				setTimeout(check, 50);
			};
			check();
		});
	}

	/**
	 * Enqueues an async operation to prevent concurrent connection operations.
	 *
	 * @typeParam T - The return type of the work function
	 * @param work - The async function to execute
	 * @returns Promise resolving to the work function result
	 */
	private enqueue<T>(work: () => Promise<T>): Promise<T> {
		const result = this.queue.then(work, work);
		this.queue = result.then(
			() => undefined,
			() => undefined,
		);
		return result;
	}

	/**
	 * Internal method that sends a request and waits for a response with timeout.
	 *
	 * @typeParam TResponse - The expected response type
	 * @param payload - The request payload
	 * @param timeoutMs - Maximum time to wait for response
	 * @returns Promise resolving to the backend response
	 */
	private sendRequestInternal<TResponse>(
		payload: Record<string, unknown>,
		timeoutMs: number,
	): Promise<TResponse> {
		return new Promise((resolve, reject) => {
			let isCleanedUp = false;

			const timeoutId = setTimeout(() => {
				if (!isCleanedUp) {
					cleanup();
					reject(new Error('Timeout waiting for backend response.'));
				}
			}, timeoutMs);

			const handleResponse = (response: TResponse) => {
				if (!isCleanedUp) {
					cleanup();
					resolve(response);
				}
			};

			const cleanup = () => {
				if (isCleanedUp) return;
				isCleanedUp = true;
				clearTimeout(timeoutId);
				this.connection.off(RESPONSE_METHOD, handleResponse);
			};

			this.connection.on(RESPONSE_METHOD, handleResponse);
			this.connection.invoke('ProcessRequest', payload).catch((error) => {
				if (!isCleanedUp) {
					cleanup();
					reject(error);
				}
			});
		});
	}

	/**
	 * Dispose of the SignalR client and clean up resources
	 */
	async dispose(): Promise<void> {
		this.notificationHandlers.clear();
		this.reconnectHandlers.clear();
		this.connectedHandlers.clear();

		if (this.connection.state !== HubConnectionState.Disconnected) {
			try {
				await this.connection.stop();
			} catch (error) {
				console.warn('Error stopping SignalR connection during dispose:', error);
			}
		}
	}
}
