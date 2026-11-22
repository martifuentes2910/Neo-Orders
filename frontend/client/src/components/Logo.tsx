import { useId } from 'react';
import { useTheme } from '../context/ThemeContext';
import { cn } from '@/lib/utils';

interface LogoProps {
  size?: number;
  className?: string;
  /** `mark`: sólo isotipo; `lockup`: isotipo + wordmark. */
  variant?: 'mark' | 'lockup';
  /** `auto`: color de la paleta activa; `mono`: tinta atual; `inverse`: sobre fondo oscuro. */
  tone?: 'auto' | 'mono' | 'inverse';
}

/**
 * Identidad visual de NeoOrders (KAN-22).
 * El isotipo se construye con tres barras redondeadas que sugieren el flujo
 * de pedidos y un nodo de confirmación. Se dibuja a mano en SVG para que
 * sea nítido a 16 px y no dependa de imágenes generadas.
 */
export default function Logo({ size = 36, className, variant = 'mark', tone = 'auto' }: LogoProps) {
  const { palette } = useTheme();
  const rawId = useId().replace(/:/g, '');
  const gradientId = `neo-logo-grad-${rawId}`;
  const isClean = palette === 'clean';
  const useGradient = tone === 'auto' && !isClean;
  const solid = tone === 'inverse' ? '#ffffff' : tone === 'mono' || isClean ? 'currentColor' : '#131418';

  const gradientStops =
    palette === 'menta' ? ['#0E4D41', '#1F7A67', '#2A9C84'] : ['#D94868', '#C74B68', '#A83D56'];

  const fill = useGradient ? `url(#${gradientId})` : solid;
  const markFill = useGradient ? 'rgba(255,255,255,0.96)' : fill;
  const accent = tone === 'inverse' ? 'rgba(255,255,255,0.75)' : useGradient ? '#ffffff' : 'currentColor';
  const checkStroke = tone === 'inverse' ? '#131418' : useGradient ? gradientStops[1] : 'var(--background, #fff)';
  const shadowId = `neo-logo-shadow-${rawId}`;

  return (
    <div className={cn('flex items-center', variant === 'lockup' ? 'gap-2.5' : '', className)}>
      <svg
        width={size}
        height={size}
        viewBox="0 0 48 48"
        role="img"
        aria-label="NeoOrders"
        className="shrink-0"
      >
        {useGradient && (
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="1" y2="1">
              <stop offset="0%" stopColor={gradientStops[0]} />
              <stop offset="55%" stopColor={gradientStops[1]} />
              <stop offset="100%" stopColor={gradientStops[2]} />
            </linearGradient>
            <filter id={shadowId} x="-20%" y="-20%" width="140%" height="140%">
              <feDropShadow dx="0" dy="0.6" stdDeviation="0.7" floodColor="#000000" floodOpacity="0.22" />
            </filter>
          </defs>
        )}
        <rect x="2" y="2" width="44" height="44" rx="13" fill={useGradient ? fill : 'none'} stroke={useGradient ? 'none' : solid} strokeWidth={useGradient ? 0 : 2} />
        <g filter={useGradient ? `url(#${shadowId})` : undefined}>
          <rect x="11" y="12" width="6.5" height="24" rx="3.25" fill={markFill} />
          <path d="M17.5 13.5 L31 33" stroke={markFill} strokeWidth="6.5" strokeLinecap="round" fill="none" />
          <rect x="31" y="12" width="6.5" height="24" rx="3.25" fill={markFill} />
          <circle cx="34.25" cy="12" r="4.6" fill={accent} />
          <path d="M31.8 12 L33.5 13.7 L36.7 10.2" stroke={checkStroke} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" fill="none" />
        </g>
      </svg>
      {variant === 'lockup' && (
        <span className={cn('whitespace-nowrap font-extrabold tracking-tight', tone === 'inverse' ? 'text-white' : 'text-foreground')} style={{ fontSize: Math.round(size * 0.52) }}>
          Neo<span className="font-semibold opacity-80">Orders</span>
        </span>
      )}
    </div>
  );
}
