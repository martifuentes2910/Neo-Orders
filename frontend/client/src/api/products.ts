import type { Product, CsvImportResult, PriceAdjustmentPreviewResponse, PriceAdjustmentApplyResponse } from '../types';
import { BASE, request } from './http';

export const productsApi = {
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
};
