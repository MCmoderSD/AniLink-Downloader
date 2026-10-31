import { Component, computed, inject, input, InputSignal, signal, Signal, WritableSignal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Item, Job, Part, PART_LABELS, PART_TONES, STAGE_LABELS, STAGE_TONES, Tone } from '../../data/download';
import { formatBytes } from '../../data/format';
import { DownloadService } from '../../services/download.service';
import { DownloadSyncService } from '../../services/download-sync.service';
import { errorMessage } from '../../services/http-error';
import { NotificationService } from '../../services/notification.service';
import { ConfirmActionDialogComponent } from '../confirm-action-dialog-component/confirm-action-dialog.component';
import { ProgressBarComponent } from '../progress-bar-component/progress-bar.component';

interface Bar {
  label: string;
  detail: string;
  fraction: number;
  tone: Tone;
  active: boolean;
}

@Component({
  selector: 'app-item-row',
  templateUrl: './item-row.component.html',
  styleUrls: ['./item-row.component.scss'],
  imports: [MatButtonModule, MatIconModule, ProgressBarComponent]
})
export class ItemRowComponent {

  private readonly downloads: DownloadService = inject(DownloadService);
  private readonly sync: DownloadSyncService = inject(DownloadSyncService);
  private readonly notifications: NotificationService = inject(NotificationService);
  private readonly dialog: MatDialog = inject(MatDialog);

  readonly item: InputSignal<Item> = input.required<Item>();
  readonly jobId: InputSignal<string> = input.required<string>();

  // A download of a single item is controlled by the buttons of its card, so the row only offers its own with several
  readonly manageable: InputSignal<boolean> = input(false);

  private readonly partsShown: WritableSignal<boolean> = signal(false);
  private readonly working: WritableSignal<boolean> = signal(false);

  protected readonly partsOpen: Signal<boolean> = this.partsShown.asReadonly();
  protected readonly busy: Signal<boolean> = this.working.asReadonly();

  protected readonly canPause: Signal<boolean> = computed((): boolean => {
    const stage: Item['stage'] = this.item().stage;
    return stage === 'ResolvingLinks' || stage === 'Downloading' || stage === 'Extracting';
  });

  protected readonly canResume: Signal<boolean> = computed((): boolean => this.item().stage === 'Paused');

  protected readonly canRetry: Signal<boolean> = computed((): boolean => {
    const stage: Item['stage'] = this.item().stage;
    return stage === 'Failed' || stage === 'Cancelled';
  });

  protected readonly canUnpack: Signal<boolean> = computed((): boolean => this.item().stage === 'Downloaded');

  protected readonly canCancel: Signal<boolean> = computed((): boolean => {
    const stage: Item['stage'] = this.item().stage;
    return stage !== 'Completed' && stage !== 'Failed' && stage !== 'Cancelled' && stage !== 'Downloaded';
  });

  protected readonly stageLabel: Signal<string> = computed((): string => STAGE_LABELS[this.item().stage]);
  protected readonly stageTone: Signal<Tone> = computed((): Tone => STAGE_TONES[this.item().stage]);

