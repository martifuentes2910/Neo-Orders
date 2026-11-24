import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useSettings } from '../context/SettingsContext';
import { useChartTheme } from '../hooks/useChartTheme';
import { useDashboardMetrics } from '@/features/dashboard';
import { getStatusColor, getStatusTextColor, type OrderStatus } from '../types';
import { useTheme } from '../context/ThemeContext';
import { motion, useReducedMotion } from 'framer-motion';
import {
  ShoppingCart, Users, DollarSign, TrendingUp, ArrowRight, type LucideIcon,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHeader, TableRow } from '@/components/ui/table';
import SortableTableHead from '@/components/SortableTableHead';
import ProductImage from '@/components/ProductImage';
import { useTableSort } from '@/hooks/useTableSort';
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, AreaChart, Area } from 'recharts';
import ChartGradients from '@/components/ChartGradients';
import { GRAD } from '@/lib/chartGradients';

import ChartTooltip from '@/components/ChartTooltip';
import { compactNumber } from '@/lib/format';
import type { PedidoReciente, SerieMensual, TopProducto } from '../types/reportes';

function AnimatedNumber({ value }: { value: number }) {
  const [display, setDisplay] = useState(0);
  const frameRef = useRef<number | null>(null);
  useEffect(() => {
    const duration = 1200;
    const startTime = performance.now();
    const animate = (now: number) => {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setDisplay(Math.round(value * eased));
      if (progress < 1) frameRef.current = requestAnimationFrame(animate);
    };
    frameRef.current = requestAnimationFrame(animate);
    return () => {
      if (frameRef.current !== null) cancelAnimationFrame(frameRef.current);
    };
  }, [value]);
  return <>{display.toLocaleString('es-AR')}</>;
}

