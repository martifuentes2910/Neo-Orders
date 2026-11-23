import type {
  ClientesComprasResponse,
  DashboardMetricsResponse,
  FiltrosReporteParams,
  ProductosVendidosResponse,
  ReporteResumenResponse,
  VentasVendedorResponse,
} from './types/reportes';
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

export type { CsvImportError, CsvImportResult, PriceAdjustmentItem, PriceAdjustmentPreviewResponse, PriceAdjustmentApplyResponse } from './types';

const BASE =
  (import.meta as any).env?.VITE_API_URL ||
  (typeof window !== 'undefined'
    ? `${window.location.protocol}//${window.location.hostname}:5286/api`
    : 'http://localhost:5286/api');

let csrfToken: string | null = null;

async function fetchCsrfToken(): Promise<string | null> {
  try {
    const res = await fetch(`${BASE.replace('/api', '')}/api/antiforgery/token`, {
      credentials: 'include',
    });
    if (!res.ok) return null;
    const data = await res.json();
    csrfToken = data.token;
    return csrfToken;
  } catch {
    return null;
  }
}

async function ensureCsrf(): Promise<string | null> {
  if (csrfToken) return csrfToken;
  return fetchCsrfToken();
}

async function handle<T>(res: Response): Promise<T> {
  if (res.status === 401) {
    throw new Error('UNAUTHORIZED');
  }
  if (!res.ok) {
    let message = `Error ${res.status}`;
    let body: any = null;
    try {
      body = await res.json();
      if (body?.message) message = body.message;
      else if (Array.isArray(body?.errors) && body.errors[0]?.row) message = 'El archivo contiene errores de validación.';
      else if (body?.errors) message = Object.values(body.errors).flat().join(' ');
      else if (body?.title) message = body.title;
    } catch {
      /* ignore */
    }
    const err: any = new Error(message);
    if (body) err.body = body;
    throw err;
  }
  if (res.status === 204) return undefined as T;
  const ct = res.headers.get('content-type') || '';
  if (ct.includes('application/json')) return res.json();
  return undefined as T;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const method = (options.method || 'GET').toUpperCase();
  const isFormData = typeof FormData !== 'undefined' && options.body instanceof FormData;
  const headers: Record<string, string> = {
    // KAN-23: con FormData el navegador debe fijar el boundary del multipart.
    ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
    ...(options.headers as Record<string, string>),
  };
  if (method !== 'GET' && method !== 'HEAD') {
    const t = await ensureCsrf();
    if (t) headers['X-XSRF-TOKEN'] = t;
  }
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers,
    credentials: 'include',
  });
  if (res.status === 400 && method !== 'GET') {
    csrfToken = null;
    const t = await fetchCsrfToken();
    if (t) headers['X-XSRF-TOKEN'] = t;
    const res2 = await fetch(`${BASE}${path}`, { ...options, headers, credentials: 'include' });
    return handle<T>(res2);
  }
  return handle<T>(res);
}

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
      hasta?: string
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
    create: (data: Pick<Order, 'clientId' | 'cantidad'> & { sellerId?: number | null; productId?: number | null; productName?: string; precioUnitario?: number; orderNumber?: string | null }) => request<{ id: number }>('/orders', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: Pick<Order, 'id' | 'clientId' | 'cantidad' | 'estado'> & { sellerId?: number | null; productId?: number | null; productName?: string; precioUnitario?: number }) => request<void>(`/orders/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    // KAN-16: único camino permitido para cambiar el estado de pedidos Enviado/Entregado/Cancelado.
    updateStatus: (id: number, estado: string) => request<void>(`/orders/${id}/estado`, { method: 'PATCH', body: JSON.stringify({ estado }) }),
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
    update: (id: number, data: Partial<Product>) => request<void>(`/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
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

  dashboard: {
    metricas: () => request<DashboardMetricsResponse>('/dashboard/metrics'),
  },

  notifications: {
    list: (take = 60) => request<{ items: AppNotification[]; resumen: NotificationsSummary }>(`/notifications?take=${take}`),
  },

  reports: (filtros?: FiltrosReporteParams) =>
    request<ReporteResumenResponse>(`/reports${buildReporteQuery(filtros)}`),

  reportesDetalle: {
    vendedores: (filtros?: FiltrosReporteParams) =>
      request<VentasVendedorResponse>(`/reports/detalle/vendedores${buildReporteQuery(filtros)}`),
    productos: (filtros?: FiltrosReporteParams) =>
      request<ProductosVendidosResponse>(`/reports/detalle/productos${buildReporteQuery(filtros)}`),
    clientes: (filtros?: FiltrosReporteParams) =>
      request<ClientesComprasResponse>(`/reports/detalle/clientes${buildReporteQuery(filtros)}`),
  },

  config: {
    empresa: {
      get: () => request<CompanySettings>('/config/company'),
      update: (data: Partial<CompanySettings>) => request<CompanySettings>('/config/company', { method: 'PUT', body: JSON.stringify(data) }),
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
    create: (data: { nombre: string; apellido: string; email: string; password: string; rol: string; avatar?: string }) => request<{ id: string; email: string }>('/users', { method: 'POST', body: JSON.stringify(data) }),
    remove: (id: string) => request<void>(`/users/${id}`, { method: 'DELETE' }),
  },
};
