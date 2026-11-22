import { computed, effect, inject, Service, signal, Signal, untracked, WritableSignal } from '@angular/core';
import { compareEntries, FileChange, FolderTree, parentPath, TreeNode, TreeRow } from '../data/files';
import { FileSyncService } from './file-sync.service';
import { FilesService } from './files.service';

@Service({ autoProvided: false })
export class FileTreeService {

  private readonly files: FilesService = inject(FilesService);
  private readonly sync: FileSyncService = inject(FileSyncService);

  private readonly children: WritableSignal<ReadonlyMap<string, readonly TreeNode[]>> = signal<ReadonlyMap<string, readonly TreeNode[]>>(new Map());
  private readonly expanded: WritableSignal<ReadonlySet<string>> = signal<ReadonlySet<string>>(new Set<string>());
  private readonly loading: WritableSignal<ReadonlySet<string>> = signal<ReadonlySet<string>>(new Set<string>());
  private readonly broken: WritableSignal<boolean> = signal(false);

  private seen: number = 0;

  readonly unreachable: Signal<boolean> = this.broken.asReadonly();

  readonly rootOpen: Signal<boolean> = computed((): boolean => this.expanded().has(''));

  readonly rows: Signal<TreeRow[]> = computed((): TreeRow[] => {
    const rows: TreeRow[] = [];
    const children: ReadonlyMap<string, readonly TreeNode[]> = this.children();
    const expanded: ReadonlySet<string> = this.expanded();
    const loading: ReadonlySet<string> = this.loading();

    const walk = (path: string, depth: number): void => {
      for (const node of [...(children.get(path) ?? [])].sort(compareEntries)) {
        const open: boolean = node.kind === 'Folder' && expanded.has(node.path);

        rows.push({ node, depth, open, loading: loading.has(node.path) });

        if (open) walk(node.path, depth + 1);
      }
    };

    if (expanded.has('')) walk('', 0);

    return rows;
  });

  constructor() {
    void this.load('');
    this.expand('');

    effect((): void => {
      const change: FileChange = this.sync.changes();

      untracked((): void => {
        if (change.id > this.seen) {
          this.seen = change.id;
          this.refreshAffected(change.paths);
        }
      });
    });
  }

  toggle(path: string): void {
    if (this.expanded().has(path)) this.collapse(path);
    else this.open(path);
  }

  open(path: string): void {
    this.expand(path);

    void this.load(path);
  }

  reveal(path: string): void {
    const parts: string[] = path === '' ? [] : path.split('/');

    for (let index: number = 0; index <= parts.length; index++) this.open(parts.slice(0, index).join('/'));
  }

  private collapse(path: string): void {
    this.expanded.update((paths: ReadonlySet<string>): ReadonlySet<string> => {
      const next: Set<string> = new Set(paths);

      next.delete(path);

      return next;
    });
  }

  private expand(path: string): void {
    this.expanded.update((paths: ReadonlySet<string>): ReadonlySet<string> => new Set(paths).add(path));
  }

  private refreshAffected(changed: readonly string[] | null): void {
    for (const folder of this.expanded()) {
      if (changed === null || changed.some((path: string): boolean => path === folder || parentPath(path) === folder)) void this.load(folder);
    }
  }

  private async load(path: string): Promise<void> {
    this.loading.update((paths: ReadonlySet<string>): ReadonlySet<string> => new Set(paths).add(path));

    try {
      const tree: FolderTree = await this.files.tree(path);

      this.children.update((map: ReadonlyMap<string, readonly TreeNode[]>): ReadonlyMap<string, readonly TreeNode[]> => new Map(map).set(path, tree.nodes));
      this.broken.set(false);
    } catch (error: unknown) {
      if (this.isMissing(error)) this.forget(path);
      else this.broken.set(true);
    } finally {
      this.loading.update((paths: ReadonlySet<string>): ReadonlySet<string> => {
        const next: Set<string> = new Set(paths);

        next.delete(path);

        return next;
      });
    }
  }

  private isMissing(error: unknown): boolean {
    return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
  }

  private forget(path: string): void {
    this.children.update((map: ReadonlyMap<string, readonly TreeNode[]>): ReadonlyMap<string, readonly TreeNode[]> => {
      const next: Map<string, readonly TreeNode[]> = new Map(map);

      next.delete(path);

      return next;
    });

    this.collapse(path);
  }
}