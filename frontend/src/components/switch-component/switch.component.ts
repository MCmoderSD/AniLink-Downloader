import { Component, input, InputSignal, output, OutputEmitterRef, signal, Signal, WritableSignal } from '@angular/core';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';

@Component({
  selector: 'app-switch',
  imports: [MatSlideToggleModule],
  template: `
    <mat-slide-toggle
      class="app-switch"
      [class.app-switch-ready]="flipped()"
      hideIcon
      disableRipple
      [checked]="checked()"
      [disabled]="disabled()"
      [aria-describedby]="describedBy()"
      (change)="flip($event)"
    >
      <ng-content/>
    </mat-slide-toggle>
  `,
  styles: `
    :host {
      display: inline-flex;
    }
  `
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