export default function Panel() {
  const { formatMoney } = useSettings();
  const chartTheme = useChartTheme();
  const { resolved, palette } = useTheme();
  const isDark = resolved === 'oscuro';
  const reduceMotion = useReducedMotion();
  const { metrics, loading } = useDashboardMetrics();

  const recientes: PedidoReciente[] = metrics?.recientes ?? [];
  const { sorted: recentSorted, sort, toggle } = useTableSort(recientes, {
    id: (r: PedidoReciente) => r.id,
    cliente: (r: PedidoReciente) => r.clientName,
    producto: (r: PedidoReciente) => r.productName,
    total: (r: PedidoReciente) => r.total,
    fecha: (r: PedidoReciente) => new Date(r.fecha).getTime(),
    estado: (r: PedidoReciente) => ['Pendiente', 'Procesado', 'Enviado', 'Entregado', 'Cancelado'].indexOf(r.estado),
  });

  if (loading) return <div className="flex items-center justify-center py-20 text-muted-foreground">Cargando panel…</div>;
  if (!metrics) return <div className="py-20 text-center text-muted-foreground">No se pudieron cargar las métricas.</div>;

  const serie: SerieMensual[] = metrics.serieMensual ?? [];
  const topProductos: TopProducto[] = metrics.topProductos ?? [];

  const ordenEstados: OrderStatus[] = ['Cancelado', 'Pendiente', 'Procesado', 'Enviado', 'Entregado'];
  const pieData = (metrics.porEstado || [])
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

  const barData = serie.map((m) => ({
    name: `${m.month}/${String(m.year).slice(2)}`,
    ingresos: m.ingresos,
  }));

  const unidadesData = serie.map((m) => ({
    name: `${m.month}/${String(m.year).slice(2)}`,
    unidades: m.unidades,
  }));

  const totalPedidos = pieData.reduce((acc, d) => acc + Number(d.value || 0), 0);

  const ticketPromedio = metrics.ingresos / Math.max(metrics.totalPedidos, 1);

  const cards: Array<{ label: string; value: number; icon: LucideIcon; light: string[]; dark: string[]; isMoney: boolean; cleanAccent: string }> = [
    { label: 'Total de pedidos', value: metrics.totalPedidos, icon: ShoppingCart, light: ['#ff5c7a', '#ff8e72'], dark: ['#c74b68', '#c97a6c'], isMoney: false, cleanAccent: isDark ? '#a7e36a' : '#8fd14f' },
    { label: 'Ingresos totales', value: metrics.ingresos, icon: DollarSign, light: ['#ff8e72', '#f5a65b'], dark: ['#c97a6c', '#c99a4e'], isMoney: true, cleanAccent: isDark ? '#ffa94d' : '#f08a24' },
    { label: 'Clientes', value: metrics.clientes, icon: Users, light: ['#ff5c7a', '#fda4af'], dark: ['#c74b68', '#c97a95'], isMoney: false, cleanAccent: isDark ? '#a78bfa' : '#7c5ce6' },
    { label: 'Ticket promedio', value: ticketPromedio, icon: TrendingUp, light: ['#f5a65b', '#ff8e72'], dark: ['#c99a4e', '#c97a6c'], isMoney: true, cleanAccent: isDark ? '#a7e36a' : '#8fd14f' },
  ];

  const mentaGradients = isDark
    ? [['#2A9C84', '#1F7A67'], ['#1F7A67', '#2A9C84'], ['#35C0A3', '#2A9C84'], ['#1F7A67', '#7FD8C3']]
    : [['#35C0A3', '#2A9C84'], ['#2A9C84', '#1F7A67'], ['#2A9C84', '#7FD8C3'], ['#0E4D41', '#35C0A3']];

  const container = { hidden: { opacity: 0 }, show: { opacity: 1, transition: { staggerChildren: 0.06 } } };
  const item = { hidden: { opacity: 0, y: 12 }, show: { opacity: 1, y: 0 } };
  const entrada = (delay: number) => ({
    initial: reduceMotion ? false : { opacity: 0, y: 12 },
    animate: { opacity: 1, y: 0 },
    transition: reduceMotion ? { duration: 0 } : { delay },
  });

  const recientesVisibles = recentSorted.slice(0, 7);

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      <ChartGradients />
      <motion.div {...entrada(0)} className="flex shrink-0 flex-col gap-0.5">
        <h1 className="text-[32px] font-bold tracking-tight leading-tight">Panel de control</h1>
        <p className="text-base text-muted-foreground">Resumen general de los pedidos y el negocio.</p>
      </motion.div>

      <motion.div variants={container} initial="hidden" animate="show" className="grid shrink-0 grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map((c, index) => {
          const Icon = c.icon;
          const gradient = palette === 'menta' ? mentaGradients[index] : isDark ? c.dark : c.light;
          const isClean = palette === 'clean';
          const cleanAccent = c.cleanAccent;
          return (
            <motion.div key={c.label} variants={item} className="min-w-0">
              <Card className={`overflow-hidden shadow-sm ${isClean ? 'glass-surface border-0' : 'border-0'}`}>
                <CardContent className="p-0">
                  <div
                    className={isClean ? 'relative px-5 py-5 text-foreground' : 'px-5 py-5 text-white'}
                    style={isClean ? undefined : { background: `linear-gradient(135deg, ${gradient.join(', ')})` }}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="min-w-0">
                        <p className={`truncate text-base font-medium ${isClean ? 'text-muted-foreground' : 'text-white/85'}`}>{c.label}</p>
                        <p className="mt-2 truncate text-[34px] font-bold tabular-nums leading-none tracking-tight">
                          {c.isMoney ? formatMoney(c.value) : <AnimatedNumber value={c.value} />}
                        </p>
                      </div>
                      <div
                        className={`shrink-0 rounded-xl p-2.5 ${isClean ? '' : `backdrop-blur ${isDark ? 'bg-black/20' : 'bg-white/20'}`}`}
                        style={isClean ? { color: cleanAccent, backgroundColor: `color-mix(in srgb, ${cleanAccent} 14%, transparent)` } : undefined}
                      >
                        <Icon className="h-5 w-5" />
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </motion.div>
          );
        })}
      </motion.div>

      <div className="grid min-h-0 flex-1 gap-3 xl:grid-cols-3">
        <motion.div {...entrada(0.2)} className="min-h-0 min-w-0">
          <Card className="flex h-full min-h-0 flex-col">
            <CardHeader className="shrink-0 px-5 pb-2 pt-4">
              <CardTitle className="text-lg font-semibold">Pedidos por estado</CardTitle>
            </CardHeader>
            <CardContent className="flex min-h-0 flex-1 flex-col px-5 pb-4">
              <div className="relative min-h-[180px] flex-1">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={pieData} dataKey="value" nameKey="name" innerRadius="62%" outerRadius="82%" paddingAngle={3} cornerRadius={6} stroke="hsl(var(--card))" strokeWidth={2} isAnimationActive={!reduceMotion} animationDuration={700}>
                      {pieData.map((d, i) => <Cell key={i} fill={d.color} />)}
                    </Pie>
                    <Tooltip cursor={false} content={<ChartTooltip valueLabel="Pedidos" hideLabel />} />
                  </PieChart>
                </ResponsiveContainer>
                <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
                  <span className="text-3xl font-bold tabular-nums">{totalPedidos}</span>
                  <span className="text-sm font-medium text-muted-foreground">Pedidos</span>
                </div>
              </div>
              <div className="mt-2 flex flex-wrap justify-center gap-x-3 gap-y-1.5">
                {pieData.map((d) => (
                  <span key={d.name} className="flex items-center gap-1.5 text-sm text-muted-foreground">
                    <span className="h-2.5 w-2.5 rounded-full" style={{ background: d.color }} />
                    <span>{d.name}</span>
                    <span className="font-semibold tabular-nums text-foreground">{d.value}</span>
                  </span>
                ))}
              </div>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div {...entrada(0.24)} className="min-h-0 min-w-0">
          <Card className="flex h-full min-h-0 flex-col">
            <CardHeader className="shrink-0 px-5 pb-2 pt-4">
              <CardTitle className="text-lg font-semibold">Ingresos por mes</CardTitle>
            </CardHeader>
            <CardContent className="min-h-0 flex-1 px-5 pb-4">
              <div className="h-full min-h-[180px]">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={barData} margin={{ top: 8, right: 4, left: -8, bottom: 0 }} barCategoryGap="28%">
                  <CartesianGrid vertical={false} strokeDasharray="4 6" stroke={chartTheme.grid} />
                  <XAxis dataKey="name" axisLine={false} tickLine={false} tickMargin={8} tick={{ fill: chartTheme.axis, fontSize: 13 }} />
                  <YAxis axisLine={false} tickLine={false} width={56} tick={{ fill: chartTheme.axis, fontSize: 13 }} tickFormatter={compactNumber} />
                  <Tooltip cursor={{ fill: chartTheme.cursorFill }} content={<ChartTooltip formatter={formatMoney} valueLabel="Ingresos" labelFormatter={(l) => `Mes ${l}`} />} />
                  <Bar dataKey="ingresos" fill={palette === 'clean' ? chartTheme.barPrimary : `url(#${GRAD.coral})`} radius={[8, 8, 4, 4]} maxBarSize={36} isAnimationActive={!reduceMotion} animationDuration={700} />
                </BarChart>
              </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div {...entrada(0.28)} className="min-h-0 min-w-0">
          <Card className="flex h-full min-h-0 flex-col">
            <CardHeader className="shrink-0 px-5 pb-2 pt-4">
              <CardTitle className="text-lg font-semibold">Unidades por mes</CardTitle>
            </CardHeader>
            <CardContent className="min-h-0 flex-1 px-5 pb-4">
              <div className="h-full min-h-[180px]">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={unidadesData} margin={{ top: 8, right: 4, left: -8, bottom: 0 }}>
                  <defs>
                    <linearGradient id="area-unidades" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor={chartTheme.units ?? chartTheme.barPrimary} stopOpacity={0.35} />
                      <stop offset="100%" stopColor={chartTheme.units ?? chartTheme.barPrimary} stopOpacity={0.02} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid vertical={false} strokeDasharray="4 6" stroke={chartTheme.grid} />
                  <XAxis dataKey="name" axisLine={false} tickLine={false} tickMargin={8} tick={{ fill: chartTheme.axis, fontSize: 13 }} />
                  <YAxis axisLine={false} tickLine={false} width={56} tick={{ fill: chartTheme.axis, fontSize: 13 }} tickFormatter={compactNumber} allowDecimals={false} />
                  <Tooltip cursor={{ stroke: chartTheme.grid }} content={<ChartTooltip valueLabel="Unidades" labelFormatter={(l) => `Mes ${l}`} />} />
                  <Area type="monotone" dataKey="unidades" stroke={chartTheme.units ?? chartTheme.barPrimary} strokeWidth={2.5} fill="url(#area-unidades)" dot={false} isAnimationActive={!reduceMotion} animationDuration={700} />
                </AreaChart>
              </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>
        </motion.div>
      </div>

      <div className="grid min-h-0 flex-[1.2] gap-3 xl:grid-cols-3">
        <motion.div className="min-h-0 min-w-0 xl:col-span-2" {...entrada(0.3)}>
          <Card className="flex h-full min-h-0 flex-col">
            <CardHeader className="flex shrink-0 flex-row items-start justify-between gap-4 px-5 pb-2 pt-4">
              <CardTitle className="text-lg font-semibold">Pedidos recientes</CardTitle>
              <Link to="/pedidos" className="inline-flex shrink-0 items-center gap-1 text-base font-medium text-primary hover:underline">
                Ver todos <ArrowRight className="h-4 w-4" />
              </Link>
            </CardHeader>
            <CardContent className="min-h-0 flex-1 overflow-auto px-5 pb-4">
              <Table className="text-base">
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <SortableTableHead label="#" sortKey="id" sort={sort} onSort={toggle} className="h-11 px-3 text-sm" />
                    <SortableTableHead label="Cliente" sortKey="cliente" sort={sort} onSort={toggle} className="h-11 px-3 text-sm" />
                    <SortableTableHead label="Producto" sortKey="producto" sort={sort} onSort={toggle} className="h-11 px-3 text-sm" />
                    <SortableTableHead label="Total" sortKey="total" sort={sort} onSort={toggle} align="right" className="h-11 px-3 text-right text-sm" />
                    <SortableTableHead label="Fecha" sortKey="fecha" sort={sort} onSort={toggle} className="h-11 px-3 text-sm" />
                    <SortableTableHead label="Estado" sortKey="estado" sort={sort} onSort={toggle} className="h-11 px-3 text-sm" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {recientesVisibles.map((r) => (
                    <TableRow key={r.id}>
                      <TableCell className="px-3 py-3 text-base font-medium">{r.id}</TableCell>
                      <TableCell className="max-w-[180px] truncate px-3 py-3 text-base">{r.clientName}</TableCell>
                      <TableCell className="max-w-[180px] truncate px-3 py-3 text-base">{r.productName}</TableCell>
                      <TableCell className="px-3 py-3 text-right text-base font-semibold tabular-nums">{formatMoney(r.total)}</TableCell>
                      <TableCell className="px-3 py-3 text-base text-muted-foreground">{new Date(r.fecha).toLocaleDateString('es-AR')}</TableCell>
                      <TableCell className="px-3 py-3">
                        <Badge className="px-2.5 py-0.5 text-sm font-medium" style={{ background: getStatusColor(r.estado as OrderStatus, isDark, palette), color: getStatusTextColor(r.estado as OrderStatus, isDark, palette) }}>
                          {r.estado}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div {...entrada(0.34)} className="min-h-0 min-w-0">
          <Card className="flex h-full min-h-0 flex-col">
            <CardHeader className="shrink-0 px-5 pb-2 pt-4">
              <CardTitle className="text-lg font-semibold">Top productos</CardTitle>
            </CardHeader>
            <CardContent className="min-h-0 flex-1 overflow-auto px-5 pb-4">
              {topProductos.length === 0 ? (
                <div className="flex h-full items-center justify-center text-base text-muted-foreground">Sin ventas registradas.</div>
              ) : (
                <ol className="flex h-full min-h-0 flex-col gap-5">
                  {topProductos.map((p, index) => {
                    const maximo = topProductos[0].unidades;
                    const porcentaje = maximo > 0 ? (p.unidades / maximo) * 100 : 0;
                    return (
                      <li key={`${p.productId ?? 'sin-id'}-${p.productName}-${index}`} className="flex min-w-0 flex-col gap-2 rounded-lg py-1">
                        <div className="flex min-w-0 items-center gap-3">
                          <span className="w-5 shrink-0 text-sm font-semibold tabular-nums text-muted-foreground">{index + 1}</span>
                          <ProductImage url={p.imageUrl} name={p.productName} className="h-10 w-10 shrink-0" />
                          <div className="min-w-0 flex-1">
                            <p className="truncate text-base font-medium leading-tight" title={p.productName}>{p.productName}</p>
                            <p className="mt-0.5 truncate text-sm tabular-nums text-muted-foreground">
                              {p.unidades.toLocaleString('es-AR')} u. · {formatMoney(p.ingresos)}
                            </p>
                          </div>
                        </div>
                        <div className="ml-8 h-2 overflow-hidden rounded-full bg-muted/70">
                          <div
                            className="h-full rounded-full transition-[width]"
                            style={{
                              width: `${porcentaje}%`,
                              background: palette === 'clean'
                                ? (isDark ? '#ffa94d' : '#f08a24')
                                : palette === 'menta'
                                  ? `linear-gradient(90deg, ${chartTheme.barPeach}, ${chartTheme.barAmber})`
                                  : `linear-gradient(90deg, ${chartTheme.barPrimary}, ${chartTheme.barAmber})`,
                            }}
                          />
                        </div>
                      </li>
                    );
                  })}
                </ol>
              )}
            </CardContent>
          </Card>
        </motion.div>
      </div>
    </div>
  );
}
