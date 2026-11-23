import type { CompanySettings, User } from '../../types';

export interface PerfilFormValues {
  nombre: string;
  apellido: string;
  avatar: string;
}

export const emptyPerfilForm: PerfilFormValues = { nombre: '', apellido: '', avatar: 'avatar-1' };

export function perfilFormFromUser(user: User | null): PerfilFormValues {
  return {
    nombre: user?.nombre || '',
    apellido: user?.apellido || '',
    avatar: user?.avatar || 'avatar-1',
  };
}

export interface EmpresaFormValues {
  nombreEmpresa: string;
}

export function empresaFormFromCompany(company: CompanySettings | null): EmpresaFormValues {
  return { nombreEmpresa: company?.nombreEmpresa || '' };
}

/** Resumen en español de los datos operativos eliminados por `/config/clear`. */
export function formatClearSummary(eliminados: Record<string, number> | undefined): string {
  const e = eliminados ?? {};
  return `Eliminados: ${e.notificaciones ?? 0} notificaciones, ${e.pedidos ?? 0} pedidos, ${e.productos ?? 0} productos, ${e.vendedores ?? 0} vendedores y ${e.clientes ?? 0} clientes.`;
}
