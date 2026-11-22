import { useState } from 'react';
import { Copy, Mail, MessageCircle, Bug, Clock, CheckCircle2 } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';

const EMAIL = 'soporte@neoorders.example';
const TELEFONO = '+54 11 0000-0000';

function plantillaBug() {
  const ruta = window.location.pathname;
  const fecha = new Date().toLocaleString('es-AR');
  const navegador = navigator.userAgent;
  return [
    'Hola equipo de soporte de NeoOrders,',
    '',
    'Quiero reportar un problema:',
    '',
    `- Ruta: ${ruta}`,
    '- Rol: [Admin/Empleado]',
    `- Navegador: ${navegador}`,
    `- Fecha y hora: ${fecha}`,
    '- Descripción: [qué pasó]',
    '- Pasos para reproducir:',
    '  1. [paso 1]',
    '  2. [paso 2]',
    '  3. [paso 3]',
    '- Resultado esperado: [qué esperabas]',
    '- Resultado obtenido: [qué pasó realmente]',
    '',
    'Gracias.',
  ].join('\n');
}

export default function Soporte() {
  const [copiado, setCopiado] = useState<'email' | 'plantilla' | null>(null);

  const copiar = async (texto: string, tipo: 'email' | 'plantilla') => {
    try {
      await navigator.clipboard.writeText(texto);
      setCopiado(tipo);
      toast.success(tipo === 'email' ? 'Email copiado' : 'Plantilla copiada');
      setTimeout(() => setCopiado(null), 1800);
    } catch {
      toast.error('No se pudo copiar al portapapeles');
    }
  };

  const asunto = encodeURIComponent('Reporte de bug - NeoOrders');
  const cuerpo = encodeURIComponent(plantillaBug());
  const mailto = `mailto:${EMAIL}?subject=${asunto}&body=${cuerpo}`;

  return (
    <div className="w-full space-y-6">
      <div>
        <h1 className="text-[32px] font-bold tracking-tight">Soporte</h1>
        <p className="text-sm text-muted-foreground">
          Canales de contacto y guía para reportar problemas o consultas sobre NeoOrders.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base"><Mail className="h-4 w-4" /> Email</CardTitle>
            <CardDescription>Consultas generales y seguimiento.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="font-medium">{EMAIL}</p>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" className="gap-2" onClick={() => copiar(EMAIL, 'email')}>
                {copiado === 'email' ? <CheckCircle2 className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                Copiar
              </Button>
              <Button asChild size="sm" className="gap-2">
                <a href={`mailto:${EMAIL}`}><Mail className="h-4 w-4" /> Escribir</a>
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base"><MessageCircle className="h-4 w-4" /> Teléfono</CardTitle>
            <CardDescription>Contacto rápido en horario laboral.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="font-medium">{TELEFONO}</p>
            <p className="text-xs text-muted-foreground">Número de contacto para consultas operativas.</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base"><Clock className="h-4 w-4" /> Horario</CardTitle>
            <CardDescription>Disponibilidad del equipo.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <p className="font-medium">Lunes a viernes</p>
            <p className="text-sm text-muted-foreground">09:00 a 18:00 (ART)</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base"><Bug className="h-4 w-4" /> Reportar un bug</CardTitle>
          <CardDescription>
            Incluí contexto suficiente para que el problema se pueda reproducir y resolver rápido.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <ol className="list-decimal space-y-2 pl-5 text-sm text-muted-foreground">
            <li>Indicá la ruta/pantalla donde ocurrió (por ejemplo, <code>/pedidos</code>).</li>
            <li>Describí qué esperabas que pase y qué pasó realmente.</li>
            <li>Agregá pasos para reproducirlo, tu rol y la fecha/hora.</li>
            <li>Si podés, adjuntá una captura al enviar el correo.</li>
          </ol>
          <div className="flex flex-wrap gap-2">
            <Button asChild className="gap-2">
              <a href={mailto}><Mail className="h-4 w-4" /> Reportar por email</a>
            </Button>
            <Button variant="outline" className="gap-2" onClick={() => copiar(plantillaBug(), 'plantilla')}>
              {copiado === 'plantilla' ? <CheckCircle2 className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
              Copiar plantilla
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}