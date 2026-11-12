import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { Plus, Trash2, Save } from 'lucide-react';

const AVATARS = ['avatar-1', 'avatar-2', 'avatar-3', 'avatar-4', 'avatar-5'];
const AVATAR_COLORS = ['#7c3aed', '#d946ef', '#14b8a6', '#2563eb', '#f59e0b'];

export default function Configuracion({ tab }: { tab?: string }) {
  const { user, refresh } = useAuth();
  const { mode, setMode } = useTheme();
  const { company, update: updateCompany } = useSettings();
  const isAdmin = user?.roles?.includes('Admin');

  const [active, setActive] = useState(tab || 'perfil');
  useEffect(() => { if (tab) setActive(tab); }, [tab]);

  const tabs = [
    { id: 'perfil', label: 'Perfil' },
    { id: 'empresa', label: 'Empresa' },
    { id: 'moneda', label: 'Moneda' },
    ...(isAdmin ? [{ id: 'usuarios', label: 'Usuarios' }] : []),
    ...(isAdmin ? [{ id: 'datos', label: 'Datos de ejemplo' }] : []),
  ];

  return (
    <div className="page">
      <div className="page-header">
        <h1>Configuración</h1>
        <p className="page-sub">Ajustes de perfil, empresa y sistema.</p>
      </div>

      <div className="tabs">
        {tabs.map((t) => (
          <button key={t.id} className={'tab' + (active === t.id ? ' active' : '')} onClick={() => setActive(t.id)}>
            {t.label}
          </button>
        ))}
      </div>

      <div className="card">
        {active === 'perfil' && <PerfilTab user={user} refresh={refresh} mode={mode} setMode={setMode} />}
        {active === 'empresa' && isAdmin && <EmpresaTab company={company} update={updateCompany} />}
        {active === 'moneda' && isAdmin && <MonedaTab company={company} update={updateCompany} />}
        {active === 'usuarios' && isAdmin && <UsuariosTab />}
        {active === 'datos' && isAdmin && <DatosTab />}
        {!isAdmin && (active === 'empresa' || active === 'moneda' || active === 'usuarios' || active === 'datos') && (
          <p className="muted">Solo los administradores pueden acceder a esta sección.</p>
        )}
      </div>
    </div>
  );
}

