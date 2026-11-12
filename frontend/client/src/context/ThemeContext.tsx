import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { api } from '../api';

type Mode = 'claro' | 'oscuro' | 'auto';

interface ThemeContextType {
  mode: Mode;
  resolved: 'claro' | 'oscuro';
  setMode: (m: Mode) => void;
  toggle: () => void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

function resolve(mode: Mode): 'claro' | 'oscuro' {
  if (mode === 'auto') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'oscuro' : 'claro';
  }
  return mode;
}

export function ThemeProvider({ children, initialMode = 'claro' }: { children: ReactNode; initialMode?: Mode }) {
  const [mode, setModeState] = useState<Mode>(initialMode);
  const [resolved, setResolved] = useState<'claro' | 'oscuro'>(resolve(initialMode));

  useEffect(() => {
    const r = resolve(mode);
    setResolved(r);
    document.documentElement.setAttribute('data-theme', r);
    document.documentElement.classList.toggle('dark', r === 'oscuro');
    localStorage.setItem('neoorders.theme', mode);
    // persist to backend (best effort)
    api.config.perfil.update({ tema: mode }).catch(() => {});
  }, [mode]);

  useEffect(() => {
    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = () => {
      if (mode === 'auto') {
        const r = mq.matches ? 'oscuro' : 'claro';
        setResolved(r);
        document.documentElement.setAttribute('data-theme', r);
        document.documentElement.classList.toggle('dark', r === 'oscuro');
      }
    };
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, [mode]);

  const setMode = (m: Mode) => setModeState(m);
  const toggle = () => setModeState((prev) => (resolve(prev) === 'oscuro' ? 'claro' : 'oscuro'));

  return (
    <ThemeContext.Provider value={{ mode, resolved, setMode, toggle }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider');
  return ctx;
}
