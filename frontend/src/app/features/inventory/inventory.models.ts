import { PagedResult } from '../products/product.models';

export type { PagedResult };

export enum MovementType {
  Purchase = 1,
  Transfer = 2,
  Adjustment = 3,
  EstimatedSale = 4,
  InternalUse = 5
}

export enum StockLocation {
  Warehouse = 1,
  Counter = 2
}

export enum AdjustmentType {
  Loss = 1,
  Expired = 2,
  InternalUse = 3,
  Correction = 4
}

export interface InventoryRow {
  productId: number;
  name: string;
  saleUnit: string;
  warehouseStock: number;
  counterStock: number;
  totalStock: number;
  minStock: number;
  isLowStock: boolean;
}

export interface MovementRow {
  id: number;
  productId: number;
  productName: string;
  occurredAt: string;
  movementType: MovementType;
  location: StockLocation;
  quantity: number;
  notes: string | null;
}

export interface TransferRequest {
  productId: number;
  quantity: number;
  notes: string | null;
}

export interface AdjustmentRequest {
  productId: number;
  type: AdjustmentType;
  location: StockLocation;
  quantity: number;
  reason: string | null;
}

export const MOVEMENT_LABEL: Record<MovementType, string> = {
  [MovementType.Purchase]: 'Compra',
  [MovementType.Transfer]: 'Transferencia',
  [MovementType.Adjustment]: 'Ajuste',
  [MovementType.EstimatedSale]: 'Venta estimada',
  [MovementType.InternalUse]: 'Consumo interno'
};

export const ADJUSTMENT_LABEL: Record<AdjustmentType, string> = {
  [AdjustmentType.Loss]: 'Merma / pérdida',
  [AdjustmentType.Expired]: 'Vencido',
  [AdjustmentType.InternalUse]: 'Consumo interno',
  [AdjustmentType.Correction]: 'Corrección'
};
