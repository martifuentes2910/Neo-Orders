/**
 * Modelo del feature Reportes: helpers de fechas/filtros y normalización de filas.
 * Los DTOs siguen viviendo en `types/reportes.ts` y se reexportan aquí por comodidad.
 */
import type {
  FilaCliente,
  FilaProducto,
  FilaVendedor,
  ReporteDetalle,
  VistaReporte,
} from '../../types/reportes';

export type {
  FiltrosReporteParams,
  ReporteDetalle,
  ReporteResumenResponse,
  VistaReporte,
  FilaVendedor,
  FilaProducto,
  FilaCliente,
} from '../../types/reportes';

export function today() {
  return new Date().toISOString().slice(0, 10);
}

export function daysAgo(n: number) {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d.toISOString().slice(0, 10);
}

export function yearStart() {
  return `${new Date().getFullYear()}-01-01`;
}

/** Mapea el rango de fechas al preset de la UI ("all" = sin límites). */
export function periodoDe(desde: string, hasta: string): string {
  if (!desde && !hasta) return 'all';
  if (desde === daysAgo(30) && hasta === today()) return '30';
  if (desde === daysAgo(90) && hasta === today()) return '90';
  if (desde === yearStart() && hasta === today()) return 'year';
  return 'custom';
}

export const VISTAS: { value: VistaReporte; label: string }[] = [
  { value: 'resumen', label: 'Resumen' },
  { value: 'vendedores', label: 'Ventas por vendedor' },
  { value: 'productos', label: 'Productos más vendidos' },
  { value: 'clientes', label: 'Clientes con más compras' },
];

/** Filtros editados en la UI; se envían juntos al pulsar "Filtrar". */
export interface BorradorFiltros {
  desde: string;
  hasta: string;
  clientId: string;
  sellerId: string;
  estado: string;
}

export interface FilaNormalizada {
  id: string;
  nombre: string;
  pedidos: number;
  unidades: number;
  ingresos: number;
  participacion: number;
  comision?: number;
  comisionEstimada?: number;
}

export function nombreDeFila(f: FilaVendedor | FilaProducto | FilaCliente, tipo: ReporteDetalle['tipo']): string {
  if (tipo === 'vendedor') return (f as FilaVendedor).vendedor;
  if (tipo === 'producto') return (f as FilaProducto).producto;
  return (f as FilaCliente).cliente;
}

export function normalizar(fila: FilaVendedor | FilaProducto | FilaCliente, tipo: ReporteDetalle['tipo']): FilaNormalizada {
  if (tipo === 'vendedor') {
    const f = fila as FilaVendedor;
    return {
      id: String(f.vendedorId), nombre: f.vendedor, pedidos: f.pedidos, unidades: f.unidades,
      ingresos: f.ingresos, participacion: f.participacion,
      comision: f.comision, comisionEstimada: f.comisionEstimada,
    };
  }
  if (tipo === 'producto') {
    const f = fila as FilaProducto;
    return {
      id: f.producto, nombre: f.producto, pedidos: f.pedidos, unidades: f.unidades,
      ingresos: f.ingresos, participacion: f.participacion,
    };
  }
  const f = fila as FilaCliente;
  return {
    id: String(f.clienteId), nombre: f.cliente, pedidos: f.pedidos, unidades: f.unidades,
    ingresos: f.ingresos, participacion: f.participacion,
  };
}
