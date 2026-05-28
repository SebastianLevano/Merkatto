import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateDailyClosingRequest,
  DailyClosingDetail,
  DailyClosingListItem,
  DailyClosingPreview,
  PagedResult
} from './closing.models';

@Injectable({ providedIn: 'root' })
export class ClosingsService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(opts: { from?: string; to?: string; pageSize?: number } = {}): Observable<PagedResult<DailyClosingListItem>> {
    let params = new HttpParams();
    if (opts.from) params = params.set('from', opts.from);
    if (opts.to) params = params.set('to', opts.to);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    return this.http.get<PagedResult<DailyClosingListItem>>(`${this.api}/daily-closings`, { params });
  }

  preview(date: string): Observable<DailyClosingPreview> {
    const params = new HttpParams().set('date', date);
    return this.http.get<DailyClosingPreview>(`${this.api}/daily-closings/preview`, { params });
  }

  getByDate(date: string): Observable<DailyClosingDetail> {
    return this.http.get<DailyClosingDetail>(`${this.api}/daily-closings/${date}`);
  }

  getById(id: number): Observable<DailyClosingDetail> {
    return this.http.get<DailyClosingDetail>(`${this.api}/daily-closings/by-id/${id}`);
  }

  create(req: CreateDailyClosingRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/daily-closings`, req);
  }

  update(id: number, req: CreateDailyClosingRequest): Observable<void> {
    return this.http.put<void>(`${this.api}/daily-closings/${id}`, req);
  }
}
