import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import type { Client } from '../../types';
import { clientsFeatureApi } from './api';
import { clientSchema, emptyClientForm, type ClientFormValues } from './model';

let cachedClientes: Client[] | null = null;

/** Datos y acciones del slice Clientes (listado con búsqueda, alta, edición, baja, CSV). */
export function useClients() {
  const [clientes, setClientes] = useState<Client[]>(cachedClientes || []);
  const [loading, setLoading] = useState(!cachedClientes);
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Client | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);
  const [showImport, setShowImport] = useState(false);

  const { register, handleSubmit, reset, formState: { errors } } = useForm({
    resolver: zodResolver(clientSchema),
    defaultValues: emptyClientForm,
  });

  const load = async () => {
    if (!cachedClientes) setLoading(true);
    try {
      const data = await clientsFeatureApi.list(search);
      cachedClientes = data;
      setClientes(data);
    } catch {
      toast.error('Error al cargar clientes');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const skipSearch = useRef(true);
  useEffect(() => {
    if (skipSearch.current) {
      skipSearch.current = false;
      return;
    }
    const t = setTimeout(() => void load(), 300);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const openCreate = () => {
    setEditing(null);
    reset(emptyClientForm);
    setShowForm(true);
  };

  const openEdit = (c: Client) => {
    setEditing(c);
    reset({ nombre: c.nombre, email: c.email, telefono: c.telefono || '', direccion: c.direccion || '' });
    setShowForm(true);
  };

  const save = async (data: ClientFormValues) => {
    setSaving(true);
    try {
      if (editing) await clientsFeatureApi.update(editing.id, { ...data, id: editing.id });
      else await clientsFeatureApi.create(data);
      toast.success(editing ? 'Cliente actualizado' : 'Cliente creado');
      setShowForm(false);
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este cliente?')) return;
    setDeleting(id);
    try {
      await clientsFeatureApi.remove(id);
      toast.success('Cliente eliminado');
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setDeleting(null);
    }
  };

  return {
    clientes,
    loading,
    search,
    setSearch,
    showForm,
    setShowForm,
    editing,
    setEditing,
    saving,
    deleting,
    showImport,
    setShowImport,
    register,
    errors,
    submit: handleSubmit(save),
    openCreate,
    openEdit,
    load,
    save,
    remove,
    templateUrl: clientsFeatureApi.templateUrl,
    importCsv: clientsFeatureApi.importCsv,
  };
}
