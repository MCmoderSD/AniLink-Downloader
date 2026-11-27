import { computed, signal, Signal, WritableSignal } from '@angular/core';

export class FileTrail {

  private readonly folders: WritableSignal<string[]> = signal<string[]>([]);
  private readonly position: WritableSignal<number> = signal(-1);

  readonly canBack: Signal<boolean> = computed((): boolean => this.position() > 0);
  readonly canForward: Signal<boolean> = computed((): boolean => this.position() < this.folders().length - 1);

  visit(folder: string): void {
    const position: number = this.position();
    if (this.folders()[position] === folder) return;

    this.folders.update((list: string[]): string[] => [...list.slice(0, position + 1), folder]);
    this.position.set(position + 1);
  }

  replace(folder: string): void {
    let position: number = this.position();
    if (position < 0) return;

    const list: string[] = [...this.folders()];
    list[position] = folder;

    if (position > 0 && list[position - 1] === folder) {
      list.splice(position, 1);
      position--;
    }

    if (list[position + 1] === folder) list.splice(position + 1, 1);

    this.folders.set(list);
    this.position.set(position);
  }

  back(): string | null {
    return this.canBack() ? this.move(-1) : null;
  }

  forward(): string | null {
    return this.canForward() ? this.move(1) : null;
  }

  private move(step: number): string {
    this.position.update((position: number): number => position + step);

    return this.folders()[this.position()];
  }
}