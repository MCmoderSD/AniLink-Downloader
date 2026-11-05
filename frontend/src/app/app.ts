import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FooterComponent } from '../components/footer-component/footer.component';
import { NavbarComponent } from '../components/navbar-component/navbar.component';
import { NotificationsComponent } from '../components/notifications-component/notifications.component';
import { PageScrollBarComponent } from '../components/page-scroll-bar-component/page-scroll-bar.component';
import { DownloadSyncService } from '../services/download-sync.service';

@Component({
  imports: [RouterOutlet, FooterComponent, NavbarComponent, NotificationsComponent, PageScrollBarComponent],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html'
})
export class App {

  constructor() {
    inject(DownloadSyncService).connect();
  }
}