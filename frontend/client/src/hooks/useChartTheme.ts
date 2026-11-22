import { useTheme } from '../context/ThemeContext';

export function useChartTheme() {
  const { resolved, palette } = useTheme();
  const isDark = resolved === 'oscuro';

  if (palette === 'clean') {
    return {
      grid: isDark ? '#26382f' : '#d7e2d4',
      axis: isDark ? '#98ad9c' : '#5f7262',
      tooltipBg: isDark ? 'rgba(20,32,27,0.92)' : 'rgba(255,255,255,0.92)',
      tooltipBorder: isDark ? 'rgba(167,227,106,0.18)' : 'rgba(95,122,96,0.18)',
      tooltipText: isDark ? '#f2f7f3' : '#101915',
      cursorFill: isDark ? 'rgba(167,227,106,0.12)' : 'rgba(143,209,79,0.1)',
      barPrimary: '#A8D358',
      barPeach: isDark ? '#f5b46b' : '#f3a35c',
      barAmber: isDark ? '#a78bfa' : '#7c5ce6',
      units: isDark ? '#a78bfa' : '#7c5ce6',
      income: isDark ? '#f5b46b' : '#f3a35c',
    };
  }

  if (palette === 'menta') {
    return {
      grid: isDark ? '#1d3b32' : '#dcebd3',
      axis: isDark ? '#8fb3a5' : '#5e7a6e',
      tooltipBg: isDark ? '#102a23' : '#ffffff',
      tooltipBorder: isDark ? '#1d3b32' : '#dcebd3',
      tooltipText: isDark ? '#e5f1d4' : '#0e4d41',
      cursorFill: isDark ? 'rgba(53,192,163,0.12)' : 'rgba(53,192,163,0.08)',
      barPrimary: isDark ? '#35c0a3' : '#2a9c84',
      barPeach: isDark ? '#2a9c84' : '#7fd8c3',
      barAmber: isDark ? '#c9a83f' : '#f2c94c',
      units: isDark ? '#7fd8c3' : '#35c0a3',
      income: isDark ? '#f0a84b' : '#f08a24',
    };
  }

  return {
    grid: isDark ? '#23262f' : '#e8e5df',
    axis: isDark ? '#8b90a0' : '#7b8092',
    tooltipBg: isDark ? '#1a1d26' : '#ffffff',
    tooltipBorder: isDark ? '#2a2d3a' : '#e8e5df',
    tooltipText: isDark ? '#e8eaf0' : '#131418',
    cursorFill: isDark ? 'rgba(255,255,255,0.04)' : 'rgba(255,92,122,0.06)',
    barPrimary: isDark ? '#c74b68' : '#ff5c7a',
    barPeach: isDark ? '#c97a6c' : '#ff8e72',
    barAmber: isDark ? '#c99a4e' : '#f5a65b',
    units: isDark ? '#c99a4e' : '#f5a65b',
    income: isDark ? '#f0a84b' : '#f08a24',
  };
}
