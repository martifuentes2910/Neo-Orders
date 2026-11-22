import { useEffect, useRef, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import {
  ORDER_STATUSES,
  estadosDestino,
  esEstadoBloqueado,
  getStatusColor,
  getStatusTextColor,
  type OrderStatus,
} from '../types';
import { useTheme } from '../context/ThemeContext';
import { useAuth } from '../context/AuthContext';
import { motion, useReducedMotion } from 'framer-motion';
import { Plus, Pencil, Trash2, Search, ArrowRight, Upload } from 'lucide-react';
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
import CsvImportDialog from '@/components/CsvImportDialog';

let cachedOrders: any[] | null = null;

const esTerminal = (estado: OrderStatus) => estadosDestino(estado).length === 1;

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
  const { formatMoney } = useSettings();
  const { resolved, palette } = useTheme();
  const reduceMotion = useReducedMotion();
  const isDark = resolved === 'oscuro';
  const { user } = useAuth();
  const isAdmin = user?.roles?.includes('Admin');
  const [orders, setOrders] = useState<any[]>(cachedOrders || []);
  const [clients, setClients] = useState<any[]>([]);
  const [sellers, setSellers] = useState<any[]>([]);
  const [products, setProducts] = useState<any[]>([]);
  const [loading, setLoading] = useState(!cachedOrders);
  const [search, setSearch] = useState('');
  const [estadoFilter, setEstadoFilter] = useState('Todos');
  const [clientFilter, setClientFilter] = useState('');
  const [sellerFilter, setSellerFilter] = useState('');
  const [productFilter, setProductFilter] = useState('');
  const [desde, setDesde] = useState('');
  const [hasta, setHasta] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<any | null>(null);
  const [form, setForm] = useState<any>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);
  const [showImport, setShowImport] = useState(false);
  // KAN-13: producto, nombre y precio son fijos cuando se edita un pedido.
  const bloqueado = !!editing;
  // KAN-16: Enviado/Entregado/Cancelado sólo permiten cambiar el estado.
  const editingEstado: OrderStatus | null = editing ? (editing.estado as OrderStatus) : null;
  const soloEstado = !!editingEstado && esEstadoBloqueado(editingEstado);
  const opcionesEstado = editingEstado ? estadosDestino(editingEstado) : ORDER_STATUSES;
  const productoDelCatalogo = products.find((p) => p.id === form.productId);
  const productoDadoDeBaja = bloqueado && products.length > 0 && !productoDelCatalogo;
  // La FK se pone en null al borrar el producto; hace falta un valor centinela válido para Radix.
  const productSelectValue = productoDadoDeBaja && !form.productId
    ? '__sin_catalogo__'
    : (form.productId ? String(form.productId) : '');

  const { sorted, sort, toggle } = useTableSort(orders, {
    id: (o) => o.id,
    cliente: (o) => o.clientName,
    vendedor: (o) => o.sellerName,
    producto: (o) => o.productName,
    cantidad: (o) => o.cantidad,
    precio: (o) => o.precioUnitario,
    total: (o) => o.total,
    estado: (o) => ORDER_STATUSES.indexOf(o.estado as OrderStatus),
    fecha: (o) => new Date(o.fecha).getTime(),
  });

  const load = async () => {
    if (!cachedOrders) setLoading(true);
    try {
      const data = await api.orders.list(
        search,
        estadoFilter,
        clientFilter === '' ? undefined : Number(clientFilter),
        sellerFilter === '' ? undefined : Number(sellerFilter),
        productFilter === '' ? undefined : Number(productFilter),
        desde || undefined,
        hasta || undefined
      );
      cachedOrders = data;
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
  }, [estadoFilter, clientFilter, sellerFilter, productFilter, desde, hasta]);

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

  useEffect(() => {
    api.clients.list().then(setClients).catch(() => {});
    api.sellers.list(true).then(setSellers).catch(() => {});
    api.products.list().then(setProducts).catch(() => {});
  }, []);

  const hasFilters = !!(
    search ||
    estadoFilter !== 'Todos' ||
    clientFilter ||
    sellerFilter ||
    productFilter ||
    desde ||
    hasta
  );

  const clearFilters = () => {
    setSearch('');
    setEstadoFilter('Todos');
    setClientFilter('');
    setSellerFilter('');
    setProductFilter('');
    setDesde('');
    setHasta('');
  };

  const onProductChange = (id: string) => {
    if (bloqueado) return;
    const numId = id ? Number(id) : 0;
    const p = products.find((x) => x.id === numId);
    if (p) {
      setForm((f: any) => ({
        ...f,
        productId: p.id,
        productName: p.name,
        precioUnitario: p.basePrice,
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
      precioUnitario: o.precioUnitario,
      estado: o.estado,
    });
    setErrors({});
    setShowForm(true);
  };

  const validate = () => {
    const e: Record<string, string> = {};
    if (!form.clientId) e.clientId = 'Seleccioná un cliente';
    // KAN-16: en alta, el catálogo es la única fuente del producto.
    if (!editing && !form.productId) e.productId = 'Seleccioná un producto del catálogo';
    if (!form.cantidad || form.cantidad < 1) e.cantidad = 'Cantidad inválida';
    if (!form.precioUnitario || form.precioUnitario <= 0) e.precioUnitario = 'Precio inválido';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const save = async () => {
    if (!soloEstado && !validate()) return;
    setSaving(true);
    try {
      if (soloEstado) {
        await api.orders.updateStatus(editing.id, form.estado);
        toast.success('Estado actualizado');
      } else {
        const payload = {
          clientId: form.clientId,
          sellerId: form.sellerId,
          productId: form.productId,
          cantidad: form.cantidad,
          precioUnitario: form.precioUnitario,
          estado: editing ? form.estado : 'Pendiente',
        };
        if (editing) {
          await api.orders.update(editing.id, { ...payload, id: editing.id });
          toast.success('Pedido actualizado');
        } else {
          await api.orders.create(payload);
          toast.success('Pedido creado');
        }
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
        <motion.div initial={reduceMotion ? false : { opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { duration: 0.25 }}>
          <h1 className="text-[32px] font-bold tracking-tight">Pedidos</h1>
          <p className="text-sm text-muted-foreground">Gestión de pedidos de clientes.</p>
        </motion.div>
        {isAdmin && (
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => setShowImport(true)} className="gap-2">
              <Upload className="h-4 w-4" /> Importar CSV
            </Button>
            <Button onClick={openCreate} className="gap-2">
              <Plus className="h-4 w-4" /> Nuevo pedido
            </Button>
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative min-w-[220px] flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input placeholder="Buscar por cliente o producto…" value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9" />
        </div>
        <Select value={clientFilter || 'all'} onValueChange={(v) => setClientFilter(v === 'all' ? '' : v)}>
          <SelectTrigger className="w-full sm:w-48">
            <SelectValue placeholder="Cliente" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los clientes</SelectItem>
            {clients.map((c) => <SelectItem key={c.id} value={String(c.id)}>{c.nombre}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select value={sellerFilter || 'all'} onValueChange={(v) => setSellerFilter(v === 'all' ? '' : v)}>
          <SelectTrigger className="w-full sm:w-48">
            <SelectValue placeholder="Vendedor" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los vendedores</SelectItem>
            {sellers.map((s) => <SelectItem key={s.id} value={String(s.id)}>{s.name}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select value={productFilter || 'all'} onValueChange={(v) => setProductFilter(v === 'all' ? '' : v)}>
          <SelectTrigger className="w-full sm:w-56">
            <SelectValue placeholder="Producto" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los productos</SelectItem>
            {products.map((p) => <SelectItem key={p.id} value={String(p.id)}>{p.name} ({p.sku})</SelectItem>)}
          </SelectContent>
        </Select>
        <Input type="date" aria-label="Desde" value={desde} onChange={(e) => setDesde(e.target.value)} className="w-full sm:w-40" />
        <Input type="date" aria-label="Hasta" value={hasta} onChange={(e) => setHasta(e.target.value)} className="w-full sm:w-40" />
        <Select value={estadoFilter} onValueChange={setEstadoFilter}>
          <SelectTrigger className="w-full sm:w-52">
            <SelectValue placeholder="Estado" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Todos">Todos los estados</SelectItem>
            {ORDER_STATUSES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
          </SelectContent>
        </Select>
        <Button variant="outline" size="sm" onClick={clearFilters} disabled={!hasFilters}>Limpiar filtros</Button>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="space-y-3 p-6">
              {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : orders.length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-12 text-center text-muted-foreground">
              <p>{hasFilters ? 'No se encontraron pedidos con los filtros aplicados.' : 'No hay pedidos.'}</p>
              {hasFilters && <Button variant="outline" size="sm" onClick={clearFilters}>Limpiar filtros</Button>}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <SortableTableHead label="#" sortKey="id" sort={sort} onSort={toggle} />
                  <TableHead>N° ext.</TableHead>
                  <SortableTableHead label="Cliente" sortKey="cliente" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Vendedor" sortKey="vendedor" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Producto" sortKey="producto" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Cant." sortKey="cantidad" sort={sort} onSort={toggle} align="right" className="text-right" />
                  <SortableTableHead label="Precio unit." sortKey="precio" sort={sort} onSort={toggle} align="right" className="text-right" />
                  <SortableTableHead label="Total" sortKey="total" sort={sort} onSort={toggle} align="right" className="text-right" />
                  <SortableTableHead label="Estado" sortKey="estado" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Fecha" sortKey="fecha" sort={sort} onSort={toggle} />
                  {isAdmin && <TableHead className="text-right">Acciones</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {sorted.map((o) => (
                  <TableRow key={o.id}>
                    <TableCell className="font-medium">{o.id}</TableCell>
                    <TableCell className="text-muted-foreground">{o.orderNumber || '-'}</TableCell>
                    <TableCell>{o.clientName}</TableCell>
                    <TableCell className="text-muted-foreground">{o.sellerName || '-'}</TableCell>
                    <TableCell>{o.productName}</TableCell>
                    <TableCell className="text-right tabular-nums">{o.cantidad}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatMoney(o.precioUnitario)}</TableCell>
                    <TableCell className="text-right font-semibold tabular-nums text-amber-700 dark:text-amber-300">{formatMoney(o.total)}</TableCell>
                    <TableCell>
                      <Badge style={{ background: getStatusColor(o.estado as OrderStatus, isDark, palette), color: getStatusTextColor(o.estado as OrderStatus, isDark, palette) }}>
                        {o.estado}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{new Date(o.fecha).toLocaleDateString('es-AR')}</TableCell>
                    {isAdmin && (
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => openEdit(o)}
                            disabled={esTerminal(o.estado as OrderStatus)}
                            title={
                              esTerminal(o.estado as OrderStatus)
                                ? 'Pedido finalizado: no se puede editar ni cambiar el estado'
                                : esEstadoBloqueado(o.estado as OrderStatus)
                                  ? 'Cambiar estado del pedido'
                                  : 'Editar pedido'
                            }
                            aria-label={
                              esEstadoBloqueado(o.estado as OrderStatus)
                                ? 'Cambiar estado del pedido'
                                : 'Editar pedido'
                            }
                          >
                            {esEstadoBloqueado(o.estado as OrderStatus)
                              ? <ArrowRight className="h-3.5 w-3.5" />
                              : <Pencil className="h-3.5 w-3.5" />}
                          </Button>
                          <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => remove(o.id)} disabled={deleting === o.id}>
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
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
        title="Importar pedidos desde CSV"
        templateUrl={api.orders.templateUrl()}
        templateName="plantilla-pedidos.csv"
        onImport={(file) => api.orders.importCsv(file)}
        onImported={load}
      />

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {soloEstado ? `Cambiar estado del pedido #${editing?.id}` : editing ? 'Editar pedido' : 'Nuevo pedido'}
            </DialogTitle>
          </DialogHeader>
          {soloEstado ? (
            <div className="grid gap-4 py-4">
              <p className="text-sm text-muted-foreground">
                El pedido está <strong>{form.estado}</strong>. No se pueden modificar sus datos; sólo se permite cambiar el estado.
              </p>
              <div className="grid gap-2">
                <Label>Estado</Label>
                <Select value={form.estado} onValueChange={(v) => setForm({ ...form, estado: v })}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {opcionesEstado.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
          ) : (
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
                <Select value={form.sellerId ? String(form.sellerId) : 'none'} onValueChange={(v) => setForm({ ...form, sellerId: v === 'none' ? null : Number(v) })}>
                  <SelectTrigger>
                    <SelectValue placeholder="Sin vendedor" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sin vendedor</SelectItem>
                    {sellers.map((s) => <SelectItem key={s.id} value={String(s.id)}>{s.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="grid gap-2">
                <Label>Producto del catálogo</Label>
                <Select value={productSelectValue} onValueChange={onProductChange} disabled={bloqueado}>
                  <SelectTrigger>
                    <SelectValue placeholder={productoDadoDeBaja ? 'Producto dado de baja' : 'Seleccionar producto…'} />
                  </SelectTrigger>
                  <SelectContent>
                    {productoDadoDeBaja && (
                      <SelectItem value={productSelectValue}>{form.productName} (dado de baja)</SelectItem>
                    )}
                    {products.map((p) => <SelectItem key={p.id} value={String(p.id)}>{p.name} ({p.sku})</SelectItem>)}
                  </SelectContent>
                </Select>
                {errors.productId && <p className="text-sm text-destructive">{errors.productId}</p>}
                {bloqueado && (
                  <p className="text-xs text-muted-foreground">El producto del pedido no se puede cambiar.</p>
                )}
              </div>
              <div className="grid gap-2">
                <Label>Producto</Label>
                {form.productName ? (
                  <p className="rounded-md border bg-muted/30 px-3 py-2 text-sm">{form.productName}</p>
                ) : (
                  <p className="text-sm text-muted-foreground">Elegí un producto del catálogo.</p>
                )}
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="grid gap-2">
                  <Label>Cantidad</Label>
                  <Input type="number" min={1} value={form.cantidad} onChange={(e) => setForm({ ...form, cantidad: Number(e.target.value) })} />
                  {errors.cantidad && <p className="text-sm text-destructive">{errors.cantidad}</p>}
                </div>
                <div className="grid gap-2">
                  <Label>Precio unitario</Label>
                  <Input
                    type="number"
                    step="0.01"
                    min={0}
                    value={form.precioUnitario}
                    readOnly={bloqueado}
                    onChange={bloqueado ? undefined : (e) => setForm({ ...form, precioUnitario: Number(e.target.value) })}
                    className={bloqueado ? 'bg-muted/50 text-muted-foreground' : ''}
                  />
                  {bloqueado ? (
                    <p className="text-xs text-muted-foreground">Precio histórico: no se puede modificar.</p>
                  ) : (
                    errors.precioUnitario && <p className="text-sm text-destructive">{errors.precioUnitario}</p>
                  )}
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
                      {opcionesEstado.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
                    </SelectContent>
                  </Select>
                  <p className="text-xs text-muted-foreground">Transiciones permitidas: {opcionesEstado.join(', ')}.</p>
                </div>
              )}
              {!editing && (
                <p className="text-sm text-muted-foreground">El pedido se crea en estado <strong>Pendiente</strong>.</p>
              )}
              <div className="rounded-lg bg-muted/50 p-3 text-sm">
                Total: <strong className="text-amber-700 dark:text-amber-300">{formatMoney(form.cantidad * form.precioUnitario)}</strong>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowForm(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={save} disabled={saving}>
              {saving ? 'Guardando…' : soloEstado ? 'Actualizar estado' : editing ? 'Actualizar' : 'Crear'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
