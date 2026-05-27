import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DashboardService } from './dashboard.service';
import { DashboardSummary } from './dashboard.models';

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './dashboard.html'
})
export class Dashboard {
  private readonly service = inject(DashboardService);

  protected readonly loading = signal(true);
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly Math = Math;

  constructor() {
    this.service.summary().subscribe({
      next: (s) => {
        this.summary.set(s);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected barHeight(value: number, max: number): string {
    if (max <= 0) return '0%';
    return `${Math.max(4, Math.round((value / max) * 100))}%`;
  }
}
