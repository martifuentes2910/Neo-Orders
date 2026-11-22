import {
  LayoutDashboard,
  ShoppingCart,
  Users,
  Package,
  Briefcase,
  BarChart3,
  Bell,
  Settings,
  type LucideIcon,
} from 'lucide-react';

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  adminOnly?: boolean;
}

export const NAV: NavItem[] = [
  { to: '/panel', label: 'Panel', icon: LayoutDashboard },
  { to: '/pedidos', label: 'Pedidos', icon: ShoppingCart },
  { to: '/clientes', label: 'Clientes', icon: Users },
  { to: '/productos', label: 'Productos', icon: Package },
  { to: '/vendedores', label: 'Vendedores', icon: Briefcase },
  { to: '/reportes', label: 'Reportes', icon: BarChart3 },
  { to: '/notificaciones', label: 'Notificaciones', icon: Bell },
  { to: '/configuracion', label: 'Configuración', icon: Settings },
];

export function navFor(roles?: string[] | null): NavItem[] {
  const isAdmin = roles?.includes('Admin') ?? false;
  return isAdmin ? NAV : NAV.filter((n) => !n.adminOnly);
}
