import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResult, LoginRequest, User } from './auth.models';

/**
 * Holds auth state with signals. The access token lives only in memory (never localStorage);
 * the refresh token is an httpOnly cookie the browser sends automatically to /auth/refresh.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/auth`;

  private readonly _accessToken = signal<string | null>(null);
  private readonly _user = signal<User | null>(null);

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly isAdmin = computed(() => this._user()?.role === 1);
  readonly mustChangePassword = computed(() => this._user()?.mustChangePassword === true);

  get accessToken(): string | null {
    return this._accessToken();
  }

  login(request: LoginRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${this.base}/login`, request, { withCredentials: true })
      .pipe(tap((result) => this.setSession(result)));
  }

  /** Exchanges the refresh cookie for a fresh access token (used on startup and on 401). */
  refresh(): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${this.base}/refresh`, {}, { withCredentials: true })
      .pipe(tap((result) => this.setSession(result)));
  }

  logout(): Observable<void> {
    return this.http
      .post<void>(`${this.base}/logout`, {}, { withCredentials: true })
      .pipe(tap(() => this.clearSession()));
  }

  private setSession(result: AuthResult): void {
    this._accessToken.set(result.accessToken);
    this._user.set(result.user);
  }

  clearSession(): void {
    this._accessToken.set(null);
    this._user.set(null);
  }
}
