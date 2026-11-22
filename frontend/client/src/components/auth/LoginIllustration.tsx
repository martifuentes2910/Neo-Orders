interface LoginIllustrationProps {
  className?: string;
}

/**
 * Ilustración editorial plana para el panel de marca del Login (KAN-22).
 * Una sola variante neutra: se tiñe con `currentColor` y
 * `var(--illustration-accent)` según la paleta activa, sin imágenes raster.
 */
export default function LoginIllustration({ className }: LoginIllustrationProps) {
  return (
    <svg viewBox="0 0 420 300" role="img" aria-label="Flujo de pedidos de NeoOrders" className={className} fill="none">
      <g opacity="0.35" stroke="currentColor" strokeWidth="2">
        <circle cx="66" cy="54" r="6" />
        <circle cx="352" cy="242" r="8" />
        <path d="M32 236 L32 266 L62 266" strokeLinecap="round" />
        <path d="M356 48 L386 48 L386 78" strokeLinecap="round" />
      </g>

      {/* Tarjetas de pedido apiladas */}
      <rect x="52" y="72" width="198" height="132" rx="16" fill="currentColor" opacity="0.12" transform="rotate(-6 52 72)" />
      <rect x="70" y="64" width="198" height="132" rx="16" fill="currentColor" opacity="0.18" transform="rotate(4 70 64)" />
      <rect x="88" y="74" width="204" height="140" rx="18" fill="currentColor" opacity="0.92" />
      <rect x="108" y="96" width="74" height="10" rx="5" fill="var(--illustration-accent)" />
      <rect x="108" y="118" width="132" height="7" rx="3.5" fill="currentColor" opacity="0.35" />
      <rect x="108" y="134" width="104" height="7" rx="3.5" fill="currentColor" opacity="0.25" />
      <rect x="108" y="166" width="52" height="28" rx="8" fill="var(--illustration-accent)" opacity="0.85" />
      <rect x="170" y="166" width="52" height="28" rx="8" fill="currentColor" opacity="0.18" />

      {/* Paquete y conexión */}
      <path d="M276 186 L330 168 L366 190 L312 210 Z" fill="var(--illustration-accent)" opacity="0.9" />
      <path d="M276 186 L312 210 L312 252 L276 228 Z" fill="currentColor" opacity="0.35" />
      <path d="M312 210 L366 190 L366 230 L312 252 Z" fill="currentColor" opacity="0.55" />
      <path d="M294 177 L330 160 L348 170" stroke="currentColor" strokeWidth="3" strokeLinecap="round" opacity="0.5" />

      {/* Confirmación */}
      <circle cx="330" cy="110" r="30" fill="currentColor" opacity="0.14" />
      <circle cx="330" cy="110" r="22" fill="var(--illustration-accent)" />
      <path d="M319 110 L327 118 L342 101" stroke="#fff" strokeWidth="5" strokeLinecap="round" strokeLinejoin="round" />

      <path d="M238 144 C266 144 276 118 300 116" stroke="currentColor" strokeWidth="2.5" strokeDasharray="6 7" strokeLinecap="round" opacity="0.55" />
    </svg>
  );
}
