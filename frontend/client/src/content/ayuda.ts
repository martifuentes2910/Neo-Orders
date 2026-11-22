export interface AyudaItem {
  titulo: string;
  resumen: string;
  bullets: string[];
}

export const AYUDA_POR_RUTA: Record<string, AyudaItem> = {
  '/panel': {
    titulo: 'Panel de control',
    resumen: 'Resumen general del negocio con indicadores, gráficos y accesos rápidos a lo más reciente.',
    bullets: [
      'Las tarjetas KPI muestran totales, ingresos y estados clave del período actual.',
      'Los gráficos comparan pedidos, ingresos y unidades por mes; pasá el cursor para ver el detalle.',
      'La tabla de pedidos recientes y el Top productos te llevan rápido a lo que más se mueve.',
    ],
  },
  '/pedidos': {
    titulo: 'Pedidos',
    resumen: 'Gestión completa de pedidos: creación, edición, estado y seguimiento.',
    bullets: [
      'Usá filtros y búsqueda para encontrar pedidos por cliente, producto, vendedor o estado.',
      'El estado del pedido sigue el flujo permitido; los pedidos finalizados no se pueden editar.',
      'Al crear o editar, verificá cantidades y totales antes de guardar.',
    ],
  },
  '/clientes': {
    titulo: 'Clientes',
    resumen: 'Directorio de clientes con sus datos de contacto y acceso a su historial.',
    bullets: [
      'Buscá por nombre, email o teléfono para ubicar un cliente rápidamente.',
      'Revisá los datos de contacto antes de registrar pedidos asociados.',
      'Mantené la información actualizada para evitar errores en envíos y facturación.',
    ],
  },
  '/productos': {
    titulo: 'Productos',
    resumen: 'Catálogo de productos con precios, stock e imágenes.',
    bullets: [
      'Controlá stock y precio antes de usar un producto en nuevos pedidos.',
      'Las imágenes ayudan a identificar productos; usá archivos JPG, PNG o WebP.',
      'Si un producto no tiene stock suficiente, actualizalo antes de venderlo.',
    ],
  },
  '/vendedores': {
    titulo: 'Vendedores',
    resumen: 'Listado de vendedores y su información de contacto.',
    bullets: [
      'Usá esta vista para consultar quién gestiona cada pedido.',
      'Mantené email y teléfono actualizados para coordinar ventas.',
      'El rol Admin puede crear, editar o eliminar vendedores.',
    ],
  },
  '/reportes': {
    titulo: 'Reportes',
    resumen: 'Análisis visual de ventas, estados y rendimiento del negocio.',
    bullets: [
      'Compará ingresos, unidades y estados para detectar tendencias.',
      'Los gráficos usan la paleta activa; los valores se muestran al pasar el cursor.',
      'Usá estos datos como apoyo, verificando siempre el contexto operativo.',
    ],
  },
  '/notificaciones': {
    titulo: 'Notificaciones',
    resumen: 'Actividad reciente del sistema y avisos importantes.',
    bullets: [
      'Revisá las notificaciones no leídas para enterarte de cambios recientes.',
      'Los avisos pueden incluir pedidos, stock, clientes u otros eventos del sistema.',
      'Marcá como leídas las notificaciones que ya atendiste.',
    ],
  },
  '/configuracion': {
    titulo: 'Configuración',
    resumen: 'Ajustes personales, de empresa, usuarios y apariencia.',
    bullets: [
      'En Perfil podés actualizar tu nombre, apellido y avatar.',
      'En Apariencia elegís paleta y modo de color; la preferencia se guarda en tu usuario.',
      'El rol Admin también puede gestionar empresa, usuarios y acceso a soporte.',
    ],
  },
  '/soporte': {
    titulo: 'Soporte',
    resumen: 'Canales de contacto y guía para reportar problemas.',
    bullets: [
      'Usá el email o el teléfono de contacto para consultas operativas.',
      'Para reportar un bug, incluí ruta, rol, navegador, fecha y pasos de reproducción.',
      'El botón “Reportar un bug” prepara un correo con una plantilla útil.',
    ],
  },
};

export function getAyudaParaRuta(pathname: string): AyudaItem {
  const ruta = Object.keys(AYUDA_POR_RUTA)
    .filter((key) => pathname === key || pathname.startsWith(`${key}/`))
    .sort((a, b) => b.length - a.length)[0];
  return AYUDA_POR_RUTA[ruta ?? '/panel'];
}