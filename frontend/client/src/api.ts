import type {
  Client,
  CompanySettings,
  Order,
  Product,
  Seller,
  User,
} from './types';
import type { FiltrosReporteParams } from './types/reportes';
import { BASE } from './api/http';
import { dashboardApi, reportsApi } from './api/reports';
import { authApi } from './api/auth';
import { clientsApi } from './api/clients';
import { ordersApi } from './api/orders';
import { productsApi } from './api/products';
import { sellersApi } from './api/sellers';
import { notificationsApi } from './api/notifications';
import { configApi } from './api/config';
import { usersApi } from './api/users';

export type {
  CsvImportError,
  CsvImportResult,
  PriceAdjustmentItem,
  PriceAdjustmentPreviewResponse,
  PriceAdjustmentApplyResponse,
} from './types';

export const api = {
  login: (email: string, password: string) => authApi.login(email, password),
  logout: () => authApi.logout(),
  me: () => authApi.me(),

  clients: {
    list: (search?: string) => clientsApi.list(search),
    create: (data: Partial<Client>) => clientsApi.create(data),
    update: (id: number, data: Partial<Client>) => clientsApi.update(id, data),
    remove: (id: number) => clientsApi.remove(id),
    templateUrl: () => `${BASE}/clients/import-csv/template`,
    importCsv: (file: File) => clientsApi.importCsv(file),
  },

  orders: {
    list: (
      search?: string,
      estado?: string,
      clientId?: number,
      sellerId?: number,
      productId?: number,
      desde?: string,
      hasta?: string,
    ) => ordersApi.list(search, estado, clientId, sellerId, productId, desde, hasta),
    create: (
      data: Pick<Order, 'clientId' | 'cantidad'> & {
        sellerId?: number | null;
        productId?: number | null;
        productName?: string;
        precioUnitario?: number;
        orderNumber?: string | null;
      },
    ) => ordersApi.create(data),
    update: (
      id: number,
      data: Pick<Order, 'id' | 'clientId' | 'cantidad' | 'estado'> & {
        sellerId?: number | null;
        productId?: number | null;
        productName?: string;
        precioUnitario?: number;
      },
    ) => ordersApi.update(id, data),
    // KAN-16: único camino permitido para cambiar el estado de pedidos Enviado/Entregado/Cancelado.
    updateStatus: (id: number, estado: string) => ordersApi.updateStatus(id, estado),
    remove: (id: number) => ordersApi.remove(id),
    templateUrl: () => `${BASE}/orders/import-csv/template`,
    importCsv: (file: File) => ordersApi.importCsv(file),
  },

  products: {
    list: (search?: string, category?: string, onlyInStock?: boolean, sku?: string, nombre?: string) =>
      productsApi.list(search, category, onlyInStock, sku, nombre),
    categories: () => productsApi.categories(),
    create: (data: Partial<Product>) => productsApi.create(data),
    update: (id: number, data: Partial<Product>) => productsApi.update(id, data),
    // KAN-23: la imagen se sube en un endpoint dedicado para no cambiar el contrato JSON.
    uploadImage: (id: number, file: File) => productsApi.uploadImage(id, file),
    removeImage: (id: number) => productsApi.removeImage(id),
    previewPriceAdjustment: (percent: number) => productsApi.previewPriceAdjustment(percent),
    applyPriceAdjustment: (percent: number) => productsApi.applyPriceAdjustment(percent),
    remove: (id: number) => productsApi.remove(id),
    templateUrl: () => `${BASE}/products/import-csv/template`,
    importCsv: (file: File) => productsApi.importCsv(file),
  },

  sellers: {
    list: (onlyActive?: boolean) => sellersApi.list(onlyActive),
    create: (data: Partial<Seller>) => sellersApi.create(data),
    update: (id: number, data: Partial<Seller>) => sellersApi.update(id, data),
    remove: (id: number) => sellersApi.remove(id),
  },

  dashboard: dashboardApi,

  notifications: {
    list: (take = 60) => notificationsApi.list(take),
  },

  reports: (filtros?: FiltrosReporteParams) => reportsApi.resumen(filtros),

  reportesDetalle: {
    vendedores: (filtros?: FiltrosReporteParams) => reportsApi.detalle.vendedores(filtros),
    productos: (filtros?: FiltrosReporteParams) => reportsApi.detalle.productos(filtros),
    clientes: (filtros?: FiltrosReporteParams) => reportsApi.detalle.clientes(filtros),
  },

  config: {
    empresa: {
      get: () => configApi.empresa.get(),
      update: (data: Partial<CompanySettings>) => configApi.empresa.update(data),
    },
    perfil: {
      get: () => configApi.perfil.get(),
      update: (data: Partial<User>) => configApi.perfil.update(data),
    },
    seed: () => configApi.seed(),
    clear: () => configApi.clear(),
  },

  users: {
    list: () => usersApi.list(),
    create: (data: { nombre: string; apellido: string; email: string; password: string; rol: string; avatar?: string }) =>
      usersApi.create(data),
    remove: (id: string) => usersApi.remove(id),
  },
};
