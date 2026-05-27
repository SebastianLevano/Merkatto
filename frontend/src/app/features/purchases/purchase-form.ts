import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { ProductsService } from '../products/products.service';
import { ProductListItem } from '../products/product.models';
import { PurchasesService } from './purchases.service';
import { Supplier } from './purchase.models';

@Component({
  selector: 'app-purchase-form',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  templateUrl: './purchase-form.html'
})
export class PurchaseForm {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(PurchasesService);
  private readonly productsService = inject(ProductsService);
  private readonly router = inject(Router);

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly suppliers = signal<Supplier[]>([]);
  protected readonly products = signal<ProductListItem[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    supplierId: [null as number | null],
    date: [new Date().toISOString().slice(0, 10), [Validators.required]],
    notes: [''],
    items: this.fb.array<FormGroup>([])
  });

  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly total = computed(() => {
    this.value();
    return this.items.controls.reduce((sum, g) => {
      const qty = Number(g.get('quantity')?.value) || 0;
      const cost = Number(g.get('unitCost')?.value) || 0;
      return sum + qty * cost;
    }, 0);
  });

  constructor() {
    this.service.suppliers().subscribe((s) => this.suppliers.set(s));
    this.productsService.list({ pageSize: 200, activeOnly: true }).subscribe((r) => this.products.set(r.items));
    this.addItem();
  }

  get items(): FormArray<FormGroup> {
    return this.form.controls.items;
  }

  protected addItem(): void {
    this.items.push(
      this.fb.group({
        productId: [null as number | null, [Validators.required]],
        quantity: [1, [Validators.required, Validators.min(0.001)]],
        unitCost: [0, [Validators.required, Validators.min(0)]]
      })
    );
  }

  protected removeItem(index: number): void {
    this.items.removeAt(index);
  }

  protected lineSubtotal(group: FormGroup): number {
    return (Number(group.get('quantity')?.value) || 0) * (Number(group.get('unitCost')?.value) || 0);
  }

  protected save(): void {
    if (this.form.invalid || this.items.length === 0 || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    const raw = this.form.getRawValue();
    this.service
      .create({
        supplierId: raw.supplierId,
        date: raw.date,
        notes: raw.notes || null,
        items: this.items.controls.map((g) => ({
          productId: Number(g.get('productId')!.value),
          quantity: Number(g.get('quantity')!.value),
          unitCost: Number(g.get('unitCost')!.value)
        }))
      })
      .subscribe({
        next: () => this.router.navigate(['/compras']),
        error: () => {
          this.error.set('No se pudo registrar la compra. Revisa los datos.');
          this.saving.set(false);
        }
      });
  }
}
