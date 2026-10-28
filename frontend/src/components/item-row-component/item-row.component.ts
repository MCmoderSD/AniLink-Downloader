import { Component, computed, input, InputSignal, signal, Signal, WritableSignal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Item, Part, PART_LABELS, PART_TONES, STAGE_LABELS, STAGE_TONES, Tone } from '../../data/download';
import { formatBytes } from '../../data/format';
import { ProgressBarComponent } from '../progress-bar-component/progress-bar.component';

interface Bar {
  label: string;
  detail: string;
  fraction: number;
  tone: Tone;
  active: boolean;
}

@Component({
  selector: 'app-item-row',
  templateUrl: './item-row.component.html',
  styleUrls: ['./item-row.component.scss'],
  imports: [MatIconModule, ProgressBarComponent]
})
export class ItemRowComponent {

  readonly item: InputSignal<Item> = input.required<Item>();

  private readonly partsShown: WritableSignal<boolean> = signal(false);

  protected readonly partsOpen: Signal<boolean> = this.partsShown.asReadonly();

  protected readonly stageLabel: Signal<string> = computed((): string => STAGE_LABELS[this.item().stage]);
  protected readonly stageTone: Signal<Tone> = computed((): Tone => STAGE_TONES[this.item().stage]);

  protected readonly bar: Signal<Bar> = computed((): Bar => {
    const item: Item = this.item();

    switch (item.stage) {
      case 'ResolvingLinks':
        return { label: 'Debrid links', detail: `${item.links.done} of ${item.links.total}`, fraction: item.links.fraction, tone: 'info', active: true };

      case 'Downloading':
        return { label: 'Download', detail: this.bytes(item), fraction: item.download.fraction, tone: 'info', active: item.download.done > 0 };

      case 'Extracting':
        return { label: 'Unpacking', detail: '', fraction: item.extractPercent / 100, tone: 'info', active: true };

      case 'Organizing':
        return { label: 'Sorting files', detail: '', fraction: 1, tone: 'info', active: true };

      case 'PasswordRequired':
        return { label: 'Downloaded, password needed', detail: this.bytes(item), fraction: 1, tone: 'warning', active: false };

      case 'Completed':
        return { label: 'Done', detail: item.files.join(', '), fraction: 1, tone: 'success', active: false };

      case 'Failed':
        return { label: 'Stopped', detail: this.bytes(item), fraction: item.download.fraction, tone: 'danger', active: false };

      case 'Cancelled':
        return { label: 'Canceled', detail: this.bytes(item), fraction: item.download.fraction, tone: 'neutral', active: false };

      case 'Paused':
        return item.links.done < item.links.total
          ? { label: 'Paused, Debrid links', detail: `${item.links.done} of ${item.links.total}`, fraction: item.links.fraction, tone: 'warning', active: false }
          : { label: 'Paused', detail: this.bytes(item), fraction: item.download.fraction, tone: 'warning', active: false };
    }
  });

  protected readonly partCount: Signal<number> = computed((): number => this.item().parts.length);

  protected togglePartsShown(): void {
    this.partsShown.update((value: boolean): boolean => !value);
  }

  protected partLabel(part: Part): string {
    return PART_LABELS[part.state];
  }

  protected partTone(part: Part): Tone {
    return PART_TONES[part.state];
  }

  protected partBytes(part: Part): string {
    return part.bytesTotal > 0 ? `${formatBytes(part.bytesDone)} / ${formatBytes(part.bytesTotal)}` : '';
  }

  private bytes(item: Item): string {
    return item.download.total > 0 ? `${formatBytes(item.download.done)} / ${formatBytes(item.download.total)}` : '';
  }
}