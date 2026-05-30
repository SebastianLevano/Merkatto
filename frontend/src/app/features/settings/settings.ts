import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { SettingsService } from './settings.service';

@Component({
  selector: 'app-settings',
  imports: [FormsModule],
  templateUrl: './settings.html'
})
export class Settings implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly auth = inject(AuthService);

  protected readonly isAdmin = this.auth.isAdmin;

  // Business name section
  protected businessName = '';
  protected savingName = signal(false);
  protected nameSuccess = signal(false);
  protected nameError = signal<string | null>(null);

  // Change password section
  protected currentPassword = '';
  protected newPassword = '';
  protected confirmPassword = '';
  protected savingPassword = signal(false);
  protected passwordSuccess = signal(false);
  protected passwordError = signal<string | null>(null);

  ngOnInit(): void {
    this.businessName = this.settingsService.businessName();
    if (!this.businessName) {
      this.settingsService.load().subscribe({ next: (s) => (this.businessName = s.businessName) });
    }
  }

  protected saveName(): void {
    if (!this.businessName.trim() || this.savingName()) return;
    this.savingName.set(true);
    this.nameError.set(null);
    this.nameSuccess.set(false);
    this.settingsService.updateBusinessName(this.businessName.trim()).subscribe({
      next: () => {
        this.savingName.set(false);
        this.nameSuccess.set(true);
        setTimeout(() => this.nameSuccess.set(false), 3000);
      },
      error: (err) => {
        this.nameError.set(err?.error?.detail ?? 'No se pudo guardar.');
        this.savingName.set(false);
      }
    });
  }

  protected savePassword(): void {
    if (this.savingPassword()) return;
    this.passwordError.set(null);
    this.passwordSuccess.set(false);

    if (!this.currentPassword || !this.newPassword) {
      this.passwordError.set('Completá todos los campos.');
      return;
    }
    if (this.newPassword.length < 8) {
      this.passwordError.set('La nueva contraseña debe tener al menos 8 caracteres.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError.set('Las contraseñas no coinciden.');
      return;
    }

    this.savingPassword.set(true);
    this.settingsService.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.savingPassword.set(false);
        this.passwordSuccess.set(true);
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        setTimeout(() => this.passwordSuccess.set(false), 4000);
      },
      error: (err) => {
        this.passwordError.set(err?.error?.detail ?? 'La contraseña actual es incorrecta.');
        this.savingPassword.set(false);
      }
    });
  }
}
