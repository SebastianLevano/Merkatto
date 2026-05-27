import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ProductsService } from './products.service';
import { LookupItem } from './product.models';

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  templateUrl: './product-form.html'
})
export class ProductForm {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ProductsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly categories = signal<LookupItem[]>([]);
  protected readonly brands = signal<LookupItem[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    internalCode: [''],
    categoryId: [null as number | null, [Validators.required]],
    brandId: [null as number | null],
    purchaseUnit: ['paquete', [Validators.required]],
    lastPurchaseCost: [0, [Validators.required, Validators.min(0)]],
    unitsPerPurchaseUnit: [1, [Validators.required, Validators.min(1)]],
    saleUnit: ['unidad', [Validators.required]],
    salePrice: [0, [Validators.required, Validators.min(0)]],
    minStock: [0, [Validators.min(0)]],
    initialWarehouseStock: [0, [Validators.min(0)]]
  });

  // Live preview mirrors the backend math: unitCost = cost / unitsPerPackage.
  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly unitCost = computed(() => {
    const v = this.value();
    const units = Number(v.unitsPerPurchaseUnit) || 0;
    return units > 0 ? Number(v.lastPurchaseCost) / units : 0;
  });
  protected readonly margin = computed(() => Number(this.value().salePrice) - this.unitCost());

  constructor() {
    this.service.categories().subscribe((c) => this.categories.set(c));
    this.service.brands().subscribe((b) => this.brands.set(b));

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      this.id.set(id);
      this.form.controls.initialWarehouseStock.disable();
      this.service.get(id).subscribe((p) => {
        this.form.patchValue({
          name: p.name,
          internalCode: p.internalCode ?? '',
          categoryId: p.categoryId,
          brandId: p.brandId,
          purchaseUnit: p.purchaseUnit,
          lastPurchaseCost: p.lastPurchaseCost,
          unitsPerPurchaseUnit: p.unitsPerPurchaseUnit,
          saleUnit: p.saleUnit,
          salePrice: p.salePrice,
          minStock: p.minStock
        });
      });
    }
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    const raw = this.form.getRawValue();
    const payload = {
      name: raw.name,
      internalCode: raw.internalCode || null,
      categoryId: raw.categoryId!,
      brandId: raw.brandId,
      purchaseUnit: raw.purchaseUnit,
      lastPurchaseCost: raw.lastPurchaseCost,
      unitsPerPurchaseUnit: raw.unitsPerPurchaseUnit,
      saleUnit: raw.saleUnit,
      salePrice: raw.salePrice,
      minStock: raw.minStock
    };

    const request: Observable<unknown> = this.id()
      ? this.service.update(this.id()!, payload)
      : this.service.create({ ...payload, initialWarehouseStock: raw.initialWarehouseStock });

    request.subscribe({
      next: () => this.router.navigate(['/productos']),
      error: () => {
        this.error.set('No se pudo guardar el producto. Revisa los datos.');
        this.saving.set(false);
      }
    });
  }
}
