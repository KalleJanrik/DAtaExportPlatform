import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { PipelineRunDto } from '../models/api.models';
import { RunsService } from '../services/runs.service';

@Component({
  selector: 'app-run-detail',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule],
  templateUrl: './run-detail.component.html',
})
export class RunDetailComponent implements OnInit {
  run?: PipelineRunDto;
  notFound = false;
  displayedColumns = ['appId', 'fileName', 'recordCount', 'fileSizeBytes', 'status', 'exportedAt', 'errorMessage'];

  constructor(private route: ActivatedRoute, private runsService: RunsService) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.runsService.getRun(id).subscribe({
      next: run => (this.run = run),
      error: err => {
        if (err?.status === 404) this.notFound = true;
      },
    });
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  statusClass(status: string): string {
    return 'status-' + status.toLowerCase();
  }
}
