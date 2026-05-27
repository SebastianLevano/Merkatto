import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreditService } from './credit.service';
import { CreditCustomerListItem } from './credit.models';

@Component({
  selector: 'app-credit-list',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe],
  templateUrl: './credit-list.html'
})
export class CreditList {
  private readonly service = inject(CreditService);

  protected readonly loading = signal(true);
  protected readonly customers = signal<CreditCustomerListItem[]>([]);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly totalOwed = computed(() => this.customers().reduce((s, c) => s + Math.max(c.balance, 0), 0));

  // Add-customer dialog
  protected readonly showDialog = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected newName = '';
  protected newPhone = '';

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.customers(this.search.value).subscribe({
      next: (c) => {
        this.customers.set(c);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected openDialog(): void {
    this.newName = '';
    this.newPhone = '';
    this.error.set(null);
    this.showDialog.set(true);
  }

  protected save(): void {
    if (!this.newName.trim() || this.saving()) {
      this.error.set('Ingresa el nombre del cliente.');
      return;
    }
    this.saving.set(true);
    this.service.createCustomer({ name: this.newName.trim(), phone: this.newPhone || null, notes: null }).subscribe({
      next: () => {
        this.saving.set(false);
        this.showDialog.set(false);
        this.load();
      },
      error: () => {
        this.error.set('No se pudo crear el cliente.');
        this.saving.set(false);
      }
    });
  }
}
