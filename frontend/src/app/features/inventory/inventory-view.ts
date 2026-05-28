import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProductsService } from '../products/products.service';
import { BatchCountItem, ProductListItem } from '../products/product.models';
import { InventoryService } from './inventory.service';
import { ADJUSTMENT_LABEL, AdjustmentType, StockLocation } from './inventory.models';
import { SectionTabs } from './section-tabs';

interface ConteoItem {
  productId: number;
  name: string;
  saleUnit: string;
  warehouseStock: number;
  counterStock: number;
}

type DialogMode = 'transfer' | 'adjust' | null;

/**
 * Inventario tab: full stock view (warehouse/counter, prices, days of stock) plus the daily
 * count and the transfer/adjust actions. Stock updates as products are added (purchases) and
 * via the daily count.
 */
@Component({
  selector: 'app-inventory-view',
  imports: [ReactiveFormsModule, FormsModule, DecimalPipe, SectionTabs],
  templateUrl: './inventory-view.html'
})
export class InventoryView {
  private readonly service = inject(ProductsService);
  private readonly invService = inject(InventoryService);

  protected readonly loading = signal(false);
  protected readonly products = signal<ProductListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly lowStockOnly = signal(false);

  // Transfer / adjust dialog
  protected readonly StockLocation = StockLocation;
  protected readonly adjustmentTypes = Object.entries(ADJUSTMENT_LABEL).map(([v, label]) => ({
    value: Number(v) as AdjustmentType,
    label
  }));
  protected readonly dialogMode = signal<DialogMode>(null);
  protected readonly current = signal<ProductListItem | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected qty = 1;
  protected notes = '';
  protected adjType: AdjustmentType = AdjustmentType.Loss;
  protected adjLocation: StockLocation = StockLocation.Counter;
  protected adjDirection: 'remove' | 'add' = 'remove';

  // Conteo diario
  protected readonly conteoMode = signal(false);
  protected readonly conteoSaving = signal(false);
  protected readonly conteoError = signal<string | null>(null);
  protected conteoItems: ConteoItem[] = [];
  protected conteoDate = new Date().toISOString().slice(0, 10);
  protected readonly today = new Date().toISOString().slice(0, 10);

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list({ search: this.search.value, activeOnly: true, pageSize: 200 }).subscribe({
      next: (res) => {
        const items = this.lowStockOnly() ? res.items.filter((p) => p.isLowStock) : res.items;
        this.products.set(items);
        this.total.set(items.length);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected toggleLowStock(): void {
    this.lowStockOnly.update((v) => !v);
    this.load();
  }

  // --- Transfer / Adjust dialogs ---

  protected openTransfer(row: ProductListItem): void {
    this.current.set(row);
    this.qty = 1;
    this.notes = '';
    this.error.set(null);
    this.dialogMode.set('transfer');
  }

  protected openAdjust(row: ProductListItem): void {
    this.current.set(row);
    this.qty = 1;
    this.adjType = AdjustmentType.Loss;
    this.adjLocation = StockLocation.Counter;
    this.adjDirection = 'remove';
    this.notes = '';
    this.error.set(null);
    this.dialogMode.set('adjust');
  }

  protected closeDialog(): void {
    this.dialogMode.set(null);
    this.current.set(null);
  }

  protected confirmDialog(): void {
    const row = this.current();
    if (!row || this.qty <= 0 || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);

    const done = {
      next: () => { this.saving.set(false); this.closeDialog(); this.load(); },
      error: () => { this.error.set('No se pudo completar la operación.'); this.saving.set(false); }
    };

    if (this.dialogMode() === 'transfer') {
      this.invService.transfer({ productId: row.id, quantity: this.qty, notes: this.notes || null }).subscribe(done);
    } else {
      const signed = this.adjDirection === 'remove' ? -Math.abs(this.qty) : Math.abs(this.qty);
      this.invService.adjust({
        productId: row.id, type: this.adjType,
        location: this.adjLocation, quantity: signed, reason: this.notes || null
      }).subscribe(done);
    }
  }

  // --- Conteo diario ---

  protected enterConteo(): void {
    this.conteoItems = this.products().map((p) => ({
      productId: p.id,
      name: p.name,
      saleUnit: p.saleUnit,
      warehouseStock: p.warehouseStock,
      counterStock: p.counterStock
    }));
    this.conteoDate = new Date().toISOString().slice(0, 10);
    this.conteoError.set(null);
    this.conteoMode.set(true);
  }

  protected cancelConteo(): void {
    this.conteoMode.set(false);
    this.conteoItems = [];
  }

  protected submitConteo(): void {
    if (this.conteoSaving()) return;
    const items: BatchCountItem[] = this.conteoItems.map((i) => ({
      productId: i.productId,
      warehouseStock: Number(i.warehouseStock) || 0,
      counterStock: Number(i.counterStock) || 0
    }));
    this.conteoSaving.set(true);
    this.conteoError.set(null);
    this.invService.batchCount(items, this.conteoDate).subscribe({
      next: () => {
        this.conteoSaving.set(false);
        this.conteoMode.set(false);
        this.conteoItems = [];
        this.load();
      },
      error: () => {
        this.conteoError.set('No se pudo guardar el conteo.');
        this.conteoSaving.set(false);
      }
    });
  }
}
