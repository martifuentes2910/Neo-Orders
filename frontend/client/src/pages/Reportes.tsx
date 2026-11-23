import { useSettings } from '../context/SettingsContext';
import { useChartTheme } from '../hooks/useChartTheme';
import { ORDER_STATUSES } from '../types';
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

import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';
import ChartGradients from '@/components/ChartGradients';
import { GRAD } from '@/lib/chartGradients';
import ChartTooltip from '@/components/ChartTooltip';
import { compactNumber } from '@/lib/format';
import { VISTAS, useReports, type VistaReporte } from '@/features/reports';

const GRAD_POR_VISTA: Record<Exclude<VistaReporte, 'resumen'>, string> = {
  vendedores: GRAD.coralH,
  productos: GRAD.peachH,
  clientes: GRAD.amberH,
};

/** Página de Reportes: composición visual; filtros, fetch y derivaciones viven en `useReports`. */
export default function Reportes({ vista = 'resumen' }: { vista?: VistaReporte }) {
  const { formatMoney } = useSettings();
  const chartTheme = useChartTheme();
  const { resolved, palette } = useTheme();
  const isDark = resolved === 'oscuro';
  const reduceMotion = useReducedMotion();
  const {
    estado, page, pageSize, borrador, period, setBorradorFiltro, cambiarPeriodo,
    aplicar, clear, setPaginacion, cambiarVista, clients, sellers, resumen, detalle,
    loading, error, pieData, barMes, topProd, topCli, ventasVend, totalPedidos,
    sorted, sort, toggle, graficoData,
  } = useReports({ vista, isDark, palette });

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
