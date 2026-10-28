import { Component, computed, inject, input, InputSignal, signal, Signal, WritableSignal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Item, Job, jobSubtitle, STATUS_LABELS, STATUS_TONES, Tone } from '../../data/download';
import { formatBytes, formatEta, formatSpeed } from '../../data/format';
import { DownloadService } from '../../services/download.service';
import { DownloadSyncService } from '../../services/download-sync.service';
import { errorMessage } from '../../services/http-error';
import { NotificationService } from '../../services/notification.service';
import { ActivityLogComponent } from '../activity-log-component/activity-log.component';
import { ConfirmActionDialogComponent } from '../confirm-action-dialog-component/confirm-action-dialog.component';
import { ItemRowComponent } from '../item-row-component/item-row.component';
import { PasswordPromptDialogComponent } from '../password-prompt-dialog-component/password-prompt-dialog.component';
import { ProgressBarComponent } from '../progress-bar-component/progress-bar.component';

let nextCardId: number = 0;

@Component({
  selector: 'app-job-card',
  templateUrl: './job-card.component.html',
  styleUrls: ['./job-card.component.scss'],
  imports: [MatButtonModule, MatIconModule, ProgressBarComponent, ItemRowComponent, ActivityLogComponent]
})
export class JobCardComponent {

  private readonly downloads: DownloadService = inject(DownloadService);
  private readonly sync: DownloadSyncService = inject(DownloadSyncService);
  private readonly notifications: NotificationService = inject(NotificationService);
  private readonly dialog: MatDialog = inject(MatDialog);

  private readonly detailsShown: WritableSignal<boolean> = signal(false);
  private readonly working: WritableSignal<boolean> = signal(false);

  readonly job: InputSignal<Job> = input.required<Job>();

  protected readonly titleId: string = `job-title-${nextCardId++}`;

  protected readonly detailsOpen: Signal<boolean> = this.detailsShown.asReadonly();
  protected readonly busy: Signal<boolean> = this.working.asReadonly();

  protected readonly subtitle: Signal<string> = computed((): string => jobSubtitle(this.job()));
  protected readonly statusLabel: Signal<string> = computed((): string => STATUS_LABELS[this.job().status]);
  protected readonly statusTone: Signal<Tone> = computed((): Tone => STATUS_TONES[this.job().status]);

  protected readonly running: Signal<boolean> = computed((): boolean => this.job().status === 'Active');

  protected readonly canPause: Signal<boolean> = computed((): boolean => this.job().status === 'Active');

  protected readonly canResume: Signal<boolean> = computed((): boolean => this.job().status === 'Paused');

  protected readonly canCancel: Signal<boolean> = computed((): boolean =>
    this.job().status === 'Active' || this.job().status === 'Paused' || this.job().status === 'PasswordRequired');

  protected readonly canRetry: Signal<boolean> = computed((): boolean =>
    this.job().items.some((item: Item): boolean => item.stage === 'Failed' || item.stage === 'Cancelled'));

  protected readonly waiting: Signal<Item[]> = computed((): Item[] =>
    this.job().items.filter((item: Item): boolean => item.stage === 'PasswordRequired'));

  protected readonly linksTone: Signal<Tone> = computed((): Tone =>
    this.job().links.total > 0 && this.job().links.done >= this.job().links.total ? 'success' : this.job().status === 'Failed' ? 'danger' : 'info');

  protected readonly downloadTone: Signal<Tone> = computed((): Tone => {
    const status: Job['status'] = this.job().status;
    return status === 'Completed' ? 'success' : status === 'Failed' ? 'danger' : status === 'Cancelled' ? 'neutral' : status === 'Paused' ? 'warning' : 'info';
  });

  protected readonly linksDetail: Signal<string> = computed((): string => `${this.job().links.done} of ${this.job().links.total} links`);

  protected readonly downloadDetail: Signal<string> = computed((): string => {
    const progress: Job['download'] = this.job().download;
    if (progress.total <= 0) return 'waiting for the first links';

    const parts: string[] = [`${formatBytes(progress.done)} of ${formatBytes(progress.total)}`];

    if (this.running() && this.job().speedBytesPerSecond > 0) {
      parts.push(formatSpeed(this.job().speedBytesPerSecond));

      const eta: string | null = formatEta(progress.total - progress.done, this.job().speedBytesPerSecond);
      if (eta) parts.push(eta);
    }

    return parts.join(' · ');
  });

  protected readonly summary: Signal<string> = computed((): string => {
    const job: Job = this.job();
    const noun: string = job.kind === 'Movie' ? 'file' : job.items.length === 1 ? 'episode' : 'episodes';
    const failed: number = job.items.filter((item: Item): boolean => item.stage === 'Failed').length;

    return `${job.itemsCompleted} of ${job.items.length} ${noun} done${failed > 0 ? ` · ${failed} failed` : ''}`;
  });

  protected toggleDetails(): void {
    this.detailsShown.update((value: boolean): boolean => !value);
  }

  protected async cancel(): Promise<void> {
    const confirmed: boolean = await ConfirmActionDialogComponent.confirm(this.dialog, {
      title: 'Cancel this download?',
      message: `${this.job().name} stops here. What has been downloaded stays on disk, and Retry picks it up again.`,
      confirmLabel: 'Cancel download',
      cancelLabel: 'Keep running'
    });

    if (confirmed) await this.act((): Promise<Job> => this.downloads.cancel(this.job().id), 'The download could not be canceled.');
  }

  protected async pause(): Promise<void> {
    await this.act((): Promise<Job> => this.downloads.pause(this.job().id), 'The download could not be paused.');
  }

  protected async resume(): Promise<void> {
    await this.act((): Promise<Job> => this.downloads.resume(this.job().id), 'The download could not be resumed.');
  }

  protected async retry(): Promise<void> {
    await this.act((): Promise<Job> => this.downloads.retry(this.job().id), 'Nothing could be retried.');
  }

  protected async enterPassword(): Promise<void> {
    const password: string | undefined = await PasswordPromptDialogComponent.ask(this.dialog, {
      jobName: this.job().name,
      waiting: this.waiting().map((item: Item): string => item.label)
    });

    if (password) await this.act((): Promise<Job> => this.downloads.submitPassword(this.job().id, password), 'The password could not be sent.');
  }

  protected async remove(): Promise<void> {
    const confirmed: boolean = await ConfirmActionDialogComponent.confirm(this.dialog, {
      title: 'Remove from the list?',
      message: `${this.job().name} disappears from this list${this.running() ? ' and stops downloading' : ''}. Files on disk are not deleted.`,
      confirmLabel: 'Remove'
    });

    if (!confirmed) return;

    this.working.set(true);

    try {
      await this.downloads.remove(this.job().id);
    } catch (error: unknown) {
      this.notifications.failure(errorMessage(error, 'The download could not be removed.'));
    } finally {
      this.working.set(false);
    }
  }

  private async act(call: () => Promise<Job>, failure: string): Promise<void> {
    this.working.set(true);

    try {
      this.sync.apply(await call());
    } catch (error: unknown) {
      this.notifications.failure(errorMessage(error, failure));
    } finally {
      this.working.set(false);
    }
  }
}