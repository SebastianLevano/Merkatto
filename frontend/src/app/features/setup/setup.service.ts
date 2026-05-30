import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface SetupStatus {
  needsSetup: boolean;
  defaultBusinessName?: string;
  defaultAdminEmail?: string;
}

export interface InitializeRequest {
  businessName: string;
  adminName: string;
  adminEmail: string;
  adminPassword: string;
}

@Injectable({ providedIn: 'root' })
export class SetupService {
  private http = inject(HttpClient);
  private _status: SetupStatus | null = null;

  async getStatus(): Promise<SetupStatus> {
    if (!this._status) {
      this._status = await firstValueFrom(
        this.http.get<SetupStatus>('/api/v1/setup/status')
      );
    }
    return this._status;
  }

  async initialize(req: InitializeRequest): Promise<void> {
    await firstValueFrom(this.http.post('/api/v1/setup/initialize', req));
    this._status = { needsSetup: false };
  }

  markDone(): void {
    this._status = { needsSetup: false };
  }
}
