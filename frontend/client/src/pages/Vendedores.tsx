import { useEffect, useState } from 'react';
import { api } from '../api';
import { Plus, Pencil, Trash2 } from 'lucide-react';

const emptyForm = { name: '', email: '', phone: '', commissionPercentage: 5, active: true };

export default function Vendedores() {
  const [sellers, setSellers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<any | null>(null);
  const [form, setForm] = useState<any>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<number | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await api.sellers.list();
      setSellers(data);
    } catch {
      /* ignore */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (s: any) => {
    setEditing(s);
    setForm({ name: s.name, email: s.email, phone: s.phone || '', commissionPercentage: s.commissionPercentage, active: s.active });
    setErrors({});
    setShowForm(true);
  };

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
      setShowForm(false);
      await load();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!confirm('¿Eliminar este vendedor?')) return;
    setDeleting(id);
    try {
      await api.sellers.remove(id);
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
        <h1>Vendedores</h1>
        <p className="page-sub">Gestión de vendedores y comisiones.</p>
      </div>

      <div className="toolbar">
        <button className="btn btn-primary" onClick={openCreate}>
          <Plus size={16} /> Nuevo vendedor
        </button>
      </div>

      <div className="card table-card">
        {loading ? (
          <div className="loading">Cargando vendedores…</div>
        ) : sellers.length === 0 ? (
          <div className="empty">No hay vendedores.</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>#</th>
                <th>Nombre</th>
                <th>Email</th>
                <th>Teléfono</th>
                <th>Comisión %</th>
                <th>Activo</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {sellers.map((s) => (
                <tr key={s.id}>
                  <td>{s.id}</td>
                  <td>{s.name}</td>
                  <td>{s.email}</td>
                  <td>{s.phone || '-'}</td>
                  <td>{s.commissionPercentage}%</td>
                  <td>{s.active ? 'Sí' : 'No'}</td>
                  <td className="actions">
                    <button className="btn btn-sm btn-edit" onClick={() => openEdit(s)}><Pencil size={14} /></button>
                    <button className="btn btn-sm btn-delete" onClick={() => remove(s.id)} disabled={deleting === s.id}>
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
              <h2>{editing ? 'Editar vendedor' : 'Nuevo vendedor'}</h2>
              <button className="close" onClick={() => setShowForm(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-grid">
                <label>
                  Nombre
                  <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
                  {errors.name && <span className="err">{errors.name}</span>}
                </label>
                <label>
                  Email
                  <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                </label>
                <label>
                  Teléfono
                  <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
                </label>
                <label>
                  Comisión %
                  <input type="number" step="0.1" min={0} max={100} value={form.commissionPercentage} onChange={(e) => setForm({ ...form, commissionPercentage: Number(e.target.value) })} />
                </label>
                <label>
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
