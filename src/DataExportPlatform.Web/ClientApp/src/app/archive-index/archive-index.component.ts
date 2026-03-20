import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { ArchiveSummaryDto } from '../models/api.models';
import { ArchiveService } from '../services/archive.service';

@Component({
  selector: 'app-archive-index',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule],
  templateUrl: './archive-index.component.html',
})
export class ArchiveIndexComponent implements OnInit {
  summaries: ArchiveSummaryDto[] = [];
  loadError = false;

  constructor(private archive: ArchiveService, private router: Router) {}

  ngOnInit() {
    this.archive.getSummaries().subscribe({
      next: s => (this.summaries = s),
      error: () => (this.loadError = true),
    });
  }

  viewJob(appId: string) {
    this.router.navigate(['/archive', appId]);
  }
}
