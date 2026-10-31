import { Service, signal, Signal, WritableSignal } from '@angular/core';
import { AppConfig } from '../data/download';
import { ApiService } from './api.service';

@Service()
export class ConfigService extends ApiService {

  private readonly current: WritableSignal<AppConfig | null> = signal<AppConfig | null>(null);

  readonly config: Signal<AppConfig | null> = this.current.asReadonly();

  constructor() {
    super('config');
  }

  async load(): Promise<void> {
    if (this.current()) return;

    try {
      this.current.set(await this.get<AppConfig>());
    } catch {
      // The page works without it; it only explains what the server does with the form.
    }
  }
}