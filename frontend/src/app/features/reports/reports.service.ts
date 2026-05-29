import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  downloadClosingsPdf(year: number, month: number): void {
    const params = new HttpParams().set('year', year).set('month', month);
    this.http
      .get(`${this.api}/reports/closings`, { params, responseType: 'blob' })
      .subscribe((blob) => this.triggerDownload(blob, `cierres-${year}-${String(month).padStart(2, '0')}.pdf`));
  }

  downloadExpensesExcel(year: number, month: number): void {
    const params = new HttpParams().set('year', year).set('month', month);
    this.http
      .get(`${this.api}/reports/expenses`, { params, responseType: 'blob' })
      .subscribe((blob) => this.triggerDownload(blob, `gastos-${year}-${String(month).padStart(2, '0')}.xlsx`));
  }

  downloadPurchasesExcel(year: number, month: number): void {
    const params = new HttpParams().set('year', year).set('month', month);
    this.http
      .get(`${this.api}/reports/purchases`, { params, responseType: 'blob' })
      .subscribe((blob) => this.triggerDownload(blob, `compras-${year}-${String(month).padStart(2, '0')}.xlsx`));
  }

  private triggerDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
