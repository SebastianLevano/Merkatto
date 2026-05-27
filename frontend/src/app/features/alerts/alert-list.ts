import { Component, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { AlertItem, AlertSeverity, AlertType } from './alert.models';
import { AlertsService } from './alerts.service';

@Component({
  selector: 'app-alert-list',
  imports: [NgClass],
  templateUrl: './alert-list.html'
})
export class AlertList {
  private readonly service = inject(AlertsService);

  protected readonly loading = signal(true);
  protected readonly alerts = signal<AlertItem[]>([]);

  constructor() {
    this.service.list().subscribe({
      next: (a) => { this.alerts.set(a); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  protected severityClass(s: AlertSeverity): string {
    if (s === AlertSeverity.Critical) return 'border-red-300 bg-red-50';
    if (s === AlertSeverity.Warning)  return 'border-amber-300 bg-amber-50';
    return 'border-slate-200 bg-white';
  }

  protected iconClass(s: AlertSeverity): string {
    if (s === AlertSeverity.Critical) return 'text-red-500';
    if (s === AlertSeverity.Warning)  return 'text-amber-500';
    return 'text-slate-400';
  }

  protected icon(t: AlertType): string {
    if (t === AlertType.StockOut || t === AlertType.StockLow) return '▼';
    if (t === AlertType.NoClosure) return '◷';
    return '⚠';
  }

  protected label(t: AlertType): string {
    if (t === AlertType.StockOut)         return 'Sin stock';
    if (t === AlertType.StockLow)         return 'Stock bajo';
    if (t === AlertType.NoClosure)        return 'Sin cierre';
    if (t === AlertType.HighPendingCredit) return 'Fiado elevado';
    return 'Alerta';
  }
}
