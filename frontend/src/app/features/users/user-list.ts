import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { UsersService } from './users.service';
import { ROLE_LABEL, Role, UserItem } from './user.models';

@Component({
  selector: 'app-user-list',
  imports: [FormsModule, DatePipe],
  templateUrl: './user-list.html'
})
export class UserList {
  private readonly service = inject(UsersService);
  private readonly auth = inject(AuthService);

  protected readonly ROLE_LABEL = ROLE_LABEL;
  protected readonly currentUserId = this.auth.user()?.id;

  protected readonly loading = signal(true);
  protected readonly users = signal<UserItem[]>([]);

  // Create dialog
  protected readonly showCreate = signal(false);
  protected readonly creating = signal(false);
  protected readonly createError = signal<string | null>(null);
  protected createEmail = '';
  protected createFullName = '';
  protected createRole: Role = 2;
  protected createPassword = '';

  // Edit dialog
  protected readonly showEdit = signal(false);
  protected editingUser: UserItem | null = null;
  protected readonly saving = signal(false);
  protected readonly editError = signal<string | null>(null);
  protected editFullName = '';
  protected editRole: Role = 2;
  protected editIsActive = true;

  // Reset password dialog
  protected readonly showReset = signal(false);
  protected resetUser: UserItem | null = null;
  protected readonly resetting = signal(false);
  protected readonly resetError = signal<string | null>(null);
  protected resetPassword = '';

  // Deactivate confirm
  protected readonly toDeactivate = signal<UserItem | null>(null);
  protected readonly deactivating = signal(false);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  // --- Create ---
  protected openCreate(): void {
    this.createEmail = '';
    this.createFullName = '';
    this.createRole = 2;
    this.createPassword = '';
    this.createError.set(null);
    this.showCreate.set(true);
  }

  protected submitCreate(): void {
    if (this.creating()) return;
    this.createError.set(null);
    if (!this.createEmail || !this.createFullName || !this.createPassword) {
      this.createError.set('Completá todos los campos.');
      return;
    }
    if (this.createPassword.length < 8) {
      this.createError.set('La contraseña debe tener al menos 8 caracteres.');
      return;
    }
    this.creating.set(true);
    this.service
      .create({
        email: this.createEmail.trim(),
        fullName: this.createFullName.trim(),
        role: this.createRole,
        password: this.createPassword
      })
      .subscribe({
        next: () => {
          this.showCreate.set(false);
          this.creating.set(false);
          this.load();
        },
        error: (err) => {
          this.createError.set(err?.error?.detail ?? 'No se pudo crear el usuario.');
          this.creating.set(false);
        }
      });
  }

  // --- Edit ---
  protected openEdit(user: UserItem): void {
    this.editingUser = user;
    this.editFullName = user.fullName;
    this.editRole = user.role;
    this.editIsActive = user.isActive;
    this.editError.set(null);
    this.showEdit.set(true);
  }

  protected submitEdit(): void {
    if (!this.editingUser || this.saving()) return;
    this.editError.set(null);
    if (!this.editFullName.trim()) {
      this.editError.set('El nombre no puede estar vacío.');
      return;
    }
    this.saving.set(true);
    this.service
      .update(this.editingUser.id, {
        fullName: this.editFullName.trim(),
        role: this.editRole,
        isActive: this.editIsActive
      })
      .subscribe({
        next: () => {
          this.showEdit.set(false);
          this.saving.set(false);
          this.load();
        },
        error: (err) => {
          this.editError.set(err?.error?.detail ?? 'No se pudo guardar.');
          this.saving.set(false);
        }
      });
  }

  // --- Reset password ---
  protected openReset(user: UserItem): void {
    this.resetUser = user;
    this.resetPassword = '';
    this.resetError.set(null);
    this.showReset.set(true);
  }

  protected submitReset(): void {
    if (!this.resetUser || this.resetting()) return;
    this.resetError.set(null);
    if (this.resetPassword.length < 8) {
      this.resetError.set('La contraseña debe tener al menos 8 caracteres.');
      return;
    }
    this.resetting.set(true);
    this.service.resetPassword(this.resetUser.id, this.resetPassword).subscribe({
      next: () => {
        this.showReset.set(false);
        this.resetting.set(false);
        this.load();
      },
      error: (err) => {
        this.resetError.set(err?.error?.detail ?? 'No se pudo resetear la contraseña.');
        this.resetting.set(false);
      }
    });
  }

  // --- Deactivate ---
  protected confirmDeactivate(user: UserItem): void {
    this.toDeactivate.set(user);
  }

  protected executeDeactivate(): void {
    const user = this.toDeactivate();
    if (!user || this.deactivating()) return;
    this.deactivating.set(true);
    this.service.deactivate(user.id).subscribe({
      next: () => {
        this.toDeactivate.set(null);
        this.deactivating.set(false);
        this.load();
      },
      error: (err) => {
        this.toDeactivate.set(null);
        this.deactivating.set(false);
        alert(err?.error?.detail ?? 'No se pudo desactivar el usuario.');
      }
    });
  }
}
