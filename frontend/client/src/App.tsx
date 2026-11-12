import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'sonner';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ThemeProvider } from './context/ThemeContext';
import { SettingsProvider } from './context/SettingsContext';
import { lazy, Suspense } from 'react';
import AppLayout from './layouts/AppLayout';
const Login = lazy(() => import('./pages/Login'));
const Panel = lazy(() => import('./pages/Panel'));
const Pedidos = lazy(() => import('./pages/Pedidos'));
const Clientes = lazy(() => import('./pages/Clientes'));
const Productos = lazy(() => import('./pages/Productos'));
const Vendedores = lazy(() => import('./pages/Vendedores'));
const Reportes = lazy(() => import('./pages/Reportes'));
const Configuracion = lazy(() => import('./pages/Configuracion'));
import './index.css';

function Protected({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth();
  if (loading) return <div className="loading-screen">Cargando…</div>;
  if (!user) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function AdminOnly({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  if (!user?.roles?.includes('Admin')) return <Navigate to="/panel" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ThemeProvider initialMode={(localStorage.getItem('neoorders.theme') as any) || 'claro'}>
          <SettingsProvider>
            <Toaster richColors position="bottom-right" />
            <Suspense fallback={<div className="flex h-screen items-center justify-center text-muted-foreground">Cargando…</div>}>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route
                path="/"
                element={
                  <Protected>
                    <AppLayout />
                  </Protected>
                }
              >
                <Route index element={<Navigate to="/panel" replace />} />
                <Route path="panel" element={<Panel />} />
                <Route path="pedidos" element={<Pedidos />} />
                <Route path="clientes" element={<Clientes />} />
                <Route path="productos" element={<Productos />} />
                <Route path="vendedores" element={<Vendedores />} />
                <Route path="reportes" element={<Reportes />} />
                <Route
                  path="configuracion"
                  element={
                    <Configuracion />
                  }
                />
                <Route path="configuracion/usuarios" element={<AdminOnly><Configuracion tab="usuarios" /></AdminOnly>} />
              </Route>
              <Route path="*" element={<Navigate to="/panel" replace />} />
            </Routes>
            </Suspense>
          </SettingsProvider>
        </ThemeProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
