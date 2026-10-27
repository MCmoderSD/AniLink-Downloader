import { Component, computed, inject, Signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { JobCardComponent } from '../../components/job-card-component/job-card.component';
import { Job } from '../../data/download';
import { DownloadSyncService } from '../../services/download-sync.service';

@Component({
  selector: 'app-downloads-page',
  templateUrl: './downloads-page.component.html',
  styleUrl: './downloads-page.component.scss',
  imports: [MatButtonModule, MatIconModule, RouterLink, JobCardComponent],
})
export class DownloadsPageComponent {

  private readonly sync: DownloadSyncService = inject(DownloadSyncService);

  protected readonly jobs: Signal<Job[]> = this.sync.jobs;
  protected readonly loaded: Signal<boolean> = this.sync.loaded;
  protected readonly connected: Signal<boolean> = this.sync.connected;

  protected readonly running: Signal<number> = computed((): number =>
    this.jobs().filter((job: Job): boolean => job.status === 'Active').length);

  protected readonly subtitle: Signal<string> = computed((): string => {
    const total: number = this.jobs().length;
    if (total === 0) return 'Nothing queued.';

    return `${this.running()} running of ${total} ${total === 1 ? 'download' : 'downloads'}`;
  });
}
