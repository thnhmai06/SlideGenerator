import {
	HubConnection,
	HubConnectionBuilder,
	HubConnectionState,
	HttpTransportType,
	LogLevel,
} from "@microsoft/signalr";

const DEFAULT_BACKEND_URL = "http://127.0.0.1:5000";

const RESPONSE_METHOD = "ReceiveResponse";
const NOTIFICATION_METHOD = "ReceiveNotification";

function normalizeBaseUrl(url: string): string {
	const trimmed = url.trim();
	if (!trimmed) return "";

	const withScheme = /^https?:\/\//i.test(trimmed)
		? trimmed
		: `http://${trimmed}`;

	const normalizedHost = withScheme.replace(
		/^(https?:\/\/)localhost(?=[:/]|$)/i,
		(_, scheme: string) => `${scheme}127.0.0.1`
	);

	return normalizedHost.endsWith("/")
		? normalizedHost.slice(0, -1)
		: normalizedHost;
}

export function getBackendBaseUrl(): string {
	const stored = localStorage.getItem("slidegen.backend.url") ?? "";
	const normalized = normalizeBaseUrl(stored);
	return normalized || normalizeBaseUrl(DEFAULT_BACKEND_URL);
}

export class SignalRHubClient {
	private readonly connection: HubConnection;
	private queue: Promise<void> = Promise.resolve();

	constructor(hubPath: string) {
		const baseUrl = getBackendBaseUrl();
		this.connection = new HubConnectionBuilder()
			.withUrl(`${baseUrl}${hubPath}`, {
				withCredentials: false,
				skipNegotiation: true,
				transport: HttpTransportType.WebSockets,
			})
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Warning)
			.build();
	}

	async sendRequest<TResponse>(
		payload: Record<string, unknown>,
		timeoutMs = 15000
	): Promise<TResponse> {
		return this.enqueue(async () => {
			await this.ensureConnected();
			return await this.sendRequestInternal<TResponse>(payload, timeoutMs);
		});
	}

	async invoke(methodName: string, ...args: unknown[]): Promise<void> {
		await this.enqueue(async () => {
			await this.ensureConnected();
			await this.connection.invoke(methodName, ...args);
		});
	}

	onNotification(handler: (payload: unknown) => void): () => void {
		this.connection.on(NOTIFICATION_METHOD, handler);
		return () => this.connection.off(NOTIFICATION_METHOD, handler);
	}

	private async ensureConnected(): Promise<void> {
		const state = this.connection.state;
		if (state === HubConnectionState.Connected) return;

		if (
			state === HubConnectionState.Connecting ||
			state === HubConnectionState.Reconnecting
		) {
			await this.waitForConnected();
			return;
		}

		await this.connection.start();
	}

	private waitForConnected(): Promise<void> {
		return new Promise((resolve, reject) => {
			const check = () => {
				if (this.connection.state === HubConnectionState.Connected) {
					resolve();
					return;
				}
				if (this.connection.state === HubConnectionState.Disconnected) {
					reject(new Error("SignalR disconnected while connecting."));
					return;
				}
				setTimeout(check, 50);
			};
			check();
		});
	}

	private enqueue<T>(work: () => Promise<T>): Promise<T> {
		const result = this.queue.then(work, work);
		this.queue = result.then(
			() => undefined,
			() => undefined
		);
		return result;
	}

	private sendRequestInternal<TResponse>(
		payload: Record<string, unknown>,
		timeoutMs: number
	): Promise<TResponse> {
		return new Promise((resolve, reject) => {
			const timeoutId = setTimeout(() => {
				cleanup();
				reject(new Error("Timeout waiting for backend response."));
			}, timeoutMs);

			const handleResponse = (response: TResponse) => {
				cleanup();
				resolve(response);
			};

			const cleanup = () => {
				clearTimeout(timeoutId);
				this.connection.off(RESPONSE_METHOD, handleResponse);
			};

			this.connection.on(RESPONSE_METHOD, handleResponse);
			this.connection.invoke("ProcessRequest", payload).catch((error) => {
				cleanup();
				reject(error);
			});
		});
	}
}
