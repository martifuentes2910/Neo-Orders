export interface UserFormValues {
  email: string;
  password: string;
  nombre: string;
  apellido: string;
  avatar: string;
  rol: string;
}

export const emptyUserForm: UserFormValues = {
  email: '',
  password: '',
  nombre: '',
  apellido: '',
  avatar: 'avatar-1',
  rol: 'Empleado',
};

export const USER_ROLES = [
  { value: 'Empleado', label: 'Empleado' },
  { value: 'Admin', label: 'Administrador' },
] as const;
