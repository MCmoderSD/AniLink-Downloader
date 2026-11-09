import { Service } from '@angular/core';
import { DownloadDraft } from '../data/download';

@Service()
export class DownloadDraftService {

  private draft: DownloadDraft | null = null;

  set(draft: DownloadDraft): void {
    this.draft = draft;
  }

  take(): DownloadDraft | null {
    const draft: DownloadDraft | null = this.draft;
    this.draft = null;

    return draft;
  }
}