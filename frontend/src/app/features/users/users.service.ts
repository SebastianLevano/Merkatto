import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateUserRequest, UpdateUserRequest, UserItem } from './user.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/users`;

  list(): Observable<UserItem[]> {
    return this.http.get<UserItem[]>(this.base);
  }

  create(req: CreateUserRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.base, req);
  }

  update(id: number, req: UpdateUserRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, req);
  }

  resetPassword(id: number, password: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reset-password`, { password });
  }

  deactivate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
