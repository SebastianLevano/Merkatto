import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, of } from 'rxjs';

interface AppInfo {
  version: string;
  updateAvailable: boolean;
  updateVersion?: string;
}

@Injectable({ providedIn: 'root' })
export class UpdateService {
  private http = inject(HttpClient);

  readonly updateAvailable = signal(false);
  readonly updateVersion   = signal<string | undefined>(undefined);
  readonly currentVersion  = signal<string>('');

  /** Call once after login. Polls every 30 min. */
  startPolling(): void {
    this.check();
    setInterval(() => this.check(), 30 * 60 * 1000);
  }

  private check(): void {
    this.http.get<AppInfo>('/api/v1/app/info')
      .pipe(catchError(() => of(null)))
      .subscribe(info => {
        if (!info) return;
        this.currentVersion.set(info.version);
        this.updateAvailable.set(info.updateAvailable);
        this.updateVersion.set(info.updateVersion);
      });
  }
}
