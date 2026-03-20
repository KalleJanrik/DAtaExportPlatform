import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, EMPTY, interval, Observable, Subscription } from 'rxjs';
import { switchMap, takeWhile, catchError, tap } from 'rxjs/operators';
import { PipelineRunDto } from '../models/api.models';

const SILENT_HEADERS = new HttpHeaders({ 'X-Silent-Error': 'true' });

@Injectable({ providedIn: 'root' })
export class PollingService {
  readonly isPolling$ = new BehaviorSubject<boolean>(false);

  constructor(private http: HttpClient) {}

  /**
   * Returns an Observable that:
   * 1. Immediately fetches /api/runs (using normal error handling — NOT silent).
   * 2. If any run is Running, starts polling every 5 seconds (silent errors).
   * 3. Stops when no run is Running.
   * Subscribe in ngOnInit, unsubscribe in ngOnDestroy.
   */
  getRuns$(): Observable<PipelineRunDto[]> {
    return new Observable<PipelineRunDto[]>(subscriber => {
      this.isPolling$.next(false);
      let innerSub: Subscription | undefined;

      // Initial fetch — normal error handling (NOT silent)
      const initialSub = this.http.get<PipelineRunDto[]>('/api/runs').subscribe({
        next: runs => {
          subscriber.next(runs);
          const hasRunning = runs.some(r => r.status === 'Running');
          if (!hasRunning) {
            subscriber.complete();
            return;
          }

          this.isPolling$.next(true);

          innerSub = interval(5000)
            .pipe(
              switchMap(() =>
                this.http.get<PipelineRunDto[]>('/api/runs', { headers: SILENT_HEADERS }).pipe(
                  catchError(() => EMPTY)
                )
              ),
              tap(runs => subscriber.next(runs)),
              takeWhile(runs => runs.some(r => r.status === 'Running'))
            )
            .subscribe({
              complete: () => {
                this.isPolling$.next(false);
                subscriber.complete();
              },
            });
        },
        error: err => subscriber.error(err),
      });

      return () => {
        initialSub.unsubscribe();
        innerSub?.unsubscribe();
        this.isPolling$.next(false);
      };
    });
  }
}
