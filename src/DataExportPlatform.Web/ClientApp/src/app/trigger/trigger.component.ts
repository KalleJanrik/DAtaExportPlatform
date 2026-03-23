import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { JobInfo, PipelineService } from '../services/pipeline.service';

@Component({
  selector: 'app-trigger',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './trigger.component.html',
})
export class TriggerComponent implements OnInit, OnDestroy {
  jobs: JobInfo[] = [];
  selectedMap: Record<string, boolean> = {};
  loadError = false;
  running = false;

  private jobsSub?: Subscription;
  private triggerSub?: Subscription;

  constructor(private pipeline: PipelineService, private snackBar: MatSnackBar) {}

  ngOnInit() {
    this.jobsSub = this.pipeline.getJobs().subscribe({
      next: jobs => {
        this.jobs = jobs;
        this.selectedMap = Object.fromEntries(jobs.map(j => [j.appId, true]));
      },
      error: () => {
        this.loadError = true;
      },
    });
  }

  ngOnDestroy() {
    this.jobsSub?.unsubscribe();
    this.triggerSub?.unsubscribe();
  }

  toggle(appId: string) {
    this.selectedMap = { ...this.selectedMap, [appId]: !this.selectedMap[appId] };
  }

  get selectedIds(): string[] {
    return Object.entries(this.selectedMap)
      .filter(([, v]) => v)
      .map(([k]) => k);
  }

  trigger() {
    this.running = true;
    this.triggerSub = this.pipeline.trigger(this.selectedIds).subscribe({
      next: res => {
        this.running = false;
        this.snackBar.open(res.message, 'Close', { duration: 5000 });
      },
      error: () => {
        this.running = false;
        // 5xx handled by global error interceptor
      },
    });
  }
}
