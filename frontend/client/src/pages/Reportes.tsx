import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { useChartTheme } from '../hooks/useChartTheme';
import { api } from '../api';
import { ORDER_STATUSES, getStatusColor } from '../types';
import { useTheme } from '../context/ThemeContext';
import { motion } from 'framer-motion';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';

function today() { return new Date().toISOString().slice(0, 10); }
function daysAgo(n: number) { const d = new Date(); d.setDate(d.getDate() - n); return d.toISOString().slice(0, 10); }
function yearStart() { return `${new Date().getFullYear()}-01-01`; }

export default function Reportes() {
  const { formatMoney } = useSettings();
  const chartTheme = useChartTheme();
  const { resolved } = useTheme();
  const isDark = resolved === 'oscuro';
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [clients, setClients] = useState<any[]>([]);
  const [sellers, setSellers] = useState<any[]>([]);

  const [period, setPeriod] = useState('30');
  const [desde, setDesde] = useState(daysAgo(30));
  const [hasta, setHasta] = useState(today());
  const [clientId, setClientId] = useState('');
  const [sellerId, setSellerId] = useState('');
  const [estado, setEstado] = useState('Todos');

  const applyPreset = (p: string) => {
    setPeriod(p);
    if (p === '30') { setDesde(daysAgo(30)); setHasta(today()); }
    else if (p === '90') { setDesde(daysAgo(90)); setHasta(today()); }
    else if (p === 'year') { setDesde(yearStart()); setHasta(today()); }
    else if (p === 'all') { setDesde(''); setHasta(''); }
  };

  const load = async () => {
    setLoading(true);
    try {
      const res = await api.reports({
        desde: desde || undefined,
        hasta: hasta || undefined,
        clientId: clientId ? Number(clientId) : undefined,
        sellerId: sellerId ? Number(sellerId) : undefined,
        estado,
      });
      setData(res);
    } catch {
      /* ignore */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [desde, hasta, clientId, sellerId, estado]);
  useEffect(() => {
    api.clients.list().then(setClients).catch(() => {});
    api.sellers.list(true).then(setSellers).catch(() => {});
  }, []);

  const clear = () => {
    setPeriod('30');
    setDesde(daysAgo(30));
    setHasta(today());
    setClientId('');
    setSellerId('');
    setEstado('Todos');
  };

  if (loading && !data) return <div className="flex items-center justify-center py-20 text-muted-foreground">Cargando reportes…</div>;

  const pieData = (data?.porEstado || []).map((e: any) => ({
    name: e.estado, value: e.cantidad, color: getStatusColor(e.estado as any, isDark) || '#6b7280',
  }));
  const barMes = (data?.porMes || []).map((m: any) => ({ name: `${m.month}/${String(m.year).slice(2)}`, ingresos: m.ingresos }));
  const topProd = (data?.topProductos || []).map((p: any) => ({ name: p.producto, ingresos: p.ingresos }));
  const topCli = (data?.topClientes || []).map((c: any) => ({ name: c.cliente, total: c.total }));
  const ventasVend = (data?.ventasPorVendedor || []).map((v: any) => ({ name: v.vendedor, total: v.total }));

  return (
    <div className="space-y-6">
      <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
        <h1 className="text-2xl font-bold tracking-tight">Reportes</h1>
        <p className="text-sm text-muted-foreground">Análisis y gráficos del negocio.</p>
      </motion.div>

      <Card>
        <CardContent className="p-4">
          <div className="flex flex-wrap items-end gap-3">
            <div className="space-y-1">
              <Label className="text-xs uppercase tracking-wider text-muted-foreground">Período</Label>
              <Select value={period} onValueChange={applyPreset}>
                <SelectTrigger className="w-40"><SelectValue /></SelectTrigger>
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
              <Label className="text-xs uppercase tracking-wider text-muted-foreground">Desde</Label>
              <Input type="date" value={desde} onChange={(e) => { setDesde(e.target.value); setPeriod('custom'); }} className="w-36" />
            </div>
            <div className="space-y-1">
              <Label className="text-xs uppercase tracking-wider text-muted-foreground">Hasta</Label>
              <Input type="date" value={hasta} onChange={(e) => { setHasta(e.target.value); setPeriod('custom'); }} className="w-36" />
            </div>
            <div className="space-y-1">
              <Label className="text-xs uppercase tracking-wider text-muted-foreground">Cliente</Label>
              <Select value={clientId || 'all'} onValueChange={(v) => setClientId(v === 'all' ? '' : v)}>
                <SelectTrigger className="w-40"><SelectValue placeholder="Todos" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  {clients.map((c) => <SelectItem key={c.id} value={String(c.id)}>{c.nombre}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label className="text-xs uppercase tracking-wider text-muted-foreground">Vendedor</Label>
              <Select value={sellerId || 'all'} onValueChange={(v) => setSellerId(v === 'all' ? '' : v)}>
                <SelectTrigger className="w-40"><SelectValue placeholder="Todos" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  {sellers.map((s) => <SelectItem key={s.id} value={String(s.id)}>{s.name}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label className="text-xs uppercase tracking-wider text-muted-foreground">Estado</Label>
              <Select value={estado} onValueChange={setEstado}>
                <SelectTrigger className="w-36"><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Todos">Todos</SelectItem>
                  {ORDER_STATUSES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <Button variant="outline" size="sm" onClick={clear}>Limpiar</Button>
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-2">
        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}>
          <Card>
            <CardHeader><CardTitle className="text-base">Pedidos por estado</CardTitle></CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={260}>
                <PieChart>
                  <Pie data={pieData} dataKey="value" nameKey="name" innerRadius={55} outerRadius={90} paddingAngle={3}>
                    {pieData.map((d: any, i: number) => <Cell key={i} fill={d.color} />)}
                  </Pie>
                  <Tooltip contentStyle={{ background: chartTheme.tooltipBg, border: '1px solid ' + chartTheme.tooltipBorder, borderRadius: '8px', color: chartTheme.tooltipText }} />
                </PieChart>
              </ResponsiveContainer>
              <div className="mt-2 flex flex-wrap gap-3 text-xs text-muted-foreground">
                {pieData.map((d: any) => (
                  <span key={d.name} className="flex items-center gap-1.5">
                    <span className="h-2.5 w-2.5 rounded-full" style={{ background: d.color }} />
                    {d.name} ({d.value})
                  </span>
                ))}
              </div>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }}>
          <Card>
            <CardHeader><CardTitle className="text-base">Ingresos por mes</CardTitle></CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={barMes}>
                  <CartesianGrid strokeDasharray="3 3" stroke={chartTheme.grid} />
                  <XAxis dataKey="name" stroke={chartTheme.axis} fontSize={12} />
                  <YAxis stroke={chartTheme.axis} fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <Tooltip formatter={(v: any) => formatMoney(Number(v))} contentStyle={{ background: chartTheme.tooltipBg, border: '1px solid ' + chartTheme.tooltipBorder, borderRadius: '8px', color: chartTheme.tooltipText }} />
                  <Bar dataKey="ingresos" fill="hsl(var(--primary))" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 }}>
          <Card>
            <CardHeader><CardTitle className="text-base">Productos más vendidos</CardTitle></CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={topProd} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" stroke={chartTheme.grid} />
                  <XAxis type="number" stroke={chartTheme.axis} fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <YAxis type="category" dataKey="name" stroke={chartTheme.axis} fontSize={11} width={120} />
                  <Tooltip formatter={(v: any) => formatMoney(Number(v))} contentStyle={{ background: chartTheme.tooltipBg, border: '1px solid ' + chartTheme.tooltipBorder, borderRadius: '8px', color: chartTheme.tooltipText }} />
                  <Bar dataKey="ingresos" fill="#ff8e72" radius={[0, 6, 6, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.4 }}>
          <Card>
            <CardHeader><CardTitle className="text-base">Clientes con más compras</CardTitle></CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={topCli} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" stroke={chartTheme.grid} />
                  <XAxis type="number" stroke={chartTheme.axis} fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <YAxis type="category" dataKey="name" stroke={chartTheme.axis} fontSize={11} width={120} />
                  <Tooltip formatter={(v: any) => formatMoney(Number(v))} contentStyle={{ background: chartTheme.tooltipBg, border: '1px solid ' + chartTheme.tooltipBorder, borderRadius: '8px', color: chartTheme.tooltipText }} />
                  <Bar dataKey="total" fill="#f5a65b" radius={[0, 6, 6, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.5 }} className="md:col-span-2">
          <Card>
            <CardHeader><CardTitle className="text-base">Ventas por vendedor</CardTitle></CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={ventasVend} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" stroke={chartTheme.grid} />
                  <XAxis type="number" stroke={chartTheme.axis} fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <YAxis type="category" dataKey="name" stroke={chartTheme.axis} fontSize={11} width={120} />
                  <Tooltip formatter={(v: any) => formatMoney(Number(v))} contentStyle={{ background: chartTheme.tooltipBg, border: '1px solid ' + chartTheme.tooltipBorder, borderRadius: '8px', color: chartTheme.tooltipText }} />
                  <Bar dataKey="total" fill="hsl(var(--primary))" radius={[0, 6, 6, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </motion.div>
      </div>
    </div>
  );
}
