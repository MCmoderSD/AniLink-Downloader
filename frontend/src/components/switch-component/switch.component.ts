import { Component, input, InputSignal, output, OutputEmitterRef, signal, Signal, WritableSignal } from '@angular/core';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';

@Component({
  selector: 'app-switch',
  imports: [MatSlideToggleModule],
  templateUrl: './switch.component.html',
  styleUrls: ['./switch.component.scss']
})
export class SwitchComponent {

  private readonly touched: WritableSignal<boolean> = signal(false);

  readonly checked: InputSignal<boolean> = input(false);
  readonly disabled: InputSignal<boolean> = input(false);
  readonly describedBy: InputSignal<string> = input('');

  readonly toggled: OutputEmitterRef<boolean> = output<boolean>();

  protected readonly flipped: Signal<boolean> = this.touched.asReadonly();

  protected flip(event: MatSlideToggleChange): void {
    this.touched.set(true);
    this.toggled.emit(event.checked);
  }
}