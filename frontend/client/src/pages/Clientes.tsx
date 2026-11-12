import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { Plus, Pencil, Trash2, Search } from 'lucide-react';

const emptyForm = { nombre: '', email: '', telefono: '', direccion: '' };

export default function Clientes() {
  const { formatMoney } = useSettings();
  const [clientes, setClientes] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<any | null>(null);
  const [form, setForm] = useState<any>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await api.clients.list(search);
      setClientes(data);
    } catch {
      /* ignore */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const t = setTimeout(() => load(), 300);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (c: any) => {
    setEditing(c);
    setForm({ nombre: c.nombre, email: c.email, telefono: c.telefono || '', direccion: c.direccion || '' });
    setErrors({});
    setShowForm(true);
  };

  const validate = () => {
    const e: Record<string, string> = {};
    if (!form.nombre?.trim() || form.nombre.trim().length < 2) e.nombre = 'Nombre inválido';
    if (!form.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) e.email = 'Email inválido';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const save = async () => {
    if (!validate()) return;
    setSaving(true);
    try {
      if (editing) await api.clients.update(editing.id, { ...form, id: editing.id });
      else await api.clients.create(form);
      setShowForm(false);
      await load();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este cliente?')) return;
    setDeleting(id);
    try {
      await api.clients.remove(id);
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
        <h1>Clientes</h1>
        <p className="page-sub">Gestión de clientes y su historial.</p>
      </div>

      <div className="toolbar">
        <div className="search-box">
          <Search size={16} />
          <input placeholder="Buscar por nombre o email…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <button className="btn btn-primary" onClick={openCreate}>
          <Plus size={16} /> Nuevo cliente
        </button>
      </div>

      <div className="card table-card">
        {loading ? (
          <div className="loading">Cargando clientes…</div>
        ) : clientes.length === 0 ? (
          <div className="empty">No hay clientes.</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>#</th>
                <th>Nombre</th>
                <th>Email</th>
                <th>Teléfono</th>
                <th>Dirección</th>
                <th>Pedidos</th>
                <th>Total gastado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {clientes.map((c) => (
                <tr key={c.id}>
                  <td>{c.id}</td>
                  <td>{c.nombre}</td>
                  <td>{c.email}</td>
                  <td>{c.telefono || '-'}</td>
                  <td>{c.direccion || '-'}</td>
                  <td>{c.pedidosCount ?? 0}</td>
                  <td className="total">{formatMoney(c.totalSpent || 0)}</td>
                  <td className="actions">
                    <button className="btn btn-sm btn-edit" onClick={() => openEdit(c)}><Pencil size={14} /></button>
                    <button className="btn btn-sm btn-delete" onClick={() => remove(c.id)} disabled={deleting === c.id}>
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
              <h2>{editing ? 'Editar cliente' : 'Nuevo cliente'}</h2>
              <button className="close" onClick={() => setShowForm(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-grid">
                <label>
                  Nombre
                  <input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
                  {errors.nombre && <span className="err">{errors.nombre}</span>}
                </label>
                <label>
                  Email
                  <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                  {errors.email && <span className="err">{errors.email}</span>}
                </label>
                <label>
                  Teléfono
                  <input value={form.telefono} onChange={(e) => setForm({ ...form, telefono: e.target.value })} />
                </label>
                <label className="full">
                  Dirección
                  <input value={form.direccion} onChange={(e) => setForm({ ...form, direccion: e.target.value })} />
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
