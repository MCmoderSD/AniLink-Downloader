import { NgOptimizedImage } from '@angular/common';
import { Component, inject, Signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DownloadSyncService } from '../../services/download-sync.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  imports: [MatButtonModule, MatIconModule, NgOptimizedImage, RouterLink, RouterLinkActive]
})
export class NavbarComponent {

  private readonly sync: DownloadSyncService = inject(DownloadSyncService);

  protected readonly unreachable: Signal<boolean> = this.sync.unreachable;
}