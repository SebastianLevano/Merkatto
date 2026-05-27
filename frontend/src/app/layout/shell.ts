import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html'
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = this.auth.user;

  // Desktop-first sidebar. Modules light up as Phase 1 lands.
  protected readonly nav: NavItem[] = [
    { label: 'Dashboard', path: '/', icon: '◈' },
    { label: 'Productos', path: '/productos', icon: '▤' },
    { label: 'Compras', path: '/compras', icon: '▦' },
    { label: 'Inventario', path: '/inventario', icon: '▣' },
    { label: 'Cierre diario', path: '/cierre', icon: '◷' },
    { label: 'Gastos', path: '/gastos', icon: '▾' },
    { label: 'Fiados', path: '/fiados', icon: '☷' }
  ];

  protected logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }
}
