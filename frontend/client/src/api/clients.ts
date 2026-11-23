import type { Client, CsvImportResult } from '../types';
import { BASE, request } from './http';

export const clientsApi = {
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
};
