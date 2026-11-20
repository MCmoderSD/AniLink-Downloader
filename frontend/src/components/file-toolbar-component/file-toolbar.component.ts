import { Component, computed, input, InputSignal, output, OutputEmitterRef, Signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { formatBytes } from '../../data/format';

@Component({
  selector: 'app-file-toolbar',
  templateUrl: './file-toolbar.component.html',
  styleUrls: ['./file-toolbar.component.scss'],
  imports: [MatButtonModule, MatIconModule]
})
export class FileToolbarComponent {

  readonly count: InputSignal<number> = input.required<number>();
  readonly bytes: InputSignal<number> = input.required<number>();
  readonly files: InputSignal<number> = input.required<number>();
  readonly busy: InputSignal<boolean> = input<boolean>(false);

  readonly cleared: OutputEmitterRef<void> = output<void>();
  readonly deleteRequested: OutputEmitterRef<void> = output<void>();

  protected readonly summary: Signal<string> = computed((): string =>
    this.count() === 0
      ? 'Nothing selected'
      : `${this.count()} selected · ${formatBytes(this.bytes())} in ${this.files()} ${this.files() === 1 ? 'file' : 'files'}`);
}