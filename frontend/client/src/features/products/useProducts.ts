import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import type { PriceAdjustmentPreviewResponse, Product } from '../../types';
import { productsFeatureApi } from './api';
import {
  emptyProductForm,
  validateImageFile,
  validateProductForm,
  type ProductFormValues,
} from './model';

/** Datos y acciones del slice Productos: catálogo, filtros, alta/edición, imágenes, ajuste de precios y CSV. */
export function useProducts() {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);

  const [sku, setSku] = useState('');
  const [nombre, setNombre] = useState('');
  const [category, setCategory] = useState('Todos');
  const [onlyInStock, setOnlyInStock] = useState(false);

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [form, setForm] = useState<ProductFormValues>(emptyProductForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);

  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [removeImage, setRemoveImage] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [showAdjust, setShowAdjust] = useState(false);
  const [adjustPercent, setAdjustPercent] = useState('10');
  const [adjustPreview, setAdjustPreview] = useState<PriceAdjustmentPreviewResponse | null>(null);
  const [adjustLoading, setAdjustLoading] = useState(false);
  const [adjustApplying, setAdjustApplying] = useState(false);
  const [adjustError, setAdjustError] = useState<string | null>(null);

  const [showImport, setShowImport] = useState(false);
  const firstLoad = useRef(true);

  const load = async () => {
    if (firstLoad.current) setLoading(true);
    try {
      const data = await productsFeatureApi.list(
        undefined,
        category,
        onlyInStock,
        sku || undefined,
        nombre || undefined,
      );
      setProducts(data);
    } catch {
      toast.error('Error al cargar productos');
    } finally {
      setLoading(false);
      firstLoad.current = false;
    }
  };

  const loadCategories = () => {
    productsFeatureApi.categories().then(setCategories).catch(() => {});
  };

  useEffect(() => {
    const t = setTimeout(() => void load(), sku || nombre ? 300 : 0);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sku, nombre, category, onlyInStock]);

  useEffect(() => {
    loadCategories();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(
    () => () => {
      if (imagePreview?.startsWith('blob:')) URL.revokeObjectURL(imagePreview);
    },
    [imagePreview],
  );

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

  const openCreate = () => {
    setEditing(null);
    setForm(emptyProductForm);
    setErrors({});
    resetImageState();
    setShowForm(true);
  };

  const openEdit = (p: Product) => {
    setEditing(p);
    setForm({
      sku: p.sku,
      name: p.name,
      category: p.category,
      basePrice: p.basePrice,
      cost: p.cost,
      stock: p.stock,
      minStockAlert: p.minStockAlert,
      active: p.active,
    });
    setErrors({});
    resetImageState(p.imageUrl);
    setShowForm(true);
  };

  const selectImage = (file?: File) => {
    if (!file) return;
    const imageError = validateImageFile(file);
    if (imageError) {
      setErrors((prev) => ({ ...prev, image: imageError }));
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

  const save = async () => {
    const nextErrors = validateProductForm(form);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;
    setSaving(true);
    try {
      let id: number;
      if (editing) {
        await productsFeatureApi.update(editing.id, { ...form, id: editing.id });
        id = editing.id;
      } else {
        const created = await productsFeatureApi.create(form);
        id = created.id;
      }

      // KAN-23: si falla la imagen, el producto ya quedó guardado y se avisa sin perder el alta.
      if (imageFile) {
        try {
          await productsFeatureApi.uploadImage(id, imageFile);
        } catch (err: unknown) {
          toast.warning('Producto guardado, pero no se pudo subir la imagen', {
            description: err instanceof Error ? err.message : 'Error inesperado',
          });
        }
      } else if (removeImage && editing) {
        try {
          await productsFeatureApi.removeImage(id);
        } catch (err: unknown) {
          toast.warning('Producto guardado, pero no se pudo quitar la imagen', {
            description: err instanceof Error ? err.message : 'Error inesperado',
          });
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
      const preview = await productsFeatureApi.previewPriceAdjustment(percent);
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
      const result = await productsFeatureApi.applyPriceAdjustment(percent);
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
      await productsFeatureApi.remove(id);
      toast.success('Producto eliminado');
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setDeleting(null);
    }
  };

  const onImported = async () => {
    await load();
    loadCategories();
  };

  return {
    products,
    categories,
    loading,
    sku,
    setSku,
    nombre,
    setNombre,
    category,
    setCategory,
    onlyInStock,
    setOnlyInStock,
    hasFilters,
    clearFilters,
    showForm,
    setShowForm,
    editing,
    form,
    setForm,
    errors,
    saving,
    deleting,
    imageFile,
    imagePreview,
    removeImage,
    fileInputRef,
    openCreate,
    openEdit,
    selectImage,
    clearImage,
    save,
    showAdjust,
    setShowAdjust,
    adjustPercent,
    setAdjustPercent,
    adjustPreview,
    setAdjustPreview,
    adjustLoading,
    adjustApplying,
    adjustError,
    setAdjustError,
    openAdjust,
    previewAdjust,
    applyAdjust,
    showImport,
    setShowImport,
    remove,
    load,
    onImported,
    templateUrl: productsFeatureApi.templateUrl,
    importCsv: productsFeatureApi.importCsv,
  };
}
