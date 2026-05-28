import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ClosingsService } from './closings.service';

@Component({
  selector: 'app-closing-form',
  imports: [FormsModule, RouterLink],
  templateUrl: './closing-form.html'
})
export class ClosingForm {
  private readonly service = inject(ClosingsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly totalExpenses = signal(0);
  protected readonly alreadyClosed = signal(false);

  protected businessDate = new Date().toISOString().slice(0, 10);
  protected cash = 0;
  protected yape = 0;
  protected plin = 0;
  protected pos = 0;
  protected posCommissionPercent = 3.5;
  protected quickPurchases = 0;
  protected notes = '';

  constructor() {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      this.id.set(id);
      this.service.getById(id).subscribe({
        next: (c) => {
          this.businessDate = c.businessDate;
          this.cash = c.cashAmount;
          this.yape = c.yapeAmount;
          this.plin = c.plinAmount;
          this.pos = c.posAmount;
          this.posCommissionPercent = c.posCommissionRate * 100;
          this.quickPurchases = c.quickPurchases;
          this.notes = c.notes ?? '';
          this.totalExpenses.set(c.totalExpenses);
          this.alreadyClosed.set(false); // editing an existing one, OK
        },
        error: () => this.error.set('No se pudo cargar el cierre.')
      });
    } else {
      this.loadPreview();
    }
  }

  protected loadPreview(): void {
    if (this.id()) return; // edit mode: don't replace expenses
    this.service.preview(this.businessDate).subscribe((p) => {
      this.totalExpenses.set(p.totalExpenses);
      this.alreadyClosed.set(p.alreadyClosed);
    });
  }

  protected get grossIncome(): number {
    return this.num(this.cash) + this.num(this.yape) + this.num(this.plin) + this.num(this.pos);
  }

  protected get commission(): number {
    return this.num(this.pos) * (this.num(this.posCommissionPercent) / 100);
  }

  protected get netFlow(): number {
    return this.grossIncome - this.totalExpenses() - this.num(this.quickPurchases) - this.commission;
  }

  protected save(): void {
    if (this.saving()) return;
    if (!this.id() && this.alreadyClosed()) return;
    this.saving.set(true);
    this.error.set(null);
    const payload = {
      businessDate: this.businessDate,
      cashAmount: this.num(this.cash),
      yapeAmount: this.num(this.yape),
      plinAmount: this.num(this.plin),
      posAmount: this.num(this.pos),
      posCommissionRate: this.num(this.posCommissionPercent) / 100,
      quickPurchases: this.num(this.quickPurchases),
      notes: this.notes || null
    };
    const req: Observable<unknown> = this.id()
      ? this.service.update(this.id()!, payload)
      : this.service.create(payload);
    req.subscribe({
      next: () => this.router.navigate(['/cierre']),
      error: (err: { error?: { detail?: string } }) => {
        this.error.set(err?.error?.detail ?? 'No se pudo guardar el cierre.');
        this.saving.set(false);
      }
    });
  }

  private num(v: unknown): number {
    const n = Number(v);
    return isNaN(n) ? 0 : n;
  }
}
