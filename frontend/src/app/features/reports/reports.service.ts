import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { switchMap, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { DesktopService } from '../../core/desktop.service';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly desktop = inject(DesktopService);
  private readonly api = environment.apiUrl;

  downloadClosingsPdf(year: number, month: number): void {
    this.download('closings', year, month, `cierres-${year}-${String(month).padStart(2, '0')}.pdf`);
  }

  downloadExpensesExcel(year: number, month: number): void {
    this.download('expenses', year, month, `gastos-${year}-${String(month).padStart(2, '0')}.xlsx`);
  }

  downloadPurchasesExcel(year: number, month: number): void {
    this.download('purchases', year, month, `compras-${year}-${String(month).padStart(2, '0')}.xlsx`);
  }

  private download(type: string, year: number, month: number, filename: string): void {
    const params = new HttpParams().set('year', year).set('month', month);
    this.desktop.isDesktop().pipe(
      switchMap(isDesktop => {
        if (isDesktop) {
          return this.http.get(`${this.api}/reports/${type}/open`, { params });
        }
        return this.http.get(`${this.api}/reports/${type}`, { params, responseType: 'blob' }).pipe(
          tap(blob => this.triggerDownload(blob, filename)),
        );
      }),
    ).subscribe();
  }

  private triggerDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
