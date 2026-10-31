import { type Routes } from '@angular/router';

// No route sets a title on purpose: the tab always reads what index.html says, whichever page is open.
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'downloads'
  },
  {
    path: 'downloads',
    loadComponent: () => import('../pages/downloads-page/downloads-page.component').then((module): typeof module.DownloadsPageComponent => module.DownloadsPageComponent)
  },
  {
    path: 'add-download',
    loadComponent: () => import('../pages/new-download-page/new-download-page.component').then((module): typeof module.NewDownloadPageComponent => module.NewDownloadPageComponent)
  },
  {
    path: '**',
    loadComponent: () => import('../pages/error-page/error-page.component').then((module): typeof module.ErrorPageComponent => module.ErrorPageComponent)
  }
];