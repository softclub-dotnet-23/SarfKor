import { useCallback, useEffect, useState } from 'react'
import { ApiError } from '../../lib/api'
import { getNotifications, markNotificationAsRead, type Notification } from '../../lib/api/notifications'
import { BellIcon } from '../../components/icons'

const NOTIFICATION_TYPE_LABEL: Record<string, string> = {
  LowStock: 'Мало товара на складе',
  PriceDrop: 'Цена снизилась',
  ReportResolved: 'Жалоба рассмотрена',
  ExpiringOfferPublished: 'Новая акция с истекающим сроком',
}

function Card({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`rounded-3xl bg-[color:var(--bg-card)] p-6 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)] ${className}`}>
      {children}
    </div>
  )
}

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<Notification[] | null>(null)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getNotifications()
      const sorted = [...res.notifications].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      setNotifications(sorted)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить уведомления')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function markRead(notificationId: number) {
    setNotifications((cur) => cur && cur.map((n) => (n.notificationId === notificationId ? { ...n, isRead: true } : n)))
    try {
      await markNotificationAsRead(notificationId)
    } catch {
      // Revert the optimistic update — the read receipt didn't actually land.
      setNotifications((cur) => cur && cur.map((n) => (n.notificationId === notificationId ? { ...n, isRead: false } : n)))
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-1 text-[22px] font-extrabold tracking-tight">Уведомления</h1>
      <p className="mb-6 text-[14px] text-[color:var(--text-secondary)]">Обновления о ценах, акциях и ваших обращениях</p>

      <Card>
        {notifications === null && !error && <p className="text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p>}
        {error && <p className="text-[13px] text-[color:var(--text-secondary)]">{error}</p>}
        {notifications && notifications.length === 0 && (
          <p className="text-[13px] text-[color:var(--text-tertiary)]">Уведомлений пока нет</p>
        )}
        {notifications && notifications.length > 0 && (
          <div className="flex flex-col divide-y divide-[color:var(--border-subtle)]">
            {notifications.map((n) => (
              <button
                key={n.notificationId}
                onClick={() => !n.isRead && markRead(n.notificationId)}
                disabled={n.isRead}
                className={`flex w-full items-start gap-3 rounded-xl px-3 py-3.5 text-left transition-colors first:mt-0 last:mb-0 -mx-3 ${
                  n.isRead ? 'cursor-default' : 'bg-[color:var(--bg-section)] hover:bg-[color:var(--bg-section)]/70'
                }`}
              >
                <span
                  className={`mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-full ${
                    n.isRead ? 'bg-[color:var(--bg-section)] text-[color:var(--text-tertiary)]' : 'bg-[color:var(--color-brand-light)] text-[color:var(--color-brand)]'
                  }`}
                >
                  <BellIcon width={15} height={15} />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className={`text-[13px] font-bold ${n.isRead ? 'text-[color:var(--text-secondary)]' : 'text-[color:var(--text-primary)]'}`}>
                      {NOTIFICATION_TYPE_LABEL[n.type] ?? n.type}
                    </span>
                    {!n.isRead && <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-[color:var(--color-brand)]" />}
                  </div>
                  <p className="mt-0.5 text-[13.5px] text-[color:var(--text-secondary)]">{n.message}</p>
                  <p className="mt-1 text-[11.5px] text-[color:var(--text-tertiary)]">{new Date(n.createdAt).toLocaleString('ru-RU')}</p>
                </div>
              </button>
            ))}
          </div>
        )}
      </Card>
    </div>
  )
}
