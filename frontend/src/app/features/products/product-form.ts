import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ProductsService } from './products.service';
import { LookupItem } from './product.models';

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe],
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

  // Inline "+ Nueva categoría" dialog
  protected readonly categoryDialog = signal(false);
  protected readonly categorySaving = signal(false);
  protected readonly categoryError = signal<string | null>(null);
  protected newCategoryName = '';

  // Collapsible "ya tengo este producto en stock" section (only on Create)
  protected readonly initialOpen = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    internalCode: [''],
    categoryId: [null as number | null, [Validators.required]],
    brandId: [null as number | null],
    saleUnit: ['unidad', [Validators.required]],
    salePrice: [0, [Validators.required, Validators.min(0)]],
    minStock: [0, [Validators.min(0)]],
    // Initial-load block (only used when section is open and on create)
    initialPaquetes: [null as number | null, [Validators.min(1)]],
    initialUnidadesPorPaquete: [null as number | null, [Validators.min(1)]],
    initialCostoPorPaquete: [null as number | null, [Validators.min(0)]]
  });

  // Live preview of the initial load: unit cost and starting stock.
  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly initialUnitCost = computed(() => {
    const v = this.value();
    const u = Number(v.initialUnidadesPorPaquete) || 0;
    const c = Number(v.initialCostoPorPaquete) || 0;
    return u > 0 ? c / u : 0;
  });
  protected readonly initialStockUnits = computed(() => {
    const v = this.value();
    const p = Number(v.initialPaquetes) || 0;
    const u = Number(v.initialUnidadesPorPaquete) || 0;
    return p * u;
  });
  protected readonly initialMargin = computed(() => Number(this.value().salePrice) - this.initialUnitCost());

  constructor() {
    this.service.categories().subscribe((c) => this.categories.set(c));
    this.service.brands().subscribe((b) => this.brands.set(b));

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      this.id.set(id);
      this.service.get(id).subscribe((p) => {
        this.form.patchValue({
          name: p.name,
          internalCode: p.internalCode ?? '',
          categoryId: p.categoryId,
          brandId: p.brandId,
          saleUnit: p.saleUnit,
          salePrice: p.salePrice,
          minStock: p.minStock
        });
      });
    }
  }

  protected toggleInitial(): void {
    this.initialOpen.update((v) => !v);
    if (!this.initialOpen()) {
      this.form.patchValue({
        initialPaquetes: null,
        initialUnidadesPorPaquete: null,
        initialCostoPorPaquete: null
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
    const basePayload = {
      name: raw.name,
      internalCode: raw.internalCode || null,
      categoryId: raw.categoryId!,
      brandId: raw.brandId,
      saleUnit: raw.saleUnit,
      salePrice: raw.salePrice,
      minStock: raw.minStock
    };

    const request: Observable<unknown> = this.id()
      ? this.service.update(this.id()!, basePayload)
      : this.service.create({
          ...basePayload,
          initialPaquetes: this.initialOpen() ? raw.initialPaquetes : null,
          initialUnidadesPorPaquete: this.initialOpen() ? raw.initialUnidadesPorPaquete : null,
          initialCostoPorPaquete: this.initialOpen() ? raw.initialCostoPorPaquete : null
        });

    request.subscribe({
      next: () => this.router.navigate(['/inventario/productos']),
      error: () => {
        this.error.set('No se pudo guardar el producto. Revisa los datos.');
        this.saving.set(false);
      }
    });
  }

  // --- Inline category creation ---

  protected openCategoryDialog(): void {
    this.newCategoryName = '';
    this.categoryError.set(null);
    this.categoryDialog.set(true);
  }

  protected closeCategoryDialog(): void {
    if (this.categorySaving()) return;
    this.categoryDialog.set(false);
  }

  protected saveCategory(): void {
    const name = this.newCategoryName.trim();
    if (!name || this.categorySaving()) return;
    this.categorySaving.set(true);
    this.categoryError.set(null);
    this.service.createCategory(name).subscribe({
      next: ({ id }) => {
        this.service.categories().subscribe((list) => {
          this.categories.set(list);
          this.form.controls.categoryId.setValue(id);
          this.categorySaving.set(false);
          this.categoryDialog.set(false);
        });
      },
      error: (err) => {
        this.categoryError.set(
          err?.status === 409 ? 'Ya existe una categoría con ese nombre.' : 'No se pudo crear la categoría.'
        );
        this.categorySaving.set(false);
      }
    });
  }
}
