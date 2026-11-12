export type OrderStatus = 'Pendiente' | 'En proceso' | 'Enviado' | 'Entregado' | 'Cancelado';

export const ORDER_STATUSES: OrderStatus[] = [
  'Pendiente',
  'En proceso',
  'Enviado',
  'Entregado',
  'Cancelado',
];

export const STATUS_COLORS: Record<OrderStatus, string> = {
  Pendiente: '#f59e0b',
  'En proceso': '#3b82f6',
  Enviado: '#8b5cf6',
  Entregado: '#10b981',
  Cancelado: '#f43f5e',
};

export interface Order {
  id?: number;
  clientId: number;
  clientName?: string;
  clientEmail?: string;
  sellerId?: number | null;
  sellerName?: string;
  productId?: number | null;
  productName: string;
  cantidad: number;
  precioUnitario: number;
  total?: number;
  fecha?: string;
  estado: OrderStatus;
}

export interface Client {
  id?: number;
  nombre: string;
  email: string;
  telefono?: string;
  direccion?: string;
  fechaAlta?: string;
  ordersCount?: number;
  totalSpent?: number;
}

export interface Product {
  id?: number;
  sku: string;
  name: string;
  category: string;
  basePrice: number;
  cost?: number;
  stock: number;
  minStockAlert?: number;
  active?: boolean;
  createdAt?: string;
  isCriticalStock?: boolean;
}

export interface Seller {
  id?: number;
  name: string;
  email: string;
  phone?: string;
  commissionPercentage?: number;
  active?: boolean;
}

export interface User {
  id: string;
  email: string;
  nombre: string;
  apellido: string;
  avatar: string;
  tema: 'claro' | 'oscuro' | 'auto';
  roles: string[];
}

export interface CompanySettings {
  id?: number;
  nombreEmpresa: string;
  moneda: 'ARS' | 'USD';
  tipoDeCambio: number;
}

export const AVATARS = ['avatar-1', 'avatar-2', 'avatar-3', 'avatar-4', 'avatar-5'] as const;
