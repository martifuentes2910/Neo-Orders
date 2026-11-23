export type OrderStatus = 'Pendiente' | 'Procesado' | 'Enviado' | 'Entregado' | 'Cancelado';

export const ORDER_STATUSES: OrderStatus[] = [
  'Pendiente',
  'Procesado',
  'Enviado',
  'Entregado',
  'Cancelado',
];

// KAN-16/KAN-35: espejo de OrderStatuses en backend/OrdersApi/Models/Order.cs.
// Pendiente -> Procesado/Cancelado; Procesado -> Enviado/Cancelado;
// Enviado -> Entregado; Entregado y Cancelado son terminales.
export const ORDER_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  Pendiente: ['Procesado', 'Cancelado'],
  Procesado: ['Enviado', 'Cancelado'],
  Enviado: ['Entregado'],
  Entregado: [],
  Cancelado: [],
};

// KAN-16/KAN-35: estados con datos fijos; sólo se permite cambiar el estado.
export const ORDER_EDIT_LOCKED: OrderStatus[] = ['Procesado', 'Enviado', 'Entregado', 'Cancelado'];

export const esEstadoBloqueado = (estado: OrderStatus): boolean => ORDER_EDIT_LOCKED.includes(estado);

/** Incluye el estado actual para que el selector siempre tenga un valor válido. */
export const estadosDestino = (estado: OrderStatus): OrderStatus[] => [
  estado,
  ...ORDER_TRANSITIONS[estado],
];

export type PaletteId = 'coral' | 'menta' | 'clean';

export const PALETTES: { id: PaletteId; nombre: string; descripcion: string; swatches: string[] }[] = [
  {
    id: 'coral',
    nombre: 'Coral',
    descripcion: 'La paleta cálida original de NeoOrders.',
    swatches: ['#ff5c7a', '#ff8e72', '#f5a65b', '#fda4af'],
  },
  {
    id: 'menta',
    nombre: 'Menta',
    descripcion: 'Verde profundo, menta y un toque de amarillo.',
    swatches: ['#35C0A3', '#0E4D41', '#E5F1D4', '#F2C94C'],
  },
  {
    id: 'clean',
    nombre: 'Clean',
    descripcion: 'Base sage neutra con acentos verde, naranja y violeta.',
    swatches: ['#A8D358', '#F3A35C', '#7C5CE6', '#EEF4EC'],
  },
];

const STATUS_COLORS: Record<PaletteId, Record<OrderStatus, string>> = {
  coral: {
    Pendiente: '#F3B33D',
    Procesado: '#C93F57',
    Enviado: '#FFB199',
    Entregado: '#D6E89A',
    Cancelado: '#A62B2F',
  },
  menta: {
    Pendiente: '#F2C94C',
    Procesado: '#35C0A3',
    Enviado: '#7FD8C3',
    Entregado: '#CFE8A0',
    Cancelado: '#B4462F',
  },
  clean: {
    Pendiente: '#FFB74D',
    Procesado: '#60A5FA',
    Enviado: '#A78BFA',
    Entregado: '#BEF264',
    Cancelado: '#F87171',
  },
};

const STATUS_COLORS_DARK: Record<PaletteId, Record<OrderStatus, string>> = {
  coral: {
    Pendiente: '#A26512',
    Procesado: '#A32F4C',
    Enviado: '#A25540',
    Entregado: '#91A65C',
    Cancelado: '#8F2626',
  },
  menta: {
    Pendiente: '#BE9233',
    Procesado: '#3FB89C',
    Enviado: '#1F7A67',
    Entregado: '#b9c77e',
    Cancelado: '#8f3b2a',
  },
  clean: {
    Pendiente: '#F59E0B',
    Procesado: '#3B82F6',
    Enviado: '#8B5CF6',
    Entregado: '#65D91A',
    Cancelado: '#EF4444',
  },
};

export function getStatusColor(status: OrderStatus, isDark: boolean, palette: PaletteId = 'coral'): string {
  return isDark ? STATUS_COLORS_DARK[palette][status] : STATUS_COLORS[palette][status];
}

const DARK_TEXT = '#3f2d0a';
const LIGHT_TEXT = '#ffffff';

