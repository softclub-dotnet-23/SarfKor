import { apiFetch } from './client'

export type NotificationType = 'LowStock' | 'PriceDrop' | 'ReportResolved' | 'ExpiringOfferPublished'

export interface Notification {
  notificationId: number
  type: NotificationType
  message: string
  isRead: boolean
  createdAt: string
}

export function getNotifications() {
  return apiFetch<{ notifications: Notification[] }>('/api/notifications')
}

export function markNotificationAsRead(notificationId: number) {
  return apiFetch<{ outcome: 'MarkedRead' | 'NotFound' | 'Forbidden' }>(`/api/notifications/${notificationId}/read`, {
    method: 'POST',
  })
}
