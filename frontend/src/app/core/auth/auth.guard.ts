import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Allows navigation when authenticated; otherwise attempts a refresh, then redirects to login.
 * A user flagged `mustChangePassword` is forced onto the change-password screen first.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const resolve = () =>
    auth.mustChangePassword() ? router.createUrlTree(['/cambiar-contrasena']) : true;

  if (auth.isAuthenticated()) {
    return resolve();
  }

  return auth.refresh().pipe(
    map(() => resolve()),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};

/** Guards the forced change-password screen: requires auth, and bounces out if no change is pending. */
export const mustChangePasswordGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const resolve = () => (auth.mustChangePassword() ? true : router.createUrlTree(['/']));

  if (auth.isAuthenticated()) {
    return resolve();
  }

  return auth.refresh().pipe(
    map(() => resolve()),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};
