import { afterNextRender, Component, computed, DestroyRef, ElementRef, inject, input, InputSignal, linkedSignal, output, OutputEmitterRef, Signal, signal, viewChildren, WritableSignal } from '@angular/core';

export interface SegmentedOption {
  value: string;
  label: string;
}

interface PillSpan {
  left: number;
  right: number;
}

type PillDirection = 'none' | 'forward' | 'backward';

@Component({
  selector: 'app-segmented-switch',
  templateUrl: './segmented-switch.component.html',
  styleUrls: ['./segmented-switch.component.scss'],
  host: {
    role: 'radiogroup',
    '[attr.aria-label]': 'label()',
    '[class.segmented-ready]': 'ready()',
    '[class.segmented-forward]': "direction() === 'forward'",
    '[class.segmented-backward]': "direction() === 'backward'"
  }
})
export class SegmentedSwitchComponent {

  readonly options: InputSignal<SegmentedOption[]> = input.required<SegmentedOption[]>();
  readonly value: InputSignal<string> = input.required<string>();
  readonly label: InputSignal<string> = input.required<string>();

  readonly valueChange: OutputEmitterRef<string> = output<string>();

  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  private readonly choices: Signal<readonly ElementRef<HTMLButtonElement>[]> =
    viewChildren<ElementRef<HTMLButtonElement>>('choice');

  private readonly spans: WritableSignal<PillSpan[]> = signal([]);
  private readonly settled: WritableSignal<boolean> = signal(false);

  protected readonly ready: Signal<boolean> = this.settled.asReadonly();

  protected readonly index: Signal<number> = computed((): number =>
    Math.max(0, this.options().findIndex((option: SegmentedOption): boolean => option.value === this.value())));

  protected readonly direction: Signal<PillDirection> = linkedSignal<number, PillDirection>({
    source: this.index,
    computation: (next: number, previous?: { source: number; value: PillDirection }): PillDirection => {
      if (!previous || next === previous.source) return previous?.value ?? 'none';
      return next > previous.source ? 'forward' : 'backward';
    }
  });

  protected readonly pill: Signal<PillSpan | null> = computed((): PillSpan | null => this.spans()[this.index()] ?? null);

  constructor() {
    afterNextRender((): void => {
      this.measure();

      const sizes: ResizeObserver | undefined = typeof ResizeObserver === 'function' ? new ResizeObserver((): void => this.measure()) : undefined;
      sizes?.observe(this.host.nativeElement);

      const frame: number = requestAnimationFrame((): void => this.settled.set(true));

      this.destroyRef.onDestroy((): void => {
        cancelAnimationFrame(frame);
        sizes?.disconnect();
      });
    });
  }

  protected select(option: SegmentedOption): void {
    if (option.value !== this.value()) this.valueChange.emit(option.value);
  }

  protected step(event: KeyboardEvent, from: number): void {
    const last: number = this.options().length - 1;

    let target: number;

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        target = from + 1;
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        target = from - 1;
        break;
      case 'Home':
        target = 0;
        break;
      case 'End':
        target = last;
        break;
      default:
        return;
    }

    event.preventDefault();

    const next: number = Math.min(last, Math.max(0, target));
    const option: SegmentedOption | undefined = this.options()[next];
    if (!option) return;

    this.choices()[next]?.nativeElement.focus();
    this.select(option);
  }

  private measure(): void {
    const width: number = this.host.nativeElement.clientWidth;

    this.spans.set(this.choices().map(({ nativeElement }: ElementRef<HTMLButtonElement>): PillSpan => ({
      left: nativeElement.offsetLeft,
      right: width - nativeElement.offsetLeft - nativeElement.offsetWidth
    })));
  }
}