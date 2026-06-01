import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Operational routes (dashboard, inventory, sales, etc.). The system Administrator manages users
 * only, so they are bounced to the users screen; the bodega Encargado is allowed through.
 */
export const operatorGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const resolve = () =>
    auth.isAdmin() ? router.createUrlTree(['/configuracion/usuarios']) : true;

  if (auth.isAuthenticated()) {
    return resolve();
  }

  return auth.refresh().pipe(
    map(() => resolve()),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};

/** Admin-only routes (user management). Non-admins are sent back to the operational home. */
export const adminOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const resolve = () => (auth.isAdmin() ? true : router.createUrlTree(['/']));

  if (auth.isAuthenticated()) {
    return resolve();
  }

  return auth.refresh().pipe(
    map(() => resolve()),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};
