import { DOCUMENT } from '@angular/common';
import { inject, Service } from '@angular/core';
import { API_BASE } from './api.service';

export interface StreamListener {
  close(): void;
}

const CLOSED: StreamListener = { close: (): void => undefined };

@Service()
export class SseService {

  private readonly view: (Window & typeof globalThis) | null = inject(DOCUMENT).defaultView;

  // The browser reconnects an EventSource by itself, so `opened` fires again after every drop. That is the
  // moment to reload whatever the stream only describes changes to.
  open<T>(
    path: string,
    receive: (message: T) => void,
    opened: () => void = (): void => undefined,
    interrupted: () => void = (): void => undefined,
  ): StreamListener {
    const supported: typeof EventSource | undefined = this.view?.EventSource;
    if (!supported) return CLOSED;

    const source: EventSource = new supported(`${API_BASE}/${path}`);

    source.onopen = (): void => opened();
    source.onerror = (): void => interrupted();

    source.onmessage = (event: MessageEvent<string>): void => {
      try {
        receive(JSON.parse(event.data) as T);
      } catch {
        // A payload this build does not understand is not worth taking the page down for.
      }
    };

    return { close: (): void => source.close() };
  }
}
