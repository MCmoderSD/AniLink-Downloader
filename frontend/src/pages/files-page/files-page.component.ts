import { DOCUMENT } from '@angular/common';
import { Component, computed, effect, inject, input, InputSignal, linkedSignal, signal, Signal, untracked, WritableSignal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router, RouterLink } from '@angular/router';
import { ConfirmActionDialogComponent } from '../../components/confirm-action-dialog-component/confirm-action-dialog.component';
import { FileBreadcrumbComponent } from '../../components/file-breadcrumb-component/file-breadcrumb.component';
import { FileListComponent, FileToggle } from '../../components/file-list-component/file-list.component';
import { FileNavComponent } from '../../components/file-nav-component/file-nav.component';
import { FileToolbarComponent } from '../../components/file-toolbar-component/file-toolbar.component';
import { FileTreeComponent } from '../../components/file-tree-component/file-tree.component';
import { compareEntries, fileCount, FileBatch, FileEntry, FilesPhase, FolderListing, isLocked, lastName, parentPath, summarizeDeletion, Summary } from '../../data/files';
import { formatBytes } from '../../data/format';
import { FileSyncService } from '../../services/file-sync.service';
import { FileTrail } from '../../services/file-trail';
import { FileTreeService } from '../../services/file-tree.service';
import { FilesService } from '../../services/files.service';
import { errorMessage } from '../../services/http-error';
import { NotificationService } from '../../services/notification.service';

const LISTED_NAMES: number = 10;

@Component({
  selector: 'app-files-page',
  templateUrl: './files-page.component.html',
  styleUrls: ['./files-page.component.scss'],
  imports: [MatButtonModule, MatIconModule, RouterLink, FileBreadcrumbComponent, FileListComponent, FileNavComponent, FileToolbarComponent, FileTreeComponent],
  providers: [FileSyncService, FileTreeService]
})
export class FilesPageComponent {

  private readonly sync: FileSyncService = inject(FileSyncService);
  private readonly files: FilesService = inject(FilesService);
  private readonly notifications: NotificationService = inject(NotificationService);
  private readonly dialog: MatDialog = inject(MatDialog);
  private readonly router: Router = inject(Router);

  private readonly working: WritableSignal<boolean> = signal(false);

  private readonly shown: WritableSignal<boolean> = signal(inject(DOCUMENT).defaultView?.matchMedia('(min-width: 60rem)').matches ?? false);

  protected readonly trail: FileTrail = new FileTrail();
  protected readonly treeShown: Signal<boolean> = this.shown.asReadonly();

  readonly path: InputSignal<string | undefined> = input<string>();

  protected readonly busy: Signal<boolean> = this.working.asReadonly();
  protected readonly phase: Signal<FilesPhase> = this.sync.phase;
  protected readonly problem: Signal<string> = this.sync.problem;
  protected readonly listing: Signal<FolderListing | null> = this.sync.listing;

  protected readonly folder: Signal<string> = computed((): string => this.path() ?? '');
  protected readonly parent: Signal<string> = computed((): string => parentPath(this.folder()));

  protected readonly entries: Signal<FileEntry[]> = computed((): FileEntry[] => [...(this.listing()?.entries ?? [])].sort(compareEntries));

  protected readonly caption: Signal<string> = computed((): string =>
    `Contents of ${this.folder() === '' ? 'the root folder' : lastName(this.folder())}`);

  protected readonly subtitle: Signal<string> = computed((): string => {
    if (!this.listing()) return '';

    const entries: FileEntry[] = this.entries();
    if (entries.length === 0) return 'Empty.';

    const folders: number = entries.filter((entry: FileEntry): boolean => entry.kind === 'Folder').length;
    const files: number = entries.length - folders;
    const size: number = entries.reduce((sum: number, entry: FileEntry): number => sum + entry.size, 0);

    return `${folders} ${folders === 1 ? 'folder' : 'folders'} · ${files} ${files === 1 ? 'file' : 'files'} · ${formatBytes(size)}`;
  });

