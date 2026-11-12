import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { useSettings } from '../context/SettingsContext';
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
} from 'lucide-react';

const NAV = [
  { to: '/panel', label: 'Panel', icon: LayoutDashboard },
  { to: '/pedidos', label: 'Pedidos', icon: ShoppingCart },
  { to: '/clientes', label: 'Clientes', icon: Users },
  { to: '/productos', label: 'Productos', icon: Package },
  { to: '/vendedores', label: 'Vendedores', icon: Briefcase },
  { to: '/reportes', label: 'Reportes', icon: BarChart3 },
  { to: '/configuracion', label: 'Configuración', icon: Settings },
];

export default function AppLayout() {
  const { user, logout } = useAuth();
  const { resolved, toggle } = useTheme();
  const { company } = useSettings();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <Logo />
          {company?.nombreEmpresa && <span className="brand-name">{company.nombreEmpresa}</span>}
        </div>
        <nav className="sidebar-nav">
          {NAV.map((n) => {
            const Icon = n.icon;
            return (
              <NavLink
                key={n.to}
                to={n.to}
                className={({ isActive }) => 'nav-link' + (isActive ? ' active' : '')}
              >
                <Icon size={18} />
                <span>{n.label}</span>
              </NavLink>
            );
          })}
        </nav>
        <div className="sidebar-footer">
          <button className="theme-toggle" onClick={toggle} title="Cambiar tema">
            {resolved === 'oscuro' ? <Sun size={18} /> : <Moon size={18} />}
            <span>{resolved === 'oscuro' ? 'Modo claro' : 'Modo oscuro'}</span>
          </button>
          <div className="user-chip">
            <AvatarBadge avatar={user?.avatar} nombre={user?.nombre} />
            <div className="user-info">
              <strong>{user?.nombre} {user?.apellido}</strong>
              <span>{user?.roles?.join(', ')}</span>
            </div>
          </div>
          <button className="btn-logout" onClick={handleLogout}>
            <LogOut size={16} /> Salir
          </button>
        </div>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}

function Logo() {
  return (
    <svg className="logo" viewBox="0 0 40 40" width="34" height="34" aria-label="NeoOrders">
      <defs>
        <linearGradient id="lg" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#ff5c7a" />
          <stop offset="50%" stopColor="#ff8e72" />
          <stop offset="100%" stopColor="#f5a65b" />
        </linearGradient>
      </defs>
      <rect x="2" y="2" width="36" height="36" rx="10" fill="url(#lg)" />
      <path d="M12 28 V12 L20 22 L28 12 V28" stroke="#fff" strokeWidth="2.6" fill="none" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function AvatarBadge({ avatar, nombre }: { avatar?: string; nombre?: string }) {
  const idx = avatar ? parseInt(avatar.replace('avatar-', '')) || 1 : 1;
  const colors = ['#ff5c7a', '#ff8e72', '#f5a65b', '#fda4af', '#3b82f6'];
  const c = colors[(idx - 1) % colors.length];
  const initial = (nombre || '?').charAt(0).toUpperCase();
  return (
    <div className="avatar" style={{ background: c }}>
      {initial}
    </div>
  );
}
