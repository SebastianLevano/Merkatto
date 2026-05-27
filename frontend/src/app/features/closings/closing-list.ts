import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ClosingsService } from './closings.service';
import { DailyClosingListItem } from './closing.models';

@Component({
  selector: 'app-closing-list',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './closing-list.html'
})
export class ClosingList {
  private readonly service = inject(ClosingsService);

  protected readonly loading = signal(true);
  protected readonly closings = signal<DailyClosingListItem[]>([]);

  constructor() {
    this.service.list({ pageSize: 60 }).subscribe({
      next: (res) => {
        this.closings.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
