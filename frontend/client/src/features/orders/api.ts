/**
 * Capa de datos del feature Pedidos.
 *
 * Reutiliza el módulo `api/orders.ts` existente: no hay nuevo cliente HTTP ni
 * cambios de contrato. La fachada `src/api.ts` sigue siendo el punto de entrada
 * público y compatible para quien la importe.
 */
export { ordersApi as ordersFeatureApi } from '../../api/orders';
