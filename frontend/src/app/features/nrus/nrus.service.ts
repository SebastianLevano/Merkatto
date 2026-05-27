import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NrusMonthEstimate } from './nrus.models';

@Injectable({ providedIn: 'root' })
export class NrusService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  estimate(year: number, month: number): Observable<NrusMonthEstimate> {
    const params = new HttpParams().set('year', year).set('month', month);
    return this.http.get<NrusMonthEstimate>(`${this.api}/nrus/estimate`, { params });
  }

  history(months = 6): Observable<NrusMonthEstimate[]> {
    const params = new HttpParams().set('months', months);
    return this.http.get<NrusMonthEstimate[]>(`${this.api}/nrus/history`, { params });
  }
}