  protected readonly selection: WritableSignal<ReadonlySet<string>> = linkedSignal<{ folder: string; entries: FileEntry[] }, ReadonlySet<string>>({
    source: (): { folder: string; entries: FileEntry[] } => ({ folder: this.folder(), entries: this.entries() }),
    computation: (source: { folder: string; entries: FileEntry[] }, previous?: { source: { folder: string }; value: ReadonlySet<string> }): ReadonlySet<string> => {
      if (!previous || previous.source.folder !== source.folder) return new Set<string>();

      const selectable: Set<string> = new Set(source.entries.filter((entry: FileEntry): boolean => !isLocked(entry)).map((entry: FileEntry): string => entry.path));

      return new Set([...previous.value].filter((path: string): boolean => selectable.has(path)));
    }
  });

  protected readonly chosen: Signal<FileEntry[]> = computed((): FileEntry[] =>
    this.entries().filter((entry: FileEntry): boolean => this.selection().has(entry.path)));

  protected readonly chosenBytes: Signal<number> = computed((): number =>
    this.chosen().reduce((sum: number, entry: FileEntry): number => sum + entry.size, 0));

  protected readonly chosenFiles: Signal<number> = computed((): number =>
    this.chosen().reduce((sum: number, entry: FileEntry): number => sum + fileCount(entry), 0));

  constructor() {
    effect((): void => {
      const folder: string = this.folder();

      untracked((): void => {
        this.trail.visit(folder);
        this.sync.show(folder);
      });
    });

    effect((): void => {
      const folder: string = this.folder();
      if (this.phase() !== 'missing' || folder === '') return;

      const parent: string = this.parent();

      untracked((): void => {
        this.notifications.info(`“${lastName(folder)}” is no longer there.`);
        this.trail.replace(parent);
        void this.router.navigate(['/files'], { queryParams: { path: parent || null }, replaceUrl: true });
      });
    });
  }

  protected back(): void {
    this.go(this.trail.back());
  }

  protected forward(): void {
    this.go(this.trail.forward());
  }

  protected up(): void {
    if (this.folder() !== '') this.go(this.parent());
  }

  protected home(): void {
    if (this.folder() !== '') this.go('');
  }

  protected refresh(): void {
    void this.sync.refresh();
  }

  protected open(folder: string): void {
    this.go(folder);
  }

  protected toggleTree(): void {
    this.shown.update((shown: boolean): boolean => !shown);
  }

  private go(folder: string | null): void {
    if (folder !== null) void this.router.navigate(['/files'], { queryParams: { path: folder || null } });
  }

  protected toggle(change: FileToggle): void {
    this.selection.update((selected: ReadonlySet<string>): ReadonlySet<string> => {
      const next: Set<string> = new Set(selected);

      if (change.checked) next.add(change.path);
      else next.delete(change.path);

      return next;
    });
  }

  protected toggleAll(checked: boolean): void {
    this.selection.set(checked
      ? new Set(this.entries().filter((entry: FileEntry): boolean => !isLocked(entry)).map((entry: FileEntry): string => entry.path))
      : new Set<string>());
  }

  protected clear(): void {
    this.selection.set(new Set<string>());
  }

  protected async remove(): Promise<void> {
    const chosen: FileEntry[] = this.chosen();
    if (chosen.length === 0 || this.working()) return;

    const files: number = this.chosenFiles();
    const names: string[] = chosen.map((entry: FileEntry): string => entry.name);

    const confirmed: boolean = await ConfirmActionDialogComponent.confirm(this.dialog, {
      title: chosen.length === 1 ? `Delete “${chosen[0].name}”?` : `Delete ${chosen.length} items?`,
      message: `${formatBytes(this.chosenBytes())} in ${files} ${files === 1 ? 'file' : 'files'} will be deleted permanently. This cannot be undone.`,
      items: names.length > LISTED_NAMES ? [...names.slice(0, LISTED_NAMES), `and ${names.length - LISTED_NAMES} more`] : names,
      confirmLabel: 'Delete permanently',
      cancelLabel: 'Keep',
      tone: 'danger'
    });

    if (!confirmed) return;

    this.working.set(true);

    try {
      const batch: FileBatch = await this.files.remove(chosen.map((entry: FileEntry): string => entry.path));
      const summary: Summary = summarizeDeletion(batch.results);

      this.notifications[summary.kind](summary.text);
      await this.sync.refresh();
    } catch (error: unknown) {
      this.notifications.failure(errorMessage(error, 'The files could not be deleted.'));
    } finally {
      this.working.set(false);
    }
  }
}