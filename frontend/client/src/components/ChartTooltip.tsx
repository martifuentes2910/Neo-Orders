interface TooltipItem {
  name?: string;
  value?: number | string;
  color?: string;
  payload?: { color?: string } & Record<string, unknown>;
}

interface ChartTooltipProps {
  active?: boolean;
  payload?: TooltipItem[];
  label?: string | number;
  formatter?: (value: number) => string;
  labelFormatter?: (label: string | number) => string;
  valueLabel?: string;
  hideLabel?: boolean;
}

/**
 * Tooltip propio para Recharts: tarjeta con la paleta del tema en lugar del
 * cuadro gris por defecto. Recharts inyecta `active`, `payload` y `label`.
 */
export default function ChartTooltip({
  active,
  payload,
  label,
  formatter,
  labelFormatter,
  valueLabel,
  hideLabel,
}: ChartTooltipProps) {
  if (!active || !payload?.length) return null;

  return (
    <div className="min-w-[132px] rounded-xl border border-border bg-popover px-3.5 py-2.5 text-popover-foreground shadow-xl">
      {!hideLabel && label !== undefined && (
        <p className="mb-1.5 text-xs font-semibold uppercase tracking-widest text-muted-foreground">
          {labelFormatter ? labelFormatter(label) : label}
        </p>
      )}
      <div className="space-y-1">
        {payload.map((item, index) => {
          const color = item.color || item.payload?.color;
          const value = Number(item.value ?? 0);
          return (
            <div key={`${item.name ?? 'item'}-${index}`} className="flex items-center gap-2 text-sm">
              <span className="h-2 w-2 shrink-0 rounded-full" style={{ background: color }} />
              <span className="text-muted-foreground">{valueLabel ?? item.name}</span>
              <span className="ml-auto pl-3 font-semibold tabular-nums">
                {formatter ? formatter(value) : value.toLocaleString('es-AR')}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
