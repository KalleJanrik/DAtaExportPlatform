import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from './services/auth.service';
import { PollingService } from './services/polling.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe,
    MatSidenavModule, MatToolbarModule, MatListModule, MatDividerModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  username$!: Observable<string>;
  isPolling$!: Observable<boolean>;

  constructor(
    private auth: AuthService,
    public pollingService: PollingService,
  ) {}

  ngOnInit() {
    this.username$ = this.auth.getUsername();
    this.isPolling$ = this.pollingService.isPolling$;
  }
}
