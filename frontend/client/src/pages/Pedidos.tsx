import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { ORDER_STATUSES, STATUS_COLORS, type OrderStatus } from '../types';
import { Plus, Pencil, Trash2, Search } from 'lucide-react';

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
      /* ignore */
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

  const onProductChange = (id: number) => {
    const p = products.find((x) => x.id === id);
    if (p) {
      setForm((f: any) => ({
        ...f,
        productId: p.id,
        productName: p.name,
        precioUnitario: toDisplay(p.basePrice),
      }));
    } else {
      setForm((f: any) => ({ ...f, productId: null, productName: '' }));
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
      } else {
        await api.orders.create(payload);
      }
      setShowForm(false);
      await load();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este pedido?')) return;
    setDeleting(id);
    try {
      await api.orders.remove(id);
      await load();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setDeleting(null);
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <h1>Pedidos</h1>
        <p className="page-sub">Gestión de pedidos de clientes.</p>
      </div>

      <div className="toolbar">
        <div className="search-box">
          <Search size={16} />
          <input placeholder="Buscar por cliente o producto…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <select className="filter" value={estadoFilter} onChange={(e) => setEstadoFilter(e.target.value)}>
          <option value="Todos">Todos los estados</option>
          {ORDER_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <button className="btn btn-primary" onClick={openCreate}>
          <Plus size={16} /> Nuevo pedido
        </button>
      </div>

      <div className="card table-card">
        {loading ? (
          <div className="loading">Cargando pedidos…</div>
        ) : orders.length === 0 ? (
          <div className="empty">No hay pedidos.</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>#</th>
                <th>Cliente</th>
                <th>Vendedor</th>
                <th>Producto</th>
                <th>Cant.</th>
                <th>Precio unit.</th>
                <th>Total</th>
                <th>Estado</th>
                <th>Fecha</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id}>
                  <td>{o.id}</td>
                  <td>{o.clientName}</td>
                  <td>{o.sellerName || '-'}</td>
                  <td>{o.productName}</td>
                  <td>{o.cantidad}</td>
                  <td>{formatMoney(o.precioUnitario)}</td>
                  <td className="total">{formatMoney(o.total)}</td>
                  <td>
                    <span className="badge" style={{ background: STATUS_COLORS[o.estado as OrderStatus] }}>
                      {o.estado}
                    </span>
                  </td>
                  <td>{new Date(o.fecha).toLocaleDateString('es-AR')}</td>
                  <td className="actions">
                    <button className="btn btn-sm btn-edit" onClick={() => openEdit(o)}><Pencil size={14} /></button>
                    <button className="btn btn-sm btn-delete" onClick={() => remove(o.id)} disabled={deleting === o.id}>
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showForm && (
        <div className="modal-backdrop" onClick={() => setShowForm(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editing ? 'Editar pedido' : 'Nuevo pedido'}</h2>
              <button className="close" onClick={() => setShowForm(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-grid">
                <label>
                  Cliente
                  <select value={form.clientId} onChange={(e) => setForm({ ...form, clientId: Number(e.target.value) })}>
                    {clients.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
                  </select>
                  {errors.clientId && <span className="err">{errors.clientId}</span>}
                </label>
                <label>
                  Vendedor
                  <select value={form.sellerId || ''} onChange={(e) => setForm({ ...form, sellerId: e.target.value ? Number(e.target.value) : null })}>
                    <option value="">Sin vendedor</option>
                    {sellers.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </label>
                <label className="full">
                  Producto del catálogo
                  <select value={form.productId || ''} onChange={(e) => onProductChange(e.target.value ? Number(e.target.value) : 0)}>
                    <option value="">Seleccionar producto…</option>
                    {products.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.sku})</option>)}
                  </select>
                </label>
                <label className="full">
                  Producto
                  <input value={form.productName} onChange={(e) => setForm({ ...form, productName: e.target.value })} />
                  {errors.productName && <span className="err">{errors.productName}</span>}
                </label>
                <label>
                  Cantidad
                  <input type="number" min={1} value={form.cantidad} onChange={(e) => setForm({ ...form, cantidad: Number(e.target.value) })} />
                  {errors.cantidad && <span className="err">{errors.cantidad}</span>}
                </label>
                <label>
                  Precio unitario
                  <input type="number" step="0.01" min={0} value={form.precioUnitario} onChange={(e) => setForm({ ...form, precioUnitario: Number(e.target.value) })} />
                  {errors.precioUnitario && <span className="err">{errors.precioUnitario}</span>}
                </label>
                {editing && (
                  <label>
                    Estado
                    <select value={form.estado} onChange={(e) => setForm({ ...form, estado: e.target.value })}>
                      {ORDER_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
                    </select>
                  </label>
                )}
                {!editing && (
                  <div className="total-preview full">
                    El pedido se crea en estado <strong>Pendiente</strong>.
                  </div>
                )}
                <div className="total-preview full">
                  Total: <strong>{formatMoney(toArs(form.cantidad * form.precioUnitario))}</strong>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={() => setShowForm(false)} disabled={saving}>Cancelar</button>
              <button className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Guardando…' : editing ? 'Actualizar' : 'Crear'}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
