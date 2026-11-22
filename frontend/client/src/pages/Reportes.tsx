import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useSettings } from '../context/SettingsContext';
import { useChartTheme } from '../hooks/useChartTheme';
import { api } from '../api';
import { ORDER_STATUSES, getStatusColor, type OrderStatus } from '../types';
import type {
  FilaCliente,
  FilaProducto,
  FilaVendedor,
  FiltrosReporteParams,
  ReporteDetalle,
  ReporteResumenResponse,
  VistaReporte,
} from '../types/reportes';
import { useTheme } from '../context/ThemeContext';
import { motion, useReducedMotion } from 'framer-motion';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Table, TableBody, TableCell, TableHeader, TableRow } from '@/components/ui/table';
import SortableTableHead from '@/components/SortableTableHead';
import { useTableSort } from '@/hooks/useTableSort';

import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';
import ChartGradients from '@/components/ChartGradients';
import { GRAD } from '@/lib/chartGradients';
import ChartTooltip from '@/components/ChartTooltip';
import { compactNumber } from '@/lib/format';

function today() { return new Date().toISOString().slice(0, 10); }
function daysAgo(n: number) { const d = new Date(); d.setDate(d.getDate() - n); return d.toISOString().slice(0, 10); }
function yearStart() { return `${new Date().getFullYear()}-01-01`; }

function periodoDe(desde: string, hasta: string): string {
  if (!desde && !hasta) return 'all';
  if (desde === daysAgo(30) && hasta === today()) return '30';
  if (desde === daysAgo(90) && hasta === today()) return '90';
  if (desde === yearStart() && hasta === today()) return 'year';
  return 'custom';
}

const VISTAS: { value: VistaReporte; label: string }[] = [
  { value: 'resumen', label: 'Resumen' },
  { value: 'vendedores', label: 'Ventas por vendedor' },
  { value: 'productos', label: 'Productos más vendidos' },
  { value: 'clientes', label: 'Clientes con más compras' },
];

const GRAD_POR_VISTA: Record<Exclude<VistaReporte, 'resumen'>, string> = {
  vendedores: GRAD.coralH,
  productos: GRAD.peachH,
  clientes: GRAD.amberH,
};

interface FilaNormalizada {
  id: string;
  nombre: string;
  pedidos: number;
  unidades: number;
  ingresos: number;
  participacion: number;
  comision?: number;
  comisionEstimada?: number;
}

/** Filtros editados en la UI; se envían juntos al pulsar "Filtrar". */
interface BorradorFiltros {
  desde: string;
  hasta: string;
  clientId: string;
  sellerId: string;
  estado: string;
}

function nombreDeFila(f: FilaVendedor | FilaProducto | FilaCliente, tipo: ReporteDetalle['tipo']): string {
  if (tipo === 'vendedor') return (f as FilaVendedor).vendedor;
  if (tipo === 'producto') return (f as FilaProducto).producto;
  return (f as FilaCliente).cliente;
}

function normalizar(fila: FilaVendedor | FilaProducto | FilaCliente, tipo: ReporteDetalle['tipo']): FilaNormalizada {
  if (tipo === 'vendedor') {
    const f = fila as FilaVendedor;
    return {
      id: String(f.vendedorId), nombre: f.vendedor, pedidos: f.pedidos, unidades: f.unidades,
      ingresos: f.ingresos, participacion: f.participacion,
      comision: f.comision, comisionEstimada: f.comisionEstimada,
    };
  }
  if (tipo === 'producto') {
    const f = fila as FilaProducto;
    return {
      id: f.producto, nombre: f.producto, pedidos: f.pedidos, unidades: f.unidades,
      ingresos: f.ingresos, participacion: f.participacion,
    };
  }
  const f = fila as FilaCliente;
  return {
    id: String(f.clienteId), nombre: f.cliente, pedidos: f.pedidos, unidades: f.unidades,
    ingresos: f.ingresos, participacion: f.participacion,
  };
}

