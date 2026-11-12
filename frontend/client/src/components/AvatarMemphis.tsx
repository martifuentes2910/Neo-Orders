import { cn } from '@/lib/utils';

const AVATAR_PRESETS = [
  // Preset 1: Circles
  (color: string) => (
    <svg viewBox="0 0 64 64" className="h-full w-full">
      <rect width="64" height="64" rx="16" fill={color} opacity="0.15" />
      <circle cx="32" cy="24" r="12" fill={color} />
      <circle cx="20" cy="44" r="8" fill={color} opacity="0.6" />
      <circle cx="44" cy="44" r="6" fill={color} opacity="0.4" />
    </svg>
  ),
  // Preset 2: Triangle + dots
  (color: string) => (
    <svg viewBox="0 0 64 64" className="h-full w-full">
      <rect width="64" height="64" rx="16" fill={color} opacity="0.15" />
      <polygon points="32,12 52,44 12,44" fill={color} />
      <circle cx="32" cy="30" r="4" fill="#fff" opacity="0.8" />
      <circle cx="24" cy="40" r="3" fill="#fff" opacity="0.6" />
      <circle cx="40" cy="40" r="3" fill="#fff" opacity="0.6" />
    </svg>
  ),
  // Preset 3: Squares + arch
  (color: string) => (
    <svg viewBox="0 0 64 64" className="h-full w-full">
      <rect width="64" height="64" rx="16" fill={color} opacity="0.15" />
      <rect x="16" y="16" width="14" height="14" rx="3" fill={color} />
      <rect x="34" y="16" width="14" height="14" rx="3" fill={color} opacity="0.7" />
      <path d="M16 40 Q32 28 48 40" stroke={color} strokeWidth="4" fill="none" strokeLinecap="round" />
      <circle cx="32" cy="36" r="4" fill={color} opacity="0.5" />
    </svg>
  ),
  // Preset 4: Hexagon + lines
  (color: string) => (
    <svg viewBox="0 0 64 64" className="h-full w-full">
      <rect width="64" height="64" rx="16" fill={color} opacity="0.15" />
      <polygon points="32,10 50,22 50,42 32,54 14,42 14,22" fill={color} opacity="0.9" />
      <line x1="20" y1="28" x2="44" y2="28" stroke="#fff" strokeWidth="2" opacity="0.7" />
      <line x1="24" y1="36" x2="40" y2="36" stroke="#fff" strokeWidth="2" opacity="0.5" />
    </svg>
  ),
  // Preset 5: Abstract face
  (color: string) => (
    <svg viewBox="0 0 64 64" className="h-full w-full">
      <rect width="64" height="64" rx="16" fill={color} opacity="0.15" />
      <circle cx="32" cy="28" r="16" fill={color} />
      <circle cx="26" cy="26" r="3" fill="#fff" />
      <circle cx="38" cy="26" r="3" fill="#fff" />
      <path d="M24 34 Q32 40 40 34" stroke="#fff" strokeWidth="2.5" fill="none" strokeLinecap="round" />
      <rect x="20" y="48" width="24" height="4" rx="2" fill={color} opacity="0.5" />
    </svg>
  ),
  // Preset 6: Waves + diamond
  (color: string) => (
    <svg viewBox="0 0 64 64" className="h-full w-full">
      <rect width="64" height="64" rx="16" fill={color} opacity="0.15" />
      <polygon points="32,8 56,32 32,56 8,32" fill={color} opacity="0.8" />
      <path d="M8 48 Q20 36 32 48 Q44 60 56 48" stroke="#fff" strokeWidth="2" fill="none" opacity="0.6" />
      <circle cx="32" cy="32" r="6" fill="#fff" opacity="0.9" />
    </svg>
  ),
];

const AVATAR_COLORS = ['#ff5c7a', '#ff8e72', '#f5a65b', '#fda4af', '#10b981', '#3b82f6'];

interface AvatarMemphisProps {
  preset?: number;
  size?: number;
  className?: string;
}

export default function AvatarMemphis({ preset = 0, size = 40, className }: AvatarMemphisProps) {
  const idx = ((preset % AVATAR_PRESETS.length) + AVATAR_PRESETS.length) % AVATAR_PRESETS.length;
  const color = AVATAR_COLORS[idx % AVATAR_COLORS.length];
  return (
    <div
      className={cn('overflow-hidden rounded-full', className)}
      style={{ width: size, height: size }}
    >
      {AVATAR_PRESETS[idx](color)}
    </div>
  );
}

export { AVATAR_PRESETS, AVATAR_COLORS };
export const AVATAR_COUNT = AVATAR_PRESETS.length;
