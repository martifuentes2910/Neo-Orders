import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { useChartTheme } from '../hooks/useChartTheme';
import { api } from '../api';
import { getStatusColor } from '../types';
import { useTheme } from '../context/ThemeContext';
import { motion } from 'framer-motion';
import { ShoppingCart, Users, Clock, CheckCircle, DollarSign, Briefcase } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';

function AnimatedNumber({ value }: { value: number }) {
  const [display, setDisplay] = useState(0);
  useEffect(() => {
    const duration = 1200;
    const startTime = performance.now();
    const animate = (now: number) => {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setDisplay(Math.round(value * eased));
      if (progress < 1) requestAnimationFrame(animate);
    };
    requestAnimationFrame(animate);
  }, [value]);
  return <>{display.toLocaleString('es-AR')}</>;
}

export default function Panel() {
  const { formatMoney } = useSettings();
  const chartTheme = useChartTheme();
  const { resolved } = useTheme();
  const isDark = resolved === 'oscuro';
  const [metrics, setMetrics] = useState<any>(null);
  const [reportes, setReportes] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([api.dashboard.metricas(), api.reports()])
      .then(([m, r]) => { setMetrics(m); setReportes(r); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="flex items-center justify-center py-20 text-muted-foreground">Cargando panel…</div>;
  if (!metrics) return <div className="py-20 text-center text-muted-foreground">No se pudieron cargar las métricas.</div>;

  const pieData = (metrics.porEstado || []).map((e: any) => ({
    name: e.estado,
    value: e.cantidad,
    color: getStatusColor(e.estado as any, isDark) || '#6b7280',
  }));

  const barData = (reportes?.porMes || []).map((m: any) => ({
    name: `${m.month}/${String(m.year).slice(2)}`,
    ingresos: m.ingresos,
  }));

  const cards = [
    { label: 'Total de pedidos', value: metrics.totalPedidos, icon: ShoppingCart, gradient: 'from-coral to-peach', isMoney: false },
    { label: 'Ingresos totales', value: metrics.ingresos, icon: DollarSign, gradient: 'from-peach to-amber', isMoney: true },
    { label: 'Pendientes', value: metrics.pendientes, icon: Clock, gradient: 'from-amber to-rose', isMoney: false },
    { label: 'Entregados', value: metrics.entregados, icon: CheckCircle, gradient: 'from-coral to-rose', isMoney: false },
    { label: 'Clientes', value: metrics.clientes, icon: Users, gradient: 'from-peach to-coral', isMoney: false },
    { label: 'Vendedores', value: metrics.vendedores, icon: Briefcase, gradient: 'from-amber to-peach', isMoney: false },
  ];

  const container = { hidden: { opacity: 0 }, show: { opacity: 1, transition: { staggerChildren: 0.06 } } };
  const item = { hidden: { opacity: 0, y: 12 }, show: { opacity: 1, y: 0 } };

  return (
    <div className="flex flex-col gap-6">
      <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.25 }}>
        <h1 className="text-2xl font-bold tracking-tight">Panel de control</h1>
        <p className="text-sm text-muted-foreground">Resumen general de los pedidos y el negocio.</p>
      </motion.div>

      <motion.div variants={container} initial="hidden" animate="show" className="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-6">
        {cards.map((c) => {
          const Icon = c.icon;
          return (
            <motion.div key={c.label} variants={item}>
              <Card className="overflow-hidden border-0 shadow-md">
                <CardContent className="p-0">
                  <div className={`bg-gradient-to-br ${c.gradient} p-4 text-white`}>
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-xs font-medium opacity-90">{c.label}</p>
                        <p className="mt-1 text-2xl font-bold">
                          {c.isMoney ? formatMoney(c.value) : <AnimatedNumber value={c.value} />}
                        </p>
                      </div>
                      <div className="rounded-xl bg-white/20 p-2.5 backdrop-blur">
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

      <div className="grid gap-4 lg:grid-cols-2">
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }}>
          <Card className="h-full">
            <CardHeader>
              <CardTitle className="text-base">Pedidos por estado</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={220}>
                <PieChart>
                  <Pie data={pieData} dataKey="value" nameKey="name" innerRadius={55} outerRadius={85} paddingAngle={3}>
                    {pieData.map((d: any, i: number) => (
                      <Cell key={i} fill={d.color} />
                    ))}
                  </Pie>
                  <Tooltip contentStyle={{ background: chartTheme.tooltipBg, border: `1px solid ${chartTheme.tooltipBorder}`, borderRadius: '8px', color: chartTheme.tooltipText, fontSize: '12px' }} />
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

        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.25 }}>
          <Card className="h-full">
            <CardHeader>
              <CardTitle className="text-base">Ingresos por mes</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={barData}>
                  <CartesianGrid strokeDasharray="3 3" stroke={chartTheme.grid} />
                  <XAxis dataKey="name" stroke={chartTheme.axis} fontSize={12} />
                  <YAxis stroke={chartTheme.axis} fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <Tooltip formatter={(v: any) => formatMoney(Number(v))} contentStyle={{ background: chartTheme.tooltipBg, border: `1px solid ${chartTheme.tooltipBorder}`, borderRadius: '8px', color: chartTheme.tooltipText, fontSize: '12px' }} />
                  <Bar dataKey="ingresos" fill="hsl(var(--primary))" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </motion.div>
      </div>

      <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 }}>
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Pedidos recientes</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead>#</TableHead>
                  <TableHead>Cliente</TableHead>
                  <TableHead>Producto</TableHead>
                  <TableHead className="text-right">Total</TableHead>
                  <TableHead>Fecha</TableHead>
                  <TableHead>Estado</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(metrics.recientes || []).map((r: any) => (
                  <TableRow key={r.id}>
                    <TableCell className="font-medium">{r.id}</TableCell>
                    <TableCell>{r.clientName}</TableCell>
                    <TableCell>{r.productName}</TableCell>
                    <TableCell className="text-right font-semibold text-emerald-600 dark:text-emerald-400">{formatMoney(r.total)}</TableCell>
                    <TableCell className="text-muted-foreground">{new Date(r.fecha).toLocaleDateString('es-AR')}</TableCell>
                    <TableCell>
                      <Badge style={{ background: getStatusColor(r.estado as any, isDark) }} className="text-white">
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
    </div>
  );
}
