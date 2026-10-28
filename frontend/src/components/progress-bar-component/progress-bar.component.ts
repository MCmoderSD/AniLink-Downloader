import { Component, computed, input, InputSignal, Signal } from '@angular/core';
import { Tone } from '../../data/download';

@Component({
  selector: 'app-progress-bar',
  templateUrl: './progress-bar.component.html',
  styleUrls: ['./progress-bar.component.scss'],
  host: {
    '[attr.data-tone]': 'tone()',
    '[class.progress-compact]': 'compact()'
  }
})
export class ProgressBarComponent {

  readonly label: InputSignal<string> = input.required<string>();
  readonly detail: InputSignal<string> = input<string>('');
  readonly fraction: InputSignal<number> = input.required<number>();
  readonly tone: InputSignal<Tone> = input<Tone>('info');
  readonly compact: InputSignal<boolean> = input<boolean>(false);
  readonly active: InputSignal<boolean> = input<boolean>(false);

  protected readonly percent: Signal<number> = computed((): number =>
    Math.round(Math.min(Math.max(this.fraction(), 0), 1) * 100));

  protected readonly valueText: Signal<string> = computed((): string =>
    this.detail() ? `${this.percent()}%, ${this.detail()}` : `${this.percent()}%`);
}