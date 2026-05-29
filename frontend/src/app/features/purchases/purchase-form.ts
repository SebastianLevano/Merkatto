import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
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
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly suppliers = signal<Supplier[]>([]);
  protected readonly products = signal<ProductListItem[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    supplierName: [''],
    date: [new Date().toISOString().slice(0, 10), [Validators.required]],
    notes: [''],
    items: this.fb.array<FormGroup>([])
  });

  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly total = computed(() => {
    this.value();
    return this.items.controls.reduce((sum, g) => {
      const paquetes = Number(g.get('paquetes')?.value) || 0;
      const costo = Number(g.get('costoPorPaquete')?.value) || 0;
      return sum + paquetes * costo;
    }, 0);
  });

  constructor() {
    this.service.suppliers().subscribe((s) => this.suppliers.set(s));
    this.productsService.list({ pageSize: 200, activeOnly: true }).subscribe((r) => this.products.set(r.items));

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      this.id.set(id);
      this.service.get(id).subscribe((p) => {
        this.form.patchValue({
          supplierName: p.supplierName ?? '',
          date: p.date,
          notes: p.notes ?? ''
        });
        for (const it of p.items) {
          this.items.push(
            this.fb.group({
              productId: [it.productId, [Validators.required]],
              paquetes: [it.quantity, [Validators.required, Validators.min(0.001)]],
              unidadesPorPaquete: [it.conversionFactor, [Validators.required, Validators.min(1)]],
              costoPorPaquete: [it.unitCost, [Validators.required, Validators.min(0)]]
            })
          );
        }
      });
    } else {
      this.addItem();
    }
  }

  get items(): FormArray<FormGroup> {
    return this.form.controls.items;
  }

  protected addItem(): void {
    this.items.push(
      this.fb.group({
        productId: [null as number | null, [Validators.required]],
        paquetes: [1, [Validators.required, Validators.min(0.001)]],
        unidadesPorPaquete: [1, [Validators.required, Validators.min(1)]],
        costoPorPaquete: [0, [Validators.required, Validators.min(0)]]
      })
    );
  }

  protected removeItem(index: number): void {
    this.items.removeAt(index);
  }

  protected lineSubtotal(group: FormGroup): number {
    return (Number(group.get('paquetes')?.value) || 0) * (Number(group.get('costoPorPaquete')?.value) || 0);
  }

  protected lineUnits(group: FormGroup): number {
    return (Number(group.get('paquetes')?.value) || 0) * (Number(group.get('unidadesPorPaquete')?.value) || 0);
  }

  protected save(): void {
    if (this.form.invalid || this.items.length === 0 || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    const raw = this.form.getRawValue();
    const payload = {
      supplierName: raw.supplierName?.trim() || null,
      date: raw.date,
      notes: raw.notes || null,
      items: this.items.controls.map((g) => ({
        productId: Number(g.get('productId')!.value),
        paquetes: Number(g.get('paquetes')!.value),
        unidadesPorPaquete: Number(g.get('unidadesPorPaquete')!.value),
        costoPorPaquete: Number(g.get('costoPorPaquete')!.value)
      }))
    };
    const req: Observable<unknown> = this.id()
      ? this.service.update(this.id()!, payload)
      : this.service.create(payload);
    req.subscribe({
      next: () => this.router.navigate(this.id() ? ['/compras', this.id()] : ['/compras']),
      error: (err: { error?: { detail?: string } }) => {
        this.error.set(err?.error?.detail ?? 'No se pudo guardar la compra. Revisa los datos.');
        this.saving.set(false);
      }
    });
  }
}
