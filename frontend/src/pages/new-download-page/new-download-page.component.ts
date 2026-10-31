import { Component, computed, effect, inject, signal, Signal, WritableSignal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { Router, RouterLink } from '@angular/router';
import { NumberStepperComponent } from '../../components/number-stepper-component/number-stepper.component';
import { RESIZE_GRIP_SIZE, ResizeGripComponent } from '../../components/resize-grip-component/resize-grip.component';
import { ScrollBarComponent } from '../../components/scroll-bar-component/scroll-bar.component';
import { ScrollBarDirective } from '../../components/scroll-bar-component/scroll-bar.directive';
import {
  SegmentedOption,
  SegmentedSwitchComponent
} from '../../components/segmented-switch-component/segmented-switch.component';
import {
  Analysis,
  AnalyzedEpisode,
  AnalyzeRequest,
  AppConfig,
  Job,
  Limits,
  ReleaseKind
} from '../../data/download';
import { ConfigService } from '../../services/config.service';
import { DownloadService } from '../../services/download.service';
import { DownloadSyncService } from '../../services/download-sync.service';
import { errorMessage } from '../../services/http-error';
import { NotificationService } from '../../services/notification.service';

const ANALYZE_DELAY_MS: number = 350;

const ENCODER: TextEncoder = new TextEncoder();

const KINDS: SegmentedOption[] = [
  { value: 'Season', label: 'Season' },
  { value: 'Movie', label: 'Movie' }
];

@Component({
  selector: 'app-new-download-page',
  templateUrl: './new-download-page.component.html',
  styleUrls: ['./new-download-page.component.scss'],
  imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatSlideToggleModule, NumberStepperComponent, ResizeGripComponent, RouterLink, ScrollBarComponent, ScrollBarDirective, SegmentedSwitchComponent]
})
export class NewDownloadPageComponent {

  private readonly downloads: DownloadService = inject(DownloadService);
  private readonly sync: DownloadSyncService = inject(DownloadSyncService);
  private readonly notifications: NotificationService = inject(NotificationService);
  private readonly router: Router = inject(Router);

  private readonly nameValue: WritableSignal<string> = signal('');
  private readonly kindValue: WritableSignal<ReleaseKind> = signal<ReleaseKind>('Season');
  private readonly linksValue: WritableSignal<string> = signal('');
  private readonly formatValue: WritableSignal<string> = signal('');
  private readonly seasonValue: WritableSignal<string> = signal('');
  private readonly passwordValue: WritableSignal<string> = signal('');
  private readonly passwordShown: WritableSignal<boolean> = signal(false);
  private readonly unpackValue: WritableSignal<boolean> = signal(true);

  private readonly analysisValue: WritableSignal<Analysis | null> = signal<Analysis | null>(null);
  private readonly analysisProblem: WritableSignal<string | null> = signal<string | null>(null);
  private readonly analysisRunning: WritableSignal<boolean> = signal(false);
  private readonly sending: WritableSignal<boolean> = signal(false);
  private readonly sendProblem: WritableSignal<string | null> = signal<string | null>(null);

  private analysisTicket: number = 0;

  protected readonly kind: Signal<ReleaseKind> = this.kindValue.asReadonly();
  protected readonly seasonText: Signal<string> = this.seasonValue.asReadonly();
  protected readonly gripSize: number = RESIZE_GRIP_SIZE;
  protected readonly kinds: SegmentedOption[] = KINDS;
  protected readonly passwordVisible: Signal<boolean> = this.passwordShown.asReadonly();
  protected readonly unpack: Signal<boolean> = this.unpackValue.asReadonly();
  protected readonly analysis: Signal<Analysis | null> = this.analysisValue.asReadonly();
  protected readonly analysisError: Signal<string | null> = this.analysisProblem.asReadonly();
  protected readonly analyzing: Signal<boolean> = this.analysisRunning.asReadonly();
  protected readonly submitting: Signal<boolean> = this.sending.asReadonly();
  protected readonly submitError: Signal<string | null> = this.sendProblem.asReadonly();

  protected readonly config: Signal<AppConfig | null> = inject(ConfigService).config;

  protected readonly isSeason: Signal<boolean> = computed((): boolean => this.kindValue() === 'Season');

  protected readonly limits: Signal<Limits | null> = computed((): Limits | null => this.config()?.limits ?? null);

  protected readonly nameBytes: Signal<number> = computed((): number => ENCODER.encode(this.nameValue().trim()).length);

  protected readonly nameTooLong: Signal<boolean> = computed((): boolean => {
    const limits: Limits | null = this.limits();
    return limits !== null && this.nameBytes() > limits.nameBytes;
  });

  protected readonly nameHint: Signal<string> = computed((): string => {
    const limits: Limits | null = this.limits();

    return this.nameTooLong() && limits !== null
      ? `Too long: ${this.nameBytes()} of ${limits.nameBytes} bytes. The name becomes a file name, and file systems allow 255 bytes.`
      : 'Becomes the folder name, and the file name of a film.';
  });

  protected readonly linksProblem: Signal<string | null> = computed((): string | null => {
    const limits: Limits | null = this.limits();
    const length: number = this.linksValue().length;

    return limits !== null && length > limits.linksLength
      ? `Too many characters: ${length} of ${limits.linksLength}. Add the rest as a second download.`
      : null;
  });

  protected readonly request: Signal<AnalyzeRequest | null> = computed((): AnalyzeRequest | null => {
    const links: string = this.linksValue();
    if (!links.trim() || this.linksProblem() !== null) return null;

    return { kind: this.kindValue(), links, format: this.formatValue().trim() || null };
  });

