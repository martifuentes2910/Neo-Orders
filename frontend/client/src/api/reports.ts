import type {
  ClientesComprasResponse,
  DashboardMetricsResponse,
  FiltrosReporteParams,
  ProductosVendidosResponse,
  ReporteResumenResponse,
  VentasVendedorResponse,
} from '../types/reportes';
import { request } from './http';

function buildReporteQuery(filtros?: FiltrosReporteParams): string {
  const params = new URLSearchParams();
  if (filtros?.desde) params.set('desde', filtros.desde);
  if (filtros?.hasta) params.set('hasta', filtros.hasta);
  if (filtros?.clientId) params.set('clientId', String(filtros.clientId));
  if (filtros?.sellerId) params.set('sellerId', String(filtros.sellerId));
  if (filtros?.estado && filtros.estado !== 'Todos') params.set('estado', filtros.estado);
  if (filtros?.page && filtros.page !== 1) params.set('page', String(filtros.page));
  if (filtros?.pageSize && filtros.pageSize !== 25) params.set('pageSize', String(filtros.pageSize));
  const qs = params.toString();
  return qs ? `?${qs}` : '';
}

export const dashboardApi = {
  metricas: () => request<DashboardMetricsResponse>('/dashboard/metrics'),
};

export const reportsApi = {
  resumen: (filtros?: FiltrosReporteParams) =>
    request<ReporteResumenResponse>(`/reports${buildReporteQuery(filtros)}`),
  detalle: {
    vendedores: (filtros?: FiltrosReporteParams) =>
      request<VentasVendedorResponse>(`/reports/ventas-vendedor${buildReporteQuery(filtros)}`),
    productos: (filtros?: FiltrosReporteParams) =>
      request<ProductosVendidosResponse>(`/reports/productos-vendidos${buildReporteQuery(filtros)}`),
    clientes: (filtros?: FiltrosReporteParams) =>
      request<ClientesComprasResponse>(`/reports/clientes-compras${buildReporteQuery(filtros)}`),
  },
};
