import type { SerieMensual } from '../types/reportes';

export type MetricaSerie = 'pedidos' | 'ingresos' | 'unidades' | 'ticketPromedio';

export interface VariacionMensual {
  texto: string;
  positiva: boolean | null;
}

/**
 * Variación del último mes con datos frente al inmediatamente anterior.
 * Sin dos meses, o con el mes anterior en cero, no se puede calcular: se muestra `—`.
 * La serie ya viene sin cancelados desde la API (KAN-24).
 */
export function variacionMensual(serie: SerieMensual[], clave: MetricaSerie): VariacionMensual {
  if (!serie || serie.length < 2) return { texto: '—', positiva: null };
  const anterior = serie[serie.length - 2][clave];
  const actual = serie[serie.length - 1][clave];
  if (!anterior) return { texto: '—', positiva: null };

  const porcentaje = ((actual - anterior) / anterior) * 100;
  const texto = `${porcentaje > 0 ? '+' : ''}${porcentaje.toLocaleString('es-AR', { maximumFractionDigits: 1 })} %`;
  return { texto, positiva: porcentaje > 0 ? true : porcentaje < 0 ? false : null };
}

/** Últimos seis meses de la serie, listos para un sparkline. */
export function puntosSparkline(serie: SerieMensual[], clave: MetricaSerie) {
  return serie.slice(-6).map((m) => ({
    name: `${m.month}/${String(m.year).slice(2)}`,
    value: m[clave],
  }));
}
