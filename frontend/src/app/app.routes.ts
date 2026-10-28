import { type Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'downloads'
  },
  {
    path: 'downloads',
    loadComponent: () => import('../pages/downloads-page/downloads-page.component').then((module): typeof module.DownloadsPageComponent => module.DownloadsPageComponent),
    title: 'Downloads'
  },
  {
    path: 'add-download',
    loadComponent: () => import('../pages/new-download-page/new-download-page.component').then((module): typeof module.NewDownloadPageComponent => module.NewDownloadPageComponent),
    title: 'New download'
  },
  {
    path: '**',
    loadComponent: () => import('../pages/error-page/error-page.component').then((module): typeof module.ErrorPageComponent => module.ErrorPageComponent),
    title: 'Page not found'
  }
];