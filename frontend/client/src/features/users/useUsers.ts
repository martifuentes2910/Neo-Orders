import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { useTableSort } from '../../hooks/useTableSort';
import type { UserListItem } from '../../types';
import { usersFeatureApi } from './api';
import { emptyUserForm, type UserFormValues } from './model';

/** Listado, alta y baja de usuarios del sistema (sección de administración). */
export function useUsers() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<UserFormValues>(emptyUserForm);
  const [saving, setSaving] = useState(false);

  const { sorted, sort, toggle } = useTableSort(users, {
    email: (u) => u.email,
    nombre: (u) => `${u.nombre ?? ''} ${u.apellido ?? ''}`.trim(),
    rol: (u) => (u.roles ?? []).join(', '),
  });

  const load = async () => {
    setLoading(true);
    try {
      setUsers(await usersFeatureApi.list());
    } catch {
      toast.error('Error al cargar usuarios');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const save = async () => {
    setSaving(true);
    try {
      await usersFeatureApi.create(form);
      toast.success('Usuario creado');
      setShowForm(false);
      setForm(emptyUserForm);
      await load();
    } catch (e: unknown) {
      toast.error(e instanceof Error ? e.message : 'Error inesperado');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    if (!confirm('¿Eliminar este usuario?')) return;
    try {
      await usersFeatureApi.remove(id);
      toast.success('Usuario eliminado');
      await load();
    } catch (e: unknown) {
      toast.error(e instanceof Error ? e.message : 'Error inesperado');
    }
  };

  return {
    users: sorted,
    loading,
    showForm,
    setShowForm,
    form,
    setForm,
    saving,
    sort,
    toggle,
    save,
    remove,
  };
}
