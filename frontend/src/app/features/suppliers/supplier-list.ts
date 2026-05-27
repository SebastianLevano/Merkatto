import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PurchasesService } from '../purchases/purchases.service';
import { Supplier } from '../purchases/purchase.models';

type DialogMode = 'create' | 'edit' | null;

@Component({
  selector: 'app-supplier-list',
  imports: [FormsModule, RouterLink],
  templateUrl: './supplier-list.html'
})
export class SupplierList {
  private readonly service = inject(PurchasesService);
  private readonly auth = inject(AuthService);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly loading = signal(true);
  protected readonly suppliers = signal<Supplier[]>([]);
  protected readonly dialogMode = signal<DialogMode>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected editTarget: Supplier | null = null;
  protected formName = '';
  protected formPhone = '';
  protected formNotes = '';

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.suppliers().subscribe({
      next: (s) => { this.suppliers.set(s); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  protected openCreate(): void {
    this.editTarget = null;
    this.formName = '';
    this.formPhone = '';
    this.formNotes = '';
    this.error.set(null);
    this.dialogMode.set('create');
  }

  protected openEdit(s: Supplier): void {
    this.editTarget = s;
    this.formName = s.name;
    this.formPhone = s.phone ?? '';
    this.formNotes = s.notes ?? '';
    this.error.set(null);
    this.dialogMode.set('edit');
  }

  protected close(): void {
    this.dialogMode.set(null);
    this.editTarget = null;
  }

  protected save(): void {
    if (!this.formName.trim() || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    const name = this.formName.trim();
    const phone = this.formPhone.trim() || null;
    const notes = this.formNotes.trim() || null;

    const done = {
      next: () => { this.saving.set(false); this.close(); this.load(); },
      error: () => { this.error.set('No se pudo guardar.'); this.saving.set(false); }
    };

    if (this.dialogMode() === 'edit' && this.editTarget) {
      this.service.updateSupplier(this.editTarget.id, name, phone, notes).subscribe(done);
    } else {
      this.service.createSupplier(name, phone, notes).subscribe(done);
    }
  }
}
