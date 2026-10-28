export type NotificationKind = 'info' | 'success' | 'warning' | 'failure';

export interface Notification {
  id: number;
  kind: NotificationKind;
  message: string;
}