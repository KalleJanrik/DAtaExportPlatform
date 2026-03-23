import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface JobInfo {
  appId: string;
}

@Injectable({ providedIn: 'root' })
export class PipelineService {
  constructor(private http: HttpClient) {}

  getJobs(): Observable<JobInfo[]> {
    return this.http.get<JobInfo[]>('/api/pipeline/jobs');
  }

  trigger(jobs: string[]): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/pipeline/trigger', { jobs });
  }
}
