import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from '../components/navbar-component/navbar.component';
import { NotificationsComponent } from '../components/notifications-component/notifications.component';
import { PageScrollBarComponent } from '../components/page-scroll-bar-component/page-scroll-bar.component';
import { DownloadSyncService } from '../services/download-sync.service';

@Component({
  imports: [RouterOutlet, NavbarComponent, NotificationsComponent, PageScrollBarComponent],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {

  constructor() {
    // Connected for the whole session, not per page: a finished episode is announced wherever the user is.
    inject(DownloadSyncService).connect();
  }
}
