import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateProductRequest,
  LookupItem,
  PagedResult,
  ProductDetail,
  ProductListItem,
  UpdateProductRequest
} from './product.models';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(opts: { search?: string; page?: number; pageSize?: number; activeOnly?: boolean } = {}): Observable<
    PagedResult<ProductListItem>
  > {
    let params = new HttpParams();
    if (opts.search) params = params.set('search', opts.search);
    if (opts.page) params = params.set('page', opts.page);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    if (opts.activeOnly != null) params = params.set('activeOnly', opts.activeOnly);
    return this.http.get<PagedResult<ProductListItem>>(`${this.api}/products`, { params });
  }

  get(id: number): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.api}/products/${id}`);
  }

  create(req: CreateProductRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/products`, req);
  }

  update(id: number, req: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.api}/products/${id}`, req);
  }

  setActive(id: number, active: boolean): Observable<void> {
    const action = active ? 'activate' : 'deactivate';
    return this.http.post<void>(`${this.api}/products/${id}/${action}`, {});
  }

  categories(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.api}/categories`);
  }

  brands(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.api}/brands`);
  }

  createCategory(name: string): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/categories`, { name });
  }
}
