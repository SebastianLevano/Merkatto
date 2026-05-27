import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClosingsService } from './closings.service';

@Component({
  selector: 'app-closing-form',
  imports: [FormsModule, RouterLink],
  templateUrl: './closing-form.html'
})
export class ClosingForm {
  private readonly service = inject(ClosingsService);
  private readonly router = inject(Router);

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
    this.loadPreview();
  }

  protected loadPreview(): void {
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
    if (this.alreadyClosed() || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    this.service
      .create({
        businessDate: this.businessDate,
        cashAmount: this.num(this.cash),
        yapeAmount: this.num(this.yape),
        plinAmount: this.num(this.plin),
        posAmount: this.num(this.pos),
        posCommissionRate: this.num(this.posCommissionPercent) / 100,
        quickPurchases: this.num(this.quickPurchases),
        notes: this.notes || null
      })
      .subscribe({
        next: () => this.router.navigate(['/cierre']),
        error: () => {
          this.error.set('No se pudo registrar el cierre.');
          this.saving.set(false);
        }
      });
  }

  private num(v: unknown): number {
    const n = Number(v);
    return isNaN(n) ? 0 : n;
  }
}
