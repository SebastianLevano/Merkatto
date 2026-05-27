import { PagedResult } from '../products/product.models';

export type { PagedResult };

export interface TimelineEntry {
  id: number;
  action: string;
  entityName: string;
  entityId: string | null;
  userDisplay: string;
  timestamp: string;
  ipAddress: string | null;
}

export const ENTITY_LABEL: Record<string, string> = {
  Product: 'Producto',
  Purchase: 'Compra',
  DailyClosing: 'Cierre diario',
  Expense: 'Gasto',
  CreditCustomer: 'Cliente (fiados)',
  CreditSale: 'Fiado',
  CreditPayment: 'Pago de fiado',
  Supplier: 'Proveedor',
  User: 'Usuario',
  Category: 'Categoría',
  Brand: 'Marca'
};

export const ACTION_LABEL: Record<string, string> = {
  Created: 'registrado/a',
  Updated: 'actualizado/a',
  Deleted: 'eliminado/a'
};
