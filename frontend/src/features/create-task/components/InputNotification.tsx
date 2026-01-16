import React from 'react';
import { getAssetPath } from '@/shared/utils/paths';
import type { InputNotificationProps } from '../types';
import { splitNotificationText } from '../utils';

export const InputNotification: React.FC<InputNotificationProps> = ({
	notification,
	isClosing,
	onClose,
	t,
}) => {
	if (!notification) return null;
	return (
		<div
			className={`app-notification message ${
				notification.type === 'error' ? 'message-error' : 'message-success'
			}${isClosing ? ' app-notification--closing' : ''}`}
		>
			{(() => {
				const { title, detail } = splitNotificationText(notification.text);
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
				onClick={onClose}
				aria-label={t('common.close')}
			>
				<img
					src={getAssetPath('images', 'close.png')}
					alt=""
					className="notification-close__icon"
				/>
			</button>
		</div>
	);
};
