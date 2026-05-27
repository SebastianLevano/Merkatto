import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth/auth.service';
import { ProductsService } from './products.service';
import { BatchCountItem, ProductListItem } from './product.models';
import { InventoryService } from '../inventory/inventory.service';
import { ADJUSTMENT_LABEL, AdjustmentType, StockLocation } from '../inventory/inventory.models';

interface ConteoItem {
  productId: number;
  name: string;
  saleUnit: string;
  warehouseStock: number;
  counterStock: number;
}

type DialogMode = 'transfer' | 'adjust' | null;

@Component({
  selector: 'app-product-list',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe],
  templateUrl: './product-list.html'
})
export class ProductList {
  private readonly service = inject(ProductsService);
  private readonly invService = inject(InventoryService);
  private readonly auth = inject(AuthService);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(false);
  protected readonly products = signal<ProductListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly search = new FormControl('', { nonNullable: true });

  // Inventory dialog
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

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list({ search: this.search.value, pageSize: 100 }).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected toggleActive(p: ProductListItem): void {
    this.service.setActive(p.id, !p.isActive).subscribe(() => this.load());
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
    this.invService.batchCount(items).subscribe({
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
