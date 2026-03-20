import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, interval, Observable, of } from 'rxjs';
import { switchMap, takeWhile, catchError } from 'rxjs/operators';
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

      // Initial fetch — normal error handling (NOT silent)
      this.http.get<PipelineRunDto[]>('/api/runs').subscribe({
        next: runs => {
          subscriber.next(runs);
          const hasRunning = runs.some(r => r.status === 'Running');
          if (!hasRunning) {
            subscriber.complete();
            return;
          }

          this.isPolling$.next(true);

          interval(5000)
            .pipe(
              switchMap(() =>
                this.http.get<PipelineRunDto[]>('/api/runs', { headers: SILENT_HEADERS }).pipe(
                  catchError(() => of([] as PipelineRunDto[]))
                )
              ),
              takeWhile(runs => {
                subscriber.next(runs);
                return runs.some(r => r.status === 'Running');
              }, true)
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
    });
  }
}
