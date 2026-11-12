import { cn } from '@/lib/utils';

interface LogoProps {
  size?: number;
  className?: string;
  showText?: boolean;
}

export default function Logo({ size = 36, className, showText = false }: LogoProps) {
  return (
    <div className={cn('flex items-center gap-2', className)}>
      <svg width={size} height={size} viewBox="0 0 48 48" aria-label="NeoOrders" className="shrink-0">
        <defs>
          <linearGradient id="logo-grad" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stopColor="#ff5c7a" />
            <stop offset="50%" stopColor="#ff8e72" />
            <stop offset="100%" stopColor="#f5a65b" />
          </linearGradient>
          <linearGradient id="logo-grad2" x1="1" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#fda4af" />
            <stop offset="100%" stopColor="#ff5c7a" />
          </linearGradient>
        </defs>
        {/* Rounded square base */}
        <rect x="2" y="2" width="44" height="44" rx="12" fill="url(#logo-grad)" />
        {/* Geometric shapes */}
        <circle cx="18" cy="18" r="8" fill="rgba(255,255,255,0.25)" />
        <rect x="22" y="22" width="16" height="16" rx="4" fill="rgba(255,255,255,0.2)" transform="rotate(15 30 30)" />
        <path d="M14 34 L24 14 L34 34" stroke="#fff" strokeWidth="2.8" fill="none" strokeLinecap="round" strokeLinejoin="round" />
        <circle cx="30" cy="20" r="4" fill="url(#logo-grad2)" opacity="0.9" />
      </svg>
      {showText && (
        <span className="text-sm font-bold tracking-tight">NeoOrders</span>
      )}
    </div>
  );
}
