import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ExpensesService } from './expenses.service';
import { EXPENSE_LABEL, ExpenseItem, ExpenseType } from './expense.models';
import { ReportsService } from '../reports/reports.service';

function startOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}

@Component({
  selector: 'app-expense-list',
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './expense-list.html'
})
export class ExpenseList {
  private readonly service = inject(ExpensesService);
  private readonly auth = inject(AuthService);
  private readonly reports = inject(ReportsService);

  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly EXPENSE_LABEL = EXPENSE_LABEL;
  protected readonly expenseTypes = Object.entries(EXPENSE_LABEL).map(([v, label]) => ({
    value: Number(v) as ExpenseType,
    label
  }));

  protected readonly loading = signal(true);
  protected readonly expenses = signal<ExpenseItem[]>([]);
  protected readonly total = computed(() => this.expenses().reduce((s, e) => s + e.amount, 0));

  protected from = startOfMonth();
  protected to = new Date().toISOString().slice(0, 10);

  // Dialog: shared for create and edit. editingId === null => create.
  protected readonly showDialog = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected newDate = new Date().toISOString().slice(0, 10);
  protected newType: ExpenseType = ExpenseType.Luz;
  protected newAmount: number | null = null;
  protected newDescription = '';

  // Delete confirmation
  protected readonly toDelete = signal<ExpenseItem | null>(null);
  protected readonly deleting = signal(false);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list({ from: this.from, to: this.to, pageSize: 200 }).subscribe({
      next: (res) => {
        this.expenses.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected label(type: ExpenseType): string {
    return EXPENSE_LABEL[type];
  }

  protected openDialog(): void {
    this.editingId.set(null);
    this.newDate = new Date().toISOString().slice(0, 10);
    this.newType = ExpenseType.Luz;
    this.newAmount = null;
    this.newDescription = '';
    this.error.set(null);
    this.showDialog.set(true);
  }

  protected openEdit(e: ExpenseItem): void {
    if (!this.isAdmin()) return;
    this.editingId.set(e.id);
    this.newDate = e.date;
    this.newType = e.type;
    this.newAmount = e.amount;
    this.newDescription = e.description ?? '';
    this.error.set(null);
    this.showDialog.set(true);
  }

  protected closeDialog(): void {
    if (this.saving()) return;
    this.showDialog.set(false);
  }

  protected save(): void {
    if (!this.newAmount || this.newAmount <= 0 || this.saving()) {
      this.error.set('Ingresa un monto válido.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    const payload = {
      date: this.newDate,
      type: this.newType,
      amount: this.newAmount,
      description: this.newDescription || null
    };
    const req: Observable<unknown> = this.editingId()
      ? this.service.update(this.editingId()!, payload)
      : this.service.create(payload);
    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.showDialog.set(false);
        this.load();
      },
      error: () => {
        this.error.set('No se pudo guardar el gasto.');
        this.saving.set(false);
      }
    });
  }

  // --- Delete ---
  protected askDelete(e: ExpenseItem, ev: Event): void {
    ev.stopPropagation();
    this.toDelete.set(e);
  }

  protected cancelDelete(): void {
    this.toDelete.set(null);
  }

  protected confirmDelete(): void {
    const e = this.toDelete();
    if (!e || this.deleting()) return;
    this.deleting.set(true);
    this.service.delete(e.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.toDelete.set(null);
        this.load();
      },
      error: () => {
        this.deleting.set(false);
      }
    });
  }

  protected downloadExcel(): void {
    const d = new Date(this.from);
    this.reports.downloadExpensesExcel(d.getFullYear(), d.getMonth() + 1);
  }
}
