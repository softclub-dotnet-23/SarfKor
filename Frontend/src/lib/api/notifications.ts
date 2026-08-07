import { apiFetch } from './client'

export type NotificationType = 'LowStock' | 'PriceDrop' | 'ReportResolved' | 'ExpiringOfferPublished'

export interface Notification {
  notificationId: number
  type: NotificationType
  message: string
  isRead: boolean
  createdAt: string
}

export interface GetNotificationsResult {
  notifications: Notification[]
}

export function getNotifications() {
  return apiFetch<GetNotificationsResult>('/api/notifications')
}

export function markNotificationAsRead(notificationId: number) {
  return apiFetch<{ outcome: string }>(`/api/notifications/${notificationId}/read`, { method: 'POST' })
}
