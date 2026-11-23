import type { Seller } from '../types';
import { request } from './http';

export const sellersApi = {
  list: (onlyActive?: boolean) => request<Seller[]>(`/sellers${onlyActive ? '?onlyActive=true' : ''}`),
  create: (data: Partial<Seller>) => request<Seller>('/sellers', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: Partial<Seller>) => request<void>(`/sellers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  remove: (id: number) => request<void>(`/sellers/${id}`, { method: 'DELETE' }),
};
