import { useEffect, useState } from 'react';
import { useSettings } from '../context/SettingsContext';
import { api } from '../api';
import { ORDER_STATUSES, STATUS_COLORS } from '../types';
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';

function today() { return new Date().toISOString().slice(0, 10); }
function daysAgo(n: number) { const d = new Date(); d.setDate(d.getDate() - n); return d.toISOString().slice(0, 10); }
function yearStart() { return `${new Date().getFullYear()}-01-01`; }

export default function Reportes() {
  const { formatMoney } = useSettings();
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

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [desde, hasta, clientId, sellerId, estado]);

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

  if (loading && !data) return <div className="loading">Cargando reportes…</div>;

  const pieData = (data?.porEstado || []).map((e: any) => ({
    name: e.estado,
    value: e.cantidad,
    color: STATUS_COLORS[e.estado as keyof typeof STATUS_COLORS] || '#6b7280',
  }));

  const barMes = (data?.porMes || []).map((m: any) => ({
    name: `${m.month}/${String(m.year).slice(2)}`,
    ingresos: m.ingresos,
  }));

  const topProd = (data?.topProductos || []).map((p: any) => ({
    name: p.producto,
    ingresos: p.ingresos,
  }));

  const topCli = (data?.topClientes || []).map((c: any) => ({
    name: c.cliente,
    total: c.total,
  }));

  const ventasVend = (data?.ventasPorVendedor || []).map((v: any) => ({
    name: v.vendedor,
    total: v.total,
  }));

  return (
    <div className="page">
      <div className="page-header">
        <h1>Reportes</h1>
        <p className="page-sub">Análisis y gráficos del negocio.</p>
      </div>

      <div className="card filter-bar">
        <div className="filter-group">
          <label>Período</label>
          <select value={period} onChange={(e) => applyPreset(e.target.value)}>
            <option value="30">Últimos 30 días</option>
            <option value="90">Últimos 90 días</option>
            <option value="year">Este año</option>
            <option value="all">Todo el historial</option>
            <option value="custom">Personalizado</option>
          </select>
        </div>
        <div className="filter-group">
          <label>Desde</label>
          <input type="date" value={desde} onChange={(e) => { setDesde(e.target.value); setPeriod('custom'); }} />
        </div>
        <div className="filter-group">
          <label>Hasta</label>
          <input type="date" value={hasta} onChange={(e) => { setHasta(e.target.value); setPeriod('custom'); }} />
        </div>
        <div className="filter-group">
          <label>Cliente</label>
          <select value={clientId} onChange={(e) => setClientId(e.target.value)}>
            <option value="">Todos</option>
            {clients.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
          </select>
        </div>
        <div className="filter-group">
          <label>Vendedor</label>
          <select value={sellerId} onChange={(e) => setSellerId(e.target.value)}>
            <option value="">Todos</option>
            {sellers.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
        <div className="filter-group">
          <label>Estado</label>
          <select value={estado} onChange={(e) => setEstado(e.target.value)}>
            <option value="Todos">Todos</option>
            {ORDER_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
        <button className="btn btn-ghost btn-sm" onClick={clear}>Limpiar</button>
      </div>

      <div className="charts-grid">
        <div className="card">
          <h3>Pedidos por estado</h3>
          <ResponsiveContainer width="100%" height={260}>
            <PieChart>
              <Pie data={pieData} dataKey="value" nameKey="name" innerRadius={55} outerRadius={90} paddingAngle={3}>
                {pieData.map((d: any, i: number) => <Cell key={i} fill={d.color} />)}
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
            <BarChart data={barMes}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="name" stroke="var(--muted)" fontSize={12} />
              <YAxis stroke="var(--muted)" fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
              <Tooltip formatter={(v: any) => formatMoney(Number(v))} />
              <Bar dataKey="ingresos" fill="var(--primary)" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="card">
          <h3>Productos más vendidos</h3>
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={topProd} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis type="number" stroke="var(--muted)" fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
              <YAxis type="category" dataKey="name" stroke="var(--muted)" fontSize={11} width={120} />
              <Tooltip formatter={(v: any) => formatMoney(Number(v))} />
              <Bar dataKey="ingresos" fill="var(--accent-peach)" radius={[0, 6, 6, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="card">
          <h3>Clientes con más compras</h3>
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={topCli} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis type="number" stroke="var(--muted)" fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
              <YAxis type="category" dataKey="name" stroke="var(--muted)" fontSize={11} width={120} />
              <Tooltip formatter={(v: any) => formatMoney(Number(v))} />
              <Bar dataKey="total" fill="var(--accent-amber)" radius={[0, 6, 6, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="card">
          <h3>Ventas por vendedor</h3>
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={ventasVend} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis type="number" stroke="var(--muted)" fontSize={12} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
              <YAxis type="category" dataKey="name" stroke="var(--muted)" fontSize={11} width={120} />
              <Tooltip formatter={(v: any) => formatMoney(Number(v))} />
              <Bar dataKey="total" fill="var(--primary)" radius={[0, 6, 6, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}
