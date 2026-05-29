import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PurchasesService } from './purchases.service';
import { PurchaseDetail as PurchaseDetailModel } from './purchase.models';

@Component({
  selector: 'app-purchase-detail',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './purchase-detail.html'
})
export class PurchaseDetail {
  private readonly service = inject(PurchasesService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(true);
  protected readonly purchase = signal<PurchaseDetailModel | null>(null);
  protected readonly error = signal<string | null>(null);

  private get purchaseId(): number {
    return Number(this.route.snapshot.paramMap.get('id'));
  }

  constructor() {
    this.service.get(this.purchaseId).subscribe({
      next: (p) => {
        this.purchase.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar la compra.');
        this.loading.set(false);
      }
    });
  }
}
