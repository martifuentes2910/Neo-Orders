import { useMemo, useState } from 'react';

export type SortDirection = 'asc' | 'desc';
export type SortValue = string | number | boolean | null | undefined;

export interface SortState {
  key: string | null;
  direction: SortDirection;
}

type Accessors<T> = Record<string, (row: T) => SortValue>;

function compareValues(a: SortValue, b: SortValue): number {
  const aEmpty = a === null || a === undefined || a === '';
  const bEmpty = b === null || b === undefined || b === '';
  if (aEmpty && bEmpty) return 0;
  if (aEmpty) return 1;
  if (bEmpty) return -1;

  if (typeof a === 'number' && typeof b === 'number') return a - b;
  if (typeof a === 'boolean' && typeof b === 'boolean') return Number(a) - Number(b);

  return String(a).localeCompare(String(b), 'es', { sensitivity: 'base', numeric: true });
}

/**
 * Ordena filas ya cargadas/filtradas. Click 1: ascendente, click 2: descendente,
 * click 3: vuelve al orden original.
 */
export function useTableSort<T>(rows: T[], accessors: Accessors<T>) {
  const [sort, setSort] = useState<SortState>({ key: null, direction: 'asc' });

  const toggle = (key: string) => {
    setSort((prev) => {
      if (prev.key !== key) return { key, direction: 'asc' };
      if (prev.direction === 'asc') return { key, direction: 'desc' };
      return { key: null, direction: 'asc' };
    });
  };

  const sorted = useMemo(() => {
    if (!sort.key) return rows;
    const get = accessors[sort.key];
    if (!get) return rows;

    return [...rows].sort((a, b) => {
      const av = get(a);
      const bv = get(b);
      const aEmpty = av === null || av === undefined || av === '';
      const bEmpty = bv === null || bv === undefined || bv === '';
      const cmp = compareValues(av, bv);
      // Los valores vacíos quedan siempre al final, en cualquier dirección.
      if (aEmpty || bEmpty) return cmp;
      return sort.direction === 'asc' ? cmp : -cmp;
    });
  }, [rows, sort, accessors]);

  return { sorted, sort, toggle };
}
