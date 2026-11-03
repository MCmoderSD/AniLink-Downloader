import { type Routes } from '@angular/router';
import { DownloadsPageComponent } from '../pages/downloads-page/downloads-page.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'downloads'
  },
  // The page that opens first is part of the main bundle. Loaded lazily it would sit behind two more requests that
  // follow one another, and on a phone connection each of them costs a round trip before anything is on the screen.
  {
    path: 'downloads',
    component: DownloadsPageComponent
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