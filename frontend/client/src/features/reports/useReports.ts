import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTableSort } from '../../hooks/useTableSort';
import { getStatusColor, type Client, type OrderStatus, type PaletteId, type Seller } from '../../types';
import type {
  FiltrosReporteParams,
  ReporteDetalle,
  ReporteResumenResponse,
  VistaReporte,
} from '../../types/reportes';
import { clientsFeatureApi } from '../clients/api';
import { sellersFeatureApi } from '../sellers/api';
import { reportsFeatureApi } from './api';
import {
  daysAgo,
  nombreDeFila,
  normalizar,
  periodoDe,
  today,
  yearStart,
  type BorradorFiltros,
  type FilaNormalizada,
} from './model';

interface UseReportsArgs {
  vista: VistaReporte;
  isDark: boolean;
  palette: PaletteId;
}

/**
 * Toda la lógica del slice Reportes: filtros en la URL, borrador, catálogos,
 * fetch del resumen/detalle, paginación y datos derivados para los gráficos.
 * La page queda como capa visual.
 */
export function useReports({ vista, isDark, palette }: UseReportsArgs) {
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
  const [clients, setClients] = useState<Client[]>([]);
  const [sellers, setSellers] = useState<Seller[]>([]);

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
    clientsFeatureApi.list().then(setClients).catch(() => {});
    sellersFeatureApi.list(true).then(setSellers).catch(() => {});
  }, []);

  useEffect(() => {
    if (vista !== 'resumen') return;
    let activo = true;
    reportsFeatureApi.resumen(filtros)
      .then((data) => { if (activo) { setResumen(data); setError(null); } })
      .catch((e: Error) => { if (activo) setError(e.message || 'No se pudieron cargar los reportes.'); })
      .finally(() => { if (activo) setLoading(false); });
    return () => { activo = false; };
  }, [vista, filtros]);

  useEffect(() => {
    if (vista === 'resumen') return;
    let activo = true;
    const peticion = vista === 'vendedores'
      ? reportsFeatureApi.detalle.vendedores({ ...filtros, page, pageSize })
      : vista === 'productos'
        ? reportsFeatureApi.detalle.productos({ ...filtros, page, pageSize })
        : reportsFeatureApi.detalle.clientes({ ...filtros, page, pageSize });
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

  const filas = useMemo(
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

  return {
    // Filtros (URL + borrador)
    desde, hasta, clientId, sellerId, estado, page, pageSize,
    borrador, period,
    setBorradorFiltro, cambiarPeriodo, aplicar, clear, setPaginacion, cambiarVista,
    // Catálogos y resultados
    clients, sellers, resumen, detalle, loading, error,
    // Datos derivados para gráficos y tabla
    pieData, barMes, topProd, topCli, ventasVend, totalPedidos,
    sorted, sort, toggle, graficoData,
  };
}
