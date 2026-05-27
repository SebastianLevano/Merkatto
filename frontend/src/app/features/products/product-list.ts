import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth/auth.service';
import { ProductsService } from './products.service';
import { ProductListItem } from './product.models';

@Component({
  selector: 'app-product-list',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  templateUrl: './product-list.html'
})
export class ProductList {
  private readonly service = inject(ProductsService);
  private readonly auth = inject(AuthService);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(false);
  protected readonly products = signal<ProductListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly search = new FormControl('', { nonNullable: true });

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list({ search: this.search.value, pageSize: 50 }).subscribe({
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
}