function relativeLuminance(hex: string): number {
  const channel = (offset: number) => {
    const value = parseInt(hex.slice(offset, offset + 2), 16) / 255;
    return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * channel(1) + 0.7152 * channel(3) + 0.0722 * channel(5);
}

function contrastRatio(a: string, b: string): number {
  const la = relativeLuminance(a);
  const lb = relativeLuminance(b);
  return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
}

/** Elige texto oscuro o blanco según el contraste real del fondo, también en modo oscuro. */
export function getStatusTextColor(status: OrderStatus, isDark: boolean, palette: PaletteId = 'coral'): string {
  const background = getStatusColor(status, isDark, palette);
  return contrastRatio(background, DARK_TEXT) >= contrastRatio(background, LIGHT_TEXT) ? DARK_TEXT : LIGHT_TEXT;
}

export interface User {
  id: string;
  email: string;
  nombre: string;
  apellido: string;
  avatar: string;
  tema: 'claro' | 'oscuro' | 'auto';
  paleta?: PaletteId;
  roles: string[];
}

export interface CompanySettings {
  id?: number;
  nombreEmpresa: string;
}

export type NotificationKind = 'cliente' | 'pedido' | 'producto' | 'vendedor' | 'usuario' | 'sistema';
export type NotificationAction = 'creado' | 'actualizado' | 'eliminado';

export interface AppNotification {
  id: number;
  tipo: NotificationKind;
  accion: NotificationAction;
  titulo: string;
  mensaje: string;
  link?: string | null;
  usuario?: string | null;
  fecha: string;
  entidadId?: number | null;
}

export interface NotificationsSummary {
  total: number;
  hoy: number;
  stockCritico: number;
  pendientes: number;
}

export interface Client {
  id: number;
  nombre: string;
  email: string;
  telefono?: string | null;
  direccion?: string | null;
  fechaAlta?: string;
  ordersCount?: number;
  totalSpent?: number;
}

export interface Seller {
  id: number;
  name: string;
  email: string;
  phone: string;
  commissionPercentage: number;
  active: boolean;
}

export interface Product {
  id: number;
  sku: string;
  name: string;
  category: string;
  basePrice: number;
  cost: number;
  stock: number;
  minStockAlert: number;
  active: boolean;
  createdAt?: string;
  isCriticalStock?: boolean;
  imageUrl?: string | null;
}

export interface Order {
  id: number;
  clientId: number;
  clientName?: string;
  clientEmail?: string;
  sellerId?: number | null;
  sellerName?: string;
  productId?: number | null;
  productName: string;
  cantidad: number;
  precioUnitario: number;
  total: number;
  fecha: string;
  estado: OrderStatus;
  orderNumber?: string | null;
}

export interface CsvImportError {
  row: number;
  field: string;
  value: string;
  message: string;
}

export interface CsvImportResult {
  totalRows: number;
  validRows: number;
  imported: number;
  errors: CsvImportError[];
}

export interface PriceAdjustmentItem {
  id: number;
  sku: string;
  name: string;
  currentBasePrice: number;
  newBasePrice: number;
}

export interface PriceAdjustmentPreviewResponse {
  percent: number;
  factor: number;
  affectedCount: number;
  skippedInactiveCount: number;
  items: PriceAdjustmentItem[];
  errors: string[];
}

export interface PriceAdjustmentApplyResponse {
  affectedCount: number;
  percent: number;
  factor: number;
  appliedAtUtc: string;
}

export interface Client {
  id: number;
  nombre: string;
  email: string;
  telefono?: string | null;
  direccion?: string | null;
  fechaAlta?: string;
  ordersCount?: number;
  totalSpent?: number;
}

export interface Seller {
  id: number;
  name: string;
  email: string;
  phone: string;
  commissionPercentage: number;
  active: boolean;
}

export interface Product {
  id: number;
  sku: string;
  name: string;
  category: string;
  basePrice: number;
  cost: number;
  stock: number;
  minStockAlert: number;
  active: boolean;
  createdAt?: string;
  isCriticalStock?: boolean;
  imageUrl?: string | null;
}

export interface Order {
  id: number;
  clientId: number;
  clientName?: string;
  clientEmail?: string;
  sellerId?: number | null;
  sellerName?: string;
  productId?: number | null;
  productName: string;
  cantidad: number;
  precioUnitario: number;
  total: number;
  fecha: string;
  estado: OrderStatus;
  orderNumber?: string | null;
}

export interface CsvImportError {
  row: number;
  field: string;
  value: string;
  message: string;
}

export interface CsvImportResult {
  totalRows: number;
  validRows: number;
  imported: number;
  errors: CsvImportError[];
}

export interface PriceAdjustmentItem {
  id: number;
  sku: string;
  name: string;
  currentBasePrice: number;
  newBasePrice: number;
}

export interface PriceAdjustmentPreviewResponse {
  percent: number;
  factor: number;
  affectedCount: number;
  skippedInactiveCount: number;
  items: PriceAdjustmentItem[];
  errors: string[];
}

export interface PriceAdjustmentApplyResponse {
  affectedCount: number;
  percent: number;
  factor: number;
  appliedAtUtc: string;
}
