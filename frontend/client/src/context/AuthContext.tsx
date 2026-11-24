import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { authFeatureApi } from '../features/auth/api';
import type { User } from '../types';

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refresh: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

function toUser(data: User): User {
  return {
    id: data.id,
    email: data.email,
    nombre: data.nombre,
    apellido: data.apellido,
    avatar: data.avatar,
    tema: data.tema,
    paleta: data.paleta,
    roles: data.roles,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = async () => {
    try {
      setUser(toUser(await authFeatureApi.me()));
    } catch {
      setUser(null);
    }
  };

  useEffect(() => {
    refresh().finally(() => setLoading(false));
  }, []);

  const login = async (email: string, password: string) => {
    setUser(toUser(await authFeatureApi.login(email, password)));
  };

  const logout = async () => {
    try { await authFeatureApi.logout(); } catch { /* ignore */ }
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, refresh }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
