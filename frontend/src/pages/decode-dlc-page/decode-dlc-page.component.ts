import { DOCUMENT } from '@angular/common';
import {
  afterNextRender,
  Component,
  computed,
  ElementRef,
  inject,
  Injector,
  signal,
  Signal,
  viewChild,
  WritableSignal
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { FileDropDirective } from '../../components/file-drop-component/file-drop.directive';
import { ScrollBarDirective } from '../../components/scroll-bar-component/scroll-bar.directive';
import {
  DlcContainer,
  DlcDetail,
  DlcPackage,
  downloadsText,
  filesOf,
  headerDetails,
  linksOf,
  packageDetails,
  packageLinks,
  sizeOf
} from '../../data/dlc';
import { AppConfig } from '../../data/download';
import { formatBytes } from '../../data/format';
import { copyText } from '../../services/clipboard';
import { ConfigService } from '../../services/config.service';
import { DlcService } from '../../services/dlc.service';
import { DownloadDraftService } from '../../services/download-draft.service';
import { errorMessage } from '../../services/http-error';
import { NotificationService } from '../../services/notification.service';
import { saveTextFile } from '../../services/save-file';

const DOWNLOADS_FILE: string = 'Downloads.txt';

function isDlcFile(file: File): boolean {
  return file.name.toLowerCase().endsWith('.dlc');
}

@Component({
  selector: 'app-decode-dlc-page',
  templateUrl: './decode-dlc-page.component.html',
  styleUrls: ['./decode-dlc-page.component.scss'],
  imports: [FileDropDirective, MatButtonModule, MatIconModule, ScrollBarDirective]
})
export class DecodeDlcPageComponent {

  private readonly dlc: DlcService = inject(DlcService);
  private readonly notifications: NotificationService = inject(NotificationService);
  private readonly document: Document = inject(DOCUMENT);
  private readonly configuration: ConfigService = inject(ConfigService);
  private readonly config: Signal<AppConfig | null> = this.configuration.config;
  private readonly injector: Injector = inject(Injector);
  private readonly drafts: DownloadDraftService = inject(DownloadDraftService);
  private readonly router: Router = inject(Router);

  private readonly chooseButton: Signal<ElementRef<HTMLElement> | undefined> = viewChild<HTMLElement, ElementRef<HTMLElement>>('choose', { read: ElementRef });
  private readonly resultTitle: Signal<ElementRef<HTMLElement> | undefined> = viewChild<ElementRef<HTMLElement>>('title');

  private readonly nameValue: WritableSignal<string> = signal('');
  private readonly containerValue: WritableSignal<DlcContainer | null> = signal<DlcContainer | null>(null);
  private readonly problemValue: WritableSignal<string | null> = signal<string | null>(null);
  private readonly running: WritableSignal<boolean> = signal(false);
  private readonly revealed: WritableSignal<ReadonlySet<string>> = signal<ReadonlySet<string>>(new Set<string>());

  protected readonly formatBytes: (bytes: number) => string = formatBytes;
  protected readonly packageDetails: (pack: DlcPackage) => DlcDetail[] = packageDetails;
  protected readonly packageLinks: (pack: DlcPackage) => string[] = packageLinks;

  protected readonly fileName: Signal<string> = this.nameValue.asReadonly();
  protected readonly container: Signal<DlcContainer | null> = this.containerValue.asReadonly();
  protected readonly problem: Signal<string | null> = this.problemValue.asReadonly();
  protected readonly decoding: Signal<boolean> = this.running.asReadonly();
  protected readonly shown: Signal<ReadonlySet<string>> = this.revealed.asReadonly();

  protected readonly limitText: Signal<string | null> = computed((): string | null => {
    const bytes: number | undefined = this.config()?.limits.dlcBytes;

    return bytes === undefined ? null : formatBytes(bytes);
  });

  protected readonly packageCount: Signal<number> = computed((): number => this.containerValue()?.packages.length ?? 0);

  protected readonly fileCount: Signal<number> = computed((): number => {
    const container: DlcContainer | null = this.containerValue();

    return container ? filesOf(container).length : 0;
  });

  protected readonly linkCount: Signal<number> = computed((): number => {
    const container: DlcContainer | null = this.containerValue();

    return container ? linksOf(container).length : 0;
  });

  protected readonly totalBytes: Signal<number> = computed((): number => {
    const container: DlcContainer | null = this.containerValue();

    return container ? sizeOf(container) : 0;
  });

  protected readonly details: Signal<DlcDetail[]> = computed((): DlcDetail[] => {
    const container: DlcContainer | null = this.containerValue();

    return container ? headerDetails(container.header) : [];
  });

  constructor() {
    void this.configuration.load();
  }

  protected pick(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const files: File[] = Array.from(input.files ?? []);

    input.value = '';

    void this.decodeFiles(files);
  }

  protected async decodeFiles(files: File[]): Promise<void> {
    if (this.running() || files.length === 0) return;

    const file: File | undefined = files.find(isDlcFile);

    if (!file) {
      this.reject(`${files[0].name} is not a .dlc file.`);
      return;
    }

    const limit: number | undefined = this.config()?.limits.dlcBytes;

    if (file.size === 0) {
      this.reject(`${file.name} is empty.`);
      return;
    }

    if (limit !== undefined && file.size > limit) {
      this.reject(`${file.name} is larger than ${formatBytes(limit)}.`);
      return;
    }

    if (files.length > 1) this.notifications.info(`Only ${file.name} is decoded, one file at a time.`);

    this.nameValue.set(file.name);
    this.containerValue.set(null);
    this.problemValue.set(null);
    this.revealed.set(new Set<string>());
    this.running.set(true);

    try {
      this.containerValue.set(await this.dlc.decode(file));
      this.focusAfterRender(this.resultTitle);
    } catch (error: unknown) {
      this.problemValue.set(errorMessage(error, `${file.name} could not be decoded.`));
    } finally {
      this.running.set(false);
    }
  }

  protected back(): void {
    this.nameValue.set('');
    this.containerValue.set(null);
    this.problemValue.set(null);
    this.revealed.set(new Set<string>());

    this.focusAfterRender(this.chooseButton);
  }

  protected toggleSecret(key: string): void {
    this.revealed.update((keys: ReadonlySet<string>): ReadonlySet<string> => {
      const next: Set<string> = new Set<string>(keys);

      if (!next.delete(key)) next.add(key);

      return next;
    });
  }

  protected async copySecret(secret: string): Promise<void> {
    if (await copyText(this.document, secret)) {
      this.notifications.success('Password copied.');
    } else {
      this.notifications.failure('The password could not be copied.');
    }
  }

  protected async saveLinks(): Promise<void> {
    const container: DlcContainer | null = this.containerValue();
    if (!container) return;

    try {
      const saved: string | null = await saveTextFile(this.document, DOWNLOADS_FILE, downloadsText(container));

      if (saved) this.notifications.success(`Saved ${saved}.`);
    } catch {
      this.notifications.failure('The links could not be saved.');
    }
  }

  protected async addDownload(pack: DlcPackage): Promise<void> {
    this.drafts.set({
      name: pack.name ?? '',
      links: packageLinks(pack).join('\n'),
      passwords: pack.passwords.filter((password: string): boolean => password.trim() !== '')
    });

    await this.router.navigateByUrl('/add-download');
  }

  private focusAfterRender(target: Signal<ElementRef<HTMLElement> | undefined>): void {
    afterNextRender((): void => target()?.nativeElement.focus(), { injector: this.injector });
  }

  private reject(text: string): void {
    this.containerValue.set(null);
    this.problemValue.set(text);
  }
}