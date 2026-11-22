import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { api } from '../api';
import { useAuth } from './AuthContext';
import type { AppNotification, NotificationsSummary } from '../types';

interface NotificationsContextType {
  items: AppNotification[];
  summary: NotificationsSummary;
  loading: boolean;
  unreadCount: number;
  isRead: (id: number) => boolean;
  markRead: (id: number) => void;
  markAllRead: () => void;
  refresh: () => Promise<void>;
}

const EMPTY_SUMMARY: NotificationsSummary = { total: 0, hoy: 0, stockCritico: 0, pendientes: 0 };

const NotificationsContext = createContext<NotificationsContextType | undefined>(undefined);

function storageKey(userId: string) {
  return `neoorders.notifications.read.${userId}`;
}

function loadReadIds(userId: string): number[] {
  try {
    const raw = localStorage.getItem(storageKey(userId));
    const parsed = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((n) => typeof n === 'number') : [];
  } catch {
    return [];
  }
}

export function NotificationsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const [items, setItems] = useState<AppNotification[]>([]);
  const [summary, setSummary] = useState<NotificationsSummary>(EMPTY_SUMMARY);
  const [loading, setLoading] = useState(false);
  const [readIds, setReadIds] = useState<number[]>([]);

  const refresh = useCallback(async () => {
    if (!user) return;
    try {
      const data = await api.notifications.list();
      setItems(Array.isArray(data?.items) ? data.items : []);
      setSummary({ ...EMPTY_SUMMARY, ...(data?.resumen || {}) });
    } catch {
      // Silencioso: las notificaciones no deben interrumpir la navegación.
    }
  }, [user]);

  // Cada usuario tiene su propio registro de leídas en este navegador.
  useEffect(() => {
    if (!user) {
      setItems([]);
      setSummary(EMPTY_SUMMARY);
      setReadIds([]);
      return;
    }
    setReadIds(loadReadIds(user.id));
  }, [user]);

  useEffect(() => {
    if (!user) return;
    setLoading(true);
    refresh().finally(() => setLoading(false));

    const interval = window.setInterval(refresh, 60_000);
    const onFocus = () => refresh();
    window.addEventListener('focus', onFocus);
    return () => {
      window.clearInterval(interval);
      window.removeEventListener('focus', onFocus);
    };
  }, [user, refresh]);

  const persist = useCallback(
    (next: number[]) => {
      setReadIds(next);
      if (!user) return;
      try {
        localStorage.setItem(storageKey(user.id), JSON.stringify(next.slice(-500)));
      } catch {
        // almacenamiento no disponible
      }
    },
    [user],
  );

  const isRead = useCallback((id: number) => readIds.includes(id), [readIds]);

  const markRead = useCallback(
    (id: number) => {
      if (readIds.includes(id)) return;
      persist([...readIds, id]);
    },
    [persist, readIds],
  );

  const markAllRead = useCallback(() => {
    const ids = items.map((n) => n.id);
    const merged = Array.from(new Set([...readIds, ...ids]));
    persist(merged);
  }, [items, persist, readIds]);

  const unreadCount = useMemo(
    () => items.reduce((count, n) => (readIds.includes(n.id) ? count : count + 1), 0),
    [items, readIds],
  );

  const value = useMemo(
    () => ({ items, summary, loading, unreadCount, isRead, markRead, markAllRead, refresh }),
    [items, summary, loading, unreadCount, isRead, markRead, markAllRead, refresh],
  );

  return <NotificationsContext.Provider value={value}>{children}</NotificationsContext.Provider>;
}

export function useNotifications() {
  const ctx = useContext(NotificationsContext);
  if (!ctx) throw new Error('useNotifications must be used within NotificationsProvider');
  return ctx;
}
