import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import type { Client, Order, Product, Seller } from '../../types';
import { clientsFeatureApi } from '../clients/api';
import { productsFeatureApi } from '../products/api';
import { sellersFeatureApi } from '../sellers/api';
import { ordersFeatureApi } from './api';
import {
  buildOrderPayload,
  deriveOrderFormFlags,
  emptyOrderForm,
  validateOrderForm,
  type OrderFormValues,
} from './model';

// Cache compartido entre montajes, igual que en la page original.
let cachedOrders: Order[] | null = null;

/**
 * Toda la lógica de datos y acciones del slice Pedidos:
 * listado con filtros, catálogos, alta/edición/cambio de estado/baja,
 * importación CSV y plantilla. La page queda como capa visual.
 */
export function useOrders() {
  const [orders, setOrders] = useState<Order[]>(cachedOrders || []);
  const [clients, setClients] = useState<Client[]>([]);
  const [sellers, setSellers] = useState<Seller[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(!cachedOrders);

  const [search, setSearch] = useState('');
  const [estadoFilter, setEstadoFilter] = useState('Todos');
  const [clientFilter, setClientFilter] = useState('');
  const [sellerFilter, setSellerFilter] = useState('');
  const [productFilter, setProductFilter] = useState('');
  const [desde, setDesde] = useState('');
  const [hasta, setHasta] = useState('');

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Order | null>(null);
  const [form, setForm] = useState<OrderFormValues>(emptyOrderForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);
  const [showImport, setShowImport] = useState(false);

  const load = async () => {
    if (!cachedOrders) setLoading(true);
    try {
      const data = await ordersFeatureApi.list(
        search,
        estadoFilter,
        clientFilter === '' ? undefined : Number(clientFilter),
        sellerFilter === '' ? undefined : Number(sellerFilter),
        productFilter === '' ? undefined : Number(productFilter),
        desde || undefined,
        hasta || undefined,
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
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [estadoFilter, clientFilter, sellerFilter, productFilter, desde, hasta]);

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

  useEffect(() => {
    clientsFeatureApi.list().then(setClients).catch(() => {});
    sellersFeatureApi.list(true).then(setSellers).catch(() => {});
    productsFeatureApi.list().then(setProducts).catch(() => {});
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

  const flags = deriveOrderFormFlags(editing, form, products);

  const onProductChange = (id: string) => {
    if (flags.bloqueado) return;
    const numId = id ? Number(id) : 0;
    const p = products.find((x) => x.id === numId);
    if (p) {
      setForm((f) => ({
        ...f,
        productId: p.id,
        productName: p.name,
        precioUnitario: p.basePrice,
      }));
    } else {
      setForm((f) => ({ ...f, productId: null }));
    }
  };

  const openCreate = () => {
    setEditing(null);
    setForm({ ...emptyOrderForm, clientId: clients[0]?.id || 0, sellerId: sellers[0]?.id || null });
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (o: Order) => {
    setEditing(o);
    setForm({
      clientId: o.clientId,
      sellerId: o.sellerId ?? null,
      productId: o.productId ?? null,
      productName: o.productName,
      cantidad: o.cantidad,
      precioUnitario: o.precioUnitario,
      estado: o.estado,
    });
    setErrors({});
    setShowForm(true);
  };

  const save = async () => {
    if (!flags.soloEstado) {
      const nextErrors = validateOrderForm(form, editing);
      setErrors(nextErrors);
      if (Object.keys(nextErrors).length > 0) return;
    }
    setSaving(true);
    try {
      if (flags.soloEstado) {
        await ordersFeatureApi.updateStatus(editing!.id, form.estado);
        toast.success('Estado actualizado');
      } else {
        const payload = buildOrderPayload(form, editing);
        if (editing) {
          await ordersFeatureApi.update(editing.id, { ...payload, id: editing.id });
          toast.success('Pedido actualizado');
        } else {
          await ordersFeatureApi.create(payload);
          toast.success('Pedido creado');
        }
      }
      setShowForm(false);
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este pedido?')) return;
    setDeleting(id);
    try {
      await ordersFeatureApi.remove(id);
      toast.success('Pedido eliminado');
      await load();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Error inesperado');
    } finally {
      setDeleting(null);
    }
  };

  return {
    // Datos y catálogos
    orders,
    clients,
    sellers,
    products,
    loading,
    // Filtros
    search,
    setSearch,
    estadoFilter,
    setEstadoFilter,
    clientFilter,
    setClientFilter,
    sellerFilter,
    setSellerFilter,
    productFilter,
    setProductFilter,
    desde,
    setDesde,
    hasta,
    setHasta,
    hasFilters,
    clearFilters,
    // Diálogo y formulario
    showForm,
    setShowForm,
    editing,
    form,
    setForm,
    errors,
    saving,
    deleting,
    showImport,
    setShowImport,
    ...flags,
    onProductChange,
    openCreate,
    openEdit,
    // Acciones
    save,
    remove,
    load,
    templateUrl: ordersFeatureApi.templateUrl,
    importCsv: ordersFeatureApi.importCsv,
  };
}
