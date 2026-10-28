import { DatePipe } from '@angular/common';
import { Component, computed, input, InputSignal, Signal } from '@angular/core';
import { Notice, NOTICE_TONES, Tone } from '../../data/download';
import { ScrollBarDirective } from '../scroll-bar-component/scroll-bar.directive';

@Component({
  selector: 'app-activity-log',
  templateUrl: './activity-log.component.html',
  styleUrls: ['./activity-log.component.scss'],
  imports: [DatePipe, ScrollBarDirective]
})
export class ActivityLogComponent {

  readonly entries: InputSignal<Notice[]> = input.required<Notice[]>();

  protected readonly newestFirst: Signal<Notice[]> = computed((): Notice[] => [...this.entries()].reverse());

  protected tone(entry: Notice): Tone {
    return NOTICE_TONES[entry.level];
  }
}