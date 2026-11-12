import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { Plus, Pencil, Trash2, Search } from 'lucide-react';

const emptyForm = { sku: '', name: '', category: 'General', basePrice: 0, cost: 0, stock: 0, minStockAlert: 5, active: true };

export default function Productos() {
  const { formatMoney } = useSettings();
  const [products, setProducts] = useState<any[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('Todos');
  const [onlyInStock, setOnlyInStock] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<any | null>(null);
  const [form, setForm] = useState<any>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await api.products.list(search, category, onlyInStock);
      setProducts(data);
    } catch {
      /* ignore */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [category, onlyInStock]);

  useEffect(() => {
    const t = setTimeout(() => load(), 300);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  useEffect(() => {
    api.products.categories().then(setCategories).catch(() => {});
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (p: any) => {
    setEditing(p);
    setForm({ sku: p.sku, name: p.name, category: p.category, basePrice: p.basePrice, cost: p.cost, stock: p.stock, minStockAlert: p.minStockAlert, active: p.active });
    setErrors({});
    setShowForm(true);
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
      if (editing) await api.products.update(editing.id, { ...form, id: editing.id });
      else await api.products.create(form);
      setShowForm(false);
      await load();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este producto?')) return;
    setDeleting(id);
    try {
      await api.products.remove(id);
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
        <h1>Productos</h1>
        <p className="page-sub">Catálogo de productos y control de stock.</p>
      </div>

      <div className="toolbar">
        <div className="search-box">
          <Search size={16} />
          <input placeholder="Buscar por nombre o SKU…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <select className="filter" value={category} onChange={(e) => setCategory(e.target.value)}>
          <option value="Todos">Todas las categorías</option>
          {categories.map((c) => <option key={c} value={c}>{c}</option>)}
        </select>
        <label className="checkbox">
          <input type="checkbox" checked={onlyInStock} onChange={(e) => setOnlyInStock(e.target.checked)} />
          Solo con stock
        </label>
        <button className="btn btn-primary" onClick={openCreate}>
          <Plus size={16} /> Nuevo producto
        </button>
      </div>

      <div className="card table-card">
        {loading ? (
          <div className="loading">Cargando productos…</div>
        ) : products.length === 0 ? (
          <div className="empty">No hay productos.</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Nombre</th>
                <th>Categoría</th>
                <th>Precio base</th>
                <th>Stock</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {products.map((p) => (
                <tr key={p.id}>
                  <td>{p.sku}</td>
                  <td>{p.name}</td>
                  <td>{p.category}</td>
                  <td>{formatMoney(p.basePrice)}</td>
                  <td>
                    <span className={'badge ' + (p.isCriticalStock ? 'badge-danger' : 'badge-ok')}>
                      {p.stock}
                    </span>
                  </td>
                  <td className="actions">
                    <button className="btn btn-sm btn-edit" onClick={() => openEdit(p)}><Pencil size={14} /></button>
                    <button className="btn btn-sm btn-delete" onClick={() => remove(p.id)} disabled={deleting === p.id}>
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
              <h2>{editing ? 'Editar producto' : 'Nuevo producto'}</h2>
              <button className="close" onClick={() => setShowForm(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-grid">
                <label>
                  SKU
                  <input value={form.sku} onChange={(e) => setForm({ ...form, sku: e.target.value })} />
                  {errors.sku && <span className="err">{errors.sku}</span>}
                </label>
                <label>
                  Nombre
                  <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
                  {errors.name && <span className="err">{errors.name}</span>}
                </label>
                <label>
                  Categoría
                  <input value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })} />
                  {errors.category && <span className="err">{errors.category}</span>}
                </label>
                <label>
                  Precio base
                  <input type="number" step="0.01" min={0} value={form.basePrice} onChange={(e) => setForm({ ...form, basePrice: Number(e.target.value) })} />
                  {errors.basePrice && <span className="err">{errors.basePrice}</span>}
                </label>
                <label>
                  Costo
                  <input type="number" step="0.01" min={0} value={form.cost} onChange={(e) => setForm({ ...form, cost: Number(e.target.value) })} />
                </label>
                <label>
                  Stock
                  <input type="number" min={0} value={form.stock} onChange={(e) => setForm({ ...form, stock: Number(e.target.value) })} />
                </label>
                <label>
                  Stock mínimo (alerta)
                  <input type="number" min={0} value={form.minStockAlert} onChange={(e) => setForm({ ...form, minStockAlert: Number(e.target.value) })} />
                </label>
                <label className="full">
                  Activo
                  <select value={form.active ? '1' : '0'} onChange={(e) => setForm({ ...form, active: e.target.value === '1' })}>
                    <option value="1">Sí</option>
                    <option value="0">No</option>
                  </select>
                </label>
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
