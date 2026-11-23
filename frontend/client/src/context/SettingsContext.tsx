import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { configFeatureApi } from '../features/config/api';
import { useAuth } from './AuthContext';
import type { CompanySettings } from '../types';

interface SettingsContextType {
  company: CompanySettings | null;
  refresh: () => Promise<void>;
  update: (data: Partial<CompanySettings>) => Promise<void>;
  formatMoney: (amountInArs: number) => string;
}

const SettingsContext = createContext<SettingsContextType | undefined>(undefined);

function toCompany(data: Partial<CompanySettings> | null | undefined): CompanySettings {
  return {
    id: data?.id,
    nombreEmpresa: data?.nombreEmpresa ?? 'NeoOrders',
  };
}

// Todos los montos del sistema se manejan en pesos argentinos.
const money = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: 'ARS',
  maximumFractionDigits: 2,
});

export function SettingsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const [company, setCompany] = useState<CompanySettings | null>(null);

  const refresh = async () => {
    try {
      setCompany(toCompany(await configFeatureApi.empresa.get()));
    } catch {
      setCompany({ nombreEmpresa: 'NeoOrders' });
    }
  };

  // Refetch when the session changes: on the login page the request is
  // unauthenticated, so the company would otherwise stay on the fallback.
  useEffect(() => {
    void refresh();
  }, [user?.id]);

  const update = async (data: Partial<CompanySettings>) => {
    const res = await configFeatureApi.empresa.update({ ...company, ...data });
    setCompany(toCompany(res));
  };

  const formatMoney = (amountInArs: number) => {
    try {
      return money.format(amountInArs);
    } catch {
      return `$ ${amountInArs.toLocaleString('es-AR')}`;
    }
  };

  return (
    <SettingsContext.Provider value={{ company, refresh, update, formatMoney }}>
      {children}
    </SettingsContext.Provider>
  );
}

export function useSettings() {
  const ctx = useContext(SettingsContext);
  if (!ctx) throw new Error('useSettings must be used within SettingsProvider');
  return ctx;
}
