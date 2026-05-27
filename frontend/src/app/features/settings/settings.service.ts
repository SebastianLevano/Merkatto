import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PublicSettings {
  businessName: string;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  readonly businessName = signal('');

  load(): Observable<PublicSettings> {
    return this.http.get<PublicSettings>(`${this.api}/settings`).pipe(
      tap((s) => this.businessName.set(s.businessName))
    );
  }

  updateBusinessName(name: string): Observable<void> {
    return this.http.put<void>(`${this.api}/settings/business-name`, { businessName: name }).pipe(
      tap(() => this.businessName.set(name))
    );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.api}/auth/change-password`, { currentPassword, newPassword });
  }
}
