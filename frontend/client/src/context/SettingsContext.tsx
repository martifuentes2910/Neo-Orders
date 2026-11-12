import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { api } from '../api';
import type { CompanySettings } from '../types';

interface SettingsContextType {
  company: CompanySettings | null;
  loading: boolean;
  refresh: () => Promise<void>;
  update: (data: Partial<CompanySettings>) => Promise<void>;
  formatMoney: (amountInArs: number) => string;
  toDisplay: (amountInArs: number) => number;
  toArs: (displayAmount: number) => number;
  currencySymbol: string;
}

const SettingsContext = createContext<SettingsContextType | undefined>(undefined);

export function SettingsProvider({ children }: { children: ReactNode }) {
  const [company, setCompany] = useState<CompanySettings | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = async () => {
    try {
      const data = await api.config.empresa.get();
      setCompany({
        id: data.id,
        nombreEmpresa: data.nombreEmpresa,
        moneda: data.moneda,
        tipoDeCambio: data.tipoDeCambio,
      });
    } catch {
      setCompany({ nombreEmpresa: 'NeoOrders', moneda: 'ARS', tipoDeCambio: 1 });
    }
  };

  useEffect(() => {
    refresh().finally(() => setLoading(false));
  }, []);

  const update = async (data: Partial<CompanySettings>) => {
    const res = await api.config.empresa.update({ ...company, ...data });
    setCompany({
      id: res.id,
      nombreEmpresa: res.nombreEmpresa,
      moneda: res.moneda,
      tipoDeCambio: res.tipoDeCambio,
    });
  };

  const currency = company?.moneda || 'ARS';
  const rate = company?.tipoDeCambio || 1;
  const currencySymbol = currency === 'USD' ? 'US$' : '$';

  const toDisplay = (amountInArs: number) => (currency === 'USD' ? amountInArs / rate : amountInArs);
  const toArs = (displayAmount: number) => (currency === 'USD' ? displayAmount * rate : displayAmount);

  const formatMoney = (amountInArs: number) => {
    const value = toDisplay(amountInArs);
    try {
      return new Intl.NumberFormat(currency === 'USD' ? 'en-US' : 'es-AR', {
        style: 'currency',
        currency: currency === 'USD' ? 'USD' : 'ARS',
        maximumFractionDigits: 2,
      }).format(value);
    } catch {
      return `${currencySymbol} ${value.toLocaleString()}`;
    }
  };

  return (
    <SettingsContext.Provider value={{ company, loading, refresh, update, formatMoney, toDisplay, toArs, currencySymbol }}>
      {children}
    </SettingsContext.Provider>
  );
}

export function useSettings() {
  const ctx = useContext(SettingsContext);
  if (!ctx) throw new Error('useSettings must be used within SettingsProvider');
  return ctx;
}
