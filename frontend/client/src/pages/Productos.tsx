import { useEffect, useRef, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { useAuth } from '../context/AuthContext';
import { api } from '../api';
import type { PriceAdjustmentPreviewResponse, Product } from '../types';
import { Plus, Pencil, Trash2, Image as ImageIcon, Percent, Upload } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Checkbox } from '@/components/ui/checkbox';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import SortableTableHead from '@/components/SortableTableHead';
import ProductImage from '@/components/ProductImage';
import { useTableSort } from '@/hooks/useTableSort';
import { toast } from 'sonner';
import { motion, useReducedMotion } from 'framer-motion';
import CsvImportDialog from '@/components/CsvImportDialog';

const emptyForm = { sku: '', name: '', category: 'General', basePrice: 0, cost: 0, stock: 0, minStockAlert: 5, active: true };
const IMAGE_MAX_BYTES = 2 * 1024 * 1024;
const IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp';

export default function Productos() {
  const { formatMoney } = useSettings();
  const reduceMotion = useReducedMotion();
  const { user } = useAuth();
  const isAdmin = user?.roles?.includes('Admin');
  const [products, setProducts] = useState<any[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [sku, setSku] = useState('');
  const [nombre, setNombre] = useState('');
  const [category, setCategory] = useState('Todos');
  const [onlyInStock, setOnlyInStock] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [form, setForm] = useState<any>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [removeImage, setRemoveImage] = useState(false);
  const [showAdjust, setShowAdjust] = useState(false);
  const [adjustPercent, setAdjustPercent] = useState('10');
  const [adjustPreview, setAdjustPreview] = useState<PriceAdjustmentPreviewResponse | null>(null);
  const [adjustLoading, setAdjustLoading] = useState(false);
  const [adjustApplying, setAdjustApplying] = useState(false);
  const [adjustError, setAdjustError] = useState<string | null>(null);
  const [showImport, setShowImport] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const firstLoad = useRef(true);

  const { sorted, sort, toggle } = useTableSort(products, {
    sku: (p) => p.sku,
    nombre: (p) => p.name,
    categoria: (p) => p.category,
    precio: (p) => p.basePrice,
    stock: (p) => p.stock,
  });

  const load = async () => {
    if (firstLoad.current) setLoading(true);
    try {
      const data = await api.products.list(
        undefined,
        category,
        onlyInStock,
        sku || undefined,
        nombre || undefined
      );
      setProducts(data);
    } catch {
      toast.error('Error al cargar productos');
    } finally {
      setLoading(false);
      firstLoad.current = false;
    }
  };

  useEffect(() => {
    const t = setTimeout(load, sku || nombre ? 300 : 0);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sku, nombre, category, onlyInStock]);
  useEffect(() => { api.products.categories().then(setCategories).catch(() => {}); }, []);
  useEffect(() => () => {
    if (imagePreview?.startsWith('blob:')) URL.revokeObjectURL(imagePreview);
  }, [imagePreview]);

  const hasFilters = !!(sku || nombre || category !== 'Todos' || onlyInStock);

  const clearFilters = () => {
    setSku('');
    setNombre('');
    setCategory('Todos');
    setOnlyInStock(false);
  };

  const resetImageState = (url?: string | null) => {
    setImageFile(null);
    setImagePreview(url ?? null);
    setRemoveImage(false);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const openCreate = () => { setEditing(null); setForm(emptyForm); setErrors({}); resetImageState(); setShowForm(true); };
  const openEdit = (p: Product) => {
    setEditing(p);
    setForm({ sku: p.sku, name: p.name, category: p.category, basePrice: p.basePrice, cost: p.cost, stock: p.stock, minStockAlert: p.minStockAlert, active: p.active });
    setErrors({});
    resetImageState(p.imageUrl);
    setShowForm(true);
  };

  const selectImage = (file?: File) => {
    if (!file) return;
    if (file.size > IMAGE_MAX_BYTES) {
      setErrors((prev) => ({ ...prev, image: 'La imagen no puede superar los 2 MB.' }));
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }
    if (!IMAGE_ACCEPT.split(',').includes(file.type)) {
      setErrors((prev) => ({ ...prev, image: 'Formato no admitido. Use JPG, PNG o WebP.' }));
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }
    setErrors((prev) => ({ ...prev, image: '' }));
    setImageFile(file);
    setRemoveImage(false);
    setImagePreview(URL.createObjectURL(file));
  };

  const clearImage = () => {
    setImageFile(null);
    setImagePreview(null);
    setRemoveImage(true);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const validate = () => {
    const e: Record<string, string> = {};
    if (!form.sku?.trim()) e.sku = 'SKU obligatorio';
    if (!form.name?.trim()) e.name = 'Nombre obligatorio';
    if (!form.category?.trim()) e.category = 'Categoría obligatoria';
    if (!form.basePrice || form.basePrice <= 0) e.basePrice = 'Precio inválido';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const save = async () => {
    if (!validate()) return;
    setSaving(true);
    try {
      let id: number;
      if (editing) {
        await api.products.update(editing.id, { ...form, id: editing.id });
        id = editing.id;
      } else {
        const created = await api.products.create(form);
        id = created.id;
      }

      // KAN-23: si falla la imagen, el producto ya quedó guardado y se avisa sin perder el alta.
      if (imageFile) {
        try {
          await api.products.uploadImage(id, imageFile);
        } catch (err: unknown) {
          toast.warning('Producto guardado, pero no se pudo subir la imagen', { description: err instanceof Error ? err.message : 'Error inesperado' });
        }
      } else if (removeImage && editing) {
        try {
          await api.products.removeImage(id);
        } catch (err: unknown) {
          toast.warning('Producto guardado, pero no se pudo quitar la imagen', { description: err instanceof Error ? err.message : 'Error inesperado' });
        }
      }

      toast.success(editing ? 'Producto actualizado' : 'Producto creado');
      setShowForm(false);
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setSaving(false);
    }
  };

  const openAdjust = () => {
    setAdjustPercent('10');
    setAdjustPreview(null);
    setAdjustError(null);
    setShowAdjust(true);
  };

  const previewAdjust = async () => {
    const percent = Number(adjustPercent);
    if (!Number.isFinite(percent)) {
      setAdjustError('Ingresá un porcentaje válido.');
      return;
    }
    if (percent < -90 || percent > 200) {
      setAdjustError('El porcentaje debe estar entre -90 y 200.');
      return;
    }
    setAdjustLoading(true);
    setAdjustError(null);
    try {
      const preview = await api.products.previewPriceAdjustment(percent);
      setAdjustPreview(preview);
      if (preview.errors.length > 0) setAdjustError(preview.errors[0]);
    } catch (err: unknown) {
      setAdjustPreview(null);
      setAdjustError(err instanceof Error ? err.message : 'No se pudo generar la vista previa.');
    } finally {
      setAdjustLoading(false);
    }
  };

  const applyAdjust = async () => {
    const percent = Number(adjustPercent);
    if (!Number.isFinite(percent) || !adjustPreview) return;
    setAdjustApplying(true);
    setAdjustError(null);
    try {
      const result = await api.products.applyPriceAdjustment(percent);
      toast.success(`Precios actualizados: ${result.affectedCount} producto(s) activo(s).`);
      setShowAdjust(false);
      setAdjustPreview(null);
      await load();
    } catch (err: unknown) {
      setAdjustError(err instanceof Error ? err.message : 'No se pudo aplicar el ajuste.');
    } finally {
      setAdjustApplying(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este producto?')) return;
    setDeleting(id);
    try {
      await api.products.remove(id);
      toast.success('Producto eliminado');
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
          <h1 className="text-[32px] font-bold tracking-tight">Productos</h1>
          <p className="text-sm text-muted-foreground">Catálogo de productos y control de stock.</p>
        </motion.div>
        {isAdmin && (
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => setShowImport(true)} className="gap-2">
              <Upload className="h-4 w-4" /> Importar CSV
            </Button>
            <Button variant="outline" onClick={openAdjust} className="gap-2">
              <Percent className="h-4 w-4" /> Actualizar precios
            </Button>
            <Button onClick={openCreate} className="gap-2">
              <Plus className="h-4 w-4" /> Nuevo producto
            </Button>
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <Input placeholder="Filtrar por SKU…" aria-label="SKU" value={sku} onChange={(e) => setSku(e.target.value)} className="w-full sm:w-48" />
        <Input placeholder="Filtrar por nombre…" aria-label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} className="w-full sm:w-56" />
        <Select value={category} onValueChange={setCategory}>
          <SelectTrigger className="w-full sm:w-48">
            <SelectValue placeholder="Categoría" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Todos">Todas las categorías</SelectItem>
            {categories.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
          </SelectContent>
        </Select>
        <div className="flex items-center gap-2">
          <Checkbox id="stock" checked={onlyInStock} onCheckedChange={(v) => setOnlyInStock(!!v)} />
          <Label htmlFor="stock" className="text-sm">Solo con stock</Label>
        </div>
        <Button variant="outline" size="sm" onClick={clearFilters} disabled={!hasFilters}>Limpiar filtros</Button>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="space-y-3 p-6">{[...Array(5)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}</div>
          ) : products.length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-12 text-center text-muted-foreground">
              <p>{hasFilters ? 'No se encontraron productos para los criterios de búsqueda.' : 'No hay productos.'}</p>
              {hasFilters && <Button variant="outline" size="sm" onClick={clearFilters}>Limpiar filtros</Button>}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead>Imagen</TableHead>
                  <SortableTableHead label="SKU" sortKey="sku" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Nombre" sortKey="nombre" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Categoría" sortKey="categoria" sort={sort} onSort={toggle} />
                  <SortableTableHead label="Precio base" sortKey="precio" sort={sort} onSort={toggle} align="right" className="text-right" />
                  <SortableTableHead label="Stock" sortKey="stock" sort={sort} onSort={toggle} align="right" className="text-right" />
                  {isAdmin && <TableHead className="text-right">Acciones</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {sorted.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell><ProductImage url={p.imageUrl} name={p.name} /></TableCell>
                    <TableCell className="font-mono text-xs">{p.sku}</TableCell>
                    <TableCell className="font-medium">{p.name}</TableCell>
                    <TableCell>{p.category}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatMoney(p.basePrice)}</TableCell>
                    <TableCell className="text-right">
                      <Badge variant={p.isCriticalStock ? 'destructive' : 'secondary'} className="tabular-nums">{p.stock}</Badge>
                    </TableCell>
                    {isAdmin && (
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => openEdit(p)}><Pencil className="h-3.5 w-3.5" /></Button>
                          <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => remove(p.id)} disabled={deleting === p.id}><Trash2 className="h-3.5 w-3.5" /></Button>
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
        title="Importar productos desde CSV"
        templateUrl={api.products.templateUrl()}
        templateName="plantilla-productos.csv"
        onImport={(file) => api.products.importCsv(file)}
        onImported={async () => { await load(); api.products.categories().then(setCategories).catch(() => {}); }}
      />

      <Dialog open={showAdjust} onOpenChange={(open) => { if (!adjustApplying) { setShowAdjust(open); if (!open) { setAdjustPreview(null); setAdjustError(null); } } }}>
        <DialogContent className="sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Actualizar precios masivamente</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="rounded-md border border-border/60 bg-muted/20 p-3 text-sm text-muted-foreground">
              Aplica un porcentaje al <span className="font-medium text-foreground">precio base</span> de productos <span className="font-medium text-foreground">activos</span>. No modifica costo ni pedidos históricos. La vista previa es informativa: al aplicar, el servidor recalcula sobre el estado actual.
            </div>
            <div className="grid gap-2 sm:max-w-xs">
              <Label htmlFor="adjust-percent">Porcentaje (%)</Label>
              <Input
                id="adjust-percent"
                type="number"
                step="0.01"
                min={-90}
                max={200}
                value={adjustPercent}
                onChange={(e) => { setAdjustPercent(e.target.value); setAdjustPreview(null); setAdjustError(null); }}
                disabled={adjustLoading || adjustApplying}
              />
              <p className="text-xs text-muted-foreground">Rango permitido: -90 a 200. Ej.: 10 = +10%, -5 = -5%.</p>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={previewAdjust} disabled={adjustLoading || adjustApplying}>
                {adjustLoading ? 'Calculando…' : 'Vista previa'}
              </Button>
            </div>
            {adjustError && <p className="text-sm text-destructive">{adjustError}</p>}
            {adjustPreview && (
              <div className="space-y-3">
                <div className="flex flex-wrap gap-3 text-sm">
                  <Badge variant="secondary">Afectados: {adjustPreview.affectedCount}</Badge>
                  <Badge variant="outline">Inactivos excluidos: {adjustPreview.skippedInactiveCount}</Badge>
                  <Badge variant="outline">Factor: {adjustPreview.factor.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}</Badge>
                </div>
                {adjustPreview.errors.length > 0 && (
                  <div className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                    <p className="font-medium">No se puede aplicar: hay precios fuera de rango. No se realizará ningún cambio.</p>
                    <ul className="mt-2 list-disc pl-5">
                      {adjustPreview.errors.slice(0, 8).map((e, i) => <li key={i}>{e}</li>)}
                    </ul>
                  </div>
                )}
                <div className="max-h-72 overflow-auto rounded-md border">
                  <Table>
                    <TableHeader>
                      <TableRow className="hover:bg-transparent">
                        <TableHead>SKU</TableHead>
                        <TableHead>Producto</TableHead>
                        <TableHead className="text-right">Precio actual</TableHead>
                        <TableHead className="text-right">Precio nuevo</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {adjustPreview.items.length === 0 ? (
                        <TableRow><TableCell colSpan={4} className="text-center text-muted-foreground">No hay productos activos para ajustar.</TableCell></TableRow>
                      ) : adjustPreview.items.map((item) => (
                        <TableRow key={item.id}>
                          <TableCell className="font-mono text-xs">{item.sku}</TableCell>
                          <TableCell>{item.name}</TableCell>
                          <TableCell className="text-right tabular-nums">{formatMoney(item.currentBasePrice)}</TableCell>
                          <TableCell className="text-right tabular-nums font-medium">{formatMoney(item.newBasePrice)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
                {adjustPreview.affectedCount > adjustPreview.items.length && (
                  <p className="text-xs text-muted-foreground">Mostrando {adjustPreview.items.length} de {adjustPreview.affectedCount} productos afectados.</p>
                )}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAdjust(false)} disabled={adjustApplying}>Cancelar</Button>
            <Button onClick={applyAdjust} disabled={adjustApplying || adjustLoading || !adjustPreview || adjustPreview.errors.length > 0 || adjustPreview.affectedCount === 0}>
              {adjustApplying ? 'Aplicando…' : 'Aplicar ajuste'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{editing ? 'Editar producto' : 'Nuevo producto'}</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="product-image">Imagen (opcional)</Label>
              <div className="flex items-center gap-3">
                <div className="h-16 w-16 shrink-0 overflow-hidden rounded-md border bg-muted">
                  {imagePreview ? (
                    <img src={imagePreview} alt="Vista previa" className="h-full w-full object-cover" />
                  ) : (
                    <div className="flex h-full w-full items-center justify-center text-muted-foreground">
                      <ImageIcon className="h-5 w-5" aria-hidden="true" />
                      <span className="sr-only">Sin imagen</span>
                    </div>
                  )}
                </div>
                <div className="flex-1 space-y-2">
                  <Input
                    id="product-image"
                    ref={fileInputRef}
                    type="file"
                    accept={IMAGE_ACCEPT}
                    onChange={(e) => selectImage(e.target.files?.[0])}
                    aria-label="Seleccionar imagen del producto"
                  />
                  <p className="text-xs text-muted-foreground">JPG, PNG o WebP · máximo 2 MB</p>
                  {(imagePreview || editing?.imageUrl) && (
                    <Button type="button" variant="ghost" size="sm" onClick={clearImage} disabled={saving}>
                      Quitar imagen
                    </Button>
                  )}
                </div>
              </div>
              {errors.image && <p className="text-sm text-destructive">{errors.image}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label>SKU</Label>
                <Input value={form.sku} onChange={(e) => setForm({ ...form, sku: e.target.value })} />
                {errors.sku && <p className="text-sm text-destructive">{errors.sku}</p>}
              </div>
              <div className="grid gap-2">
                <Label>Nombre</Label>
                <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
                {errors.name && <p className="text-sm text-destructive">{errors.name}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label>Categoría</Label>
                <Input value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })} />
                {errors.category && <p className="text-sm text-destructive">{errors.category}</p>}
              </div>
              <div className="grid gap-2">
                <Label>Precio base</Label>
                <Input type="number" step="0.01" min={0} value={form.basePrice} onChange={(e) => setForm({ ...form, basePrice: Number(e.target.value) })} />
                {errors.basePrice && <p className="text-sm text-destructive">{errors.basePrice}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label>Costo</Label>
                <Input type="number" step="0.01" min={0} value={form.cost} onChange={(e) => setForm({ ...form, cost: Number(e.target.value) })} />
              </div>
              <div className="grid gap-2">
                <Label>Stock</Label>
                <Input type="number" min={0} value={form.stock} onChange={(e) => setForm({ ...form, stock: Number(e.target.value) })} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label>Stock mínimo (alerta)</Label>
                <Input type="number" min={0} value={form.minStockAlert} onChange={(e) => setForm({ ...form, minStockAlert: Number(e.target.value) })} />
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
