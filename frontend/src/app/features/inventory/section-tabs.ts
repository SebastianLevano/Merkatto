import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/**
 * Tab navigation shared by the Inventario section: the full inventory view, the product
 * catalog (CRUD), and the movements ledger.
 */
@Component({
  selector: 'app-section-tabs',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <div class="flex gap-1 border-b border-slate-200">
      <a
        routerLink="/inventario"
        routerLinkActive="border-slate-900 text-slate-900"
        [routerLinkActiveOptions]="{ exact: true }"
        class="border-b-2 border-transparent px-4 py-2 text-sm font-medium text-slate-500 hover:text-slate-800"
      >
        Inventario
      </a>
      <a
        routerLink="/inventario/productos"
        routerLinkActive="border-slate-900 text-slate-900"
        class="border-b-2 border-transparent px-4 py-2 text-sm font-medium text-slate-500 hover:text-slate-800"
      >
        Productos
      </a>
      <a
        routerLink="/inventario/categorias"
        routerLinkActive="border-slate-900 text-slate-900"
        class="border-b-2 border-transparent px-4 py-2 text-sm font-medium text-slate-500 hover:text-slate-800"
      >
        Categorías
      </a>
      <a
        routerLink="/inventario/movimientos"
        routerLinkActive="border-slate-900 text-slate-900"
        class="border-b-2 border-transparent px-4 py-2 text-sm font-medium text-slate-500 hover:text-slate-800"
      >
        Movimientos
      </a>
    </div>
  `
})
export class SectionTabs {}
