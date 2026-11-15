import { Component, inject, Signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Notification, NOTIFICATION_ICONS, NotificationKind } from '../../data/notification';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-notifications',
  imports: [MatIconModule],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent {

  private readonly notificationService: NotificationService = inject(NotificationService);

  protected readonly notifications: Signal<Notification[]> = this.notificationService.notifications;

  protected icon(kind: NotificationKind): string {
    return NOTIFICATION_ICONS[kind];
  }

  protected dismiss(id: number): void {
    this.notificationService.dismiss(id);
  }

  protected hold(id: number): void {
    this.notificationService.hold(id);
  }

  protected release(id: number, event: Event): void {
    if ((event.currentTarget as HTMLElement).matches(':hover, :focus-within')) return;
    this.notificationService.release(id);
  }
}