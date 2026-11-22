import { Component, input, InputSignal, output, OutputEmitterRef } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-file-nav',
  templateUrl: './file-nav.component.html',
  styleUrls: ['./file-nav.component.scss'],
  imports: [MatButtonModule, MatIconModule]
})
export class FileNavComponent {

  readonly canBack: InputSignal<boolean> = input.required<boolean>();
  readonly canForward: InputSignal<boolean> = input.required<boolean>();
  readonly canUp: InputSignal<boolean> = input.required<boolean>();

  readonly back: OutputEmitterRef<void> = output<void>();
  readonly forward: OutputEmitterRef<void> = output<void>();
  readonly up: OutputEmitterRef<void> = output<void>();
  readonly refresh: OutputEmitterRef<void> = output<void>();
}