import { useEffect, useState } from 'react';
import { api } from '../api';
import type { Seller } from '../types';
import { useAuth } from '../context/AuthContext';
import { Plus, Pencil, Trash2, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import SortableTableHead from '@/components/SortableTableHead';
import { useTableSort } from '@/hooks/useTableSort';
import { toast } from 'sonner';
import { motion, useReducedMotion } from 'framer-motion';

let cachedVendedores: Seller[] | null = null;

const emptyForm = { name: '', email: '', phone: '', commissionPercentage: 5, active: true };

export default function Vendedores() {
  const reduceMotion = useReducedMotion();
  const { user } = useAuth();
  const isAdmin = user?.roles?.includes('Admin');
  const [sellers, setSellers] = useState<any[]>(cachedVendedores || []);
  const [loading, setLoading] = useState(!cachedVendedores);
  const [search, setSearch] = useState('');
  const [activoFilter, setActivoFilter] = useState('Todos');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Seller | null>(null);
  const [form, setForm] = useState<any>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);

  const load = async () => {
    if (!cachedVendedores) setLoading(true);
    try { 
      const data = await api.sellers.list(); 
      cachedVendedores = data;
      setSellers(data); 
    } catch { toast.error('Error al cargar vendedores'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const openCreate = () => { setEditing(null); setForm(emptyForm); setErrors({}); setShowForm(true); };
  const openEdit = (s: Seller) => { setEditing(s); setForm({ name: s.name, email: s.email, phone: s.phone || '', commissionPercentage: s.commissionPercentage, active: s.active }); setErrors({}); setShowForm(true); };

  const validate = () => {
    const e: Record<string, string> = {};
    if (!form.name?.trim()) e.name = 'Nombre obligatorio';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const save = async () => {
    if (!validate()) return;
    setSaving(true);
    try {
      if (editing) await api.sellers.update(editing.id, { ...form, id: editing.id });
      else await api.sellers.create(form);
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
      await api.sellers.remove(id);
      toast.success('Vendedor eliminado');
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setDeleting(null);
    }
  };

  const filtered = sellers.filter((s) => {
    const q = search.toLowerCase();
    const matchSearch = !q || s.name?.toLowerCase().includes(q) || s.email?.toLowerCase().includes(q);
    const matchActivo = activoFilter === 'Todos' || (activoFilter === 'Activos' ? s.active : !s.active);
    return matchSearch && matchActivo;
  });

  const { sorted, sort, toggle } = useTableSort(filtered, {
    id: (s) => s.id,
    nombre: (s) => s.name,
    email: (s) => s.email,
    telefono: (s) => s.phone,
    comision: (s) => s.commissionPercentage,
    activo: (s) => s.active,
  });

  return (
    <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.25 }} className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <motion.div initial={reduceMotion ? false : { opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { duration: 0.25 }}>
          <h1 className="text-[32px] font-bold tracking-tight">Vendedores</h1>
          <p className="text-sm text-muted-foreground">Gestión de vendedores y comisiones.</p>
        </motion.div>
        {isAdmin && (
          <Button onClick={openCreate} className="gap-2">
            <Plus className="h-4 w-4" /> Nuevo vendedor
          </Button>
        )}
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input placeholder="Buscar por nombre o email…" value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9" />
        </div>
        <Select value={activoFilter} onValueChange={setActivoFilter}>
          <SelectTrigger className="w-full sm:w-40">
            <SelectValue placeholder="Estado" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Todos">Todos</SelectItem>
            <SelectItem value="Activos">Activos</SelectItem>
            <SelectItem value="Inactivos">Inactivos</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="space-y-3 p-6">{[...Array(5)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}</div>
          ) : filtered.length === 0 ? (
            <div className="py-12 text-center text-muted-foreground">No hay vendedores.</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <SortableTableHead label="#" sortKey="id" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Nombre" sortKey="nombre" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Email" sortKey="email" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Teléfono" sortKey="telefono" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Comisión %" sortKey="comision" sort={sort} onSort={toggle} align="right" className="text-right" />
                  <SortableTableHead label="Activo" sortKey="activo" sort={sort} onSort={toggle} />
                  {isAdmin && <TableHead className="text-right">Acciones</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {sorted.map((s) => (
                  <TableRow key={s.id}>
                    <TableCell className="font-medium">{s.id}</TableCell>
                    <TableCell>{s.name}</TableCell>
                    <TableCell className="text-muted-foreground">{s.email}</TableCell>
                    <TableCell>{s.phone || '-'}</TableCell>
                    <TableCell className="text-right tabular-nums">{s.commissionPercentage}%</TableCell>
                    <TableCell>
                      <Badge variant={s.active ? 'default' : 'secondary'}>{s.active ? 'Sí' : 'No'}</Badge>
                    </TableCell>
                    {isAdmin && (
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => openEdit(s)}><Pencil className="h-3.5 w-3.5" /></Button>
                          <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => remove(s.id)} disabled={deleting === s.id}><Trash2 className="h-3.5 w-3.5" /></Button>
                        </div>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{editing ? 'Editar vendedor' : 'Nuevo vendedor'}</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label>Nombre</Label>
              <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              {errors.name && <p className="text-sm text-destructive">{errors.name}</p>}
            </div>
            <div className="grid gap-2">
              <Label>Email</Label>
              <Input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
            <div className="grid gap-2">
              <Label>Teléfono</Label>
              <Input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
            </div>
            <div className="grid gap-2">
              <Label>Comisión %</Label>
              <Input type="number" step="0.1" min={0} max={100} value={form.commissionPercentage} onChange={(e) => setForm({ ...form, commissionPercentage: Number(e.target.value) })} />
            </div>
            <div className="grid gap-2">
              <Label>Activo</Label>
              <Select value={form.active ? '1' : '0'} onValueChange={(v) => setForm({ ...form, active: v === '1' })}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Sí</SelectItem>
                  <SelectItem value="0">No</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowForm(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={save} disabled={saving}>{saving ? 'Guardando…' : editing ? 'Actualizar' : 'Crear'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </motion.div>
  );
}