export default function Reportes({ vista = 'resumen' }: { vista?: VistaReporte }) {
  const { formatMoney } = useSettings();
  const chartTheme = useChartTheme();
  const { resolved, palette } = useTheme();
  const isDark = resolved === 'oscuro';
  const reduceMotion = useReducedMotion();
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();

  const desdeParam = params.get('desde');
  const hastaParam = params.get('hasta');
  // `?desde=&hasta=` significa "sin límite de fechas" (preset "Todo el historial").
  const desde = desdeParam === null ? daysAgo(30) : desdeParam;
  const hasta = hastaParam === null ? today() : hastaParam;
  const clientId = params.get('clientId') || '';
  const sellerId = params.get('sellerId') || '';
  const estado = params.get('estado') || 'Todos';
  const pageParam = Number(params.get('page') ?? '1');
  const page = Number.isInteger(pageParam) && pageParam >= 1 ? pageParam : 1;
  const sizeParam = Number(params.get('pageSize') ?? '25');
  const pageSize = Number.isInteger(sizeParam) && sizeParam >= 1 && sizeParam <= 100 ? sizeParam : 25;

  const firmaFiltros = [desde, hasta, clientId, sellerId, estado].join('\u0000');
  const [edicion, setEdicion] = useState<{ firma: string; valores: BorradorFiltros } | null>(null);
  // La URL es el estado aplicado; la edición sólo vale para esa firma, así back/forward la descarta sola.
  const borrador: BorradorFiltros = edicion?.firma === firmaFiltros
    ? edicion.valores
    : { desde, hasta, clientId, sellerId, estado };
  const [resumen, setResumen] = useState<ReporteResumenResponse | null>(null);
  const [detalle, setDetalle] = useState<ReporteDetalle | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [clients, setClients] = useState<any[]>([]);
  const [sellers, setSellers] = useState<any[]>([]);

  const filtros: FiltrosReporteParams = useMemo(() => ({
    desde: desde || undefined,
    hasta: hasta || undefined,
    clientId: clientId ? Number(clientId) : undefined,
    sellerId: sellerId ? Number(sellerId) : undefined,
    estado,
  }), [desde, hasta, clientId, sellerId, estado]);

  const period = periodoDe(borrador.desde, borrador.hasta);

  const iniciarCarga = () => {
    setLoading(true);
    setError(null);
    setDetalle(null);
  };

  const setBorradorFiltro = (key: keyof BorradorFiltros, value: string) => {
    setEdicion({ firma: firmaFiltros, valores: { ...borrador, [key]: value } });
  };

  const cambiarPeriodo = (p: string) => {
    if (p === '30') setEdicion({ firma: firmaFiltros, valores: { ...borrador, desde: daysAgo(30), hasta: today() } });
    else if (p === '90') setEdicion({ firma: firmaFiltros, valores: { ...borrador, desde: daysAgo(90), hasta: today() } });
    else if (p === 'year') setEdicion({ firma: firmaFiltros, valores: { ...borrador, desde: yearStart(), hasta: today() } });
    else if (p === 'all') setEdicion({ firma: firmaFiltros, valores: { ...borrador, desde: '', hasta: '' } });
    // "Personalizado": conserva las fechas para editarlas en los campos.
  };

  const queryDe = (origen: BorradorFiltros) => {
    const next = new URLSearchParams();
    const esPorDefecto = origen.desde === daysAgo(30) && origen.hasta === today();
    if (!esPorDefecto) {
      next.set('desde', origen.desde);
      next.set('hasta', origen.hasta);
    }
    if (origen.clientId) next.set('clientId', origen.clientId);
    if (origen.sellerId) next.set('sellerId', origen.sellerId);
    if (origen.estado && origen.estado !== 'Todos') next.set('estado', origen.estado);
    const size = params.get('pageSize');
    if (size && size !== '25') next.set('pageSize', size);
    return next;
  };

  const aplicar = (origen: BorradorFiltros = borrador) => {
    const sinCambios =
      origen.desde === desde && origen.hasta === hasta &&
      origen.clientId === clientId && origen.sellerId === sellerId &&
      origen.estado === estado;
    if (sinCambios) return;
    iniciarCarga();
    setEdicion(null);
    setParams(queryDe(origen));
  };

  const setPaginacion = (nextPage: number, nextSize?: number) => {
    iniciarCarga();
    const next = new URLSearchParams(params);
    if (nextPage === 1) next.delete('page');
    else next.set('page', String(nextPage));
    if (nextSize !== undefined) {
      if (nextSize === 25) next.delete('pageSize');
      else next.set('pageSize', String(nextSize));
      next.delete('page');
    }
    setParams(next);
  };

  const cambiarVista = (destino: VistaReporte) => {
    iniciarCarga();
    setEdicion(null);
    const qs = queryDe(borrador).toString();
    const base = destino === 'resumen' ? '/reportes' : `/reportes/${destino}`;
    navigate(`${base}${qs ? `?${qs}` : ''}`);
  };

  const clear = () => {
    const yaPorDefecto =
      desde === daysAgo(30) && hasta === today() && !clientId && !sellerId &&
      estado === 'Todos' && page === 1 && pageSize === 25;
    setEdicion(null);
    if (yaPorDefecto) return;
    iniciarCarga();
    if (vista === 'resumen') setResumen(null);
    const next = queryDe({ desde: daysAgo(30), hasta: today(), clientId: '', sellerId: '', estado: 'Todos' });
    next.delete('pageSize');
    setParams(next);
  };

  useEffect(() => {
    api.clients.list().then(setClients).catch(() => {});
    api.sellers.list(true).then(setSellers).catch(() => {});
  }, []);

  useEffect(() => {
    if (vista !== 'resumen') return;
    let activo = true;
    api.reports(filtros)
      .then((data) => { if (activo) { setResumen(data); setError(null); } })
      .catch((e: Error) => { if (activo) setError(e.message || 'No se pudieron cargar los reportes.'); })
      .finally(() => { if (activo) setLoading(false); });
    return () => { activo = false; };
  }, [vista, filtros]);

  useEffect(() => {
    if (vista === 'resumen') return;
    let activo = true;
    const peticion = vista === 'vendedores'
      ? api.reportesDetalle.vendedores({ ...filtros, page, pageSize })
      : vista === 'productos'
        ? api.reportesDetalle.productos({ ...filtros, page, pageSize })
        : api.reportesDetalle.clientes({ ...filtros, page, pageSize });
    peticion
      .then((data) => { if (activo) { setDetalle(data); setError(null); } })
      .catch((e: Error) => { if (activo) setError(e.message || 'No se pudo cargar el reporte.'); })
      .finally(() => { if (activo) setLoading(false); });
    return () => { activo = false; };
  }, [vista, filtros, page, pageSize]);

  const ordenEstados: OrderStatus[] = ['Cancelado', 'Pendiente', 'Procesado', 'Enviado', 'Entregado'];
  const pieData = (resumen?.porEstado || [])
    .map((e) => ({
      name: e.estado,
      value: e.cantidad,
      color: getStatusColor(e.estado as OrderStatus, isDark, palette) || '#6b7280',
    }))
    .sort((a, b) => {
      const ai = ordenEstados.indexOf(a.name as OrderStatus);
      const bi = ordenEstados.indexOf(b.name as OrderStatus);
      return (ai === -1 ? 99 : ai) - (bi === -1 ? 99 : bi);
    });
  const barMes = (resumen?.porMes || []).map((m) => ({ name: `${m.month}/${String(m.year).slice(2)}`, ingresos: m.ingresos }));
  const topProd = (resumen?.topProductos || []).map((p) => ({ name: p.producto, ingresos: p.ingresos }));
  const topCli = (resumen?.topClientes || []).map((c) => ({ name: c.cliente, total: c.total }));
  const ventasVend = (resumen?.ventasPorVendedor || []).map((v) => ({ name: v.vendedor, total: v.total }));
  const totalPedidos = pieData.reduce((acc, d) => acc + Number(d.value || 0), 0);

  const filas = useMemo<FilaNormalizada[]>(
    () => (detalle ? detalle.filas.map((f) => normalizar(f, detalle.tipo)) : []),
    [detalle],
  );
  const accessors = useMemo(() => ({
    nombre: (r: FilaNormalizada) => r.nombre,
    pedidos: (r: FilaNormalizada) => r.pedidos,
    unidades: (r: FilaNormalizada) => r.unidades,
    ingresos: (r: FilaNormalizada) => r.ingresos,
    participacion: (r: FilaNormalizada) => r.participacion,
    comision: (r: FilaNormalizada) => r.comision,
    comisionEstimada: (r: FilaNormalizada) => r.comisionEstimada,
  }), []);
  const { sorted, sort, toggle } = useTableSort(filas, accessors);

  const graficoData = useMemo(
    () => (detalle?.filasGrafico || []).map((f) => ({
      name: nombreDeFila(f, detalle!.tipo),
      ingresos: f.ingresos,
    })),
    [detalle],
  );

  return (
    <div className="space-y-6">
      <motion.div initial={reduceMotion ? false : { opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
        <h1 className="text-[32px] font-bold tracking-tight">Reportes</h1>
        <p className="text-sm text-muted-foreground">Análisis, gráficos y tablas del negocio.</p>
      </motion.div>

      <ChartGradients />

      <Tabs value={vista} onValueChange={(v) => cambiarVista(v as VistaReporte)} className="space-y-4">
        <TabsList>
          {VISTAS.map((v) => <TabsTrigger key={v.value} value={v.value}>{v.label}</TabsTrigger>)}
        </TabsList>

        <Card>
          <CardContent className="p-4">
            <div className="flex flex-wrap items-end gap-3">
              <div className="space-y-1">
                <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Período</Label>
                <Select value={period} onValueChange={cambiarPeriodo}>
                  <SelectTrigger className="h-9 w-[170px] text-sm"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="30">Últimos 30 días</SelectItem>
                    <SelectItem value="90">Últimos 90 días</SelectItem>
                    <SelectItem value="year">Este año</SelectItem>
                    <SelectItem value="all">Todo el historial</SelectItem>
                    <SelectItem value="custom">Personalizado</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Desde</Label>
                <Input type="date" value={borrador.desde} onChange={(e) => setBorradorFiltro('desde', e.target.value)} className="h-9 w-[150px]" />
              </div>
              <div className="space-y-1">
                <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Hasta</Label>
                <Input type="date" value={borrador.hasta} onChange={(e) => setBorradorFiltro('hasta', e.target.value)} className="h-9 w-[150px]" />
              </div>
              <div className="space-y-1">
                <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Cliente</Label>
                <Select value={borrador.clientId || 'all'} onValueChange={(v) => setBorradorFiltro('clientId', v === 'all' ? '' : v)}>
                  <SelectTrigger className="h-9 w-[190px] text-sm"><SelectValue placeholder="Todos" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos</SelectItem>
                    {clients.map((c) => <SelectItem key={c.id} value={String(c.id)}>{c.nombre}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Vendedor</Label>
                <Select value={borrador.sellerId || 'all'} onValueChange={(v) => setBorradorFiltro('sellerId', v === 'all' ? '' : v)}>
                  <SelectTrigger className="h-9 w-[190px] text-sm"><SelectValue placeholder="Todos" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos</SelectItem>
                    {sellers.map((s) => <SelectItem key={s.id} value={String(s.id)}>{s.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Estado</Label>
                <Select value={borrador.estado} onValueChange={(v) => setBorradorFiltro('estado', v)}>
                  <SelectTrigger className="h-9 w-[160px] text-sm"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Todos">Todos</SelectItem>
                    {ORDER_STATUSES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-end gap-2 sm:ml-auto">
                <Button size="sm" onClick={() => aplicar()} title="Aplicar todos los filtros seleccionados" className="h-9 min-w-[104px] px-5">Filtrar</Button>
                <Button variant="outline" size="sm" onClick={clear} className="h-9 min-w-[104px] px-5">Limpiar</Button>
              </div>
            </div>
          </CardContent>
        </Card>

        <TabsContent value="resumen" className="space-y-4">
          {error && <Card><CardContent className="p-4 text-sm text-destructive">{error}</CardContent></Card>}
          {loading && !resumen ? (
            <div className="py-20 text-center text-sm text-muted-foreground">Cargando reportes…</div>
          ) : (
            <div className="grid gap-4 md:grid-cols-2">
              <motion.div initial={reduceMotion ? false : { opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { delay: 0.1 }}>
                <Card className="h-full">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Pedidos por estado</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="relative">
                      <ResponsiveContainer width="100%" height={250}>
                        <PieChart>
                          <Pie
                            data={pieData}
                            dataKey="value"
                            nameKey="name"
                            innerRadius={66}
                            outerRadius={96}
                            paddingAngle={4}
                            cornerRadius={8}
                            stroke="hsl(var(--card))"
                            strokeWidth={2}
                            isAnimationActive={!reduceMotion}
                            animationDuration={800}
                            animationEasing="ease-out"
                          >
                            {pieData.map((d, i) => <Cell key={i} fill={d.color} />)}
                          </Pie>
                          <Tooltip cursor={false} content={<ChartTooltip valueLabel="Pedidos" hideLabel />} />
                        </PieChart>
                      </ResponsiveContainer>
                      <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
                        <span className="text-2xl font-bold tabular-nums">{totalPedidos}</span>
                        <span className="text-xs font-medium uppercase tracking-widest text-muted-foreground">pedidos</span>
                      </div>
                    </div>
                    <div className="mt-3 flex flex-wrap justify-center gap-2">
                      {pieData.map((d) => (
                        <span key={d.name} className="flex items-center gap-1.5 rounded-full border border-border bg-muted px-2.5 py-1 text-xs">
                          <span className="h-2 w-2 rounded-full" style={{ background: d.color }} />
                          <span className="text-muted-foreground">{d.name}</span>
                          <span className="font-semibold tabular-nums">{d.value}</span>
                        </span>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </motion.div>

              <motion.div initial={reduceMotion ? false : { opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { delay: 0.2 }}>
                <Card className="h-full">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Ingresos por mes</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={250}>
                      <BarChart data={barMes} margin={{ top: 8, right: 8, left: -14, bottom: 0 }} barCategoryGap="28%">
                        <CartesianGrid vertical={false} strokeDasharray="4 6" stroke={chartTheme.grid} />
                        <XAxis dataKey="name" axisLine={false} tickLine={false} tickMargin={10} tick={{ fill: chartTheme.axis, fontSize: 11 }} />
                        <YAxis axisLine={false} tickLine={false} width={54} tick={{ fill: chartTheme.axis, fontSize: 11 }} tickFormatter={(v) => compactNumber(v)} />
                        <Tooltip cursor={{ fill: chartTheme.cursorFill }} content={<ChartTooltip formatter={formatMoney} valueLabel="Ingresos" labelFormatter={(l) => `Mes ${l}`} />} />
                        <Bar dataKey="ingresos" fill={palette === 'clean' ? chartTheme.barPrimary : `url(#${GRAD.coral})`} radius={[10, 10, 4, 4]} maxBarSize={44} isAnimationActive={!reduceMotion} animationDuration={800} animationEasing="ease-out" />
                      </BarChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </motion.div>

              <motion.div initial={reduceMotion ? false : { opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { delay: 0.3 }}>
                <Card className="h-full">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Productos más vendidos</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={250}>
                      <BarChart data={topProd} layout="vertical" margin={{ top: 4, right: 16, left: 4, bottom: 0 }} barCategoryGap="30%">
                        <CartesianGrid horizontal={false} strokeDasharray="4 6" stroke={chartTheme.grid} />
                        <XAxis type="number" axisLine={false} tickLine={false} tick={{ fill: chartTheme.axis, fontSize: 11 }} tickFormatter={(v) => compactNumber(v)} />
                        <YAxis type="category" dataKey="name" axisLine={false} tickLine={false} width={120} tick={{ fill: chartTheme.axis, fontSize: 11 }} />
                        <Tooltip cursor={{ fill: chartTheme.cursorFill }} content={<ChartTooltip formatter={formatMoney} valueLabel="Ingresos" />} />
                        <Bar dataKey="ingresos" fill={palette === 'clean' ? (isDark ? '#ffa94d' : '#f08a24') : `url(#${GRAD.peachH})`} radius={[0, 8, 8, 0]} maxBarSize={26} isAnimationActive={!reduceMotion} animationDuration={800} animationEasing="ease-out" />
                      </BarChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </motion.div>

              <motion.div initial={reduceMotion ? false : { opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { delay: 0.4 }}>
                <Card className="h-full">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Clientes con más compras</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={250}>
                      <BarChart data={topCli} layout="vertical" margin={{ top: 4, right: 16, left: 4, bottom: 0 }} barCategoryGap="30%">
                        <CartesianGrid horizontal={false} strokeDasharray="4 6" stroke={chartTheme.grid} />
                        <XAxis type="number" axisLine={false} tickLine={false} tick={{ fill: chartTheme.axis, fontSize: 11 }} tickFormatter={(v) => compactNumber(v)} />
                        <YAxis type="category" dataKey="name" axisLine={false} tickLine={false} width={120} tick={{ fill: chartTheme.axis, fontSize: 11 }} />
                        <Tooltip cursor={{ fill: chartTheme.cursorFill }} content={<ChartTooltip formatter={formatMoney} valueLabel="Total" />} />
                        <Bar dataKey="total" fill={palette === 'clean' ? chartTheme.barAmber : `url(#${GRAD.amberH})`} radius={[0, 8, 8, 0]} maxBarSize={26} isAnimationActive={!reduceMotion} animationDuration={800} animationEasing="ease-out" />
                      </BarChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </motion.div>

              <motion.div initial={reduceMotion ? false : { opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { delay: 0.5 }} className="md:col-span-2">
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Ventas por vendedor</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={250}>
                      <BarChart data={ventasVend} layout="vertical" margin={{ top: 4, right: 16, left: 4, bottom: 0 }} barCategoryGap="30%">
                        <CartesianGrid horizontal={false} strokeDasharray="4 6" stroke={chartTheme.grid} />
                        <XAxis type="number" axisLine={false} tickLine={false} tick={{ fill: chartTheme.axis, fontSize: 11 }} tickFormatter={(v) => compactNumber(v)} />
                        <YAxis type="category" dataKey="name" axisLine={false} tickLine={false} width={120} tick={{ fill: chartTheme.axis, fontSize: 11 }} />
                        <Tooltip cursor={{ fill: chartTheme.cursorFill }} content={<ChartTooltip formatter={formatMoney} valueLabel="Total" />} />
                        <Bar dataKey="total" fill={palette === 'clean' ? chartTheme.barPrimary : `url(#${GRAD.coralH})`} radius={[0, 8, 8, 0]} maxBarSize={26} isAnimationActive={!reduceMotion} animationDuration={800} animationEasing="ease-out" />
                      </BarChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </motion.div>
            </div>
          )}
        </TabsContent>

        {VISTAS.filter((v) => v.value !== 'resumen').map((v) => (
          <TabsContent key={v.value} value={v.value} className="space-y-4">
            {estado === 'Cancelado' ? (
              <Card><CardContent className="p-6 text-sm text-muted-foreground">
                Los reportes excluyen pedidos cancelados; elegí otro estado para ver los datos.
              </CardContent></Card>
            ) : error ? (
              <Card><CardContent className="p-6 text-sm text-destructive">{error}</CardContent></Card>
            ) : loading && !detalle ? (
              <div className="py-20 text-center text-sm text-muted-foreground">Cargando reporte…</div>
            ) : !detalle || detalle.filasGrafico.length === 0 ? (
              <Card><CardContent className="p-6 text-sm text-muted-foreground">No hay datos para los filtros seleccionados.</CardContent></Card>
            ) : (
              <>
                <motion.div initial={reduceMotion ? false : { opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { delay: 0.1 }}>
                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">{v.label}</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <ResponsiveContainer width="100%" height={280}>
                        <BarChart data={graficoData} layout="vertical" margin={{ top: 4, right: 16, left: 4, bottom: 0 }} barCategoryGap="28%">
                          <CartesianGrid horizontal={false} strokeDasharray="4 6" stroke={chartTheme.grid} />
                          <XAxis type="number" axisLine={false} tickLine={false} tick={{ fill: chartTheme.axis, fontSize: 11 }} tickFormatter={(n) => compactNumber(n)} />
                          <YAxis type="category" dataKey="name" axisLine={false} tickLine={false} width={140} tick={{ fill: chartTheme.axis, fontSize: 11 }} />
                          <Tooltip cursor={{ fill: chartTheme.cursorFill }} content={<ChartTooltip formatter={formatMoney} valueLabel="Ingresos" />} />
                          <Bar
                            dataKey="ingresos"
                            fill={palette === 'clean' ? chartTheme.barPrimary : `url(#${GRAD_POR_VISTA[v.value as Exclude<VistaReporte, 'resumen'>]})`}
                            radius={[0, 8, 8, 0]}
                            maxBarSize={24}
                            isAnimationActive={!reduceMotion}
                            animationDuration={800}
                            animationEasing="ease-out"
                          />
                        </BarChart>
                      </ResponsiveContainer>
                    </CardContent>
                  </Card>
                </motion.div>

                <motion.div initial={reduceMotion ? false : { opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={reduceMotion ? { duration: 0 } : { delay: 0.2 }}>
                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">Detalle</CardTitle>
                      <p className="text-xs text-muted-foreground">
                        {detalle.totalFilas} filas · participación sobre el total filtrado
                      </p>
                    </CardHeader>
                    <CardContent className="px-0">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <SortableTableHead label="Nombre" sortKey="nombre" sort={sort} onSort={toggle} />
                            <SortableTableHead label="Pedidos" sortKey="pedidos" sort={sort} onSort={toggle} align="right" />
                            <SortableTableHead label="Unidades" sortKey="unidades" sort={sort} onSort={toggle} align="right" />
                            <SortableTableHead label="Ingresos" sortKey="ingresos" sort={sort} onSort={toggle} align="right" />
                            <SortableTableHead label="Participación" sortKey="participacion" sort={sort} onSort={toggle} align="right" />
                            {detalle.tipo === 'vendedor' && (
                              <>
                                <SortableTableHead label="Comisión" sortKey="comision" sort={sort} onSort={toggle} align="right" />
                                <SortableTableHead label="Comisión estimada" sortKey="comisionEstimada" sort={sort} onSort={toggle} align="right" />
                              </>
                            )}
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {sorted.map((f) => (
                            <TableRow key={f.id}>
                              <TableCell className="font-medium">{f.nombre}</TableCell>
                              <TableCell className="text-right tabular-nums">{f.pedidos}</TableCell>
                              <TableCell className="text-right tabular-nums">{f.unidades}</TableCell>
                              <TableCell className="text-right tabular-nums">{formatMoney(f.ingresos)}</TableCell>
                              <TableCell className="text-right tabular-nums">{f.participacion.toFixed(2)} %</TableCell>
                              {detalle.tipo === 'vendedor' && (
                                <>
                                  <TableCell className="text-right tabular-nums">{Number(f.comision ?? 0).toFixed(2)} %</TableCell>
                                  <TableCell className="text-right tabular-nums">{formatMoney(Number(f.comisionEstimada ?? 0))}</TableCell>
                                </>
                              )}
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>

                      <div className="flex flex-wrap items-center justify-between gap-3 px-5 py-4">
                        <span className="text-xs text-muted-foreground">
                          Página {detalle.pagina} de {detalle.totalPaginas} · {detalle.totalFilas} filas
                        </span>
                        <div className="flex items-center gap-2">
                          <Select value={String(pageSize)} onValueChange={(val) => setPaginacion(1, Number(val))}>
                            <SelectTrigger className="w-24"><SelectValue /></SelectTrigger>
                            <SelectContent>
                              {[10, 25, 50, 100].map((n) => <SelectItem key={n} value={String(n)}>{n}</SelectItem>)}
                            </SelectContent>
                          </Select>
                          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPaginacion(page - 1)}>Anterior</Button>
                          <Button variant="outline" size="sm" disabled={page >= detalle.totalPaginas} onClick={() => setPaginacion(page + 1)}>Siguiente</Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </motion.div>
              </>
            )}
          </TabsContent>
        ))}
      </Tabs>
    </div>
  );
}
