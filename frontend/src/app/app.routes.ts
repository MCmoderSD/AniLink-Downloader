import { type Routes } from '@angular/router';
import { DownloadsPageComponent } from '../pages/downloads-page/downloads-page.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'downloads'
  },
  {
    path: 'downloads',
    component: DownloadsPageComponent
  },
  {
    path: 'add-download',
    loadComponent: () => import('../pages/new-download-page/new-download-page.component').then((module): typeof module.NewDownloadPageComponent => module.NewDownloadPageComponent)
  },
  {
    path: 'decode-dlc',
    loadComponent: () => import('../pages/decode-dlc-page/decode-dlc-page.component').then((module): typeof module.DecodeDlcPageComponent => module.DecodeDlcPageComponent)
  },
  {
    path: '**',
    loadComponent: () => import('../pages/error-page/error-page.component').then((module): typeof module.ErrorPageComponent => module.ErrorPageComponent)
  }
];