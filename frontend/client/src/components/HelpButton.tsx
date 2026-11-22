import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { CircleHelp, LifeBuoy } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { getAyudaParaRuta } from '@/content/ayuda';

export default function HelpButton() {
  const [open, setOpen] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const ayuda = getAyudaParaRuta(location.pathname);

  useEffect(() => {
    setOpen(false);
  }, [location.pathname]);

  if (location.pathname === '/login') return null;

  return (
    <>
      <Button
        type="button"
        variant="outline"
        size="icon"
        aria-label="Abrir ayuda de esta vista"
        onClick={() => setOpen(true)}
        className="fixed bottom-5 right-5 z-40 h-11 w-11 rounded-full border-border/70 bg-background/90 shadow-lg backdrop-blur hover:bg-accent"
      >
        <CircleHelp className="h-5 w-5" />
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{ayuda.titulo}</DialogTitle>
            <DialogDescription>{ayuda.resumen}</DialogDescription>
          </DialogHeader>
          <ul className="space-y-2 text-sm text-muted-foreground">
            {ayuda.bullets.map((bullet) => (
              <li key={bullet} className="flex gap-2">
                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-primary" />
                <span>{bullet}</span>
              </li>
            ))}
          </ul>
          <div className="flex justify-end pt-2">
            <Button
              variant="ghost"
              size="sm"
              className="gap-2"
              onClick={() => {
                setOpen(false);
                navigate('/configuracion?tab=soporte');
              }}
            >
              <LifeBuoy className="h-4 w-4" /> Ir a soporte
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}