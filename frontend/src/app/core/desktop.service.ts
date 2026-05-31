import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DesktopService {
  private readonly http = inject(HttpClient);
  private _isDesktop: boolean | null = null;

  isDesktop(): Observable<boolean> {
    if (this._isDesktop !== null) return of(this._isDesktop);
    return this.http.get(`${environment.apiUrl}/app/mode`).pipe(
      map(() => true),
      catchError(() => of(false)),
      tap(v => (this._isDesktop = v)),
    );
  }
}
