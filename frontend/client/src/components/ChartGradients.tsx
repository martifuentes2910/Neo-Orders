import { useChartTheme } from '../hooks/useChartTheme';
import { GRAD } from '../lib/chartGradients';

function Gradient({ id, from, to, horizontal }: { id: string; from: string; to: string; horizontal?: boolean }) {
  return (
    <linearGradient id={id} x1="0" y1="0" x2={horizontal ? '1' : '0'} y2={horizontal ? '0' : '1'}>
      <stop offset="0%" stopColor={from} stopOpacity={1} />
      <stop offset="100%" stopColor={to} stopOpacity={0.72} />
    </linearGradient>
  );
}

/**
 * Definiciones SVG de gradientes para Recharts. Se renderiza una sola vez por
 * página; los `fill` de las barras referencian `url(#id)`.
 */
export default function ChartGradients() {
  const t = useChartTheme();
  return (
    <svg width="0" height="0" aria-hidden="true" className="absolute">
      <defs>
        <Gradient id={GRAD.coral} from={t.barPrimary} to={t.barPeach} />
        <Gradient id={GRAD.peach} from={t.barPeach} to={t.barAmber} />
        <Gradient id={GRAD.amber} from={t.barAmber} to={t.barPrimary} />
        <Gradient id={GRAD.coralH} from={t.barPrimary} to={t.barPeach} horizontal />
        <Gradient id={GRAD.peachH} from={t.barPeach} to={t.barAmber} horizontal />
        <Gradient id={GRAD.amberH} from={t.barAmber} to={t.barPrimary} horizontal />
      </defs>
    </svg>
  );
}
