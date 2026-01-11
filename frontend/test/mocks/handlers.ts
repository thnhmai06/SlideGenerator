import { http, HttpResponse } from 'msw';
import { DEFAULT_BACKEND_URL } from '@/shared/services/signalr/constants';

export const handlers = [
	http.get(`${DEFAULT_BACKEND_URL}/health`, () => {
		return HttpResponse.json({ IsRunning: true });
	}),
];
