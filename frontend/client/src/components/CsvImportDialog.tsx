import { useRef, useState } from 'react';
import { Download, FileUp } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { CsvImportResult } from '../types';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  templateUrl: string;
  templateName: string;
  onImport: (file: File) => Promise<CsvImportResult>;
  onImported: () => Promise<void> | void;
}

export default function CsvImportDialog({ open, onOpenChange, title, templateUrl, templateName, onImport, onImported }: Props) {
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CsvImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  const reset = () => {
    setFile(null);
    setResult(null);
    setError(null);
    if (inputRef.current) inputRef.current.value = '';
  };

  const close = (next: boolean) => {
    if (loading) return;
    if (!next) reset();
    onOpenChange(next);
  };

  const submit = async () => {
    if (!file) { setError('Seleccioná un archivo CSV.'); return; }
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const res = await onImport(file);
      setResult(res);
      await onImported();
    } catch (err: unknown) {
      const body = (err as { body?: CsvImportResult })?.body;
      if (body?.errors && Array.isArray(body.errors)) {
        setResult({ totalRows: body.totalRows ?? 0, validRows: body.validRows ?? 0, imported: body.imported ?? 0, errors: body.errors });
        setError('El archivo contiene errores. No se importó ninguna fila.');
      } else {
        setError(err instanceof Error ? err.message : 'No se pudo importar el archivo.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="rounded-md border border-border/60 bg-muted/20 p-3 text-sm text-muted-foreground">
            Formato UTF-8, delimitador <code>;</code>, primera fila con encabezados. Límite: 2 MB / 500 filas. La importación es todo-o-nada: si hay errores, no se importa ninguna fila.
          </div>
          <div className="flex flex-wrap items-end gap-3">
            <div className="grid gap-2">
              <Label htmlFor="csv-import-file">Archivo CSV</Label>
              <Input
                id="csv-import-file"
                ref={inputRef}
                type="file"
                accept=".csv,text/csv"
                onChange={(e) => { setFile(e.target.files?.[0] ?? null); setResult(null); setError(null); }}
                disabled={loading}
              />
            </div>
            <Button variant="outline" asChild className="gap-2">
              <a href={templateUrl} download={templateName}><Download className="h-4 w-4" /> Descargar plantilla</a>
            </Button>
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          {result && (
            <div className="space-y-3">
              <div className="flex flex-wrap gap-3 text-sm">
                <span className="rounded-md bg-muted px-2 py-1">Filas: {result.totalRows}</span>
                <span className="rounded-md bg-muted px-2 py-1">Válidas: {result.validRows}</span>
                <span className="rounded-md bg-muted px-2 py-1">Importadas: {result.imported}</span>
                <span className="rounded-md bg-muted px-2 py-1">Errores: {result.errors?.length ?? 0}</span>
              </div>
              {result.errors?.length > 0 && (
                <div className="max-h-72 overflow-auto rounded-md border">
                  <Table>
                    <TableHeader>
                      <TableRow className="hover:bg-transparent">
                        <TableHead>Fila</TableHead>
                        <TableHead>Campo</TableHead>
                        <TableHead>Valor</TableHead>
                        <TableHead>Mensaje</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {result.errors.map((e, i) => (
                        <TableRow key={`${e.row}-${e.field}-${i}`}>
                          <TableCell>{e.row}</TableCell>
                          <TableCell>{e.field}</TableCell>
                          <TableCell className="max-w-[220px] truncate" title={e.value}>{e.value}</TableCell>
                          <TableCell>{e.message}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => close(false)} disabled={loading}>Cerrar</Button>
          <Button onClick={submit} disabled={loading || !file} className="gap-2">
            <FileUp className="h-4 w-4" /> {loading ? 'Importando…' : 'Importar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

