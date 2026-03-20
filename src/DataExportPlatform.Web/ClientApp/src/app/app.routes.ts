import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'runs/:id', loadComponent: () => import('./run-detail/run-detail.component').then(m => m.RunDetailComponent) },
  { path: 'trigger', loadComponent: () => import('./trigger/trigger.component').then(m => m.TriggerComponent) },
  { path: 'archive', loadComponent: () => import('./archive-index/archive-index.component').then(m => m.ArchiveIndexComponent) },
  { path: 'archive/:appId', loadComponent: () => import('./archive-job/archive-job.component').then(m => m.ArchiveJobComponent) },
  { path: '**', redirectTo: '' },
];
