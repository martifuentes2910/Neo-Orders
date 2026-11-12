import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { STATUS_COLORS } from '../types';
import { ShoppingCart, Users, Clock, CheckCircle, DollarSign, Briefcase } from 'lucide-react';
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';

export default function Panel() {
  const { formatMoney } = useSettings();
  const [metrics, setMetrics] = useState<any>(null);
  const [reportes, setReportes] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([api.dashboard.metricas(), api.reports()])
      .then(([m, r]) => { setMetrics(m); setReportes(r); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Cargando panel…</div>;
  if (!metrics) return <div className="empty">No se pudieron cargar las métricas.</div>;

  const pieData = (metrics.porEstado || []).map((e: any) => ({
    name: e.estado,
    value: e.cantidad,
    color: STATUS_COLORS[e.estado as keyof typeof STATUS_COLORS] || '#6b7280',
  }));

  const barData = (reportes?.porMes || []).map((m: any) => ({
    name: `${m.month}/${String(m.year).slice(2)}`,
    ingresos: m.ingresos,
  }));

  const cards = [
    { label: 'Total de pedidos', value: metrics.totalPedidos, icon: ShoppingCart, color: 'var(--primary)' },
    { label: 'Ingresos totales', value: formatMoney(metrics.ingresos), icon: DollarSign, color: 'var(--success)' },
    { label: 'Pendientes', value: metrics.pendientes, icon: Clock, color: 'var(--warning)' },
    { label: 'Entregados', value: metrics.entregados, icon: CheckCircle, color: 'var(--accent-2)' },
    { label: 'Clientes', value: metrics.clientes, icon: Users, color: 'var(--accent)' },
    { label: 'Vendedores', value: metrics.vendedores, icon: Briefcase, color: 'var(--accent-peach)' },
  ];

  return (
    <div className="page">
      <div className="page-header">
        <h1>Panel de control</h1>
        <p className="page-sub">Resumen general de los pedidos y el negocio.</p>
      </div>

      <div className="stat-grid">
        {cards.map((c) => {
          const Icon = c.icon;
          return (
            <div className="stat-card" key={c.label}>
              <div className="stat-icon" style={{ background: c.color }}>
                <Icon size={20} color="#fff" />
              </div>
              <div>
                <span className="stat-label">{c.label}</span>
                <span className="stat-value">{c.value}</span>
              </div>
            </div>
          );
        })}
      </div>

      <div className="charts-grid">
        <div className="card">
          <h3>Pedidos por estado</h3>
          <ResponsiveContainer width="100%" height={260}>
            <PieChart>
              <Pie data={pieData} dataKey="value" nameKey="name" innerRadius={55} outerRadius={90} paddingAngle={3}>
                {pieData.map((d: any, i: number) => (
                  <Cell key={i} fill={d.color} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
          <div className="legend">
            {pieData.map((d: any) => (
              <span key={d.name} className="legend-item">
                <span className="dot" style={{ background: d.color }} />
                {d.name} ({d.value})
              </span>
            ))}
          </div>
        </div>

        <div className="card">
          <h3>Ingresos por mes</h3>
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={barData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="name" stroke="var(--muted)" fontSize={12} />
              <YAxis stroke="var(--muted)" fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
              <Tooltip formatter={(v: any) => formatMoney(Number(v))} />
              <Bar dataKey="ingresos" fill="var(--primary)" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="card">
        <h3>Pedidos recientes</h3>
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Cliente</th>
              <th>Producto</th>
              <th>Total</th>
              <th>Fecha</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            {(metrics.recientes || []).map((r: any) => (
              <tr key={r.id}>
                <td>{r.id}</td>
                <td>{r.clientName}</td>
                <td>{r.productName}</td>
                <td className="total">{formatMoney(r.total)}</td>
                <td>{new Date(r.fecha).toLocaleDateString('es-AR')}</td>
                <td>
                  <span className="badge" style={{ background: STATUS_COLORS[r.estado as keyof typeof STATUS_COLORS] }}>
                    {r.estado}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
