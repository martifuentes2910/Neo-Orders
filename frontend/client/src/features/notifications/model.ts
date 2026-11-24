import type { AppNotification, NotificationAction, NotificationKind } from '../../types';

export const FILTERS = [
  { id: 'todas', label: 'Todas' },
  { id: 'no-leidas', label: 'No leídas' },
  { id: 'altas', label: 'Altas' },
  { id: 'cambios', label: 'Cambios' },
  { id: 'bajas', label: 'Bajas' },
] as const;

export type FilterId = (typeof FILTERS)[number]['id'];

export const ACTION_BY_FILTER: Record<string, NotificationAction> = {
  altas: 'creado',
  cambios: 'actualizado',
  bajas: 'eliminado',
};

export const MODULES: { value: NotificationKind | 'todos'; label: string }[] = [
  { value: 'todos', label: 'Todos los módulos' },
  { value: 'cliente', label: 'Clientes' },
  { value: 'pedido', label: 'Pedidos' },
  { value: 'producto', label: 'Productos' },
  { value: 'vendedor', label: 'Vendedores' },
  { value: 'usuario', label: 'Usuarios' },
];

/** Filtra el feed por módulo y por tipo de evento/lectura. */
export function filterNotifications(
  items: AppNotification[],
  filter: FilterId,
  modulo: NotificationKind | 'todos',
  isRead: (id: number) => boolean,
): AppNotification[] {
  return items.filter((n) => {
    if (modulo !== 'todos' && n.tipo !== modulo) return false;
    if (filter === 'no-leidas') return !isRead(n.id);
    const action = ACTION_BY_FILTER[filter];
    if (action) return n.accion === action;
    return true;
  });
}
