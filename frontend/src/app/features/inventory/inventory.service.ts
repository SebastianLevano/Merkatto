import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdjustmentRequest,
  InventoryRow,
  MovementRow,
  PagedResult,
  TransferRequest
} from './inventory.models';
import { BatchCountItem } from '../products/product.models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(opts: { search?: string; lowStockOnly?: boolean; pageSize?: number } = {}): Observable<PagedResult<InventoryRow>> {
    let params = new HttpParams();
    if (opts.search) params = params.set('search', opts.search);
    if (opts.lowStockOnly) params = params.set('lowStockOnly', true);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    return this.http.get<PagedResult<InventoryRow>>(`${this.api}/inventory`, { params });
  }

  movements(opts: { productId?: number; pageSize?: number } = {}): Observable<PagedResult<MovementRow>> {
    let params = new HttpParams();
    if (opts.productId) params = params.set('productId', opts.productId);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    return this.http.get<PagedResult<MovementRow>>(`${this.api}/inventory/movements`, { params });
  }

  transfer(req: TransferRequest): Observable<void> {
    return this.http.post<void>(`${this.api}/inventory/transfers`, req);
  }

  adjust(req: AdjustmentRequest): Observable<void> {
    return this.http.post<void>(`${this.api}/inventory/adjustments`, req);
  }

  batchCount(items: BatchCountItem[], date?: string | null, notes?: string): Observable<void> {
    return this.http.post<void>(`${this.api}/inventory/batch-count`, {
      items,
      notes: notes ?? null,
      date: date ?? null
    });
  }
}
