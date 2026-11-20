import { HttpErrorResponse } from '@angular/common/http';
import { DOCUMENT } from '@angular/common';
import { computed, DestroyRef, effect, inject, Service, signal, Signal, WritableSignal } from '@angular/core';
import { Job, JobStatus } from '../data/download';
import { affects, FileStreamMessage, FilesPhase, FolderListing } from '../data/files';
import { DownloadSyncService } from './download-sync.service';
import { FilesService } from './files.service';
import { errorMessage } from './http-error';
import { SseService, StreamListener } from './sse.service';

const POLL_MS: number = 15000;

const LOCKING: ReadonlySet<JobStatus> = new Set<JobStatus>(['Active', 'Paused', 'PasswordRequired', 'Downloaded']);

@Service({ autoProvided: false })
export class FileSyncService {

  private readonly files: FilesService = inject(FilesService);
  private readonly sse: SseService = inject(SseService);
  private readonly downloads: DownloadSyncService = inject(DownloadSyncService);
  private readonly document: Document = inject(DOCUMENT);

  private readonly current: WritableSignal<FolderListing | null> = signal<FolderListing | null>(null);
  private readonly state: WritableSignal<FilesPhase> = signal<FilesPhase>('loading');
  private readonly reason: WritableSignal<string> = signal('');

  private folder: string = '';
  private started: boolean = false;
  private generation: number = 0;
  private running: boolean = false;
  private again: boolean = false;

  private readonly locks: Signal<string> = computed((): string =>
    this.downloads.jobs()
      .filter((job: Job): boolean => LOCKING.has(job.status))
      .map((job: Job): string => `${job.id}:${job.status}`)
      .sort()
      .join(','));

  readonly listing: Signal<FolderListing | null> = this.current.asReadonly();
  readonly phase: Signal<FilesPhase> = this.state.asReadonly();
  readonly problem: Signal<string> = this.reason.asReadonly();

  constructor() {
    const listener: StreamListener = this.sse.open<FileStreamMessage>(
      'files/stream',
      (message: FileStreamMessage): void => this.receive(message),
      (): void => void this.refresh(),
      (): void => this.state.update((phase: FilesPhase): FilesPhase => phase === 'ready' ? 'unreachable' : phase)
    );

    const visible: () => void = (): void => {
      if (this.document.visibilityState === 'visible') void this.refresh();
    };

    this.document.addEventListener('visibilitychange', visible);

    const timer: ReturnType<typeof setInterval> = setInterval(visible, POLL_MS);

    effect((): void => {
      this.locks();
      void this.refresh();
    });

    inject(DestroyRef).onDestroy((): void => {
      listener.close();
      this.document.removeEventListener('visibilitychange', visible);
      clearInterval(timer);
    });
  }

  show(path: string): void {
    if (this.started && path === this.folder) return;

    this.started = true;
    this.folder = path;
    this.generation++;

    this.current.set(null);
    this.state.set('loading');
    this.reason.set('');

    void this.refresh();
  }

  async refresh(): Promise<void> {
    if (!this.started) return;

    if (this.running) {
      this.again = true;
      return;
    }

    this.running = true;

    try {
      do {
        this.again = false;
        await this.load();
      } while (this.again);
    } finally {
      this.running = false;
    }
  }

  private async load(): Promise<void> {
    const generation: number = this.generation;

    try {
      const listing: FolderListing = await this.files.list(this.folder);
      if (generation !== this.generation) return;

      this.current.set(listing);
      this.state.set('ready');
      this.reason.set('');
    } catch (error: unknown) {
      if (generation !== this.generation) return;

      const status: number = error instanceof HttpErrorResponse ? error.status : 0;

      if (status === 404) {
        this.state.set('missing');
      } else if (status === 400) {
        this.reason.set(errorMessage(error, 'This folder cannot be shown.'));
        this.state.set('invalid');
      } else {
        this.state.set('unreachable');
      }
    }
  }

  private receive(message: FileStreamMessage): void {
    if (message.type === 'reset' || message.paths.some((path: string): boolean => affects(this.folder, path))) void this.refresh();
  }
}