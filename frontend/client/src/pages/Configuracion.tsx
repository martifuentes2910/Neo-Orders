import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import UserAvatar, { AVATAR_KEYS } from '@/components/UserAvatar';
import { useTheme } from '../context/ThemeContext';
import { useSettings } from '../context/SettingsContext';
import { useDatosConfig, useEmpresaForm, usePerfilForm } from '@/features/config';
import { useUsers } from '@/features/users';
import { motion } from 'framer-motion';
import { Plus, Trash2, Save, User as UserIcon, Building2, Users, Database, Palette, Check, AlertTriangle, LifeBuoy, Mail, MessageCircle, Clock, Bug, Copy, CheckCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { TableHead } from '@/components/ui/table';
import SortableTableHead from '@/components/SortableTableHead';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { toast } from 'sonner';
import { PALETTES, type CompanySettings, type PaletteId, type User } from '../types';
import { cn } from '@/lib/utils';

export default function Configuracion({ tab }: { tab?: string }) {
  const { user, refresh } = useAuth();
  const { mode, setMode } = useTheme();
  const { company, update: updateCompany } = useSettings();
  const [searchParams] = useSearchParams();
  const isAdmin = user?.roles?.includes('Admin');

  const requested = tab || searchParams.get('tab') || 'perfil';
  const [active, setActive] = useState(requested);
  useEffect(() => { setActive(requested); }, [requested]);

  return (
    <div className="space-y-6">
      <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
        <h1 className="text-[32px] font-bold tracking-tight">Configuración</h1>
        <p className="text-sm text-muted-foreground">Ajustes de perfil, apariencia, empresa y sistema.</p>
      </motion.div>

      <Tabs value={active} onValueChange={setActive} className="space-y-4">
        <TabsList>
          <TabsTrigger value="perfil" className="gap-2"><UserIcon className="h-4 w-4" /> Perfil</TabsTrigger>
          <TabsTrigger value="apariencia" className="gap-2"><Palette className="h-4 w-4" /> Apariencia</TabsTrigger>
          {isAdmin && <TabsTrigger value="empresa" className="gap-2"><Building2 className="h-4 w-4" /> Empresa</TabsTrigger>}
          {isAdmin && <TabsTrigger value="usuarios" className="gap-2"><Users className="h-4 w-4" /> Usuarios</TabsTrigger>}
          {isAdmin && <TabsTrigger value="datos" className="gap-2"><Database className="h-4 w-4" /> Datos</TabsTrigger>}
          <TabsTrigger value="soporte" className="gap-2"><LifeBuoy className="h-4 w-4" /> Soporte</TabsTrigger>
        </TabsList>

        <TabsContent value="perfil">
          <Card><CardContent className="p-6"><PerfilTab user={user} refresh={refresh} /></CardContent></Card>
        </TabsContent>
        <TabsContent value="apariencia">
          <Card><CardContent className="p-6"><AparienciaTab mode={mode} setMode={setMode} /></CardContent></Card>
        </TabsContent>
        {isAdmin && (
          <>
            <TabsContent value="empresa">
              <Card><CardContent className="p-6"><EmpresaTab company={company} update={updateCompany} /></CardContent></Card>
            </TabsContent>
            <TabsContent value="usuarios">
              <Card><CardContent className="p-6"><UsuariosTab /></CardContent></Card>
            </TabsContent>
            <TabsContent value="datos">
              <Card><CardContent className="p-6"><DatosTab /></CardContent></Card>
            </TabsContent>
          </>
        )}
        <TabsContent value="soporte">
          <Card><CardContent className="p-6"><SoporteTab /></CardContent></Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

function PerfilTab({ user, refresh }: { user: User | null; refresh: () => Promise<void> }) {
  const { form, setForm, saving, msg, save } = usePerfilForm(user, refresh);

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
          <p className="text-xs text-muted-foreground">Elegí uno de los retratos prediseñados.</p>
          <div className="flex flex-wrap gap-3">
            {AVATAR_KEYS.map((key) => (
              <button
                key={key}
                type="button"
                onClick={() => setForm({ ...form, avatar: key })}
                aria-label={`Avatar ${key.replace('avatar-', '')}`}
                className={cn(
                  'flex h-14 w-14 items-center justify-center rounded-full transition-all',
                  form.avatar === key
                    ? 'scale-110 ring-2 ring-primary ring-offset-2 ring-offset-background'
                    : 'opacity-75 hover:scale-105 hover:opacity-100',
                )}
              >
                <UserAvatar avatar={key} size={56} />
              </button>
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

function AparienciaTab({ mode, setMode }: { mode: 'claro' | 'oscuro' | 'auto'; setMode: (m: 'claro' | 'oscuro' | 'auto') => void }) {
  const { palette, setPalette, resolved } = useTheme();

  const choose = (id: PaletteId) => {
    setPalette(id);
    const nombre = PALETTES.find((p) => p.id === id)?.nombre ?? id;
    toast.success(`Paleta ${nombre} activada`);
  };

  const previewFor = (id: PaletteId) => {
    if (id === 'clean') {
      return resolved === 'oscuro'
        ? { canvas: '#0a0a0a', sidebar: '#141414', card: '#171717', line: '#2e2e2e', accent: '#a7e36a', text: '#f5f5f5', glass: true }
        : { canvas: '#eef4ec', sidebar: '#ffffff', card: '#ffffff', line: '#d7e2d4', accent: '#8fd14f', text: '#101915', glass: true };
    }
    if (id === 'menta') {
      return resolved === 'oscuro'
        ? { canvas: '#0A1F1A', sidebar: '#0B2E27', card: '#102A23', line: '#1D3B32', accent: '#35C0A3', text: '#E5F1D4', glass: false }
        : { canvas: '#F7FAF0', sidebar: '#FFFFFF', card: '#FFFFFF', line: '#DCEBD3', accent: '#35C0A3', text: '#0E4D41', glass: false };
    }
    return resolved === 'oscuro'
      ? { canvas: '#0d0f14', sidebar: '#0a0c10', card: '#1a1d26', line: '#262a3b', accent: '#ff5c7a', text: '#e8eaf0', glass: false }
      : { canvas: '#f7f1ec', sidebar: '#0e1017', card: '#ffffff', line: '#e8e5df', accent: '#ff5c7a', text: '#131418', glass: false };
  };

  return (
    <div className="space-y-8">
      <section className="space-y-3">
        <div>
          <h3 className="text-lg font-semibold">Paleta de colores</h3>
          <p className="text-sm text-muted-foreground">
            La preferencia es personal: se guarda en tu usuario y no afecta a los demás.
          </p>
        </div>
        <div className="grid gap-3 md:grid-cols-3">
          {PALETTES.map((p) => {
            const active = palette === p.id;
            const preview = previewFor(p.id);
            return (
              <button
                key={p.id}
                type="button"
                onClick={() => choose(p.id)}
                aria-pressed={active}
                className={cn(
                  'rounded-2xl border p-4 text-left transition-all hover:shadow-md',
                  active ? 'border-primary ring-2 ring-primary/40' : 'border-border',
                )}
              >
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <p className="flex items-center gap-2 text-sm font-semibold">
                      {p.nombre}
                      {active && <Check className="h-4 w-4 text-primary" />}
                    </p>
                    <p className="mt-0.5 text-xs text-muted-foreground">{p.descripcion}</p>
                  </div>
                </div>

                <div className="mt-3 flex gap-2">
                  {p.swatches.map((color) => (
                    <span
                      key={color}
                      className="h-6 flex-1 rounded-full border border-black/5"
                      style={{ backgroundColor: color }}
                      title={color}
                    />
                  ))}
                </div>

                <div
                  className="mt-3 flex h-20 overflow-hidden rounded-xl border"
                  style={{ backgroundColor: preview.canvas, borderColor: preview.line }}
                >
                  <div className="flex w-1/4 flex-col gap-1 p-2" style={{ backgroundColor: preview.sidebar }}>
                    {[0, 1, 2].map((i) => (
                      <span
                        key={i}
                        className="h-1.5 rounded-full"
                        style={{ backgroundColor: i === 0 ? preview.accent : preview.line, width: i === 0 ? '90%' : '70%' }}
                      />
                    ))}
                  </div>
                  <div className="flex flex-1 flex-col gap-1.5 p-2">
                    <span className="h-2 w-1/2 rounded-full" style={{ backgroundColor: preview.text, opacity: 0.7 }} />
                    <span
                      className="h-6 rounded-md"
                      style={{
                        backgroundColor: preview.glass ? `${preview.card}99` : preview.card,
                        border: `1px solid ${preview.line}`,
                        backdropFilter: preview.glass ? 'blur(6px)' : undefined,
                      }}
                    />
                    <span className="h-2 w-2/3 rounded-full" style={{ backgroundColor: preview.accent, opacity: 0.8 }} />
                  </div>
                </div>
              </button>
            );
          })}
        </div>
      </section>

      <section className="space-y-3">
        <div>
          <h3 className="text-lg font-semibold">Modo de color</h3>
          <p className="text-sm text-muted-foreground">
            Claro, oscuro o automático según el sistema. También se guarda en tu usuario.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {(['claro', 'oscuro', 'auto'] as const).map((t) => (
            <Button key={t} variant={mode === t ? 'default' : 'outline'} size="sm" onClick={() => setMode(t)}>
              {t === 'claro' ? 'Claro' : t === 'oscuro' ? 'Oscuro' : 'Automático'}
            </Button>
          ))}
        </div>
        <p className="text-xs text-muted-foreground">Modo activo ahora: {resolved === 'oscuro' ? 'oscuro' : 'claro'}.</p>
      </section>
    </div>
  );
}

function EmpresaTab({ company, update }: { company: CompanySettings | null; update: (data: Partial<CompanySettings>) => Promise<void> }) {
  const { form, setForm, saving, msg, save } = useEmpresaForm(company, update);

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold">Datos de la empresa</h3>
      <p className="text-sm text-muted-foreground">Los montos del sistema se muestran siempre en pesos argentinos (ARS).</p>
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

function UsuariosTab() {
  const { user: currentUser } = useAuth();
  const {
    users, loading, showForm, setShowForm, form, setForm, saving, sort, toggle, save, remove,
  } = useUsers();
  // El backend rechaza auto-eliminarse y borrar al último Admin; reflejarlo en la UI.
  const adminCount = users.filter((u) => u.roles?.includes('Admin')).length;

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
                <SortableTableHead label="Email" sortKey="email" sort={sort} onSort={toggle} />
                <SortableTableHead label="Nombre" sortKey="nombre" sort={sort} onSort={toggle} />
                <SortableTableHead label="Rol" sortKey="rol" sort={sort} onSort={toggle} />
                <TableHead className="px-4 py-3">Acciones</TableHead>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => {
                const isSelf = currentUser?.id === u.id;
                const isLastAdmin = u.roles?.includes('Admin') && adminCount <= 1;
                const deleteHint = isSelf
                  ? 'No puedes eliminar tu propio usuario'
                  : isLastAdmin
                    ? 'No se puede eliminar al último administrador'
                    : 'Eliminar usuario';
                return (
                  <tr key={u.id} className="border-b transition-colors hover:bg-muted/30">
                    <td className="px-4 py-3">{u.email}</td>
                    <td className="px-4 py-3">
                      <span className="flex items-center gap-2">
                        <UserAvatar avatar={u.avatar} size={28} />
                        <span>{u.nombre} {u.apellido}</span>
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant="secondary">{u.roles?.join(', ')}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-destructive hover:text-destructive disabled:opacity-40"
                        title={deleteHint}
                        aria-label={deleteHint}
                        disabled={isSelf || isLastAdmin}
                        onClick={() => remove(u.id)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </td>
                  </tr>
                );
              })}
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
              <Label>Avatar</Label>
              <div className="flex flex-wrap gap-2">
                {AVATAR_KEYS.map((key) => (
                  <button
                    key={key}
                    type="button"
                    onClick={() => setForm({ ...form, avatar: key })}
                    aria-label={`Avatar ${key.replace('avatar-', '')}`}
                    className={cn(
                      'rounded-full transition-all',
                      form.avatar === key ? 'ring-2 ring-primary ring-offset-2 ring-offset-background' : 'opacity-70 hover:opacity-100',
                    )}
                  >
                    <UserAvatar avatar={key} size={40} />
                  </button>
                ))}
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

function SoporteTab() {
  const [copiado, setCopiado] = useState<'email' | 'plantilla' | null>(null);
  const EMAIL = 'soporte@neoorders.example';
  const TELEFONO = '+54 11 0000-0000';

  const plantillaBug = () => {
    const ruta = window.location.pathname;
    const fecha = new Date().toLocaleString('es-AR');
    const navegador = navigator.userAgent;
    return [
      'Hola equipo de soporte de NeoOrders,',
      '',
      'Quiero reportar un problema:',
      '',
      `- Ruta: ${ruta}`,
      '- Rol: [Admin/Empleado]',
      `- Navegador: ${navegador}`,
      `- Fecha y hora: ${fecha}`,
      '- Descripción: [qué pasó]',
      '- Pasos para reproducir:',
      '  1. [paso 1]',
      '  2. [paso 2]',
      '  3. [paso 3]',
      '- Resultado esperado: [qué esperabas]',
      '- Resultado obtenido: [qué pasó realmente]',
      '',
      'Gracias.',
    ].join('\n');
  };

  const copiar = async (texto: string, tipo: 'email' | 'plantilla') => {
    try {
      await navigator.clipboard.writeText(texto);
      setCopiado(tipo);
      toast.success(tipo === 'email' ? 'Email copiado' : 'Plantilla copiada');
      setTimeout(() => setCopiado(null), 1800);
    } catch {
      toast.error('No se pudo copiar al portapapeles');
    }
  };

  const mailto = `mailto:${EMAIL}?subject=${encodeURIComponent('Reporte de bug - NeoOrders')}&body=${encodeURIComponent(plantillaBug())}`;

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-semibold">Soporte y contacto</h3>
        <p className="text-sm text-muted-foreground">
          Canales de contacto y guía para reportar problemas o consultas sobre NeoOrders.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-xl border bg-muted/10 p-4 space-y-3">
          <p className="flex items-center gap-2 font-medium"><Mail className="h-4 w-4" /> Email</p>
          <p className="text-sm text-muted-foreground">{EMAIL}</p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" className="gap-2" onClick={() => copiar(EMAIL, 'email')}>
              {copiado === 'email' ? <CheckCircle2 className="h-4 w-4" /> : <Copy className="h-4 w-4" />} Copiar
            </Button>
            <Button asChild size="sm" className="gap-2"><a href={`mailto:${EMAIL}`}><Mail className="h-4 w-4" /> Escribir</a></Button>
          </div>
        </div>
        <div className="rounded-xl border bg-muted/10 p-4 space-y-3">
          <p className="flex items-center gap-2 font-medium"><MessageCircle className="h-4 w-4" /> Teléfono</p>
          <p className="text-sm text-muted-foreground">{TELEFONO}</p>
          <p className="text-xs text-muted-foreground">Contacto rápido en horario laboral.</p>
        </div>
        <div className="rounded-xl border bg-muted/10 p-4 space-y-3">
          <p className="flex items-center gap-2 font-medium"><Clock className="h-4 w-4" /> Horario</p>
          <p className="text-sm text-muted-foreground">Lunes a viernes<br />09:00 a 18:00 (ART)</p>
        </div>
      </div>

      <div className="rounded-xl border bg-muted/10 p-4 space-y-4">
        <p className="flex items-center gap-2 font-medium"><Bug className="h-4 w-4" /> Reportar un bug</p>
        <ol className="list-decimal space-y-2 pl-5 text-sm text-muted-foreground">
          <li>Indicá la ruta/pantalla donde ocurrió (por ejemplo, <code>/pedidos</code>).</li>
          <li>Describí qué esperabas que pase y qué pasó realmente.</li>
          <li>Agregá pasos para reproducirlo, tu rol y la fecha/hora.</li>
          <li>Si podés, adjuntá una captura al enviar el correo.</li>
        </ol>
        <div className="flex flex-wrap gap-2">
          <Button asChild className="gap-2"><a href={mailto}><Mail className="h-4 w-4" /> Reportar por email</a></Button>
          <Button variant="outline" className="gap-2" onClick={() => copiar(plantillaBug(), 'plantilla')}>
            {copiado === 'plantilla' ? <CheckCircle2 className="h-4 w-4" /> : <Copy className="h-4 w-4" />} Copiar plantilla
          </Button>
        </div>
      </div>
    </div>
  );
}

function DatosTab() {
  const {
    msg, setMsg, loading, clearing, busy, showClear, setShowClear, confirmText, setConfirmText,
    runSeed, runClear,
  } = useDatosConfig();

  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-lg font-semibold">Datos de ejemplo</h3>
        <p className="text-sm text-muted-foreground">Verifica y carga datos de ejemplo (clientes, pedidos) si la base está vacía.</p>
      </div>

      <div className="flex flex-wrap gap-3">
        <Button onClick={runSeed} disabled={busy} className="gap-2">
          <Database className="h-4 w-4" /> {loading ? 'Procesando…' : 'Cargar datos de ejemplo'}
        </Button>
        <Button
          variant="destructive"
          onClick={() => { setMsg(null); setConfirmText(''); setShowClear(true); }}
          disabled={busy}
          className="gap-2"
        >
          <Trash2 className="h-4 w-4" /> Eliminar todos los datos
        </Button>
      </div>

      <p className="text-sm text-muted-foreground">
        La eliminación afecta sólo a los datos operativos. Los usuarios, los roles y la configuración de empresa se conservan.
      </p>
      {msg && <p className="text-sm text-muted-foreground">{msg}</p>}

      <Dialog open={showClear} onOpenChange={(open) => { setShowClear(open); if (!open) setConfirmText(''); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-destructive">
              <AlertTriangle className="h-5 w-5" /> Eliminar todos los datos
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <p className="text-sm text-muted-foreground">
              Esta acción no puede deshacerse. Se borrarán pedidos, clientes, productos, vendedores y notificaciones.
              Los usuarios, los roles y la configuración de empresa se conservan.
            </p>
            <div className="grid gap-2">
              <Label htmlFor="confirmar-eliminacion">Escribí <strong>ELIMINAR</strong> para confirmar</Label>
              <Input
                id="confirmar-eliminacion"
                value={confirmText}
                onChange={(e) => setConfirmText(e.target.value)}
                placeholder="ELIMINAR"
                autoComplete="off"
                disabled={clearing}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowClear(false)} disabled={clearing}>Cancelar</Button>
            <Button
              variant="destructive"
              onClick={runClear}
              disabled={confirmText.trim() !== 'ELIMINAR' || clearing}
            >
              {clearing ? 'Eliminando…' : 'Eliminar datos'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
