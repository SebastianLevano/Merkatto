import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportsService } from './reports.service';

@Component({
  selector: 'app-reports',
  imports: [FormsModule],
  templateUrl: './reports.html'
})
export class Reports {
  private readonly service = inject(ReportsService);

  protected readonly currentYear = new Date().getFullYear();
  protected readonly currentMonth = new Date().getMonth() + 1;

  protected year = this.currentYear;
  protected month = this.currentMonth;

  protected readonly downloading = signal<string | null>(null);

  protected readonly years = Array.from({ length: 3 }, (_, i) => this.currentYear - i);
  protected readonly months = [
    { value: 1, label: 'Enero' }, { value: 2, label: 'Febrero' },
    { value: 3, label: 'Marzo' }, { value: 4, label: 'Abril' },
    { value: 5, label: 'Mayo' }, { value: 6, label: 'Junio' },
    { value: 7, label: 'Julio' }, { value: 8, label: 'Agosto' },
    { value: 9, label: 'Septiembre' }, { value: 10, label: 'Octubre' },
    { value: 11, label: 'Noviembre' }, { value: 12, label: 'Diciembre' }
  ];

  protected download(type: 'closings-pdf' | 'expenses-excel' | 'purchases-excel'): void {
    if (this.downloading()) return;
    this.downloading.set(type);
    const done = () => this.downloading.set(null);
    if (type === 'closings-pdf') {
      this.service.downloadClosingsPdf(this.year, this.month);
    } else if (type === 'expenses-excel') {
      this.service.downloadExpensesExcel(this.year, this.month);
    } else {
      this.service.downloadPurchasesExcel(this.year, this.month);
    }
    setTimeout(done, 2000);
  }
}
