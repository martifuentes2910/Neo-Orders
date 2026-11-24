import { createContext, useContext, useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { configFeatureApi } from '../features/config/api';
import { useAuth } from './AuthContext';
import type { PaletteId } from '../types';

type Mode = 'claro' | 'oscuro' | 'auto';

interface ThemeContextType {
  mode: Mode;
  resolved: 'claro' | 'oscuro';
  palette: PaletteId;
  setMode: (m: Mode) => void;
  setPalette: (p: PaletteId) => void;
  toggle: () => void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

function systemPrefersDark(): boolean {
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function resolve(mode: Mode, systemDark = systemPrefersDark()): 'claro' | 'oscuro' {
  if (mode === 'auto') return systemDark ? 'oscuro' : 'claro';
  return mode;
}

function isPalette(value: unknown): value is PaletteId {
  return value === 'coral' || value === 'menta' || value === 'clean';
}

function storedPalette(): PaletteId {
  const value = localStorage.getItem('neoorders.palette');
  return isPalette(value) ? value : 'coral';
}

export function ThemeProvider({ children, initialMode }: { children: ReactNode; initialMode?: Mode }) {
  const { user } = useAuth();
  const [mode, setModeState] = useState<Mode>(
    () => initialMode ?? (localStorage.getItem('neoorders.theme') as Mode | null) ?? 'claro',
  );
  const [palette, setPaletteState] = useState<PaletteId>(storedPalette);
  const [systemDark, setSystemDark] = useState(systemPrefersDark);
  // Only an explicit user action should overwrite the stored preference.
  const explicitChoice = useRef(false);
  const adoptedUser = useRef<string | null>(null);

  const resolved = resolve(mode, systemDark);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', resolved);
    document.documentElement.classList.toggle('dark', resolved === 'oscuro');
    if (explicitChoice.current) localStorage.setItem('neoorders.theme', mode);
  }, [mode, resolved]);

  useEffect(() => {
    document.documentElement.setAttribute('data-palette', palette);
    localStorage.setItem('neoorders.palette', palette);
  }, [palette]);

  useEffect(() => {
    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = () => setSystemDark(mq.matches);
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, []);

  // Adopt the theme and palette saved on the server when this browser has no explicit preference.
  useEffect(() => {
    if (!user || adoptedUser.current === user.id) return;
    adoptedUser.current = user.id;
    if (!localStorage.getItem('neoorders.theme') && user.tema) {
      localStorage.setItem('neoorders.theme', user.tema);
      setModeState(user.tema);
    }
    if (isPalette(user.paleta)) {
      setPaletteState(user.paleta);
    }
  }, [user]);

  const setMode = (next: Mode) => {
    explicitChoice.current = true;
    setModeState(next);
    if (user) configFeatureApi.perfil.update({ tema: next }).catch(() => {});
  };

  const setPalette = (next: PaletteId) => {
    setPaletteState(next);
    if (user) configFeatureApi.perfil.update({ paleta: next }).catch(() => {});
  };

  const toggle = () => setMode(resolved === 'oscuro' ? 'claro' : 'oscuro');

  return (
    <ThemeContext.Provider value={{ mode, resolved, palette, setMode, setPalette, toggle }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider');
  return ctx;
}
