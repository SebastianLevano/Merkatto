import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TimelineService } from './timeline.service';
import { ACTION_LABEL, ENTITY_LABEL, TimelineEntry } from './timeline.models';

@Component({
  selector: 'app-timeline',
  imports: [DatePipe],
  templateUrl: './timeline.html'
})
export class Timeline {
  private readonly service = inject(TimelineService);

  protected readonly loading = signal(true);
  protected readonly entries = signal<TimelineEntry[]>([]);
  protected readonly page = signal(1);
  protected readonly hasMore = signal(false);

  private readonly pageSize = 50;

  constructor() {
    this.loadPage(1);
  }

  protected loadPage(page: number): void {
    this.loading.set(true);
    this.service.list({ page, pageSize: this.pageSize }).subscribe({
      next: (res) => {
        if (page === 1) {
          this.entries.set(res.items);
        } else {
          this.entries.update((prev) => [...prev, ...res.items]);
        }
        this.page.set(page);
        this.hasMore.set(page < res.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected loadMore(): void {
    this.loadPage(this.page() + 1);
  }

  protected entityLabel(name: string): string {
    return ENTITY_LABEL[name] ?? name;
  }

  protected actionLabel(action: string): string {
    return ACTION_LABEL[action] ?? action.toLowerCase();
  }

  protected actionClass(action: string): string {
    if (action === 'Created') return 'bg-emerald-100 text-emerald-700';
    if (action === 'Deleted') return 'bg-red-100 text-red-700';
    return 'bg-slate-100 text-slate-600';
  }
}
