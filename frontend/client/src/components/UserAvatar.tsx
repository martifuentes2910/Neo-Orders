import Avatar, { genConfig } from 'react-nice-avatar';
import { useTheme } from '../context/ThemeContext';
import { cn } from '@/lib/utils';
import { AVATAR_COUNT } from '@/lib/avatars';

function avatarSeed(avatar?: string): string {
  const n = Number.parseInt((avatar || 'avatar-1').replace('avatar-', ''), 10);
  const index = Number.isFinite(n) ? Math.min(Math.max(n, 1), AVATAR_COUNT) : 1;
  return `neoorders-person-${index}`;
}

interface UserAvatarProps {
  avatar?: string;
  size?: number;
  className?: string;
  /** Fuerza el fondo del avatar; por defecto se adapta a la paleta activa. */
  bgColor?: string;
}

export default function UserAvatar({ avatar, size = 36, className, bgColor }: UserAvatarProps) {
  const { palette, resolved } = useTheme();
  const isDark = resolved === 'oscuro';

  const fallbackBg =
    bgColor ??
    (palette === 'clean'
      ? isDark
        ? '#3f3f46'
        : '#d4d4d8'
      : palette === 'menta'
        ? isDark
          ? '#17332b'
          : '#e5f1d4'
        : isDark
          ? '#2b2028'
          : '#ffe4e6');

  const config = {
    ...genConfig(avatarSeed(avatar)),
    bgColor: fallbackBg,
    isGradient: false,
  };

  return (
    <Avatar
      shape="circle"
      className={cn('shrink-0', className)}
      style={{ width: size, height: size }}
      {...config}
    />
  );
}
