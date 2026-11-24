import type { Order, CsvImportResult } from '../types';
import { BASE, request } from './http';

export const ordersApi = {
  list: (
    search?: string,
    estado?: string,
    clientId?: number,
    sellerId?: number,
    productId?: number,
    desde?: string,
    hasta?: string,
  ) => {
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    if (estado && estado !== 'Todos') params.set('estado', estado);
    if (clientId) params.set('clientId', String(clientId));
    if (sellerId) params.set('sellerId', String(sellerId));
    if (productId) params.set('productId', String(productId));
    if (desde) params.set('desde', desde);
    if (hasta) params.set('hasta', hasta);
    const qs = params.toString();
    return request<Order[]>(`/orders${qs ? '?' + qs : ''}`);
  },
  create: (
    data: Pick<Order, 'clientId' | 'cantidad'> & {
      sellerId?: number | null;
      productId?: number | null;
      productName?: string;
      precioUnitario?: number;
      orderNumber?: string | null;
    },
  ) => request<{ id: number }>('/orders', { method: 'POST', body: JSON.stringify(data) }),
  update: (
    id: number,
    data: Pick<Order, 'id' | 'clientId' | 'cantidad' | 'estado'> & {
      sellerId?: number | null;
      productId?: number | null;
      productName?: string;
      precioUnitario?: number;
    },
  ) => request<void>(`/orders/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  // KAN-16: único camino permitido para cambiar el estado de pedidos Enviado/Entregado/Cancelado.
  updateStatus: (id: number, estado: string) =>
    request<void>(`/orders/${id}/estado`, { method: 'PATCH', body: JSON.stringify({ estado }) }),
  remove: (id: number) => request<void>(`/orders/${id}`, { method: 'DELETE' }),
  templateUrl: () => `${BASE}/orders/import-csv/template`,
  importCsv: (file: File) => {
    const body = new FormData();
    body.append('file', file);
    return request<CsvImportResult>('/orders/import-csv', { method: 'POST', body });
  },
};
