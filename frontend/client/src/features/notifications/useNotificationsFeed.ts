import { useMemo, useState } from 'react';
import { useNotifications } from '../../context/NotificationsContext';
import type { NotificationKind } from '../../types';
import { filterNotifications, type FilterId } from './model';

/**
 * Estado de la vista de notificaciones: reutiliza el contexto global (fuente de
 * verdad de items/leídas) y añade los filtros locales de la página.
 */
export function useNotificationsFeed() {
  const { items, summary, loading, isRead, markRead, markAllRead, unreadCount } = useNotifications();
  const [filter, setFilter] = useState<FilterId>('todas');
  const [modulo, setModulo] = useState<NotificationKind | 'todos'>('todos');

  const filtered = useMemo(
    () => filterNotifications(items, filter, modulo, isRead),
    [items, filter, modulo, isRead],
  );

  return {
    items, summary, loading, unreadCount, isRead, markRead, markAllRead,
    filter, setFilter, modulo, setModulo, filtered,
  };
}
