import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CreditService } from './credit.service';
import { CreditCustomerDetail, CreditSaleItemRequest } from './credit.models';

type DialogMode = 'sale' | 'payment';

@Component({
  selector: 'app-credit-detail',
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe],
  templateUrl: './credit-detail.html'
})
export class CreditDetail {
  private readonly service = inject(CreditService);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(true);
  protected readonly customer = signal<CreditCustomerDetail | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly dialogMode = signal<DialogMode | null>(null);

  // Payment dialog
  protected payDate = new Date().toISOString().slice(0, 10);
  protected payAmount: number | null = null;
  protected payNotes = '';

  // Sale dialog — simple amount OR itemized
  protected saleMode: 'simple' | 'items' = 'simple';
  protected saleDate = new Date().toISOString().slice(0, 10);
  protected saleAmount: number | null = null;
  protected saleNotes = '';
  protected saleItems: { description: string; quantity: number; lineTotal: number }[] = [
    { description: '', quantity: 1, lineTotal: 0 }
  ];
  protected get saleItemsTotal(): number {
    return this.saleItems.reduce((s, i) => s + (Number(i.lineTotal) || 0), 0);
  }

  private get customerId(): number {
    return Number(this.route.snapshot.paramMap.get('id'));
  }

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.customer(this.customerId).subscribe({
      next: (c) => {
        this.customer.set(c);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected openPayment(): void {
    this.payDate = new Date().toISOString().slice(0, 10);
    this.payAmount = null;
    this.payNotes = '';
    this.error.set(null);
    this.dialogMode.set('payment');
  }

  protected openSale(): void {
    this.saleMode = 'simple';
    this.saleDate = new Date().toISOString().slice(0, 10);
    this.saleAmount = null;
    this.saleNotes = '';
    this.saleItems = [{ description: '', quantity: 1, lineTotal: 0 }];
    this.error.set(null);
    this.dialogMode.set('sale');
  }

  protected addSaleItem(): void {
    this.saleItems = [...this.saleItems, { description: '', quantity: 1, lineTotal: 0 }];
  }

  protected removeSaleItem(i: number): void {
    this.saleItems = this.saleItems.filter((_, idx) => idx !== i);
  }

  protected close(): void {
    this.dialogMode.set(null);
  }

  protected savePayment(): void {
    if (!this.payAmount || this.payAmount <= 0 || this.saving()) {
      this.error.set('Ingresa un monto válido.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.service
      .addPayment({
        customerId: this.customerId,
        date: this.payDate,
        amount: this.payAmount,
        notes: this.payNotes || null
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogMode.set(null);
          this.load();
        },
        error: () => {
          this.error.set('No se pudo registrar el pago.');
          this.saving.set(false);
        }
      });
  }

  protected saveSale(): void {
    if (this.saving()) return;

    let items: CreditSaleItemRequest[] | null = null;
    let amount: number | null = null;

    if (this.saleMode === 'items') {
      const valid = this.saleItems.filter((i) => i.description.trim() && i.lineTotal > 0);
      if (valid.length === 0) {
        this.error.set('Agrega al menos un producto con monto.');
        return;
      }
      items = valid.map((i) => ({
        description: i.description.trim(),
        quantity: Number(i.quantity) || 1,
        lineTotal: Number(i.lineTotal)
      }));
    } else {
      if (!this.saleAmount || this.saleAmount <= 0) {
        this.error.set('Ingresa un monto válido.');
        return;
      }
      amount = this.saleAmount;
    }

    this.saving.set(true);
    this.error.set(null);
    this.service
      .addSale({
        customerId: this.customerId,
        date: this.saleDate,
        notes: this.saleNotes || null,
        amount,
        items
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogMode.set(null);
          this.load();
        },
        error: () => {
          this.error.set('No se pudo registrar el fiado.');
          this.saving.set(false);
        }
      });
  }
}
