import { Component, computed, effect, ElementRef, inject, input, InputSignal, output, OutputEmitterRef, signal, Signal, untracked, WritableSignal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { fileType } from '../../data/file-types';
import { EntryKind, parentPath, ROOT_NAME, TreeRow } from '../../data/files';
import { FileTreeService } from '../../services/file-tree.service';

interface TreeItem {
  path: string;
  name: string;
  label: string | null;
  kind: EntryKind;
  level: number;
  expandable: boolean;
  open: boolean;
  current: boolean;
  icon: string;
}

@Component({
  selector: 'app-file-tree',
  templateUrl: './file-tree.component.html',
  styleUrls: ['./file-tree.component.scss'],
  imports: [MatIconModule]
})
export class FileTreeComponent {

  private readonly tree: FileTreeService = inject(FileTreeService);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef<HTMLElement>);

  readonly current: InputSignal<string> = input.required<string>();

  readonly opened: OutputEmitterRef<string> = output<string>();

  protected readonly focused: WritableSignal<string | null> = signal<string | null>(null);

  protected readonly items: Signal<TreeItem[]> = computed((): TreeItem[] => {
    const current: string = this.current();
    const open: boolean = this.tree.rootOpen();

    const root: TreeItem = { path: '', name: ROOT_NAME, label: 'Root folder', kind: 'Folder', level: 1, expandable: true, open, current: current === '', icon: open ? 'folder_open' : 'folder' };

    return [
      root,
      ...this.tree.rows().map((row: TreeRow): TreeItem => ({
        path: row.node.path,
        name: row.node.name,
        label: null,
        kind: row.node.kind,
        level: row.depth + 2,
        expandable: row.node.kind === 'Folder' && row.node.hasChildren,
        open: row.open,
        current: row.node.path === current,
        icon: row.node.kind === 'Folder' ? (row.open ? 'folder_open' : 'folder') : fileType(row.node.kind, row.node.name).icon
      }))
    ];
  });

  protected readonly stop: Signal<string> = computed((): string => {
    const items: TreeItem[] = this.items();
    const focused: string | null = this.focused();

    if (focused !== null && items.some((item: TreeItem): boolean => item.path === focused)) return focused;

    return items.find((item: TreeItem): boolean => item.current)?.path ?? '';
  });

  constructor() {
    effect((): void => {
      const current: string = this.current();

      untracked((): void => this.tree.reveal(current));
    });
  }

  protected activate(item: TreeItem): void {
    if (item.kind === 'Folder') this.tree.open(item.path);

    this.opened.emit(item.kind === 'Folder' ? item.path : parentPath(item.path));
  }

  protected toggle(event: Event, item: TreeItem): void {
    event.stopPropagation();

    if (item.expandable) this.tree.toggle(item.path);
  }

  protected keydown(event: KeyboardEvent): void {
    const element: HTMLElement | null = (event.target as HTMLElement).closest<HTMLElement>('[data-index]');
    if (!element) return;

    const items: TreeItem[] = this.items();
    const index: number = Number(element.dataset['index']);
    const item: TreeItem = items[index];

    switch (event.key) {
      case 'ArrowDown':
        this.focusAt(index + 1);
        break;

      case 'ArrowUp':
        this.focusAt(index - 1);
        break;

      case 'Home':
        this.focusAt(0);
        break;

      case 'End':
        this.focusAt(items.length - 1);
        break;

      case 'ArrowRight':
        if (item.expandable && !item.open) this.tree.open(item.path);
        else if (item.expandable && items[index + 1]?.level === item.level + 1) this.focusAt(index + 1);
        break;

      case 'ArrowLeft':
        if (item.expandable && item.open) this.tree.toggle(item.path);
        else this.focusAt(this.parentIndex(items, index));
        break;

      case 'Enter':
      case ' ':
        this.activate(item);
        break;

      default:
        return;
    }

    event.preventDefault();
  }

  private parentIndex(items: TreeItem[], index: number): number {
    for (let candidate: number = index - 1; candidate >= 0; candidate--) {
      if (items[candidate].level === items[index].level - 1) return candidate;
    }

    return index;
  }

  private focusAt(index: number): void {
    const items: TreeItem[] = this.items();
    if (index < 0 || index >= items.length) return;

    this.focused.set(items[index].path);
    this.host.nativeElement.querySelector<HTMLElement>(`[data-index="${index}"]`)?.focus();
  }
}