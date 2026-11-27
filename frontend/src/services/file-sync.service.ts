import { HttpErrorResponse } from '@angular/common/http';
import { DOCUMENT } from '@angular/common';
import { computed, DestroyRef, effect, inject, Service, signal, Signal, untracked, WritableSignal } from '@angular/core';
import { Job, JobStatus } from '../data/download';
import { affects, FileChange, FileStreamMessage, FilesPhase, FolderListing } from '../data/files';
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

  private readonly latest: WritableSignal<FileChange> = signal<FileChange>({ id: 0, paths: null });

  private serial: number = 0;
  private listener: StreamListener | null = null;
  private folder: string = '';
  private listed: string | null = null;
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
  readonly changes: Signal<FileChange> = this.latest.asReadonly();

  constructor() {
    const visibility: () => void = (): void => {
      if (this.document.visibilityState !== 'visible') {
        this.close();
        return;
      }

      this.announce(null);

      if (this.listener) void this.refresh();
      else this.open();
    };

    const poll: () => void = (): void => {
      if (this.document.visibilityState !== 'visible') return;

      this.announce(null);
      void this.refresh();
    };

    this.document.addEventListener('visibilitychange', visibility);

    const timer: ReturnType<typeof setInterval> = setInterval(poll, POLL_MS);

    visibility();

    effect((): void => {
      this.locks();

      untracked((): void => void this.refresh());
    });

    inject(DestroyRef).onDestroy((): void => {
      this.close();
      this.document.removeEventListener('visibilitychange', visibility);
      clearInterval(timer);
    });
  }

  show(path: string): void {
    if (this.started && path === this.folder) return;

    this.started = true;
    this.folder = path;
    this.generation++;

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

  private open(): void {
    this.listener = this.sse.open<FileStreamMessage>(
      'files/stream',
      (message: FileStreamMessage): void => this.receive(message),
      (): void => {
        this.announce(null);
        void this.refresh();
      },
      (): void => this.state.update((phase: FilesPhase): FilesPhase => phase === 'ready' ? 'unreachable' : phase)
    );
  }

  private close(): void {
    this.listener?.close();
    this.listener = null;
  }

  private async load(): Promise<void> {
    const generation: number = this.generation;
    const folder: string = this.folder;

    try {
      const listing: FolderListing = await this.files.list(folder);
      if (generation !== this.generation) return;

      this.current.set(listing);
      this.listed = folder;
      this.state.set('ready');
      this.reason.set('');
    } catch (error: unknown) {
      if (generation !== this.generation) return;

      if (this.listed !== folder) this.current.set(null);

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
    this.announce(message.type === 'reset' ? null : message.paths);

    if (message.type === 'reset' || message.paths.some((path: string): boolean => affects(this.folder, path))) void this.refresh();
  }

  private announce(paths: readonly string[] | null): void {
    this.latest.set({ id: ++this.serial, paths });
  }
}