import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react';
import { TableHead } from '@/components/ui/table';
import { cn } from '@/lib/utils';
import type { SortState } from '@/hooks/useTableSort';

interface SortableTableHeadProps {
  label: string;
  sortKey: string;
  sort: SortState;
  onSort: (key: string) => void;
  className?: string;
  align?: 'left' | 'right' | 'center';
}

export default function SortableTableHead({
  label,
  sortKey,
  sort,
  onSort,
  className,
  align = 'left',
}: SortableTableHeadProps) {
  const active = sort.key === sortKey;
  const Icon = !active ? ArrowUpDown : sort.direction === 'asc' ? ArrowUp : ArrowDown;

  return (
    <TableHead
      className={className}
      aria-sort={active ? (sort.direction === 'asc' ? 'ascending' : 'descending') : 'none'}
    >
      <button
        type="button"
        onClick={() => onSort(sortKey)}
        className={cn(
          'group -mx-1 inline-flex items-center gap-1 whitespace-nowrap rounded px-1 py-0.5 font-medium transition-colors hover:text-foreground',
          align === 'right' && 'w-full justify-end text-right',
          align === 'center' && 'w-full justify-center text-center',
          active && 'text-foreground',
        )}
      >
        <span>{label}</span>
        <Icon
          className={cn(
            'h-3.5 w-3.5 shrink-0 transition-opacity',
            active ? 'opacity-100' : 'opacity-40 group-hover:opacity-80',
          )}
        />
      </button>
    </TableHead>
  );
}
