import type {
  AppNotification,
  Client,
  CompanySettings,
  CsvImportResult,
  NotificationsSummary,
  Order,
  PriceAdjustmentApplyResponse,
  PriceAdjustmentPreviewResponse,
  Product,
  Seller,
  User,
} from './types';
import type { FiltrosReporteParams } from './types/reportes';
import { BASE, request } from './api/http';
import { dashboardApi, reportsApi } from './api/reports';

export type {
  CsvImportError,
  CsvImportResult,
  PriceAdjustmentItem,
  PriceAdjustmentPreviewResponse,
  PriceAdjustmentApplyResponse,
} from './types';

export const api = {
  login: (email: string, password: string) =>
    request<User>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  logout: () => request<void>('/auth/logout', { method: 'POST' }),
  me: () => request<User>('/auth/me'),

  clients: {
    list: (search?: string) => request<Client[]>(`/clients${search ? '?search=' + encodeURIComponent(search) : ''}`),
    create: (data: Partial<Client>) => request<Client>('/clients', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: Partial<Client>) => request<void>(`/clients/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: number) => request<void>(`/clients/${id}`, { method: 'DELETE' }),
    templateUrl: () => `${BASE}/clients/import-csv/template`,
    importCsv: (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return request<CsvImportResult>('/clients/import-csv', { method: 'POST', body });
    },
  },

  orders: {
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
    // KAN-16: ?nico camino permitido para cambiar el estado de pedidos Enviado/Entregado/Cancelado.
    updateStatus: (id: number, estado: string) =>
      request<void>(`/orders/${id}/estado`, { method: 'PATCH', body: JSON.stringify({ estado }) }),
    remove: (id: number) => request<void>(`/orders/${id}`, { method: 'DELETE' }),
    templateUrl: () => `${BASE}/orders/import-csv/template`,
    importCsv: (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return request<CsvImportResult>('/orders/import-csv', { method: 'POST', body });
    },
  },

  products: {
    list: (search?: string, category?: string, onlyInStock?: boolean, sku?: string, nombre?: string) => {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      if (category && category !== 'Todos') params.set('category', category);
      if (onlyInStock) params.set('onlyInStock', 'true');
      if (sku) params.set('sku', sku);
      if (nombre) params.set('nombre', nombre);
      const qs = params.toString();
      return request<Product[]>(`/products${qs ? '?' + qs : ''}`);
    },
    categories: () => request<string[]>('/products/categories'),
    create: (data: Partial<Product>) => request<Product>('/products', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: Partial<Product>) =>
      request<void>(`/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    // KAN-23: la imagen se sube en un endpoint dedicado para no cambiar el contrato JSON.
    uploadImage: (id: number, file: File) => {
      const body = new FormData();
      body.append('file', file);
      return request<{ imageUrl: string }>(`/products/${id}/image`, { method: 'POST', body });
    },
    removeImage: (id: number) => request<void>(`/products/${id}/image`, { method: 'DELETE' }),
    previewPriceAdjustment: (percent: number) =>
      request<PriceAdjustmentPreviewResponse>('/products/price-adjustment/preview', {
        method: 'POST',
        body: JSON.stringify({ percent }),
      }),
    applyPriceAdjustment: (percent: number) =>
      request<PriceAdjustmentApplyResponse>('/products/price-adjustment/apply', {
        method: 'POST',
        body: JSON.stringify({ percent }),
      }),
    remove: (id: number) => request<void>(`/products/${id}`, { method: 'DELETE' }),
    templateUrl: () => `${BASE}/products/import-csv/template`,
    importCsv: (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return request<CsvImportResult>('/products/import-csv', { method: 'POST', body });
    },
  },

  sellers: {
    list: (onlyActive?: boolean) => request<Seller[]>(`/sellers${onlyActive ? '?onlyActive=true' : ''}`),
    create: (data: Partial<Seller>) => request<Seller>('/sellers', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: Partial<Seller>) => request<void>(`/sellers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: number) => request<void>(`/sellers/${id}`, { method: 'DELETE' }),
  },

  dashboard: dashboardApi,

  notifications: {
    list: (take = 60) => request<{ items: AppNotification[]; resumen: NotificationsSummary }>(`/notifications?take=${take}`),
  },

  reports: (filtros?: FiltrosReporteParams) => reportsApi.resumen(filtros),

  reportesDetalle: {
    vendedores: (filtros?: FiltrosReporteParams) => reportsApi.detalle.vendedores(filtros),
    productos: (filtros?: FiltrosReporteParams) => reportsApi.detalle.productos(filtros),
    clientes: (filtros?: FiltrosReporteParams) => reportsApi.detalle.clientes(filtros),
  },

  config: {
    empresa: {
      get: () => request<CompanySettings>('/config/company'),
      update: (data: Partial<CompanySettings>) =>
        request<CompanySettings>('/config/company', { method: 'PUT', body: JSON.stringify(data) }),
    },
    perfil: {
      get: () => request<User>('/config/profile'),
      update: (data: Partial<User>) => request<User>('/config/profile', { method: 'PUT', body: JSON.stringify(data) }),
    },
    seed: () => request<{ message: string }>('/config/seed', { method: 'POST' }),
    clear: () => request<{ message: string; eliminados: Record<string, number> }>('/config/clear', { method: 'POST' }),
  },

  users: {
    list: () => request<User[]>('/users'),
    create: (data: { nombre: string; apellido: string; email: string; password: string; rol: string; avatar?: string }) =>
      request<{ id: string; email: string }>('/users', { method: 'POST', body: JSON.stringify(data) }),
    remove: (id: string) => request<void>(`/users/${id}`, { method: 'DELETE' }),
  },
};
