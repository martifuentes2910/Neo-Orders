import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import type { Seller } from '../../types';
import { sellersFeatureApi } from './api';
import { emptySellerForm, validateSellerForm, type SellerFormValues } from './model';

let cachedVendedores: Seller[] | null = null;

/** Datos y acciones del slice Vendedores (listado, alta, edición y baja). */
export function useSellers() {
  const [sellers, setSellers] = useState<Seller[]>(cachedVendedores || []);
  const [loading, setLoading] = useState(!cachedVendedores);
  const [search, setSearch] = useState('');
  const [activoFilter, setActivoFilter] = useState('Todos');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Seller | null>(null);
  const [form, setForm] = useState<SellerFormValues>(emptySellerForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);

  const load = async () => {
    if (!cachedVendedores) setLoading(true);
    try {
      const data = await sellersFeatureApi.list();
      cachedVendedores = data;
      setSellers(data);
    } catch {
      toast.error('Error al cargar vendedores');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm(emptySellerForm);
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (s: Seller) => {
    setEditing(s);
    setForm({
      name: s.name,
      email: s.email,
      phone: s.phone || '',
      commissionPercentage: s.commissionPercentage,
      active: s.active,
    });
    setErrors({});
    setShowForm(true);
  };

  const save = async () => {
    const nextErrors = validateSellerForm(form);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;
    setSaving(true);
    try {
      if (editing) await sellersFeatureApi.update(editing.id, { ...form, id: editing.id });
      else await sellersFeatureApi.create(form);
      toast.success(editing ? 'Vendedor actualizado' : 'Vendedor creado');
      setShowForm(false);
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este vendedor?')) return;
    setDeleting(id);
    try {
      await sellersFeatureApi.remove(id);
      toast.success('Vendedor eliminado');
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setDeleting(null);
    }
  };

  // Los filtros de vendedores son locales (no hay query params en el contrato).
  const filtered = sellers.filter((s) => {
    const q = search.toLowerCase();
    const matchSearch = !q || s.name?.toLowerCase().includes(q) || s.email?.toLowerCase().includes(q);
    const matchActivo = activoFilter === 'Todos' || (activoFilter === 'Activos' ? s.active : !s.active);
    return matchSearch && matchActivo;
  });

  return {
    sellers,
    filtered,
    loading,
    search,
    setSearch,
    activoFilter,
    setActivoFilter,
    showForm,
    setShowForm,
    editing,
    form,
    setForm,
    errors,
    saving,
    deleting,
    openCreate,
    openEdit,
    save,
    remove,
    load,
  };
}
