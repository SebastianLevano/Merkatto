import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SetupService } from './setup.service';

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="min-h-screen bg-slate-50 flex items-center justify-center p-4">
      <div class="w-full max-w-md">

        <!-- Logo / Header -->
        <div class="text-center mb-8">
          <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-violet-700 mb-4">
            <span class="text-white text-2xl font-bold">M</span>
          </div>
          <h1 class="text-2xl font-bold text-slate-900">Bienvenido a Merkatto</h1>
          <p class="text-slate-500 mt-1 text-sm">Configuración inicial de tu bodega</p>
        </div>

        <!-- Card -->
        <div class="bg-white rounded-2xl shadow-sm border border-slate-200 p-8">

          @if (error()) {
            <div class="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
              {{ error() }}
            </div>
          }

          <form (ngSubmit)="submit()" #form="ngForm" class="space-y-4">

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">
                Nombre de la bodega
              </label>
              <input
                type="text"
                name="businessName"
                [(ngModel)]="businessName"
                required
                maxlength="100"
                placeholder="Bodega Martita"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500 focus:border-transparent"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">
                Tu nombre
              </label>
              <input
                type="text"
                name="adminName"
                [(ngModel)]="adminName"
                required
                maxlength="100"
                placeholder="Administrador"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500 focus:border-transparent"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">
                Email del administrador
              </label>
              <input
                type="email"
                name="adminEmail"
                [(ngModel)]="adminEmail"
                required
                maxlength="200"
                placeholder="tu@email.com"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500 focus:border-transparent"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">
                Contraseña temporal
              </label>
              <input
                type="password"
                name="adminPassword"
                [(ngModel)]="adminPassword"
                required
                minlength="8"
                maxlength="100"
                placeholder="Mínimo 8 caracteres"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500 focus:border-transparent"
              />
              <p class="mt-1 text-xs text-slate-400">
                Te pedirá cambiarla al primer ingreso.
              </p>
            </div>

            <button
              type="submit"
              [disabled]="loading() || form.invalid"
              class="w-full mt-2 rounded-lg bg-violet-700 px-4 py-2.5 text-sm font-semibold text-white
                     hover:bg-violet-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {{ loading() ? 'Configurando…' : 'Inicializar sistema' }}
            </button>

          </form>
        </div>

        <p class="text-center text-xs text-slate-400 mt-6">
          Esta pantalla solo aparece una vez.
        </p>
      </div>
    </div>
  `
})
export class Setup implements OnInit {
  private setup = inject(SetupService);
  private router = inject(Router);

  businessName = '';
  adminName = 'Administrador';
  adminEmail = '';
  adminPassword = '';

  loading = signal(false);
  error = signal('');

  async ngOnInit() {
    const status = await this.setup.getStatus();
    if (!status.needsSetup) {
      this.router.navigate(['/login']);
      return;
    }
    if (status.defaultBusinessName) this.businessName = status.defaultBusinessName;
    if (status.defaultAdminEmail)   this.adminEmail   = status.defaultAdminEmail;
  }

  async submit() {
    this.error.set('');
    this.loading.set(true);
    try {
      await this.setup.initialize({
        businessName: this.businessName,
        adminName: this.adminName,
        adminEmail: this.adminEmail,
        adminPassword: this.adminPassword,
      });
      this.router.navigate(['/login']);
    } catch (e: any) {
      this.error.set(e?.error?.detail ?? e?.error?.title ?? 'Error al inicializar. Intentá de nuevo.');
    } finally {
      this.loading.set(false);
    }
  }
}
