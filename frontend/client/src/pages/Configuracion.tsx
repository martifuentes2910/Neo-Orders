import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import AvatarMemphis from '@/components/AvatarMemphis';
import { AVATAR_COUNT } from '@/components/AvatarMemphis';
import { useTheme } from '../context/ThemeContext';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { motion } from 'framer-motion';
import { Plus, Trash2, Save, User, Building2, Coins, Users, Database } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { toast } from 'sonner';

// AVATARS replaced by AvatarMemphis presets

export default function Configuracion({ tab }: { tab?: string }) {
  const { user, refresh } = useAuth();
  const { mode, setMode } = useTheme();
  const { company, update: updateCompany } = useSettings();
  const isAdmin = user?.roles?.includes('Admin');

  const [active, setActive] = useState(tab || 'perfil');
  useEffect(() => { if (tab) setActive(tab); }, [tab]);

  return (
    <div className="space-y-6">
      <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
        <h1 className="text-2xl font-bold tracking-tight">Configuración</h1>
        <p className="text-sm text-muted-foreground">Ajustes de perfil, empresa y sistema.</p>
      </motion.div>

      <Tabs value={active} onValueChange={setActive} className="space-y-4">
        <TabsList>
          <TabsTrigger value="perfil" className="gap-2"><User className="h-4 w-4" /> Perfil</TabsTrigger>
          <TabsTrigger value="empresa" className="gap-2"><Building2 className="h-4 w-4" /> Empresa</TabsTrigger>
          <TabsTrigger value="moneda" className="gap-2"><Coins className="h-4 w-4" /> Moneda</TabsTrigger>
          {isAdmin && <TabsTrigger value="usuarios" className="gap-2"><Users className="h-4 w-4" /> Usuarios</TabsTrigger>}
          {isAdmin && <TabsTrigger value="datos" className="gap-2"><Database className="h-4 w-4" /> Datos</TabsTrigger>}
        </TabsList>

        <TabsContent value="perfil">
          <Card><CardContent className="p-6"><PerfilTab user={user} refresh={refresh} mode={mode} setMode={setMode} /></CardContent></Card>
        </TabsContent>
        {isAdmin && (
          <>
            <TabsContent value="empresa">
              <Card><CardContent className="p-6"><EmpresaTab company={company} update={updateCompany} /></CardContent></Card>
            </TabsContent>
            <TabsContent value="moneda">
              <Card><CardContent className="p-6"><MonedaTab company={company} update={updateCompany} /></CardContent></Card>
            </TabsContent>
            <TabsContent value="usuarios">
              <Card><CardContent className="p-6"><UsuariosTab /></CardContent></Card>
            </TabsContent>
            <TabsContent value="datos">
              <Card><CardContent className="p-6"><DatosTab /></CardContent></Card>
            </TabsContent>
          </>
        )}
      </Tabs>
    </div>
  );
}

