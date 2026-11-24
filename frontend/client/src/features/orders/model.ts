import {
  ORDER_STATUSES,
  esEstadoBloqueado,
  estadosDestino,
  type Order,
  type OrderStatus,
  type Product,
} from '../../types';

export interface OrderFormValues {
  clientId: number;
  sellerId: number | null;
  productId: number | null;
  productName: string;
  cantidad: number;
  precioUnitario: number;
  estado: OrderStatus;
}

export const emptyOrderForm: OrderFormValues = {
  clientId: 0,
  sellerId: 0 as number | null,
  productId: 0 as number | null,
  productName: '',
  cantidad: 1,
  precioUnitario: 0,
  estado: 'Pendiente',
};

/** Un estado es terminal cuando no tiene transiciones destino. */
export const esTerminal = (estado: OrderStatus): boolean => estadosDestino(estado).length === 1;

export interface OrderFormFlags {
  bloqueado: boolean;
  editingEstado: OrderStatus | null;
  soloEstado: boolean;
  opcionesEstado: OrderStatus[];
  productoDadoDeBaja: boolean;
  productSelectValue: string;
}

/** Deriva las reglas de edición (KAN-13/KAN-16) a partir del pedido y el catálogo. */
export function deriveOrderFormFlags(
  editing: Order | null,
  form: OrderFormValues,
  products: Product[],
): OrderFormFlags {
  // KAN-13: producto, nombre y precio son fijos cuando se edita un pedido.
  const bloqueado = !!editing;
  // KAN-16: Enviado/Entregado/Cancelado sólo permiten cambiar el estado.
  const editingEstado = editing ? (editing.estado as OrderStatus) : null;
  const soloEstado = !!editingEstado && esEstadoBloqueado(editingEstado);
  const opcionesEstado = editingEstado ? estadosDestino(editingEstado) : ORDER_STATUSES;
  const productoDelCatalogo = products.find((p) => p.id === form.productId);
  const productoDadoDeBaja = bloqueado && products.length > 0 && !productoDelCatalogo;
  // La FK se pone en null al borrar el producto; hace falta un valor centinela válido para Radix.
  const productSelectValue =
    productoDadoDeBaja && !form.productId ? '__sin_catalogo__' : form.productId ? String(form.productId) : '';

  return { bloqueado, editingEstado, soloEstado, opcionesEstado, productoDadoDeBaja, productSelectValue };
}

export type OrderFormErrors = Record<string, string>;

export function validateOrderForm(form: OrderFormValues, editing: Order | null): OrderFormErrors {
  const errors: OrderFormErrors = {};
  if (!form.clientId) errors.clientId = 'Seleccioná un cliente';
  // KAN-16: en alta, el catálogo es la única fuente del producto.
  if (!editing && !form.productId) errors.productId = 'Seleccioná un producto del catálogo';
  if (!form.cantidad || form.cantidad < 1) errors.cantidad = 'Cantidad inválida';
  if (!form.precioUnitario || form.precioUnitario <= 0) errors.precioUnitario = 'Precio inválido';
  return errors;
}

/** Construye el payload exacto que espera la API (sin campos nuevos). */
export function buildOrderPayload(form: OrderFormValues, editing: Order | null) {
  return {
    clientId: form.clientId,
    sellerId: form.sellerId,
    productId: form.productId,
    cantidad: form.cantidad,
    precioUnitario: form.precioUnitario,
    estado: editing ? form.estado : 'Pendiente',
  };
}
