import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { useSettings } from '../context/SettingsContext';
import { motion } from 'framer-motion';
import {
  LayoutDashboard,
  ShoppingCart,
  Users,
  Package,
  Briefcase,
  BarChart3,
  Settings,
  Sun,
  Moon,
  LogOut,
  Menu,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import AvatarMemphis from '@/components/AvatarMemphis';
import { Separator } from '@/components/ui/separator';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import Logo from '@/components/Logo';

const NAV = [
  { to: '/panel', label: 'Panel', icon: LayoutDashboard },
  { to: '/pedidos', label: 'Pedidos', icon: ShoppingCart },
  { to: '/clientes', label: 'Clientes', icon: Users },
  { to: '/productos', label: 'Productos', icon: Package },
  { to: '/vendedores', label: 'Vendedores', icon: Briefcase },
  { to: '/reportes', label: 'Reportes', icon: BarChart3 },
  { to: '/configuracion', label: 'Configuración', icon: Settings },
];

function SidebarContent({ collapsed, onNavigate }: { collapsed: boolean; onNavigate?: () => void }) {
  const { user, logout } = useAuth();
  const { resolved, toggle } = useTheme();
  const { company } = useSettings();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <TooltipProvider delayDuration={0}>
      <div className={`flex h-full flex-col bg-sidebar text-sidebar-foreground transition-all duration-300 ${collapsed ? 'items-center px-2 py-4' : 'gap-4 p-4'}`}>
        {/* Logo */}
        <div className={`flex items-center ${collapsed ? 'justify-center' : 'gap-3 px-2 py-1'}`}>
          <Logo size={collapsed ? 32 : 36} />
          {!collapsed && (
            <motion.span
              initial={{ opacity: 0, width: 0 }}
              animate={{ opacity: 1, width: 'auto' }}
              exit={{ opacity: 0, width: 0 }}
              className="text-sm font-bold tracking-tight text-sidebar-foreground"
            >
              {company?.nombreEmpresa || 'NeoOrders'}
            </motion.span>
          )}
        </div>

        {/* Nav */}
        <nav className="flex flex-1 flex-col gap-1">
          {NAV.map((n) => {
            const Icon = n.icon;
            const link = (
              <NavLink
                key={n.to}
                to={n.to}
                onClick={onNavigate}
                className={({ isActive }) =>
                  `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-all duration-200 ${
                    collapsed ? 'justify-center px-2' : ''
                  } ${
                    isActive
                      ? 'bg-primary/20 text-primary shadow-sm'
                      : 'text-sidebar-muted hover:bg-white/5 hover:text-sidebar-foreground'
                  }`
                }
              >
                <Icon className="h-5 w-5 shrink-0" />
                {!collapsed && <span className="truncate">{n.label}</span>}
              </NavLink>
            );
            return collapsed ? (
              <Tooltip key={n.to}>
                <TooltipTrigger asChild>{link}</TooltipTrigger>
                <TooltipContent side="right" className="bg-sidebar text-sidebar-foreground border-sidebar-border">
                  {n.label}
                </TooltipContent>
              </Tooltip>
            ) : (
              link
            );
          })}
        </nav>

        <Separator className="bg-sidebar-border" />

        <div className={`flex flex-col gap-2 ${collapsed ? 'items-center' : ''}`}>
          {/* Theme toggle */}
          {collapsed ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <button
                  onClick={toggle}
                  className="flex h-10 w-10 items-center justify-center rounded-lg text-sidebar-muted transition-colors hover:bg-white/5 hover:text-sidebar-foreground"
                >
                  {resolved === 'oscuro' ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
                </button>
              </TooltipTrigger>
              <TooltipContent side="right" className="bg-sidebar text-sidebar-foreground border-sidebar-border">
                {resolved === 'oscuro' ? 'Modo claro' : 'Modo oscuro'}
              </TooltipContent>
            </Tooltip>
          ) : (
            <button
              onClick={toggle}
              className="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-sidebar-muted transition-colors hover:bg-white/5 hover:text-sidebar-foreground"
            >
              {resolved === 'oscuro' ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
              <span>{resolved === 'oscuro' ? 'Modo claro' : 'Modo oscuro'}</span>
            </button>
          )}

          {/* User chip */}
          {collapsed ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <div className="flex h-10 w-10 items-center justify-center">
                  <AvatarMemphis preset={parseInt((user?.avatar || 'avatar-1').replace('avatar-', '')) - 1} size={36} />
                </div>
              </TooltipTrigger>
              <TooltipContent side="right" className="bg-sidebar text-sidebar-foreground border-sidebar-border">
                {user?.nombre} {user?.apellido}
              </TooltipContent>
            </Tooltip>
          ) : (
            <div className="flex items-center gap-3 px-2 py-1">
              <AvatarMemphis preset={parseInt((user?.avatar || 'avatar-1').replace('avatar-', '')) - 1} size={36} />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-semibold text-sidebar-foreground">
                  {user?.nombre} {user?.apellido}
                </p>
                <p className="truncate text-xs text-sidebar-muted">{user?.roles?.join(', ')}</p>
              </div>
            </div>
          )}

          {/* Logout */}
          {collapsed ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={handleLogout}
                  className="h-10 w-10 text-sidebar-muted hover:bg-white/5 hover:text-destructive"
                >
                  <LogOut className="h-5 w-5" />
                </Button>
              </TooltipTrigger>
              <TooltipContent side="right" className="bg-sidebar text-sidebar-foreground border-sidebar-border">
                Salir
              </TooltipContent>
            </Tooltip>
          ) : (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleLogout}
              className="justify-start gap-3 text-sidebar-muted hover:bg-white/5 hover:text-destructive"
            >
              <LogOut className="h-4 w-4" />
              <span>Salir</span>
            </Button>
          )}
        </div>
      </div>
    </TooltipProvider>
  );
}

export default function AppLayout() {
  const [collapsed, setCollapsed] = useState(() => {
    return localStorage.getItem('neoorders.sidebar') === 'collapsed';
  });

  useEffect(() => {
    localStorage.setItem('neoorders.sidebar', collapsed ? 'collapsed' : 'expanded');
  }, [collapsed]);

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {/* Desktop sidebar */}
      <aside
        className={`hidden shrink-0 border-r border-sidebar-border bg-sidebar transition-all duration-300 md:block ${
          collapsed ? 'w-20' : 'w-64'
        }`}
      >
        <div className="relative h-full">
          <SidebarContent collapsed={collapsed} />
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="absolute -right-3 top-6 z-10 flex h-6 w-6 items-center justify-center rounded-full border border-sidebar-border bg-sidebar text-sidebar-muted shadow-md transition-colors hover:text-sidebar-foreground"
          >
            {collapsed ? <ChevronRight className="h-3.5 w-3.5" /> : <ChevronLeft className="h-3.5 w-3.5" />}
          </button>
        </div>
      </aside>

      {/* Mobile sidebar */}
      <div className="fixed left-4 top-4 z-40 md:hidden">
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="outline" size="icon" className="bg-background/80 backdrop-blur">
              <Menu className="h-4 w-4" />
            </Button>
          </SheetTrigger>
          <SheetContent side="left" className="w-64 p-0">
            <SidebarContent collapsed={false} onNavigate={() => {}} />
          </SheetContent>
        </Sheet>
      </div>

      <main className="flex-1 overflow-y-auto">
        <motion.div
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3, ease: 'easeOut' }}
          className="min-h-full p-4 pt-16 md:p-6 md:pt-6"
        >
          <Outlet />
        </motion.div>
      </main>
    </div>
  );
}