  protected readonly bar: Signal<Bar> = computed((): Bar => {
    const item: Item = this.item();

    switch (item.stage) {
      case 'ResolvingLinks':
        return { label: 'Debrid links', detail: `${item.links.done} of ${item.links.total}`, fraction: item.links.fraction, tone: 'info', active: true };

      case 'Downloading':
        return { label: 'Download', detail: this.bytes(item), fraction: item.download.fraction, tone: 'info', active: item.download.done > 0 };

      case 'Downloaded':
        return { label: 'Downloaded, ready to unpack', detail: this.bytes(item), fraction: 1, tone: 'success', active: false };

      case 'Extracting':
        return { label: 'Unpacking', detail: '', fraction: item.extractPercent / 100, tone: 'info', active: true };

      case 'Organizing':
        return { label: 'Sorting files', detail: '', fraction: 1, tone: 'info', active: true };

      case 'PasswordRequired':
        return { label: 'Downloaded, password needed', detail: this.bytes(item), fraction: 1, tone: 'warning', active: false };

      case 'Completed':
        return { label: 'Done', detail: item.files.join(', '), fraction: 1, tone: 'success', active: false };

      case 'Failed':
        return { label: 'Stopped', detail: this.bytes(item), fraction: item.download.fraction, tone: 'danger', active: false };

      case 'Cancelled':
        return { label: 'Canceled', detail: this.bytes(item), fraction: item.download.fraction, tone: 'neutral', active: false };

      case 'Paused':
        return item.links.done < item.links.total
          ? { label: 'Paused, Debrid links', detail: `${item.links.done} of ${item.links.total}`, fraction: item.links.fraction, tone: 'warning', active: false }
          : { label: 'Paused', detail: this.bytes(item), fraction: item.download.fraction, tone: 'warning', active: false };
    }
  });

  protected readonly partCount: Signal<number> = computed((): number => this.item().parts.length);

  protected togglePartsShown(): void {
    this.partsShown.update((value: boolean): boolean => !value);
  }

  protected async pause(): Promise<void> {
    await this.act((): Promise<Job> => this.downloads.pauseItem(this.jobId(), this.item().number), `${this.item().label} could not be paused.`);
  }

  protected async resume(): Promise<void> {
    await this.act((): Promise<Job> => this.downloads.resumeItem(this.jobId(), this.item().number), `${this.item().label} could not be resumed.`);
  }

  protected async retry(): Promise<void> {
    await this.act((): Promise<Job> => this.downloads.retryItem(this.jobId(), this.item().number), `${this.item().label} could not be retried.`);
  }

  protected async unpack(): Promise<void> {
    await this.act((): Promise<Job> => this.downloads.unpackItem(this.jobId(), this.item().number), `${this.item().label} could not be unpacked.`);
  }

  protected async cancel(): Promise<void> {
    const confirmed: boolean = await ConfirmActionDialogComponent.confirm(this.dialog, {
      title: `Cancel ${this.item().label}?`,
      message: `${this.item().label} stops here and the rest of the download carries on. What has been downloaded stays on disk, and Retry picks it up again.`,
      confirmLabel: 'Cancel episode',
      cancelLabel: 'Keep running'
    });

    if (confirmed) await this.act((): Promise<Job> => this.downloads.cancelItem(this.jobId(), this.item().number), `${this.item().label} could not be canceled.`);
  }

  protected async remove(): Promise<void> {
    const confirmed: boolean = await ConfirmActionDialogComponent.confirm(this.dialog, {
      title: `Remove ${this.item().label}?`,
      message: `${this.item().label} disappears from this download${this.canCancel() ? ' and stops' : ''}. The rest carries on, and finished files stay on disk.`,
      confirmLabel: 'Remove'
    });

    if (confirmed) await this.act((): Promise<Job | null> => this.downloads.removeItem(this.jobId(), this.item().number), `${this.item().label} could not be removed.`);
  }

  protected partLabel(part: Part): string {
    return PART_LABELS[part.state];
  }

  protected partTone(part: Part): Tone {
    return PART_TONES[part.state];
  }

  protected partBytes(part: Part): string {
    return part.bytesTotal > 0 ? `${formatBytes(part.bytesDone)} / ${formatBytes(part.bytesTotal)}` : '';
  }

  private bytes(item: Item): string {
    return item.download.total > 0 ? `${formatBytes(item.download.done)} / ${formatBytes(item.download.total)}` : '';
  }

  private async act(call: () => Promise<Job | null>, failure: string): Promise<void> {
    this.working.set(true);

    try {
      const job: Job | null = await call();
      if (job) this.sync.apply(job);
    } catch (error: unknown) {
      this.notifications.failure(errorMessage(error, failure));
    } finally {
      this.working.set(false);
    }
  }
}