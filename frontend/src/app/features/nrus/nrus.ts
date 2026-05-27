import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { NrusMonthEstimate } from './nrus.models';
import { NrusService } from './nrus.service';

const MONTH_NAMES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

@Component({
  selector: 'app-nrus',
  imports: [DecimalPipe],
  templateUrl: './nrus.html'
})
export class Nrus {
  private readonly service = inject(NrusService);

  protected readonly loading = signal(true);
  protected readonly current = signal<NrusMonthEstimate | null>(null);
  protected readonly history = signal<NrusMonthEstimate[]>([]);

  constructor() {
    const today = new Date();
    this.service.estimate(today.getFullYear(), today.getMonth() + 1).subscribe({
      next: (e) => { this.current.set(e); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.service.history(6).subscribe((h) => this.history.set(h));
  }

  protected monthName(m: number): string {
    return MONTH_NAMES[m - 1] ?? '';
  }

  protected categoryClass(e: NrusMonthEstimate): string {
    if (e.exceedsLimit)   return 'border-red-300 bg-red-50 text-red-700';
    if (e.category === 2) return 'border-amber-300 bg-amber-50 text-amber-700';
    return 'border-emerald-300 bg-emerald-50 text-emerald-700';
  }

  protected categoryLabel(e: NrusMonthEstimate): string {
    if (e.exceedsLimit)   return 'Supera límite NRUS';
    if (e.category === 2) return 'Categoría 2';
    if (e.category === 1) return 'Categoría 1';
    return '—';
  }

  protected rowCategoryClass(e: NrusMonthEstimate): string {
    if (e.exceedsLimit)   return 'text-red-600 font-medium';
    if (e.category === 2) return 'text-amber-600 font-medium';
    return 'text-emerald-600';
  }
}
