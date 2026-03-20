import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly username$ = this.http
    .get<{ username: string }>('/api/auth/whoami')
    .pipe(map(r => r.username), shareReplay(1));

  getUsername(): Observable<string> {
    return this.username$;
  }
}
