import { Component, inject, Signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DownloadSyncService } from '../../services/download-sync.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
  imports: [MatButtonModule, MatIconModule, RouterLink, RouterLinkActive],
})
export class NavbarComponent {

  private readonly sync: DownloadSyncService = inject(DownloadSyncService);

  protected readonly connected: Signal<boolean> = this.sync.connected;
  protected readonly loaded: Signal<boolean> = this.sync.loaded;
}
