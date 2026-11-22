/** Formato compacto para ejes de gráficos (1.2 M, 850 k). */
export function compactNumber(value: number): string {
  try {
    return new Intl.NumberFormat('es-AR', {
      notation: 'compact',
      maximumFractionDigits: 1,
    }).format(value);
  } catch {
    return String(value);
  }
}

/** Fecha/hora corta en español para listados de actividad. */
export function formatDateTime(value: string | Date): string {
  try {
    const date = typeof value === 'string' ? new Date(value) : value;
    if (Number.isNaN(date.getTime())) return '';
    return new Intl.DateTimeFormat('es-AR', {
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  } catch {
    return '';
  }
}

/** "Hace 5 min", "Hoy 14:30", "12 nov 09:10". */
export function formatRelativeDate(value: string | Date): string {
  try {
    const date = typeof value === 'string' ? new Date(value) : value;
    if (Number.isNaN(date.getTime())) return '';
    const diffMs = Date.now() - date.getTime();
    const minutes = Math.floor(diffMs / 60000);
    if (minutes < 1) return 'Ahora';
    if (minutes < 60) return `Hace ${minutes} min`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24 && date.getDate() === new Date().getDate()) return `Hace ${hours} h`;
    return formatDateTime(date);
  } catch {
    return '';
  }
}
