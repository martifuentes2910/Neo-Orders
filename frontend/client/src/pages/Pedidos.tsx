import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { ORDER_STATUSES, getStatusColor, type OrderStatus } from '../types';
import { useTheme } from '../context/ThemeContext';
// no motion needed
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
import { toast } from 'sonner';

const emptyForm = {
  clientId: 0,
  sellerId: 0 as number | null,
  productId: 0 as number | null,
  productName: '',
  cantidad: 1,
  precioUnitario: 0,
  estado: 'Pendiente' as OrderStatus,
};

export default function Pedidos() {
  const { formatMoney, toArs, toDisplay } = useSettings();
  const { resolved } = useTheme();
  const isDark = resolved === 'oscuro';
  const [orders, setOrders] = useState<any[]>([]);
  const [clients, setClients] = useState<any[]>([]);
  const [sellers, setSellers] = useState<any[]>([]);
  const [products, setProducts] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [estadoFilter, setEstadoFilter] = useState('Todos');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<any | null>(null);
  const [form, setForm] = useState<any>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await api.orders.list(search, estadoFilter);
      setOrders(data);
    } catch {
      toast.error('Error al cargar pedidos');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [estadoFilter]);

  useEffect(() => {
    const t = setTimeout(() => load(), 300);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  useEffect(() => {
    api.clients.list().then(setClients).catch(() => {});
    api.sellers.list(true).then(setSellers).catch(() => {});
    api.products.list().then(setProducts).catch(() => {});
  }, []);

  const onProductChange = (id: string) => {
    const numId = id ? Number(id) : 0;
    const p = products.find((x) => x.id === numId);
    if (p) {
      setForm((f: any) => ({
        ...f,
        productId: p.id,
        productName: p.name,
        precioUnitario: toDisplay(p.basePrice),
      }));
    } else {
      setForm((f: any) => ({ ...f, productId: null }));
    }
  };

  const openCreate = () => {
    setEditing(null);
    setForm({ ...emptyForm, clientId: clients[0]?.id || 0, sellerId: sellers[0]?.id || null });
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (o: any) => {
    setEditing(o);
    setForm({
      clientId: o.clientId,
      sellerId: o.sellerId,
      productId: o.productId,
      productName: o.productName,
      cantidad: o.cantidad,
      precioUnitario: toDisplay(o.precioUnitario),
      estado: o.estado,
    });
    setErrors({});
    setShowForm(true);
  };

  const validate = () => {
    const e: Record<string, string> = {};
    if (!form.clientId) e.clientId = 'Seleccioná un cliente';
    if (!form.productName?.trim()) e.productName = 'El producto es obligatorio';
    if (!form.cantidad || form.cantidad < 1) e.cantidad = 'Cantidad inválida';
    if (!form.precioUnitario || form.precioUnitario <= 0) e.precioUnitario = 'Precio inválido';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const save = async () => {
    if (!validate()) return;
    setSaving(true);
    try {
      const payload = {
        clientId: form.clientId,
        sellerId: form.sellerId,
        productId: form.productId,
        productName: form.productName,
        cantidad: form.cantidad,
        precioUnitario: toArs(form.precioUnitario),
        estado: editing ? form.estado : 'Pendiente',
      };
      if (editing) {
        await api.orders.update(editing.id, { ...payload, id: editing.id });
        toast.success('Pedido actualizado');
      } else {
        await api.orders.create(payload);
        toast.success('Pedido creado');
      }
      setShowForm(false);
      await load();
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este pedido?')) return;
    setDeleting(id);
    try {
      await api.orders.remove(id);
      toast.success('Pedido eliminado');
      await load();
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setDeleting(null);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Pedidos</h1>
          <p className="text-sm text-muted-foreground">Gestión de pedidos de clientes.</p>
        </div>
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" /> Nuevo pedido
        </Button>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input placeholder="Buscar por cliente o producto…" value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9" />
        </div>
        <Select value={estadoFilter} onValueChange={setEstadoFilter}>
          <SelectTrigger className="w-full sm:w-48">
            <SelectValue placeholder="Estado" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Todos">Todos los estados</SelectItem>
            {ORDER_STATUSES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="space-y-3 p-6">
              {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : orders.length === 0 ? (
            <div className="py-12 text-center text-muted-foreground">No hay pedidos.</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead>#</TableHead>
                  <TableHead>Cliente</TableHead>
                  <TableHead>Vendedor</TableHead>
                  <TableHead>Producto</TableHead>
                  <TableHead className="text-right">Cant.</TableHead>
                  <TableHead className="text-right">Precio unit.</TableHead>
                  <TableHead className="text-right">Total</TableHead>
                  <TableHead>Estado</TableHead>
                  <TableHead>Fecha</TableHead>
                  <TableHead className="text-right">Acciones</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {orders.map((o) => (
                  <TableRow key={o.id}>
                    <TableCell className="font-medium">{o.id}</TableCell>
                    <TableCell>{o.clientName}</TableCell>
                    <TableCell className="text-muted-foreground">{o.sellerName || '-'}</TableCell>
                    <TableCell>{o.productName}</TableCell>
                    <TableCell className="text-right">{o.cantidad}</TableCell>
                    <TableCell className="text-right">{formatMoney(o.precioUnitario)}</TableCell>
                    <TableCell className="text-right font-semibold text-emerald-600 dark:text-emerald-400">{formatMoney(o.total)}</TableCell>
                    <TableCell>
                      <Badge style={{ background: getStatusColor(o.estado as any, isDark) }} className="text-white">
                        {o.estado}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{new Date(o.fecha).toLocaleDateString('es-AR')}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-1">
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => openEdit(o)}>
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                        <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => remove(o.id)} disabled={deleting === o.id}>
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{editing ? 'Editar pedido' : 'Nuevo pedido'}</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label>Cliente</Label>
              <Select value={String(form.clientId)} onValueChange={(v) => setForm({ ...form, clientId: Number(v) })}>
                <SelectTrigger>
                  <SelectValue placeholder="Seleccionar cliente" />
                </SelectTrigger>
                <SelectContent>
                  {clients.map((c) => <SelectItem key={c.id} value={String(c.id)}>{c.nombre}</SelectItem>)}
                </SelectContent>
              </Select>
              {errors.clientId && <p className="text-sm text-destructive">{errors.clientId}</p>}
            </div>
            <div className="grid gap-2">
              <Label>Vendedor</Label>
              <Select value={form.sellerId ? String(form.sellerId) : ''} onValueChange={(v) => setForm({ ...form, sellerId: v ? Number(v) : null })}>
                <SelectTrigger>
                  <SelectValue placeholder="Sin vendedor" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">Sin vendedor</SelectItem>
                  {sellers.map((s) => <SelectItem key={s.id} value={String(s.id)}>{s.name}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label>Producto del catálogo</Label>
              <Select value={form.productId ? String(form.productId) : ''} onValueChange={onProductChange}>
                <SelectTrigger>
                  <SelectValue placeholder="Seleccionar producto…" />
                </SelectTrigger>
                <SelectContent>
                  {products.map((p) => <SelectItem key={p.id} value={String(p.id)}>{p.name} ({p.sku})</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label>Producto</Label>
              <Input value={form.productName} onChange={(e) => setForm({ ...form, productName: e.target.value })} />
              {errors.productName && <p className="text-sm text-destructive">{errors.productName}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label>Cantidad</Label>
                <Input type="number" min={1} value={form.cantidad} onChange={(e) => setForm({ ...form, cantidad: Number(e.target.value) })} />
                {errors.cantidad && <p className="text-sm text-destructive">{errors.cantidad}</p>}
              </div>
              <div className="grid gap-2">
                <Label>Precio unitario</Label>
                <Input type="number" step="0.01" min={0} value={form.precioUnitario} onChange={(e) => setForm({ ...form, precioUnitario: Number(e.target.value) })} />
                {errors.precioUnitario && <p className="text-sm text-destructive">{errors.precioUnitario}</p>}
              </div>
            </div>
            {editing && (
              <div className="grid gap-2">
                <Label>Estado</Label>
                <Select value={form.estado} onValueChange={(v) => setForm({ ...form, estado: v })}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {ORDER_STATUSES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            )}
            {!editing && (
              <p className="text-sm text-muted-foreground">El pedido se crea en estado <strong>Pendiente</strong>.</p>
            )}
            <div className="rounded-lg bg-muted/50 p-3 text-sm">
              Total: <strong className="text-emerald-600 dark:text-emerald-400">{formatMoney(toArs(form.cantidad * form.precioUnitario))}</strong>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowForm(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={save} disabled={saving}>{saving ? 'Guardando…' : editing ? 'Actualizar' : 'Crear'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