function PerfilTab({ user, refresh, mode, setMode }: any) {
  const [form, setForm] = useState({ nombre: user?.nombre || '', apellido: user?.apellido || '', avatar: user?.avatar || 'avatar-1' });
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  const save = async () => {
    setSaving(true);
    setMsg(null);
    try {
      await api.config.perfil.update({ ...form, tema: mode });
      await refresh();
      setMsg('Perfil actualizado');
      toast.success('Perfil actualizado');
    } catch (e: any) {
      setMsg(e.message);
      toast.error(e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold">Perfil de usuario</h3>
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="grid gap-2">
          <Label>Nombre</Label>
          <Input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
        </div>
        <div className="grid gap-2">
          <Label>Apellido</Label>
          <Input value={form.apellido} onChange={(e) => setForm({ ...form, apellido: e.target.value })} />
        </div>
        <div className="sm:col-span-2 grid gap-2">
          <Label>Avatar</Label>
          <div className="flex flex-wrap gap-3">
            {Array.from({ length: AVATAR_COUNT }).map((_, i) => {
              const key = `avatar-${i + 1}`;
              return (
                <button
                  key={key}
                  type="button"
                  onClick={() => setForm({ ...form, avatar: key })}
                  className={`flex h-14 w-14 items-center justify-center rounded-full transition-all ${form.avatar === key ? 'ring-2 ring-primary ring-offset-2 scale-110' : 'opacity-70 hover:opacity-100 hover:scale-105'}`}
                >
                  <AvatarMemphis preset={i} size={56} />
                </button>
              );
            })}
          </div>
        </div>
        <div className="sm:col-span-2 grid gap-2">
          <Label>Tema</Label>
          <div className="flex gap-2">
            {(['claro', 'oscuro', 'auto'] as const).map((t) => (
              <Button key={t} variant={mode === t ? 'default' : 'outline'} size="sm" onClick={() => setMode(t)}>
                {t === 'claro' ? 'Claro' : t === 'oscuro' ? 'Oscuro' : 'Automático'}
              </Button>
            ))}
          </div>
        </div>
      </div>
      <Button onClick={save} disabled={saving} className="gap-2">
        <Save className="h-4 w-4" /> {saving ? 'Guardando…' : 'Guardar cambios'}
      </Button>
      {msg && <p className="text-sm text-muted-foreground">{msg}</p>}
    </div>
  );
}

function EmpresaTab({ company, update }: any) {
  const [form, setForm] = useState({ nombreEmpresa: company?.nombreEmpresa || '' });
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    setForm({ nombreEmpresa: company?.nombreEmpresa || '' });
  }, [company]);

  const save = async () => {
    setSaving(true);
    setMsg(null);
    try {
      await update(form);
      setMsg('Empresa actualizada');
      toast.success('Empresa actualizada');
    } catch (e: any) {
      setMsg(e.message);
      toast.error(e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold">Datos de la empresa</h3>
      <div className="grid gap-2">
        <Label>Nombre de la empresa</Label>
        <Input value={form.nombreEmpresa} onChange={(e) => setForm({ ...form, nombreEmpresa: e.target.value })} />
      </div>
      <Button onClick={save} disabled={saving} className="gap-2">
        <Save className="h-4 w-4" /> {saving ? 'Guardando…' : 'Guardar'}
      </Button>
      {msg && <p className="text-sm text-muted-foreground">{msg}</p>}
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
      toast.success('Moneda actualizada');
    } catch (e: any) {
      setMsg(e.message);
      toast.error(e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold">Configuración de moneda</h3>
      <p className="text-sm text-muted-foreground">Los montos se almacenan en pesos argentinos (ARS). Podés mostrarlos en USD usando el tipo de cambio.</p>
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="grid gap-2">
          <Label>Moneda de visualización</Label>
          <Select value={moneda} onValueChange={setMoneda}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="ARS">Pesos argentinos (ARS)</SelectItem>
              <SelectItem value="USD">Dólares (USD)</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-2">
          <Label>Tipo de cambio (ARS por 1 USD)</Label>
          <Input type="number" step="0.01" min={0.01} value={tipo} onChange={(e) => setTipo(Number(e.target.value))} />
        </div>
      </div>
      <Button onClick={save} disabled={saving} className="gap-2">
        <Save className="h-4 w-4" /> {saving ? 'Guardando…' : 'Guardar'}
      </Button>
      {msg && <p className="text-sm text-muted-foreground">{msg}</p>}
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
    try { setUsers(await api.users.list()); } catch { toast.error('Error al cargar usuarios'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const save = async () => {
    setSaving(true);
    try {
      await api.users.create(form);
      toast.success('Usuario creado');
      setShowForm(false);
      setForm({ email: '', password: '', nombre: '', apellido: '', avatar: 'avatar-1', rol: 'Empleado' });
      await load();
    } catch (e: any) {
      toast.error(e.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    if (!confirm('¿Eliminar este usuario?')) return;
    try { await api.users.remove(id); toast.success('Usuario eliminado'); await load(); } catch (e: any) { toast.error(e.message); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Usuarios del sistema</h3>
        <Button size="sm" onClick={() => setShowForm(true)} className="gap-2">
          <Plus className="h-4 w-4" /> Nuevo usuario
        </Button>
      </div>
      {loading ? (
        <div className="space-y-3">{[...Array(3)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}</div>
      ) : (
        <div className="max-h-[300px] overflow-y-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-left text-xs font-medium uppercase tracking-wider text-muted-foreground">
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Nombre</th>
                <th className="px-4 py-3">Rol</th>
                <th className="px-4 py-3">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id} className="border-b transition-colors hover:bg-muted/30">
                  <td className="px-4 py-3">{u.email}</td>
                  <td className="px-4 py-3">{u.nombre} {u.apellido}</td>
                  <td className="px-4 py-3">
                    <Badge variant="secondary">{u.roles?.join(', ')}</Badge>
                  </td>
                  <td className="px-4 py-3">
                    <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => remove(u.id)}>
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Nuevo usuario</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label>Email</Label>
              <Input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
            <div className="grid gap-2">
              <Label>Contraseña</Label>
              <Input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label>Nombre</Label>
                <Input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
              </div>
              <div className="grid gap-2">
                <Label>Apellido</Label>
                <Input value={form.apellido} onChange={(e) => setForm({ ...form, apellido: e.target.value })} />
              </div>
            </div>
            <div className="grid gap-2">
              <Label>Rol</Label>
              <Select value={form.rol} onValueChange={(v) => setForm({ ...form, rol: v })}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Empleado">Empleado</SelectItem>
                  <SelectItem value="Admin">Administrador</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowForm(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={save} disabled={saving}>{saving ? 'Guardando…' : 'Crear'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
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
      toast.success(r.message || 'Datos verificados');
    } catch (e: any) {
      setMsg(e.message);
      toast.error(e.message);
    } finally {
      setLoading(false);
    }
  };
  return (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Datos de ejemplo</h3>
      <p className="text-sm text-muted-foreground">Verifica y carga datos de ejemplo (clientes, pedidos) si la base está vacía.</p>
      <Button onClick={run} disabled={loading} className="gap-2">
        <Database className="h-4 w-4" /> {loading ? 'Procesando…' : 'Cargar datos de ejemplo'}
      </Button>
      {msg && <p className="text-sm text-muted-foreground">{msg}</p>}
    </div>
  );
}
