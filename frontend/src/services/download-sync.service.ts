import { DOCUMENT } from '@angular/common';
import { computed, inject, Service, signal, Signal, WritableSignal } from '@angular/core';
import { Job, NoticeLevel, NoticeMessage, StreamMessage } from '../data/download';
import { DownloadService } from './download.service';
import { NotificationService } from './notification.service';
import { SseService, StreamListener } from './sse.service';

@Service()
export class DownloadSyncService {

  private readonly downloads: DownloadService = inject(DownloadService);
  private readonly sse: SseService = inject(SseService);
  private readonly notifications: NotificationService = inject(NotificationService);
  private readonly document: Document = inject(DOCUMENT);

  private readonly store: WritableSignal<ReadonlyMap<string, Job>> = signal<ReadonlyMap<string, Job>>(new Map<string, Job>());
  private readonly lost: WritableSignal<boolean> = signal(false);
  private readonly ready: WritableSignal<boolean> = signal(false);

  private listener: StreamListener | null = null;

  readonly jobs: Signal<Job[]> = computed((): Job[] =>
    [...this.store().values()].sort((a: Job, b: Job): number => b.createdAt.localeCompare(a.createdAt)));

  readonly unreachable: Signal<boolean> = this.lost.asReadonly();
  readonly loaded: Signal<boolean> = this.ready.asReadonly();

  connect(): void {
    if (this.listener) return;

    this.listener = this.sse.open<StreamMessage>(
      'downloads/stream',
      (message: StreamMessage): void => this.receive(message),
      (): void => void this.reload(),
      (): void => this.lost.set(true)
    );

    // A phone puts the page to sleep when its screen goes off, and the stream falls silent with it. When the page is
    // shown again, the list is read anew instead of waiting for the connection to notice that it is gone.
    this.document.addEventListener('visibilitychange', (): void => {
      if (this.document.visibilityState === 'visible') void this.reload();
    });
  }

  apply(job: Job): void {
    this.store.update((jobs: ReadonlyMap<string, Job>): ReadonlyMap<string, Job> => new Map(jobs).set(job.id, job));
  }

  private async reload(): Promise<void> {
    try {
      const list: Job[] = await this.downloads.list();

      this.store.set(new Map(list.map((job: Job): [string, Job] => [job.id, job])));
      this.lost.set(false);
    } catch {
      this.lost.set(true);
    } finally {
      this.ready.set(true);
    }
  }

  private receive(message: StreamMessage): void {
    switch (message.type) {
      case 'job':
        this.apply(message.job);
        break;

      case 'removed':
        this.store.update((jobs: ReadonlyMap<string, Job>): ReadonlyMap<string, Job> => {
          const next: Map<string, Job> = new Map(jobs);
          next.delete(message.jobId);
          return next;
        });
        break;

      case 'notice':
        this.announce(message);
        break;
    }
  }

  private announce(message: NoticeMessage): void {
    const text: string = message.text.startsWith(message.job) ? message.text : `${message.job} · ${message.text}`;
    const level: NoticeLevel = message.level;

    if (level === 'Success') this.notifications.success(text);
    else if (level === 'Warning') this.notifications.warning(text);
    else if (level === 'Error') this.notifications.failure(text);
    else this.notifications.info(text);
  }
}