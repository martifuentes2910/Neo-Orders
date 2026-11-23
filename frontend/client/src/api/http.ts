import type { CsvImportResult } from '../types';

export type { CsvImportResult };

const BASE =
  (import.meta.env?.VITE_API_URL as string | undefined) ||
  (typeof window !== 'undefined'
    ? `${window.location.protocol}//${window.location.hostname}:5286/api`
    : 'http://localhost:5286/api');

export { BASE };

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
    let body: unknown = null;
    try {
      body = await res.json();
      const b = body as Record<string, unknown> | null;
      const errors = b?.errors;
      if (typeof b?.message === 'string') message = b.message;
      else if (Array.isArray(errors) && (errors[0] as Record<string, unknown> | undefined)?.row) message = 'El archivo contiene errores de validaci?n.';
      else if (errors && typeof errors === 'object' && !Array.isArray(errors)) message = Object.values(errors as Record<string, unknown[]>).flat().join(' ');
      else if (typeof b?.title === 'string') message = b.title;
    } catch {
      /* ignore */
    }
    const err = new Error(message) as Error & { body?: unknown };
    if (body) err.body = body;
    throw err;
  }
  if (res.status === 204) return undefined as T;
  const ct = res.headers.get('content-type') || '';
  if (ct.includes('application/json')) return res.json();
  return undefined as T;
}

export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
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
