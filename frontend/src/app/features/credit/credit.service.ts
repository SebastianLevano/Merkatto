import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateCreditSaleRequest,
  CreatePaymentRequest,
  CreditCustomerDetail,
  CreditCustomerListItem,
  SaveCustomerRequest
} from './credit.models';

@Injectable({ providedIn: 'root' })
export class CreditService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  customers(search?: string): Observable<CreditCustomerListItem[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<CreditCustomerListItem[]>(`${this.api}/credit/customers`, { params });
  }

  customer(id: number): Observable<CreditCustomerDetail> {
    return this.http.get<CreditCustomerDetail>(`${this.api}/credit/customers/${id}`);
  }

  createCustomer(req: SaveCustomerRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/credit/customers`, req);
  }

  addSale(req: CreateCreditSaleRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/credit/sales`, req);
  }

  addPayment(req: CreatePaymentRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.api}/credit/payments`, req);
  }
}
