import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PipelineRunDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class RunsService {
  constructor(private http: HttpClient) {}

  getRuns(): Observable<PipelineRunDto[]> {
    return this.http.get<PipelineRunDto[]>('/api/runs');
  }

  getRun(id: number): Observable<PipelineRunDto> {
    return this.http.get<PipelineRunDto>(`/api/runs/${id}`);
  }
}
