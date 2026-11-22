import { useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Bell, CheckCheck, Package, ShoppingCart } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useNotifications } from '../context/NotificationsContext';
import { NotificationRow, NotificationsEmpty } from '@/components/NotificationsList';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { cn } from '@/lib/utils';
import type { NotificationKind } from '../types';

const FILTERS = [
  { id: 'todas', label: 'Todas' },
  { id: 'no-leidas', label: 'No leídas' },
  { id: 'altas', label: 'Altas' },
  { id: 'cambios', label: 'Cambios' },
  { id: 'bajas', label: 'Bajas' },
] as const;

type FilterId = (typeof FILTERS)[number]['id'];

const ACTION_BY_FILTER: Record<string, string> = {
  altas: 'creado',
  cambios: 'actualizado',
  bajas: 'eliminado',
};

const MODULES: { value: NotificationKind | 'todos'; label: string }[] = [
  { value: 'todos', label: 'Todos los módulos' },
  { value: 'cliente', label: 'Clientes' },
  { value: 'pedido', label: 'Pedidos' },
  { value: 'producto', label: 'Productos' },
  { value: 'vendedor', label: 'Vendedores' },
  { value: 'usuario', label: 'Usuarios' },
];

export default function Notificaciones() {
  const { items, summary, loading, isRead, markRead, markAllRead, unreadCount } = useNotifications();
  const [filter, setFilter] = useState<FilterId>('todas');
  const [modulo, setModulo] = useState<NotificationKind | 'todos'>('todos');

  const filtered = useMemo(() => {
    return items.filter((n) => {
      if (modulo !== 'todos' && n.tipo !== modulo) return false;
      if (filter === 'no-leidas') return !isRead(n.id);
      const action = ACTION_BY_FILTER[filter];
      if (action) return n.accion === action;
      return true;
    });
  }, [items, filter, modulo, isRead]);

  return (
    <div className="w-full space-y-6">
      <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-[32px] font-bold tracking-tight">Notificaciones</h1>
          <p className="text-sm text-muted-foreground">
            Registro de altas, cambios y bajas realizadas en el sistema.
          </p>
        </div>
        <Button onClick={markAllRead} disabled={unreadCount === 0} variant="outline" className="gap-2">
          <CheckCheck className="h-4 w-4" /> Marcar todas como leídas
        </Button>
      </motion.div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryCard icon={Bell} label="Eventos" value={summary.total} hint={`${summary.hoy} hoy`} />
        <SummaryCard
          icon={Bell}
          label="Sin leer"
          value={unreadCount}
          hint="en esta sesión"
        />
        <Link to="/productos" className="block">
          <SummaryCard icon={Package} label="Stock crítico" value={summary.stockCritico} hint="productos" />
        </Link>
        <Link to="/pedidos" className="block">
          <SummaryCard icon={ShoppingCart} label="Pedidos pendientes" value={summary.pendientes} hint="por procesar" />
        </Link>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-wrap gap-2">
          {FILTERS.map((f) => (
            <button
              key={f.id}
              type="button"
              onClick={() => setFilter(f.id)}
              className={cn(
                'rounded-full border px-3 py-1.5 text-xs font-medium transition-colors',
                filter === f.id
                  ? 'border-primary bg-primary text-primary-foreground'
                  : 'border-border text-muted-foreground hover:bg-muted',
              )}
            >
              {f.label}
            </button>
          ))}
        </div>
        <Select value={modulo} onValueChange={(v) => setModulo(v as NotificationKind | 'todos')}>
          <SelectTrigger className="w-full sm:w-52">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {MODULES.map((m) => (
              <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardContent className="space-y-1 p-2 sm:p-3">
          {loading && items.length === 0 ? (
            <p className="py-12 text-center text-sm text-muted-foreground">Cargando notificaciones…</p>
          ) : filtered.length === 0 ? (
            <NotificationsEmpty message="No hay notificaciones que coincidan con el filtro." />
          ) : (
            filtered.map((n) => (
              <NotificationRow key={n.id} notification={n} read={isRead(n.id)} onRead={markRead} />
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function SummaryCard({
  icon: Icon,
  label,
  value,
  hint,
}: {
  icon: typeof Bell;
  label: string;
  value: number;
  hint: string;
}) {
  return (
    <Card>
      <CardContent className="flex items-center gap-3 p-4">
        <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <Icon className="h-5 w-5" />
        </span>
        <div>
          <p className="text-xs text-muted-foreground">{label}</p>
          <p className="text-xl font-bold tabular-nums leading-tight">{value}</p>
          <p className="text-xs text-muted-foreground">{hint}</p>
        </div>
      </CardContent>
    </Card>
  );
}
