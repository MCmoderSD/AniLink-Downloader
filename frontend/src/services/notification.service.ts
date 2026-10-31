import { Service, signal, Signal, WritableSignal } from '@angular/core';
import { Notification, NotificationKind } from '../data/notification';

const LIFETIME_MS: Record<NotificationKind, number> = {
  info: 4000,
  success: 5000,
  warning: 9000,
  failure: 12000
};

const MAX_VISIBLE: number = 5;

@Service()
export class NotificationService {

  private readonly entries: WritableSignal<Notification[]> = signal<Notification[]>([]);

  private readonly timers: Map<number, ReturnType<typeof setTimeout>> = new Map<number, ReturnType<typeof setTimeout>>();

  private nextId: number = 0;

  readonly notifications: Signal<Notification[]> = this.entries.asReadonly();

  info(message: string): void {
    this.push('info', message);
  }

  success(message: string): void {
    this.push('success', message);
  }

  warning(message: string): void {
    this.push('warning', message);
  }

  failure(message: string): void {
    this.push('failure', message);
  }

  dismiss(id: number): void {
    this.clearTimer(id);
    this.entries.update((list: Notification[]): Notification[] => list.filter((entry: Notification): boolean => entry.id !== id));
  }

  // The timer stands still while a message is under the pointer or holds focus, so it cannot vanish mid-sentence;
  // letting go starts the full time again.
  hold(id: number): void {
    this.clearTimer(id);
  }

  release(id: number): void {
    const notification: Notification | undefined = this.entries().find((entry: Notification): boolean => entry.id === id);
    if (!notification || this.timers.has(id)) return;

    this.schedule(id, notification.kind);
  }

  private push(kind: NotificationKind, message: string): void {
    const id: number = this.nextId++;
    const next: Notification[] = [...this.entries(), { id, kind, message }];

    for (const dropped of next.slice(0, Math.max(0, next.length - MAX_VISIBLE))) {
      this.clearTimer(dropped.id);
    }

    this.entries.set(next.slice(-MAX_VISIBLE));
    this.schedule(id, kind);
  }

  private schedule(id: number, kind: NotificationKind): void {
    this.timers.set(id, setTimeout((): void => this.dismiss(id), LIFETIME_MS[kind]));
  }

  private clearTimer(id: number): void {
    const timer: ReturnType<typeof setTimeout> | undefined = this.timers.get(id);
    if (timer === undefined) return;

    clearTimeout(timer);
    this.timers.delete(id);
  }
}