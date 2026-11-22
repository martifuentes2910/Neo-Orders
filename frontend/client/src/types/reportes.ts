/** Tipos del modulo de reportes (KAN-17). Corresponden a los DTOs del backend. */

export type VistaReporte = 'resumen' | 'vendedores' | 'productos' | 'clientes';

export interface FiltrosReporteParams {
  desde?: string;
  hasta?: string;
  clientId?: number;
  sellerId?: number;
  estado?: string;
  page?: number;
  pageSize?: number;
}

/* ---------- Vista resumen ---------- */

export interface EstadoResumen {
  estado: string;
  cantidad: number;
  total: number;
}

export interface MesResumen {
  year: number;
  month: number;
  cantidad: number;
  ingresos: number;
}

export interface ProductoResumen {
  producto: string;
  cantidad: number;
  ingresos: number;
}

export interface ClienteResumen {
  clienteId: number;
  cliente: string;
  pedidos: number;
  total: number;
}

export interface VendedorResumen {
  vendedorId: number;
  vendedor: string;
  comision: number;
  pedidos: number;
  total: number;
  comisionEstimada: number;
}

export interface ReporteResumenResponse {
  porEstado: EstadoResumen[];
  porMes: MesResumen[];
  topProductos: ProductoResumen[];
  topClientes: ClienteResumen[];
  ventasPorVendedor: VendedorResumen[];
}

/* ---------- Panel ---------- */

export interface PedidoReciente {
  id: number;
  clientName: string;
  sellerName: string;
  productName: string;
  total: number;
  fecha: string;
  estado: string;
}

/** Serie mensual del panel (KAN-24). Excluye pedidos cancelados. */
export interface SerieMensual {
  year: number;
  month: number;
  pedidos: number;
  ingresos: number;
  unidades: number;
  ticketPromedio: number;
}

/** Fila del Top productos del panel (KAN-24). */
export interface TopProducto {
  productId: number | null;
  productName: string;
  imageUrl: string | null;
  unidades: number;
  ingresos: number;
}

export interface DashboardMetricsResponse {
  totalPedidos: number;
  ingresos: number;
  pendientes: number;
  entregados: number;
  clientes: number;
  vendedores: number;
  porEstado: EstadoResumen[];
  porMes: MesResumen[];
  recientes: PedidoReciente[];
  /** KAN-24: campos aditivos, sin cancelados. */
  serieMensual: SerieMensual[];
  topProductos: TopProducto[];
}

/* ---------- Reportes específicos ---------- */

interface BaseDetalleResponse {
  totalFilas: number;
  pagina: number;
  tamanoPagina: number;
  totalPaginas: number;
}

export interface FilaVendedor {
  vendedorId: number;
  vendedor: string;
  pedidos: number;
  unidades: number;
  ingresos: number;
  participacion: number;
  comision: number;
  comisionEstimada: number;
}

export interface FilaProducto {
  producto: string;
  pedidos: number;
  unidades: number;
  ingresos: number;
  participacion: number;
}

export interface FilaCliente {
  clienteId: number;
  cliente: string;
  pedidos: number;
  unidades: number;
  ingresos: number;
  participacion: number;
}

export interface VentasVendedorResponse extends BaseDetalleResponse {
  tipo: 'vendedor';
  filasGrafico: FilaVendedor[];
  filas: FilaVendedor[];
}

export interface ProductosVendidosResponse extends BaseDetalleResponse {
  tipo: 'producto';
  filasGrafico: FilaProducto[];
  filas: FilaProducto[];
}

export interface ClientesComprasResponse extends BaseDetalleResponse {
  tipo: 'cliente';
  filasGrafico: FilaCliente[];
  filas: FilaCliente[];
}

/** Unión discriminada por `tipo`: el compilador exige manejar cada rama. */
export type ReporteDetalle =
  | VentasVendedorResponse
  | ProductosVendidosResponse
  | ClientesComprasResponse;
