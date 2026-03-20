import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PipelineService {
  constructor(private http: HttpClient) {}

  trigger(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/pipeline/trigger', {});
  }
}
