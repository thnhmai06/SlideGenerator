import { useCallback, useMemo, useRef, useState } from 'react';
import type { NotificationState } from '../types';
import { getErrorDetail } from '../utils';

/**
 * Options for the useNotification hook.
 */
export interface UseNotificationOptions {
	/** Translation function for notification messages */
	t: (key: string) => string;
}

/**
 * Hook for managing toast notifications with auto-hide behavior.
 *
 * @remarks
 * Provides notification display management including:
 * - Success and error notifications
 * - Automatic dismissal with configurable timing
 * - Animated close transitions
 *
 * @param options - Hook configuration
 * @returns Notification state and handlers
 *
 * @example
 * ```tsx
 * const { notification, showNotification, hideNotification } = useNotification({ t });
 * showNotification('success', 'Task created successfully');
 * ```
 */
export const useNotification = ({ t }: UseNotificationOptions) => {
	const [notification, setNotification] = useState<NotificationState | null>(null);
	const [isNotificationClosing, setIsNotificationClosing] = useState(false);
	const notificationHideTimeoutRef = useRef<number | null>(null);
	const notificationCloseTimeoutRef = useRef<number | null>(null);

	const formatErrorMessage = useCallback(
		(key: string, error: unknown): string => {
			const detail = getErrorDetail(error);
			return detail ? `${t(key)}: ${detail}` : t(key);
		},
		[t],
	);

	const clearNotificationTimeouts = useCallback(() => {
		if (notificationHideTimeoutRef.current) {
			window.clearTimeout(notificationHideTimeoutRef.current);
			notificationHideTimeoutRef.current = null;
		}
		if (notificationCloseTimeoutRef.current) {
			window.clearTimeout(notificationCloseTimeoutRef.current);
			notificationCloseTimeoutRef.current = null;
		}
	}, []);

	const hideNotification = useCallback(() => {
		clearNotificationTimeouts();
		setIsNotificationClosing(true);
		notificationCloseTimeoutRef.current = window.setTimeout(() => {
			setNotification(null);
			setIsNotificationClosing(false);
			notificationCloseTimeoutRef.current = null;
		}, 180);
	}, [clearNotificationTimeouts]);

	const showNotification = useCallback(
		(type: 'success' | 'error', text: string) => {
			clearNotificationTimeouts();
			setNotification({ type, text });
			setIsNotificationClosing(false);
			notificationHideTimeoutRef.current = window.setTimeout(() => {
				hideNotification();
				notificationHideTimeoutRef.current = null;
			}, 4000);
		},
		[clearNotificationTimeouts, hideNotification],
	);

	return useMemo(
		() => ({
			notification,
			isNotificationClosing,
			showNotification,
			hideNotification,
			formatErrorMessage,
		}),
		[notification, isNotificationClosing, showNotification, hideNotification, formatErrorMessage],
	);
};
