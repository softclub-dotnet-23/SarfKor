import { apiFetch } from './client'

export type NotificationType = 'LowStock' | 'PriceDrop' | 'ReportResolved' | 'ExpiringOfferPublished'

export interface AppNotification {
  notificationId: number
  type: NotificationType
  message: string
  isRead: boolean
  createdAt: string
}

export function getNotifications() {
  return apiFetch<{ notifications: AppNotification[] }>('/api/notifications')
}

export function markNotificationAsRead(notificationId: number) {
  return apiFetch<void>(`/api/notifications/${notificationId}/read`, { method: 'POST' })
}
