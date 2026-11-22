import { Bell, Briefcase, Package, ShoppingCart, UserCog, Users } from 'lucide-react';
import type { ComponentType } from 'react';
import { useNavigate } from 'react-router-dom';
import type { AppNotification, NotificationAction, NotificationKind } from '../types';
import { formatRelativeDate } from '@/lib/format';
import { cn } from '@/lib/utils';

const ICONS: Record<NotificationKind, ComponentType<{ className?: string }>> = {
  cliente: Users,
  pedido: ShoppingCart,
  producto: Package,
  vendedor: Briefcase,
  usuario: UserCog,
  sistema: Bell,
};

const ACTION_LABEL: Record<NotificationAction, string> = {
  creado: 'Alta',
  actualizado: 'Cambio',
  eliminado: 'Baja',
};

const ACTION_CLASS: Record<NotificationAction, string> = {
  creado: 'bg-emerald-500',
  actualizado: 'bg-primary',
  eliminado: 'bg-destructive',
};

interface NotificationRowProps {
  notification: AppNotification;
  read: boolean;
  onRead: (id: number) => void;
  onNavigate?: () => void;
}

export function NotificationRow({ notification: n, read, onRead, onNavigate }: NotificationRowProps) {
  const Icon = ICONS[n.tipo] ?? Bell;
  const navigate = useNavigate();

  const open = () => {
    onRead(n.id);
    if (n.link) {
      onNavigate?.();
      navigate(n.link);
    }
  };

  return (
    <button
      type="button"
      onClick={open}
      className={cn(
        'flex w-full items-start gap-3 rounded-xl border border-transparent px-3 py-3 text-left transition-colors hover:bg-muted/70',
        !read && 'border-border/70 bg-muted/40',
      )}
    >
      <span className="relative mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
        <Icon className="h-4 w-4" />
        <span className={cn('absolute -right-0.5 -top-0.5 h-2.5 w-2.5 rounded-full ring-2 ring-background', ACTION_CLASS[n.accion])} />
      </span>
      <span className="min-w-0 flex-1">
        <span className="flex items-center gap-2">
          <span className={cn('truncate text-sm', read ? 'font-medium' : 'font-semibold')}>{n.titulo}</span>
          {!read && <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-primary" aria-label="No leída" />}
        </span>
        <span className="mt-0.5 block text-xs text-muted-foreground">{n.mensaje}</span>
        <span className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
          <span>{formatRelativeDate(n.fecha)}</span>
          <span aria-hidden>·</span>
          <span>{ACTION_LABEL[n.accion]}</span>
          {n.usuario && (
            <>
              <span aria-hidden>·</span>
              <span className="truncate">{n.usuario}</span>
            </>
          )}
        </span>
      </span>
    </button>
  );
}

export function NotificationsEmpty({ message = 'No hay notificaciones para mostrar.' }: { message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-12 text-center text-sm text-muted-foreground">
      <span className="flex h-12 w-12 items-center justify-center rounded-full bg-muted">
        <Bell className="h-5 w-5" />
      </span>
      <p>{message}</p>
    </div>
  );
}
