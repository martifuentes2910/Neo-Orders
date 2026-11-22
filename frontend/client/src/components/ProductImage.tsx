import { useState } from 'react';
import { Image as ImageIcon } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { thumbnailUrl } from '@/lib/productImage';

type ProductImageProps = {
  url?: string | null;
  name: string;
  className?: string;
};

/** Miniatura con placeholder; al hacer clic abre la imagen completa. La reutilizará la tarjeta Top Productos. */
export default function ProductImage({ url, name, className = 'h-9 w-9' }: ProductImageProps) {
  const [failedFor, setFailedFor] = useState<string | null>(null);
  const [open, setOpen] = useState(false);

  const placeholder = (
    <div className={`${className} flex shrink-0 items-center justify-center rounded-md border border-dashed border-muted-foreground/40 bg-muted/40 text-muted-foreground`} title="Sin imagen">
      <ImageIcon className="h-4 w-4" aria-hidden="true" />
      <span className="sr-only">Sin imagen</span>
    </div>
  );

  if (!url) return placeholder;

  const src = failedFor === url ? url : (thumbnailUrl(url) ?? url);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className={`${className} shrink-0 overflow-hidden rounded-md border bg-muted`}
        title={`Ver imagen de ${name}`}
        aria-label={`Ver imagen de ${name}`}
      >
        <img
          src={src}
          alt={name}
          className="h-full w-full object-cover"
          loading="lazy"
          onError={() => setFailedFor(url)}
        />
      </button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>{name}</DialogTitle>
          </DialogHeader>
          <img src={url} alt={name} className="max-h-[70vh] w-full rounded-lg object-contain" />
        </DialogContent>
      </Dialog>
    </>
  );
}
