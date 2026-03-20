import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { Subscription } from 'rxjs';
import { PipelineRunDto } from '../models/api.models';
import { PollingService } from '../services/polling.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule, MatButtonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit, OnDestroy {
  runs: PipelineRunDto[] = [];
  loadError = false;
  displayedColumns = ['startedAt', 'duration', 'status', 'jobs', 'actions'];
  private sub?: Subscription;

  constructor(private polling: PollingService, private router: Router) {}

  ngOnInit() {
    this.sub = this.polling.getRuns$().subscribe({
      next: runs => (this.runs = runs),
      error: () => (this.loadError = true),
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }

  duration(run: PipelineRunDto): string {
    if (!run.finishedAt) return '—';
    const ms = new Date(run.finishedAt).getTime() - new Date(run.startedAt).getTime();
    const s = Math.floor(ms / 1000);
    return s < 60 ? `${s}s` : `${Math.floor(s / 60)}m ${s % 60}s`;
  }

  jobs(run: PipelineRunDto): string {
    return [...new Set(run.exportLogs.map(l => l.appId))].join(' ');
  }

  viewRun(id: number) {
    this.router.navigate(['/runs', id]);
  }

  get lastRun(): PipelineRunDto | undefined {
    return this.runs[0];
  }

  get lastFailed(): PipelineRunDto | undefined {
    return this.runs.find(r => r.status === 'Failed' || r.status === 'PartialFailure');
  }

  statusClass(status: string): string {
    return 'status-' + status.toLowerCase();
  }
}
