import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { ProductsService } from '../products/products.service';
import { LookupItem } from '../products/product.models';
import { SectionTabs } from '../inventory/section-tabs';

/**
 * Basic catalog management for categorías: list, rename inline, delete with confirmation.
 * Admin-only on writes (the backend enforces it too).
 */
@Component({
  selector: 'app-category-list',
  imports: [FormsModule, SectionTabs],
  templateUrl: './category-list.html'
})
export class CategoryList {
  private readonly service = inject(ProductsService);
  private readonly auth = inject(AuthService);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(true);
  protected readonly categories = signal<LookupItem[]>([]);

  protected readonly editingId = signal<number | null>(null);
  protected editingName = '';
  protected readonly saving = signal(false);

  // Delete confirmation
  protected readonly toDelete = signal<LookupItem | null>(null);
  protected readonly deleting = signal(false);

  protected readonly error = signal<string | null>(null);

  // Create new
  protected readonly creating = signal(false);
  protected newName = '';

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.categories().subscribe({
      next: (c) => {
        this.categories.set(c);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  // --- Inline rename ---
  protected startEdit(c: LookupItem): void {
    this.editingId.set(c.id);
    this.editingName = c.name;
    this.error.set(null);
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.editingName = '';
  }

  protected saveEdit(): void {
    const id = this.editingId();
    const name = this.editingName.trim();
    if (id == null || !name || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    this.service.updateCategory(id, name).subscribe({
      next: () => {
        this.saving.set(false);
        this.editingId.set(null);
        this.editingName = '';
        this.load();
      },
      error: (err) => {
        this.error.set(err?.status === 409 ? 'Ya existe otra categoría con ese nombre.' : 'No se pudo guardar.');
        this.saving.set(false);
      }
    });
  }

  // --- Create new ---
  protected openCreate(): void {
    this.newName = '';
    this.error.set(null);
    this.creating.set(true);
  }

  protected cancelCreate(): void {
    this.creating.set(false);
    this.newName = '';
  }

  protected confirmCreate(): void {
    const name = this.newName.trim();
    if (!name || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    this.service.createCategory(name).subscribe({
      next: () => {
        this.saving.set(false);
        this.creating.set(false);
        this.newName = '';
        this.load();
      },
      error: (err) => {
        this.error.set(err?.status === 409 ? 'Ya existe una categoría con ese nombre.' : 'No se pudo crear.');
        this.saving.set(false);
      }
    });
  }

  // --- Delete ---
  protected askDelete(c: LookupItem): void {
    this.error.set(null);
    this.toDelete.set(c);
  }

  protected cancelDelete(): void {
    this.toDelete.set(null);
  }

  protected confirmDelete(): void {
    const c = this.toDelete();
    if (!c || this.deleting()) return;
    this.deleting.set(true);
    this.error.set(null);
    this.service.deleteCategory(c.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.toDelete.set(null);
        this.load();
      },
      error: (err) => {
        this.error.set(
          err?.status === 422
            ? 'La categoría tiene productos asignados. Reasignalos antes de eliminar.'
            : 'No se pudo eliminar.'
        );
        this.deleting.set(false);
      }
    });
  }
}
