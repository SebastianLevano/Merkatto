import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
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

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(true);
  protected readonly purchases = signal<PurchaseListItem[]>([]);

  constructor() {
    this.service.list({ pageSize: 50 }).subscribe({
      next: (res) => {
        this.purchases.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
