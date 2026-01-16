import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { SettingsNotificationsProps } from '../types';
import { splitNotificationText } from '../utils';

export const SettingsNotifications: React.FC<SettingsNotificationsProps> = ({
	showRestartNotification,
	isRestartNotificationClosing,
	onRestart,
	message,
	showStatusNotification,
	isStatusNotificationClosing,
	onCloseStatus,
	showLockedNotification,
	t,
}) => (
	<>
		{showRestartNotification && (
			<div
				className={`message app-notification message-warning restart-notification${
					isRestartNotificationClosing ? ' restart-notification--closing' : ''
				}`}
			>
				<span className="restart-notification__text">{t('settings.restartRequired')}</span>
				<button className="btn btn-secondary restart-notification__action" onClick={onRestart}>
					{t('settings.restartServer')}
				</button>
			</div>
		)}
		{message && showStatusNotification && (
			<div
				className={`message app-notification message-${message.type} status-notification${
					isStatusNotificationClosing
						? ' status-notification--closing app-notification--closing'
						: ''
				}`}
			>
				{(() => {
					const { title, detail } = splitNotificationText(message.text);
					return (
						<span className="notification-text">
							<span className="notification-title">{title}</span>
							{detail ? <span className="notification-detail">{detail}</span> : null}
						</span>
					);
				})()}
				<button
					type="button"
					className="notification-close"
					onClick={onCloseStatus}
					aria-label={t('common.close')}
				>
					<img
						src={getAssetPath('images', 'close.png')}
						alt=""
						className="notification-close__icon"
					/>
				</button>
			</div>
		)}
		{showLockedNotification && (
			<div className="message app-notification message-warning">{t('settings.locked')}</div>
		)}
	</>
);
