import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { ArchiveJobDto } from '../models/api.models';
import { ArchiveService } from '../services/archive.service';

@Component({
  selector: 'app-archive-job',
  standalone: true,
  imports: [CommonModule, MatExpansionModule, MatListModule, MatIconModule],
  templateUrl: './archive-job.component.html',
})
export class ArchiveJobComponent implements OnInit {
  job?: ArchiveJobDto;
  accessDenied = false;
  notFound = false;

  constructor(private route: ActivatedRoute, private archive: ArchiveService) {}

  ngOnInit() {
    const appId = this.route.snapshot.paramMap.get('appId')!;
    this.archive.getJob(appId).subscribe({
      next: job => (this.job = job),
      error: err => {
        if (err?.status === 403) this.accessDenied = true;
        else if (err?.status === 404) this.notFound = true;
      },
    });
  }

  downloadUrl(appId: string, day: string, fileName: string): string {
    return this.archive.buildDownloadUrl(appId, day, fileName);
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
