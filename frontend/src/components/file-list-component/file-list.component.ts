import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input, InputSignal, output, OutputEmitterRef, Signal } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { fileType, FileType } from '../../data/file-types';
import { FileEntry, isLocked, lockText } from '../../data/files';
import { formatBytes, splitFileName } from '../../data/format';

export interface FileToggle {
  path: string;
  checked: boolean;
}

interface Row {
  entry: FileEntry;
  head: string;
  tail: string;
  size: string;
  icon: string;
  kind: string;
  lock: string;
  locked: boolean;
}

@Component({
  selector: 'app-file-list',
  templateUrl: './file-list.component.html',
  styleUrls: ['./file-list.component.scss'],
  imports: [DatePipe, DecimalPipe, MatCheckboxModule, MatIconModule, RouterLink]
})
export class FileListComponent {

  readonly entries: InputSignal<readonly FileEntry[]> = input.required<readonly FileEntry[]>();
  readonly selected: InputSignal<ReadonlySet<string>> = input.required<ReadonlySet<string>>();
  readonly caption: InputSignal<string> = input.required<string>();
  readonly busy: InputSignal<boolean> = input<boolean>(false);

  readonly toggled: OutputEmitterRef<FileToggle> = output<FileToggle>();
  readonly toggledAll: OutputEmitterRef<boolean> = output<boolean>();

  protected readonly rows: Signal<Row[]> = computed((): Row[] =>
    this.entries().map((entry: FileEntry): Row => {
      const name: { head: string; tail: string } = entry.kind === 'File' ? splitFileName(entry.name) : { head: entry.name, tail: '' };
      const type: FileType = fileType(entry.kind, entry.name);

      return {
        entry,
        head: name.head,
        tail: name.tail,
        size: formatBytes(entry.size),
        icon: type.icon,
        kind: type.label,
        lock: isLocked(entry) ? lockText(entry.lockedBy) : '',
        locked: isLocked(entry)
      };
    }));

  private readonly selectable: Signal<number> = computed((): number =>
    this.entries().filter((entry: FileEntry): boolean => !isLocked(entry)).length);

  private readonly chosen: Signal<number> = computed((): number =>
    this.entries().filter((entry: FileEntry): boolean => this.selected().has(entry.path)).length);

  protected readonly allChosen: Signal<boolean> = computed((): boolean => this.selectable() > 0 && this.chosen() === this.selectable());
  protected readonly someChosen: Signal<boolean> = computed((): boolean => this.chosen() > 0 && this.chosen() < this.selectable());
  protected readonly nothingToChoose: Signal<boolean> = computed((): boolean => this.selectable() === 0);
}