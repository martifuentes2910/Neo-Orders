import type { CompanySettings, User } from '../types';
import { request } from './http';

export const configApi = {
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
};
