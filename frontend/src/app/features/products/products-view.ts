import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth/auth.service';
import { ProductsService } from './products.service';
import { LookupItem, ProductListItem } from './product.models';
import { SectionTabs } from '../inventory/section-tabs';

/**
 * Productos tab: the product catalog (CRUD). Lists products with category and unit cost, lets
 * an admin create, edit, delete (soft-delete) and activate/deactivate. Filterable by category.
 */
@Component({
  selector: 'app-products-view',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, SectionTabs],
  templateUrl: './products-view.html'
})
export class ProductsView {
  private readonly service = inject(ProductsService);
  private readonly auth = inject(AuthService);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(false);
  protected readonly products = signal<ProductListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly categories = signal<LookupItem[]>([]);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected categoryId: number | null = null;

  // Delete confirmation
  protected readonly toDelete = signal<ProductListItem | null>(null);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.service.categories().subscribe((c) => this.categories.set(c));
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service
      .list({ search: this.search.value, categoryId: this.categoryId ?? undefined, pageSize: 200 })
      .subscribe({
        next: (res) => {
          this.products.set(res.items);
          this.total.set(res.total);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  protected onCategoryChange(): void {
    this.load();
  }

  protected toggleActive(p: ProductListItem): void {
    this.service.setActive(p.id, !p.isActive).subscribe(() => this.load());
  }

  protected askDelete(p: ProductListItem): void {
    this.error.set(null);
    this.toDelete.set(p);
  }

  protected cancelDelete(): void {
    this.toDelete.set(null);
  }

  protected confirmDelete(): void {
    const p = this.toDelete();
    if (!p || this.deleting()) return;
    this.deleting.set(true);
    this.error.set(null);
    this.service.delete(p.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.toDelete.set(null);
        this.load();
      },
      error: () => {
        this.error.set('No se pudo eliminar el producto.');
        this.deleting.set(false);
      }
    });
  }
}
