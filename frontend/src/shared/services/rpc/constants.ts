export const DEFAULT_BACKEND_URL =
	(import.meta.env.VITE_BACKEND_URL as string | undefined) ?? 'http://localhost:65500';

export const DEFAULT_SHEET_RPC_CHANNEL =
	(import.meta.env.VITE_SHEET_RPC_CHANNEL as string | undefined) ?? 'sheets';

export const DEFAULT_JOB_RPC_CHANNEL =
	(import.meta.env.VITE_JOB_RPC_CHANNEL as string | undefined) ?? 'jobs';

export const DEFAULT_CONFIG_RPC_CHANNEL =
	(import.meta.env.VITE_CONFIG_RPC_CHANNEL as string | undefined) ?? 'config';

export const BACKEND_URL_KEY = 'slidegen.backend.url';
export const PENDING_BACKEND_URL_KEY = 'slidegen.backend.url.pending';
export const PENDING_BACKEND_URL_SESSION_KEY = 'slidegen.backend.url.pending.defer';

export const RESPONSE_METHOD = 'ReceiveResponse';
export const NOTIFICATION_METHOD = 'ReceiveNotification';