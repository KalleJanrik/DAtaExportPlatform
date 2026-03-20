import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EMPTY, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  // Polling requests are tagged — strip client-internal header before forwarding,
  // then complete silently so no error propagates to the component
  if (req.headers.has('X-Silent-Error')) {
    const cleaned = req.clone({ headers: req.headers.delete('X-Silent-Error') });
    return next(cleaned).pipe(catchError(() => EMPTY));
  }

  return next(req).pipe(
    catchError(err => {
      const status = err?.status;

      if (status === 401) {
        snackBar.open('You are not authorized.', 'Close', { duration: 5000 });
      } else if (!status) {
        snackBar.open('Network error. Please check your connection.', 'Close', { duration: 5000 });
      } else if (status >= 500) {
        const detail = err?.error?.detail ?? err?.message ?? 'An error occurred.';
        snackBar.open(detail, 'Close', { duration: 5000 });
      }
      // 403 and 404: re-throw only — component handles inline

      return throwError(() => err);
    })
  );
};
