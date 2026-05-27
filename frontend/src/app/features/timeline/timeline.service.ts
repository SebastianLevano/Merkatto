import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult, TimelineEntry } from './timeline.models';

@Injectable({ providedIn: 'root' })
export class TimelineService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(opts: { page?: number; pageSize?: number } = {}): Observable<PagedResult<TimelineEntry>> {
    let params = new HttpParams();
    if (opts.page) params = params.set('page', opts.page);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    return this.http.get<PagedResult<TimelineEntry>>(`${this.api}/timeline`, { params });
  }
}
