import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePurchaseRequest,
  PagedResult,
  PurchaseDetail,
  PurchaseListItem,
  Supplier
} from './purchase.models';

@Injectable({ providedIn: 'root' })
export class PurchasesService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(opts: { search?: string; page?: number; pageSize?: number } = {}): Observable<PagedResult<PurchaseListItem>> {
    let params = new HttpParams();
    if (opts.search) params = params.set('search', opts.search);
    if (opts.page) params = params.set('page', opts.page);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    return this.http.get<PagedResult<PurchaseListItem>>(`${this.api}/purchases`, { params });
  }

  get(id: number): Observable<PurchaseDetail> {
    return this.http.get<PurchaseDetail>(`${this.api}/purchases/${id}`);
  }

  create(req: CreatePurchaseRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/purchases`, req);
  }

  suppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.api}/suppliers`);
  }

  createSupplier(name: string, phone: string | null): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/suppliers`, { name, phone });
  }
}
