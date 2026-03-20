import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PipelineService } from '../services/pipeline.service';

@Component({
  selector: 'app-trigger',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './trigger.component.html',
})
export class TriggerComponent {
  running = false;

  constructor(private pipeline: PipelineService, private snackBar: MatSnackBar) {}

  trigger() {
    this.running = true;
    this.pipeline.trigger().subscribe({
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
