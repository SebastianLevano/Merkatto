import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { switchMap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { SettingsService } from '../settings/settings.service';

/**
 * Full-screen, mandatory password change shown on first login (or after an admin reset).
 * On success it refreshes the token so the cleared `mustChangePassword` flag takes effect,
 * then sends the user into the app.
 */
@Component({
  selector: 'app-force-password-change',
  imports: [FormsModule],
  templateUrl: './force-password-change.html'
})
export class ForcePasswordChange {
  private readonly auth = inject(AuthService);
  private readonly settings = inject(SettingsService);
  private readonly router = inject(Router);

  protected readonly email = this.auth.user()?.email ?? '';
  protected currentPassword = '';
  protected newPassword = '';
  protected confirmPassword = '';

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.saving()) return;
    this.error.set(null);

    if (!this.currentPassword || !this.newPassword) {
      this.error.set('Completá todos los campos.');
      return;
    }
    if (this.newPassword.length < 8) {
      this.error.set('La nueva contraseña debe tener al menos 8 caracteres.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('Las contraseñas no coinciden.');
      return;
    }

    this.saving.set(true);
    this.settings
      .changePassword(this.currentPassword, this.newPassword)
      .pipe(switchMap(() => this.auth.refresh()))
      .subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => {
          this.error.set(err?.error?.detail ?? 'La contraseña actual es incorrecta.');
          this.saving.set(false);
        }
      });
  }

  protected logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }
}
