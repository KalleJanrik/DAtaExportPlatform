import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  // Polling requests are tagged — swallow all errors silently
  if (req.headers.has('X-Silent-Error')) {
    return next(req).pipe(catchError(() => throwError(() => null)));
  }

  return next(req).pipe(
    catchError(err => {
      const status = err?.status;

      if (status === 401) {
        snackBar.open('You are not authorized.', 'Close', { duration: 5000 });
      } else if (status >= 500) {
        const detail = err?.error?.detail ?? err?.message ?? 'An error occurred.';
        snackBar.open(detail, 'Close', { duration: 5000 });
      }
      // 403 and 404: re-throw only — component handles inline

      return throwError(() => err);
    })
  );
};
