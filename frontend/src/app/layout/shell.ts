import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { AlertsService } from '../features/alerts/alerts.service';
import { SettingsService } from '../features/settings/settings.service';
import { UpdateService } from '../core/update/update.service';

interface NavItem {
  label: string;
  path: string;
  icon: string;
  badge?: () => number;
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html'
})
export class Shell implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly alertsService = inject(AlertsService);
  private readonly settingsService = inject(SettingsService);
  private readonly updateService = inject(UpdateService);
  private readonly router = inject(Router);

  protected readonly user = this.auth.user;
  protected readonly alertCount = signal(0);
  protected readonly mobileOpen = signal(false);
  protected readonly businessName = this.settingsService.businessName;
  protected readonly updateAvailable = this.updateService.updateAvailable;
  protected readonly updateVersion   = this.updateService.updateVersion;

  protected readonly nav: NavItem[] = [
    { label: 'Dashboard',     path: '/',          icon: '◈' },
    { label: 'Inventario',    path: '/inventario', icon: '▤' },
    { label: 'Compras',       path: '/compras',    icon: '▦' },
    { label: 'Cierre diario', path: '/cierre',     icon: '◷' },
    { label: 'Gastos',        path: '/gastos',     icon: '▾' },
    { label: 'Fiados',        path: '/fiados',     icon: '☷' },
    { label: 'Alertas',       path: '/alertas',    icon: '⚑', badge: () => this.alertCount() },
    { label: 'NRUS',          path: '/nrus',       icon: '₲' },
    { label: 'Reportes',      path: '/reportes',   icon: '⬇' },
    { label: 'Actividad',     path: '/timeline',   icon: '◎' },
    { label: 'Configuración', path: '/configuracion', icon: '⚙' }
  ];

  ngOnInit(): void {
    this.settingsService.load().subscribe({ error: () => {} });
    this.alertsService.list().subscribe({
      next: (a) => this.alertCount.set(a.length),
      error: () => {}
    });
    this.updateService.startPolling();
  }

  protected toggleMobile(): void {
    this.mobileOpen.update((v) => !v);
  }

  protected closeMobile(): void {
    this.mobileOpen.set(false);
  }

  protected logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }
}
