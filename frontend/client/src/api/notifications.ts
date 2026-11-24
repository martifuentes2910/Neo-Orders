import type { AppNotification, NotificationsSummary } from '../types';
import { request } from './http';

export const notificationsApi = {
  list: (take = 60) => request<{ items: AppNotification[]; resumen: NotificationsSummary }>(`/notifications?take=${take}`),
};
