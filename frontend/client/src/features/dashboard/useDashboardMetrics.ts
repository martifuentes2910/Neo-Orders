import { useEffect, useState } from 'react';
import type { DashboardMetricsResponse } from '../../types/reportes';
import { dashboardApi } from './api';

/** Estadísticas globales del panel: único punto de fetch del slice dashboard. */
export function useDashboardMetrics() {
  const [metrics, setMetrics] = useState<DashboardMetricsResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    dashboardApi
      .metricas()
      .then(setMetrics)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return { metrics, loading };
}