function PerfilTab({ user, refresh, mode, setMode }: any) {
  const [form, setForm] = useState({
    nombre: user?.nombre || '',
    apellido: user?.apellido || '',
    avatar: user?.avatar || 'avatar-1',
  });
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  const save = async () => {
    setSaving(true);
    setMsg(null);
    try {
      await api.config.perfil.update({ ...form, tema: mode });
      await refresh();
      setMsg('Perfil actualizado');
    } catch (e: any) {
      setMsg(e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="form-section">
      <h3>Perfil de usuario</h3>
      <div className="form-grid">
        <label>
          Nombre
          <input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
        </label>
        <label>
          Apellido
          <input value={form.apellido} onChange={(e) => setForm({ ...form, apellido: e.target.value })} />
        </label>
        <label className="full">
          Avatar
          <div className="avatar-picker">
            {AVATARS.map((a, i) => (
              <button
                key={a}
                type="button"
                className={'avatar-option' + (form.avatar === a ? ' selected' : '')}
                style={{ background: AVATAR_COLORS[i] }}
                onClick={() => setForm({ ...form, avatar: a })}
              >
                {(user?.nombre || '?').charAt(0).toUpperCase()}
              </button>
            ))}
          </div>
        </label>
        <label className="full">
          Tema
          <div className="theme-options">
            {(['claro', 'oscuro', 'auto'] as const).map((t) => (
              <button key={t} type="button" className={'btn btn-sm' + (mode === t ? ' btn-primary' : ' btn-ghost')} onClick={() => setMode(t)}>
                {t === 'claro' ? 'Claro' : t === 'oscuro' ? 'Oscuro' : 'Automático'}
              </button>
            ))}
          </div>
        </label>
      </div>
      <div className="form-actions">
        <button className="btn btn-primary" onClick={save} disabled={saving}>
          <Save size={16} /> {saving ? 'Guardando…' : 'Guardar cambios'}
        </button>
        {msg && <span className="muted">{msg}</span>}
      </div>
    </div>
  );
}

function EmpresaTab({ company, update }: any) {
  const [form, setForm] = useState({ nombreEmpresa: company?.nombreEmpresa || '', moneda: company?.moneda || 'ARS', tipoDeCambio: company?.tipoDeCambio || 1 });
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    setForm({ nombreEmpresa: company?.nombreEmpresa || '', moneda: company?.moneda || 'ARS', tipoDeCambio: company?.tipoDeCambio || 1 });
  }, [company]);

  const save = async () => {
    setSaving(true);
    setMsg(null);
    try {
      await update(form);
      setMsg('Empresa actualizada');
    } catch (e: any) {
      setMsg(e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="form-section">
      <h3>Datos de la empresa</h3>
      <div className="form-grid">
        <label className="full">
          Nombre de la empresa
          <input value={form.nombreEmpresa} onChange={(e) => setForm({ ...form, nombreEmpresa: e.target.value })} />
        </label>
      </div>
      <div className="form-actions">
        <button className="btn btn-primary" onClick={save} disabled={saving}>
          <Save size={16} /> {saving ? 'Guardando…' : 'Guardar'}
        </button>
        {msg && <span className="muted">{msg}</span>}
      </div>
    </div>
  );
}

function MonedaTab({ company, update }: any) {
  const [moneda, setMoneda] = useState(company?.moneda || 'ARS');
  const [tipo, setTipo] = useState(company?.tipoDeCambio || 1);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    setMoneda(company?.moneda || 'ARS');
    setTipo(company?.tipoDeCambio || 1);
  }, [company]);

  const save = async () => {
    setSaving(true);
    setMsg(null);
    try {
      await update({ moneda, tipoDeCambio: Number(tipo) });
      setMsg('Moneda actualizada');
    } catch (e: any) {
      setMsg(e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="form-section">
      <h3>Configuración de moneda</h3>
      <p className="muted">Los montos se almacenan en pesos argentinos (ARS). Podés mostrarlos en USD usando el tipo de cambio.</p>
      <div className="form-grid">
        <label>
          Moneda de visualización
          <select value={moneda} onChange={(e) => setMoneda(e.target.value)}>
            <option value="ARS">Pesos argentinos (ARS)</option>
            <option value="USD">Dólares (USD)</option>
          </select>
        </label>
        <label>
          Tipo de cambio (ARS por 1 USD)
          <input type="number" step="0.01" min={0.01} value={tipo} onChange={(e) => setTipo(Number(e.target.value))} />
        </label>
      </div>
      <div className="form-actions">
        <button className="btn btn-primary" onClick={save} disabled={saving}>
          <Save size={16} /> {saving ? 'Guardando…' : 'Guardar'}
        </button>
        {msg && <span className="muted">{msg}</span>}
      </div>
    </div>
  );
}

function UsuariosTab() {
  const [users, setUsers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ email: '', password: '', nombre: '', apellido: '', avatar: 'avatar-1', rol: 'Empleado' });
  const [saving, setSaving] = useState(false);

  const load = async () => {
    setLoading(true);
    try { setUsers(await api.users.list()); } catch { /* ignore */ }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const save = async () => {
    setSaving(true);
    try {
      await api.users.create(form);
      setShowForm(false);
      setForm({ email: '', password: '', nombre: '', apellido: '', avatar: 'avatar-1', rol: 'Empleado' });
      await load();
    } catch (e: any) {
      alert(e.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    if (!confirm('¿Eliminar este usuario?')) return;
    try { await api.users.remove(id); await load(); } catch (e: any) { alert(e.message); }
  };

  return (
    <div className="form-section">
      <div className="section-header">
        <h3>Usuarios del sistema</h3>
        <button className="btn btn-primary btn-sm" onClick={() => setShowForm(true)}>
          <Plus size={14} /> Nuevo usuario
        </button>
      </div>
      {loading ? (
        <div className="loading">Cargando…</div>
      ) : (
        <table className="table">
          <thead>
            <tr><th>Email</th><th>Nombre</th><th>Rol</th><th>Acciones</th></tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>{u.email}</td>
                <td>{u.nombre} {u.apellido}</td>
                <td>{u.roles?.join(', ')}</td>
                <td className="actions">
                  <button className="btn btn-sm btn-delete" onClick={() => remove(u.id)}><Trash2 size={14} /></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {showForm && (
        <div className="modal-backdrop" onClick={() => setShowForm(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Nuevo usuario</h2>
              <button className="close" onClick={() => setShowForm(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-grid">
                <label>
                  Email
                  <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                </label>
                <label>
                  Contraseña
                  <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
                </label>
                <label>
                  Nombre
                  <input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
                </label>
                <label>
                  Apellido
                  <input value={form.apellido} onChange={(e) => setForm({ ...form, apellido: e.target.value })} />
                </label>
                <label>
                  Rol
                  <select value={form.rol} onChange={(e) => setForm({ ...form, rol: e.target.value })}>
                    <option value="Empleado">Empleado</option>
                    <option value="Admin">Administrador</option>
                  </select>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={() => setShowForm(false)} disabled={saving}>Cancelar</button>
              <button className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Guardando…' : 'Crear'}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function DatosTab() {
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const run = async () => {
    setLoading(true);
    setMsg(null);
    try {
      const r = await api.config.seed();
      setMsg(r.message || 'Datos verificados');
    } catch (e: any) {
      setMsg(e.message);
    } finally {
      setLoading(false);
    }
  };
  return (
    <div className="form-section">
      <h3>Datos de ejemplo</h3>
      <p className="muted">Verifica y carga datos de ejemplo (clientes, pedidos) si la base está vacía.</p>
      <button className="btn btn-primary" onClick={run} disabled={loading}>
        {loading ? 'Procesando…' : 'Cargar datos de ejemplo'}
      </button>
      {msg && <p className="muted" style={{ marginTop: 12 }}>{msg}</p>}
    </div>
  );
}
