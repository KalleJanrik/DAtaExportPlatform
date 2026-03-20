import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ArchiveSummaryDto, ArchiveJobDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ArchiveService {
  constructor(private http: HttpClient) {}

  getSummaries(): Observable<ArchiveSummaryDto[]> {
    return this.http.get<ArchiveSummaryDto[]>('/api/archive');
  }

  getJob(appId: string): Observable<ArchiveJobDto> {
    return this.http.get<ArchiveJobDto>(`/api/archive/${appId}`);
  }

  buildDownloadUrl(appId: string, day: string, fileName: string): string {
    return `/api/archive/${encodeURIComponent(appId)}/${encodeURIComponent(day)}/${encodeURIComponent(fileName)}`;
  }
}
