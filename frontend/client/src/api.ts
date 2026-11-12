const BASE = (import.meta as any).env?.VITE_API_URL || 'http://localhost:5286/api';

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
    try {
      const body = await res.json();
      if (body?.message) message = body.message;
      else if (body?.errors) message = Object.values(body.errors).flat().join(' ');
      else if (body?.title) message = body.title;
    } catch {
      /* ignore */
    }
    throw new Error(message);
  }
  if (res.status === 204) return undefined as T;
  const ct = res.headers.get('content-type') || '';
  if (ct.includes('application/json')) return res.json();
  return undefined as T;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const method = (options.method || 'GET').toUpperCase();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
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

export const api = {
  login: (email: string, password: string) =>
    request<any>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  logout: () => request<void>('/auth/logout', { method: 'POST' }),
  me: () => request<any>('/auth/me'),

  clients: {
    list: (search?: string) => request<any[]>(`/clients${search ? '?search=' + encodeURIComponent(search) : ''}`),
    get: (id: number) => request<any>(`/clients/${id}`),
    create: (data: any) => request<any>('/clients', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: any) => request<void>(`/clients/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: number) => request<void>(`/clients/${id}`, { method: 'DELETE' }),
  },

  orders: {
    list: (search?: string, estado?: string, clientId?: number, sellerId?: number) => {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      if (estado && estado !== 'Todos') params.set('estado', estado);
      if (clientId) params.set('clientId', String(clientId));
      if (sellerId) params.set('sellerId', String(sellerId));
      const qs = params.toString();
      return request<any[]>(`/orders${qs ? '?' + qs : ''}`);
    },
    get: (id: number) => request<any>(`/orders/${id}`),
    create: (data: any) => request<any>('/orders', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: any) => request<void>(`/orders/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: number) => request<void>(`/orders/${id}`, { method: 'DELETE' }),
    statuses: () => request<string[]>('/orders/statuses'),
  },

  products: {
    list: (search?: string, category?: string, onlyInStock?: boolean) => {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      if (category && category !== 'Todos') params.set('category', category);
      if (onlyInStock) params.set('onlyInStock', 'true');
      const qs = params.toString();
      return request<any[]>(`/products${qs ? '?' + qs : ''}`);
    },
    categories: () => request<string[]>('/products/categories'),
    get: (id: number) => request<any>(`/products/${id}`),
    create: (data: any) => request<any>('/products', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: any) => request<void>(`/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: number) => request<void>(`/products/${id}`, { method: 'DELETE' }),
  },

  sellers: {
    list: (onlyActive?: boolean) => request<any[]>(`/sellers${onlyActive ? '?onlyActive=true' : ''}`),
    get: (id: number) => request<any>(`/sellers/${id}`),
    create: (data: any) => request<any>('/sellers', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: number, data: any) => request<void>(`/sellers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: number) => request<void>(`/sellers/${id}`, { method: 'DELETE' }),
  },

  dashboard: {
    metricas: () => request<any>('/dashboard/metrics'),
  },

  reports: (filtros?: { desde?: string; hasta?: string; clientId?: number; sellerId?: number; estado?: string }) => {
    const params = new URLSearchParams();
    if (filtros?.desde) params.set('desde', filtros.desde);
    if (filtros?.hasta) params.set('hasta', filtros.hasta);
    if (filtros?.clientId) params.set('clientId', String(filtros.clientId));
    if (filtros?.sellerId) params.set('sellerId', String(filtros.sellerId));
    if (filtros?.estado && filtros.estado !== 'Todos') params.set('estado', filtros.estado);
    const qs = params.toString();
    return request<any>(`/reports${qs ? '?' + qs : ''}`);
  },

  config: {
    empresa: {
      get: () => request<any>('/config/company'),
      update: (data: any) => request<any>('/config/company', { method: 'PUT', body: JSON.stringify(data) }),
    },
    perfil: {
      get: () => request<any>('/config/profile'),
      update: (data: any) => request<any>('/config/profile', { method: 'PUT', body: JSON.stringify(data) }),
    },
    seed: () => request<any>('/config/seed', { method: 'POST' }),
  },

  users: {
    list: () => request<any[]>('/users'),
    create: (data: any) => request<any>('/users', { method: 'POST', body: JSON.stringify(data) }),
    setRol: (id: string, rol: string) => request<void>(`/users/${id}/rol`, { method: 'PUT', body: JSON.stringify({ rol }) }),
    remove: (id: string) => request<void>(`/users/${id}`, { method: 'DELETE' }),
  },

  antiforgery: fetchCsrfToken,
};
