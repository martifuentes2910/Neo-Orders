import type { User } from '../types';
import { request } from './http';

export const usersApi = {
  list: () => request<User[]>('/users'),
  create: (data: { nombre: string; apellido: string; email: string; password: string; rol: string; avatar?: string }) =>
    request<{ id: string; email: string }>('/users', { method: 'POST', body: JSON.stringify(data) }),
  remove: (id: string) => request<void>(`/users/${id}`, { method: 'DELETE' }),
};
