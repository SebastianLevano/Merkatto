import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { InventoryService } from './inventory.service';
import {
  ADJUSTMENT_LABEL,
  AdjustmentType,
  InventoryRow,
  StockLocation
} from './inventory.models';

type DialogMode = 'transfer' | 'adjust';

@Component({
  selector: 'app-inventory-list',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe],
  templateUrl: './inventory-list.html'
})
export class InventoryList {
  private readonly service = inject(InventoryService);

  protected readonly StockLocation = StockLocation;
  protected readonly adjustmentTypes = Object.entries(ADJUSTMENT_LABEL).map(([v, label]) => ({
    value: Number(v) as AdjustmentType,
    label
  }));

  protected readonly loading = signal(true);
  protected readonly rows = signal<InventoryRow[]>([]);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly lowStockOnly = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  // Dialog state
  protected readonly dialogMode = signal<DialogMode | null>(null);
  protected readonly current = signal<InventoryRow | null>(null);
  protected qty = 1;
  protected notes = '';
  protected adjType: AdjustmentType = AdjustmentType.Loss;
  protected adjLocation: StockLocation = StockLocation.Counter;
  protected adjDirection: 'remove' | 'add' = 'remove';

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list({ search: this.search.value, lowStockOnly: this.lowStockOnly(), pageSize: 100 }).subscribe({
      next: (res) => {
        this.rows.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected toggleLowStock(): void {
    this.lowStockOnly.update((v) => !v);
    this.load();
  }

  protected openTransfer(row: InventoryRow): void {
    this.current.set(row);
    this.qty = 1;
    this.notes = '';
    this.error.set(null);
    this.dialogMode.set('transfer');
  }

  protected openAdjust(row: InventoryRow): void {
    this.current.set(row);
    this.qty = 1;
    this.adjType = AdjustmentType.Loss;
    this.adjLocation = StockLocation.Counter;
    this.adjDirection = 'remove';
    this.error.set(null);
    this.dialogMode.set('adjust');
  }

  protected close(): void {
    this.dialogMode.set(null);
    this.current.set(null);
  }

  protected confirm(): void {
    const row = this.current();
    if (!row || this.qty <= 0 || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);

    const done = {
      next: () => {
        this.saving.set(false);
        this.close();
        this.load();
      },
      error: () => {
        this.error.set('No se pudo completar la operación. Revisa las cantidades.');
        this.saving.set(false);
      }
    };

    if (this.dialogMode() === 'transfer') {
      this.service.transfer({ productId: row.productId, quantity: this.qty, notes: this.notes || null }).subscribe(done);
    } else {
      const signed = this.adjDirection === 'remove' ? -Math.abs(this.qty) : Math.abs(this.qty);
      this.service
        .adjust({
          productId: row.productId,
          type: this.adjType,
          location: this.adjLocation,
          quantity: signed,
          reason: this.notes || null
        })
        .subscribe(done);
    }
  }
}
