import { Directive, output, OutputEmitterRef, signal, Signal, WritableSignal } from '@angular/core';

function carriesFiles(event: DragEvent): boolean {
  return Array.from(event.dataTransfer?.types ?? []).includes('Files');
}

@Directive({
  selector: '[appFileDrop]',
  exportAs: 'appFileDrop',
  host: {
    '(dragenter)': 'enter($event)',
    '(dragover)': 'over($event)',
    '(dragleave)': 'leave($event)',
    '(drop)': 'drop($event)'
  }
})
export class FileDropDirective {

  readonly filesDropped: OutputEmitterRef<File[]> = output<File[]>();

  private readonly hovered: WritableSignal<boolean> = signal(false);

  private depth: number = 0;

  readonly active: Signal<boolean> = this.hovered.asReadonly();

  protected enter(event: DragEvent): void {
    if (!carriesFiles(event)) return;

    event.preventDefault();
    this.depth++;
    this.hovered.set(true);
  }

  protected over(event: DragEvent): void {
    if (!carriesFiles(event)) return;

    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
  }

  protected leave(event: DragEvent): void {
    if (!carriesFiles(event)) return;

    this.depth = Math.max(0, this.depth - 1);
    if (this.depth === 0) this.hovered.set(false);
  }

  protected drop(event: DragEvent): void {
    if (!carriesFiles(event)) return;

    event.preventDefault();
    this.depth = 0;
    this.hovered.set(false);

    const files: File[] = Array.from(event.dataTransfer?.files ?? []);
    if (files.length > 0) this.filesDropped.emit(files);
  }
}