import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateExpenseRequest,
  ExpenseItem,
  ExpenseSummary,
  PagedResult
} from './expense.models';

@Injectable({ providedIn: 'root' })
export class ExpensesService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(opts: { from?: string; to?: string; pageSize?: number } = {}): Observable<PagedResult<ExpenseItem>> {
    let params = new HttpParams();
    if (opts.from) params = params.set('from', opts.from);
    if (opts.to) params = params.set('to', opts.to);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    return this.http.get<PagedResult<ExpenseItem>>(`${this.api}/expenses`, { params });
  }

  summary(opts: { from?: string; to?: string } = {}): Observable<ExpenseSummary> {
    let params = new HttpParams();
    if (opts.from) params = params.set('from', opts.from);
    if (opts.to) params = params.set('to', opts.to);
    return this.http.get<ExpenseSummary>(`${this.api}/expenses/summary`, { params });
  }

  create(req: CreateExpenseRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/expenses`, req);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/expenses/${id}`);
  }
}
