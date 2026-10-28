import { Component, inject, Signal } from '@angular/core';
import { Notification } from '../../data/notification';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent {

  private readonly notificationService: NotificationService = inject(NotificationService);

  protected readonly notifications: Signal<Notification[]> = this.notificationService.notifications;

  protected dismiss(id: number): void {
    this.notificationService.dismiss(id);
  }
}