  protected readonly seasonNumber: Signal<number | null> = computed((): number | null => {
    if (!this.isSeason()) return null;

    const typed: string = this.seasonValue().trim();
    if (typed) {
      const parsed: number = Number(typed);
      return Number.isInteger(parsed) && parsed >= 0 && parsed <= 999 ? parsed : null;
    }

    return this.analysis()?.season ?? 1;
  });

  protected readonly seasonInvalid: Signal<boolean> = computed((): boolean => this.isSeason() && this.seasonNumber() === null);

  protected readonly formatMissing: Signal<boolean> = computed((): boolean => {
    const analysis: Analysis | null = this.analysis();
    return this.isSeason() && analysis !== null && analysis.format === null;
  });

  protected readonly formatHint: Signal<string> = computed((): string => {
    const analysis: Analysis | null = this.analysis();

    if (!this.isSeason()) return '';
    if (analysis?.format) return analysis.formatDetected ? `Detected: ${analysis.format}` : `Using: ${analysis.format}`;
    if (analysis) return 'Not detected. Enter the text in front of the episode number, for example Show.S01E';

    return 'Found automatically once the links are in. Leave empty unless it fails.';
  });

  protected readonly episodes: Signal<AnalyzedEpisode[]> = computed((): AnalyzedEpisode[] => this.analysis()?.episodes ?? []);

  protected readonly partCount: Signal<number> = computed((): number =>
    this.episodes().reduce((sum: number, episode: AnalyzedEpisode): number => sum + episode.parts.length, 0));

  protected readonly unmatched: Signal<string[]> = computed((): string[] => this.analysis()?.unmatched ?? []);

  protected readonly valid: Signal<boolean> = computed((): boolean =>
    this.nameValue().trim().length > 0
    && !this.nameTooLong()
    && this.linksProblem() === null
    && this.episodes().length > 0
    && this.unmatched().length === 0
    && !this.seasonInvalid()
    && !this.analyzing()
    && !this.submitting());

  protected readonly passwordHint: Signal<string> = computed((): string => {
    const count: number = this.config()?.defaultPasswords ?? 0;

    return count === 0
      ? 'Optional. Leave it empty when the archives are not protected.'
      : `Optional. The ${count} default ${count === 1 ? 'password' : 'passwords'} from docker-compose ${count === 1 ? 'is' : 'are'} tried automatically; you are only asked if none of them opens an archive.`;
  });

  protected readonly unpackHint: Signal<string> = computed((): string => this.unpackValue()
    ? 'The archives are unpacked and sorted as soon as they are downloaded.'
    : 'The download is done once the archives are on disk. Unpack them later with the Unpack button.');

  constructor() {
    void inject(ConfigService).load();

    effect((onCleanup: (cleanup: () => void) => void): void => {
      const request: AnalyzeRequest | null = this.request();

      if (!request) {
        this.analysisTicket++;
        this.analysisValue.set(null);
        this.analysisProblem.set(null);
        this.analysisRunning.set(false);
        return;
      }

      this.analysisRunning.set(true);

      const handle: ReturnType<typeof setTimeout> = setTimeout((): void => void this.analyze(request), ANALYZE_DELAY_MS);
      onCleanup((): void => clearTimeout(handle));
    });
  }

  protected setName(event: Event): void {
    this.nameValue.set((event.target as HTMLInputElement).value);
  }

  protected setKind(kind: string): void {
    this.kindValue.set(kind as ReleaseKind);
  }

  protected setLinks(event: Event): void {
    this.linksValue.set((event.target as HTMLTextAreaElement).value);
  }

  protected setFormat(event: Event): void {
    this.formatValue.set((event.target as HTMLInputElement).value);
  }

  protected setSeason(event: Event): void {
    this.seasonValue.set((event.target as HTMLInputElement).value);
  }

  protected stepSeason(season: number): void {
    this.seasonValue.set(String(season));
  }

  protected setPassword(event: Event): void {
    this.passwordValue.set((event.target as HTMLInputElement).value);
  }

  protected setUnpack(checked: boolean): void {
    this.unpackValue.set(checked);
  }

  protected togglePassword(): void {
    this.passwordShown.update((value: boolean): boolean => !value);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    if (!this.valid()) return;

    this.sending.set(true);
    this.sendProblem.set(null);

    try {
      const job: Job = await this.downloads.create({
        name: this.nameValue().trim(),
        kind: this.kindValue(),
        links: this.linksValue(),
        format: this.formatValue().trim() || null,
        season: this.seasonNumber(),
        password: this.passwordValue() || null,
        unpack: this.unpackValue()
      });

      this.sync.apply(job);
      this.notifications.success(`${job.name} is queued`);

      await this.router.navigateByUrl('/downloads');
    } catch (error: unknown) {
      this.sendProblem.set(errorMessage(error, 'The download could not be created.'));
    } finally {
      this.sending.set(false);
    }
  }

  private async analyze(request: AnalyzeRequest): Promise<void> {
    const ticket: number = ++this.analysisTicket;

    try {
      const result: Analysis = await this.downloads.analyze(request);
      if (ticket !== this.analysisTicket) return;

      this.analysisValue.set(result);
      this.analysisProblem.set(null);
    } catch (error: unknown) {
      if (ticket !== this.analysisTicket) return;

      this.analysisValue.set(null);
      this.analysisProblem.set(errorMessage(error, 'The links could not be checked.'));
    } finally {
      if (ticket === this.analysisTicket) this.analysisRunning.set(false);
    }
  }
}