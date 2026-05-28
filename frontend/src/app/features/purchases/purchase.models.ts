import { PagedResult } from '../products/product.models';

export type { PagedResult };

export interface PurchaseListItem {
  id: number;
  date: string;
  supplierName: string | null;
  itemCount: number;
  totalCost: number;
}

export interface PurchaseItemDetail {
  productId: number;
  productName: string;
  purchaseUnit: string;
  quantity: number;
  unitCost: number;
  conversionFactor: number;
  quantityInSaleUnits: number;
  subtotal: number;
}

export interface PurchaseDetail {
  id: number;
  date: string;
  supplierId: number | null;
  supplierName: string | null;
  notes: string | null;
  totalCost: number;
  items: PurchaseItemDetail[];
}

export interface CreatePurchaseItemRequest {
  productId: number;
  paquetes: number;
  unidadesPorPaquete: number;
  costoPorPaquete: number;
}

export interface CreatePurchaseRequest {
  supplierName: string | null;
  date: string;
  notes: string | null;
  items: CreatePurchaseItemRequest[];
}

export interface Supplier {
  id: number;
  name: string;
  phone: string | null;
  notes: string | null;
}
