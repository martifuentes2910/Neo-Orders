import { useEffect, useRef, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import type { Client } from '../types';
import { Plus, Pencil, Trash2, Search, Upload } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import SortableTableHead from '@/components/SortableTableHead';
import { useTableSort } from '@/hooks/useTableSort';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { motion, useReducedMotion } from 'framer-motion';
import { toast } from 'sonner';
import CsvImportDialog from '@/components/CsvImportDialog';

let cachedClientes: Client[] | null = null;

const schema = z.object({
  nombre: z.string().min(2, 'Nombre inválido'),
  email: z.string().email('Email inválido'),
  telefono: z.string().optional().or(z.literal('')),
  direccion: z.string().optional().or(z.literal('')),
});

const emptyForm = { nombre: '', email: '', telefono: '', direccion: '' };

export default function Clientes() {
  const { formatMoney } = useSettings();
  const reduceMotion = useReducedMotion();
  const { user } = useAuth();
  const isAdmin = user?.roles?.includes('Admin');
  const [clientes, setClientes] = useState<any[]>(cachedClientes || []);
  const [loading, setLoading] = useState(!cachedClientes);
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Client | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);
  const [showImport, setShowImport] = useState(false);

  const { sorted, sort, toggle } = useTableSort(clientes, {
    id: (c) => c.id,
    nombre: (c) => c.nombre,
    email: (c) => c.email,
    telefono: (c) => c.telefono,
    direccion: (c) => c.direccion,
    pedidos: (c) => c.ordersCount ?? 0,
    total: (c) => c.totalSpent ?? 0,
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm({
    resolver: zodResolver(schema),
    defaultValues: emptyForm,
  });

  const load = async () => {
    if (!cachedClientes) setLoading(true);
    try {
      const data = await api.clients.list(search);
      cachedClientes = data;
      setClientes(data);
    } catch {
      toast.error('Error al cargar clientes');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);
  const skipSearch = useRef(true);
  useEffect(() => {
    if (skipSearch.current) {
      skipSearch.current = false;
      return;
    }
    const t = setTimeout(() => load(), 300);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const openCreate = () => { setEditing(null); reset(emptyForm); setShowForm(true); };
  const openEdit = (c: Client) => { setEditing(c); reset({ nombre: c.nombre, email: c.email, telefono: c.telefono || '', direccion: c.direccion || '' }); setShowForm(true); };

  const save = async (data: { nombre: string; email: string; telefono?: string; direccion?: string }) => {
    setSaving(true);
    try {
      if (editing) await api.clients.update(editing.id, { ...data, id: editing.id });
      else await api.clients.create(data);
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
      await api.clients.remove(id);
      toast.success('Cliente eliminado');
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setDeleting(null);
    }
  };

  return (
    <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.25 }} className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <motion.div initial={reduceMotion ? false : { opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { duration: 0.25 }}>
          <h1 className="text-[32px] font-bold tracking-tight">Clientes</h1>
          <p className="text-sm text-muted-foreground">Gestión de clientes y su historial.</p>
        </motion.div>
        {isAdmin && (
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => setShowImport(true)} className="gap-2">
              <Upload className="h-4 w-4" /> Importar CSV
            </Button>
            <Button onClick={openCreate} className="gap-2">
              <Plus className="h-4 w-4" /> Nuevo cliente
            </Button>
          </div>
        )}
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input placeholder="Buscar por nombre o email…" value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9" />
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="space-y-3 p-6">{[...Array(5)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}</div>
          ) : clientes.length === 0 ? (
            <div className="py-12 text-center text-muted-foreground">No hay clientes.</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <SortableTableHead label="#" sortKey="id" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Nombre" sortKey="nombre" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Email" sortKey="email" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Teléfono" sortKey="telefono" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Dirección" sortKey="direccion" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Pedidos" sortKey="pedidos" sort={sort} onSort={toggle} align="right" className="text-right" />
                  <SortableTableHead label="Total gastado" sortKey="total" sort={sort} onSort={toggle} align="right" className="text-right" />
                  {isAdmin && <TableHead className="text-right">Acciones</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {sorted.map((c) => (
                  <TableRow key={c.id}>
                    <TableCell className="font-medium">{c.id}</TableCell>
                    <TableCell>{c.nombre}</TableCell>
                    <TableCell className="text-muted-foreground">{c.email}</TableCell>
                    <TableCell>{c.telefono || '-'}</TableCell>
                    <TableCell>{c.direccion || '-'}</TableCell>
                    <TableCell className="text-right tabular-nums">{c.ordersCount ?? 0}</TableCell>
                    <TableCell className="text-right font-semibold tabular-nums text-amber-700 dark:text-amber-300">{formatMoney(c.totalSpent || 0)}</TableCell>
                    {isAdmin && (
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => openEdit(c)}><Pencil className="h-3.5 w-3.5" /></Button>
                          <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => remove(c.id)} disabled={deleting === c.id}><Trash2 className="h-3.5 w-3.5" /></Button>
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

      <CsvImportDialog
        open={showImport}
        onOpenChange={setShowImport}
        title="Importar clientes desde CSV"
        templateUrl={api.clients.templateUrl()}
        templateName="plantilla-clientes.csv"
        onImport={(file) => api.clients.importCsv(file)}
        onImported={load}
      />

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{editing ? 'Editar cliente' : 'Nuevo cliente'}</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label>Nombre</Label>
              <Input {...register('nombre')} />
              {errors.nombre && <p className="text-sm text-destructive">{errors.nombre.message}</p>}
            </div>
            <div className="grid gap-2">
              <Label>Email</Label>
              <Input type="email" {...register('email')} />
              {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
            </div>
            <div className="grid gap-2">
              <Label>Teléfono</Label>
              <Input {...register('telefono')} />
            </div>
            <div className="grid gap-2">
              <Label>Dirección</Label>
              <Input {...register('direccion')} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowForm(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={handleSubmit(save)} disabled={saving}>{saving ? 'Guardando…' : editing ? 'Actualizar' : 'Crear'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </motion.div>
  );
}
