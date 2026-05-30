import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SetupService } from '../../features/setup/setup.service';

export const setupGuard: CanActivateFn = async () => {
  const setup = inject(SetupService);
  const router = inject(Router);
  const status = await setup.getStatus();
  if (status.needsSetup) return router.createUrlTree(['/setup']);
  return true;
};
