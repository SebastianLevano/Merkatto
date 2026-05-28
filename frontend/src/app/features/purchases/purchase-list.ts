import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PurchasesService } from './purchases.service';
import { PurchaseListItem } from './purchase.models';

@Component({
  selector: 'app-purchase-list',
  imports: [RouterLink, DecimalPipe, DatePipe],
  templateUrl: './purchase-list.html'
})
export class PurchaseList {
  private readonly service = inject(PurchasesService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(true);
  protected readonly purchases = signal<PurchaseListItem[]>([]);

  // Delete confirmation
  protected readonly toDelete = signal<PurchaseListItem | null>(null);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list({ pageSize: 50 }).subscribe({
      next: (res) => {
        this.purchases.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected openEdit(p: PurchaseListItem): void {
    if (!this.isAdmin()) return;
    this.router.navigate(['/compras', p.id]);
  }

  protected askDelete(p: PurchaseListItem, ev: Event): void {
    ev.stopPropagation();
    this.error.set(null);
    this.toDelete.set(p);
  }

  protected cancelDelete(): void {
    this.toDelete.set(null);
    this.error.set(null);
  }

  protected confirmDelete(): void {
    const p = this.toDelete();
    if (!p || this.deleting()) return;
    this.deleting.set(true);
    this.error.set(null);
    this.service.delete(p.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.toDelete.set(null);
        this.load();
      },
      error: (err: { error?: { detail?: string } }) => {
        this.error.set(err?.error?.detail ?? 'No se pudo eliminar la compra.');
        this.deleting.set(false);
      }
    });
  }
}
