import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
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
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isAdmin = this.auth.isAdmin;
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

  /** True when the closing is editable: admin and within 7 days. */
  protected canEdit(c: DailyClosingListItem): boolean {
    if (!this.isAdmin()) return false;
    const diffMs = Date.now() - new Date(c.businessDate).getTime();
    const days = diffMs / (1000 * 60 * 60 * 24);
    return days <= 7;
  }

  protected openEdit(c: DailyClosingListItem): void {
    if (!this.canEdit(c)) return;
    this.router.navigate(['/cierre', c.id]);
  }

  protected downloadPdf(): void {
    this.reports.downloadClosingsPdf(this.reportYear, this.reportMonth);
  }
}
