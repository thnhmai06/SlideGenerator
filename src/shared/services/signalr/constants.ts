/**
 * Default backend URL for SignalR connection when no custom URL is configured.
 */
export const DEFAULT_BACKEND_URL = 'http://127.0.0.1:65500';

/**
 * LocalStorage key for storing the active backend URL.
 */
export const BACKEND_URL_KEY = 'slidegen.backend.url';

/**
 * LocalStorage key for storing a pending backend URL to be applied on next session.
 */
export const PENDING_BACKEND_URL_KEY = 'slidegen.backend.url.pending';

/**
 * SessionStorage key to track whether a pending URL has been promoted this session.
 */
export const PENDING_BACKEND_URL_SESSION_KEY = 'slidegen.backend.url.pending.defer';

/**
 * SignalR hub method name for receiving responses from the backend.
 */
export const RESPONSE_METHOD = 'ReceiveResponse';

/**
 * SignalR hub method name for receiving real-time notifications from the backend.
 */
export const NOTIFICATION_METHOD = 'ReceiveNotification';
