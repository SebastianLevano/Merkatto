import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then((m) => m.Login)
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard)
      },
      {
        path: 'productos',
        loadComponent: () => import('./features/products/product-list').then((m) => m.ProductList)
      },
      {
        path: 'productos/nuevo',
        loadComponent: () => import('./features/products/product-form').then((m) => m.ProductForm)
      },
      {
        path: 'productos/:id',
        loadComponent: () => import('./features/products/product-form').then((m) => m.ProductForm)
      },
      {
        path: 'compras',
        loadComponent: () => import('./features/purchases/purchase-list').then((m) => m.PurchaseList)
      },
      {
        path: 'compras/nueva',
        loadComponent: () => import('./features/purchases/purchase-form').then((m) => m.PurchaseForm)
      },
      {
        path: 'inventario',
        redirectTo: '/productos',
        pathMatch: 'full'
      },
      {
        path: 'inventario/movimientos',
        loadComponent: () => import('./features/inventory/inventory-movements').then((m) => m.InventoryMovements)
      },
      {
        path: 'gastos',
        loadComponent: () => import('./features/expenses/expense-list').then((m) => m.ExpenseList)
      },
      {
        path: 'cierre',
        loadComponent: () => import('./features/closings/closing-list').then((m) => m.ClosingList)
      },
      {
        path: 'cierre/nuevo',
        loadComponent: () => import('./features/closings/closing-form').then((m) => m.ClosingForm)
      },
      {
        path: 'fiados',
        loadComponent: () => import('./features/credit/credit-list').then((m) => m.CreditList)
      },
      {
        path: 'fiados/:id',
        loadComponent: () => import('./features/credit/credit-detail').then((m) => m.CreditDetail)
      },
      {
        path: 'alertas',
        loadComponent: () => import('./features/alerts/alert-list').then((m) => m.AlertList)
      },
      {
        path: 'nrus',
        loadComponent: () => import('./features/nrus/nrus').then((m) => m.Nrus)
      },
      {
        path: 'proveedores',
        loadComponent: () => import('./features/suppliers/supplier-list').then((m) => m.SupplierList)
      },
      {
        path: 'timeline',
        loadComponent: () => import('./features/timeline/timeline').then((m) => m.Timeline)
      },
      {
        path: 'configuracion',
        loadComponent: () => import('./features/settings/settings').then((m) => m.Settings)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
