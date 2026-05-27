export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ProductListItem {
  id: number;
  name: string;
  internalCode: string | null;
  categoryName: string;
  brandName: string | null;
  saleUnit: string;
  salePrice: number;
  unitCost: number;
  margin: number;
  marginRate: number;
  totalStock: number;
  minStock: number;
  isLowStock: boolean;
  isActive: boolean;
}

export interface ProductDetail {
  id: number;
  name: string;
  internalCode: string | null;
  categoryId: number;
  categoryName: string;
  brandId: number | null;
  brandName: string | null;
  purchaseUnit: string;
  lastPurchaseCost: number;
  unitsPerPurchaseUnit: number;
  saleUnit: string;
  salePrice: number;
  warehouseStock: number;
  counterStock: number;
  minStock: number;
  unitCost: number;
  margin: number;
  marginRate: number;
  isActive: boolean;
}

export interface CreateProductRequest {
  name: string;
  internalCode: string | null;
  categoryId: number;
  brandId: number | null;
  purchaseUnit: string;
  lastPurchaseCost: number;
  unitsPerPurchaseUnit: number;
  saleUnit: string;
  salePrice: number;
  minStock: number;
  initialWarehouseStock: number;
}

export type UpdateProductRequest = Omit<CreateProductRequest, 'initialWarehouseStock'>;

export interface LookupItem {
  id: number;
  name: string;
}
