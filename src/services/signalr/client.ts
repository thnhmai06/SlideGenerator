import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr'

import { getBackendBaseUrl } from './baseUrl'
import { NOTIFICATION_METHOD, RESPONSE_METHOD } from './constants'

export class SignalRHubClient {
  private readonly hubPath: string
  private connection: HubConnection
  private baseUrl: string
  private notificationHandlers = new Set<(payload: unknown) => void>()
  private reconnectHandlers = new Set<(connectionId?: string) => void>()
  private connectedHandlers = new Set<(connectionId?: string) => void>()
  private queue: Promise<void> = Promise.resolve()

  constructor(hubPath: string) {
    this.hubPath = hubPath
    this.baseUrl = getBackendBaseUrl()
    this.connection = this.buildConnection(this.baseUrl)
  }

  async sendRequest<TResponse>(
    payload: Record<string, unknown>,
    timeoutMs = 15000,
  ): Promise<TResponse> {
    return this.enqueue(async () => {
      await this.ensureConnected()
      return await this.sendRequestInternal<TResponse>(payload, timeoutMs)
    })
  }

  async invoke(methodName: string, ...args: unknown[]): Promise<void> {
    await this.enqueue(async () => {
      await this.ensureConnected()
      await this.connection.invoke(methodName, ...args)
    })
  }

  onNotification(handler: (payload: unknown) => void): () => void {
    this.notificationHandlers.add(handler)
    this.connection.on(NOTIFICATION_METHOD, handler)
    return () => {
      this.notificationHandlers.delete(handler)
      this.connection.off(NOTIFICATION_METHOD, handler)
    }
  }

  onReconnected(handler: (connectionId?: string) => void): () => void {
    this.reconnectHandlers.add(handler)
    return () => {
      this.reconnectHandlers.delete(handler)
    }
  }

  onConnected(handler: (connectionId?: string) => void): () => void {
    this.connectedHandlers.add(handler)
    return () => {
      this.connectedHandlers.delete(handler)
    }
  }

  private buildConnection(baseUrl: string): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}${this.hubPath}`, {
        withCredentials: false,
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.onreconnected((connectionId) => {
      this.reconnectHandlers.forEach((handler) => handler(connectionId ?? undefined))
    })

    return connection
  }

  private async refreshConnectionIfNeeded(): Promise<void> {
    const currentBaseUrl = getBackendBaseUrl()
    if (currentBaseUrl === this.baseUrl) return

    this.baseUrl = currentBaseUrl
    const previous = this.connection
    if (previous.state !== HubConnectionState.Disconnected) {
      try {
        await previous.stop()
      } catch (error) {
        console.warn('Failed to stop SignalR connection before reconnect:', error)
      }
    }

    this.connection = this.buildConnection(this.baseUrl)
    this.notificationHandlers.forEach((handler) => {
      this.connection.on(NOTIFICATION_METHOD, handler)
    })
  }

  private async ensureConnected(): Promise<void> {
    await this.refreshConnectionIfNeeded()
    const state = this.connection.state
    if (state === HubConnectionState.Connected) return

    if (state === HubConnectionState.Connecting || state === HubConnectionState.Reconnecting) {
      await this.waitForConnected()
      return
    }

    await this.connection.start()
    this.connectedHandlers.forEach((handler) => handler(this.connection.connectionId ?? undefined))
  }

  private waitForConnected(): Promise<void> {
    return new Promise((resolve, reject) => {
      const check = () => {
        if (this.connection.state === HubConnectionState.Connected) {
          resolve()
          return
        }
        if (this.connection.state === HubConnectionState.Disconnected) {
          reject(new Error('SignalR disconnected while connecting.'))
          return
        }
        setTimeout(check, 50)
      }
      check()
    })
  }

  private enqueue<T>(work: () => Promise<T>): Promise<T> {
    const result = this.queue.then(work, work)
    this.queue = result.then(
      () => undefined,
      () => undefined,
    )
    return result
  }

  private sendRequestInternal<TResponse>(
    payload: Record<string, unknown>,
    timeoutMs: number,
  ): Promise<TResponse> {
    return new Promise((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        cleanup()
        reject(new Error('Timeout waiting for backend response.'))
      }, timeoutMs)

      const handleResponse = (response: TResponse) => {
        cleanup()
        resolve(response)
      }

      const cleanup = () => {
        clearTimeout(timeoutId)
        this.connection.off(RESPONSE_METHOD, handleResponse)
      }

      this.connection.on(RESPONSE_METHOD, handleResponse)
      this.connection.invoke('ProcessRequest', payload).catch((error) => {
        cleanup()
        reject(error)
      })
    })
  }
}
