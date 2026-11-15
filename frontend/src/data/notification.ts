export type NotificationKind = 'info' | 'success' | 'warning' | 'failure';

export const NOTIFICATION_ICONS: Record<NotificationKind, string> = {
  info: 'info_outline',
  success: 'check_circle_outline',
  warning: 'warning_amber',
  failure: 'error_outline'
};

export interface Notification {
  id: number;
  kind: NotificationKind;
  message: string;
}