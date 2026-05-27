import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ClosingsService } from './closings.service';
import { DailyClosingListItem } from './closing.models';
import { ReportsService } from '../reports/reports.service';

@Component({
  selector: 'app-closing-list',
  imports: [RouterLink, DatePipe, DecimalPipe, FormsModule],
  templateUrl: './closing-list.html'
})
export class ClosingList {
  private readonly service = inject(ClosingsService);
  private readonly reports = inject(ReportsService);

  protected readonly loading = signal(true);
  protected readonly closings = signal<DailyClosingListItem[]>([]);

  protected reportYear = new Date().getFullYear();
  protected reportMonth = new Date().getMonth() + 1;

  constructor() {
    this.service.list({ pageSize: 60 }).subscribe({
      next: (res) => {
        this.closings.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected downloadPdf(): void {
    this.reports.downloadClosingsPdf(this.reportYear, this.reportMonth);
  }
}
