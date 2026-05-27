import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { InventoryService } from './inventory.service';
import { MOVEMENT_LABEL, MovementRow, MovementType, StockLocation } from './inventory.models';

@Component({
  selector: 'app-inventory-movements',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './inventory-movements.html'
})
export class InventoryMovements {
  private readonly service = inject(InventoryService);

  protected readonly loading = signal(true);
  protected readonly rows = signal<MovementRow[]>([]);
  protected readonly labels = MOVEMENT_LABEL;

  constructor() {
    this.service.movements({ pageSize: 100 }).subscribe({
      next: (res) => {
        this.rows.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected locationLabel(loc: StockLocation): string {
    return loc === StockLocation.Warehouse ? 'Almacén' : 'Mostrador';
  }

  protected typeLabel(t: MovementType): string {
    return this.labels[t];
  }
}
