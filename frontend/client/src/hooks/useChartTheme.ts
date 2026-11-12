import { useTheme } from '../context/ThemeContext';

export function useChartTheme() {
  const { resolved } = useTheme();
  const isDark = resolved === 'oscuro';

  return {
    grid: isDark ? '#2a2d3a' : '#e8e5df',
    axis: isDark ? '#7a7f8e' : '#7b8092',
    tooltipBg: isDark ? '#1a1d26' : '#ffffff',
    tooltipBorder: isDark ? '#2a2d3a' : '#e8e5df',
    tooltipText: isDark ? '#e8eaf0' : '#131418',
    legend: isDark ? '#7a7f8e' : '#7b8092',
  };
}
