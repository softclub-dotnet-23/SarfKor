import { useCallback, useEffect, useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { notificationsApi, ApiError, type AppNotification } from '../../lib/api'
import { BellIcon, CheckIcon } from './icons'

const TYPE_LABEL: Record<AppNotification['type'], string> = {
  LowStock: 'Низкий остаток',
  PriceDrop: 'Изменение цены',
  ReportResolved: 'Жалоба рассмотрена',
  ExpiringOfferPublished: 'Скоро истекает',
}

function timeAgo(iso: string) {
  const diffMs = Date.now() - new Date(iso).getTime()
  const minutes = Math.floor(diffMs / 60000)
  if (minutes < 1) return 'только что'
  if (minutes < 60) return `${minutes} мин назад`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} ч назад`
  return new Date(iso).toLocaleDateString('ru-RU')
}

/**
 * Header bell wired to the real /api/notifications endpoints. Nothing in the backend produces a
 * Notification row yet (no reorder/price-drop/moderation trigger writes one) — this is honest,
 * working plumbing against real data, not a mock; it will simply stay empty until a producer exists.
 */
export function NotificationBell() {
  const [items, setItems] = useState<AppNotification[] | null>(null)
  const [open, setOpen] = useState(false)
  const [error, setError] = useState('')
  const rootRef = useRef<HTMLDivElement>(null)

  const load = useCallback(async () => {
    try {
      const res = await notificationsApi.getNotifications()
      setItems(res.notifications)
      setError('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить уведомления')
    }
  }, [])

  useEffect(() => {
    load()
    const interval = setInterval(load, 60000)
    return () => clearInterval(interval)
  }, [load])

  useEffect(() => {
    if (!open) return
    function onClickOutside(e: MouseEvent) {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onClickOutside)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onClickOutside)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  const unreadCount = items?.filter((n) => !n.isRead).length ?? 0

  async function handleMarkRead(id: number) {
    setItems((cur) => cur?.map((n) => (n.notificationId === id ? { ...n, isRead: true } : n)) ?? null)
    try {
      await notificationsApi.markNotificationAsRead(id)
    } catch {
      load() // best-effort resync if the write failed after the optimistic flip
    }
  }

  return (
    <div ref={rootRef} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label={`Уведомления${unreadCount > 0 ? ` (${unreadCount} непрочитанных)` : ''}`}
        aria-expanded={open}
        className="relative grid h-9 w-9 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
      >
        <BellIcon width={17} height={17} />
        {unreadCount > 0 && (
          <span className="absolute right-1.5 top-1.5 grid h-4 min-w-4 place-items-center rounded-full bg-[color:var(--admin-danger)] px-1 text-[9px] font-bold leading-none text-white">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -8, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.18, ease: [0.16, 1, 0.3, 1] }}
            className="absolute right-0 top-full z-50 mt-2 w-[340px] max-w-[calc(100vw-2rem)] overflow-hidden rounded-2xl bg-[color:var(--admin-card)] ring-1 ring-[color:var(--admin-border)] [box-shadow:var(--admin-shadow-lift)]"
          >
            <div className="flex items-center justify-between border-b border-[color:var(--admin-border)] px-4 py-3">
              <span className="text-[13.5px] font-bold text-[color:var(--admin-text)]">Уведомления</span>
              {unreadCount > 0 && (
                <span className="text-[11px] font-medium text-[color:var(--admin-text-tertiary)]">{unreadCount} новых</span>
              )}
            </div>

            <div className="max-h-[360px] overflow-y-auto">
              {error && <div className="px-4 py-6 text-center text-[12.5px] text-[color:var(--admin-danger)]">{error}</div>}

              {!error && items === null && (
                <div className="px-4 py-6 text-center text-[12.5px] text-[color:var(--admin-text-tertiary)]">Загрузка…</div>
              )}

              {!error && items !== null && items.length === 0 && (
                <div className="flex flex-col items-center gap-2 px-4 py-8 text-center">
                  <BellIcon width={22} height={22} className="text-[color:var(--admin-text-tertiary)]" />
                  <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">Пока нет уведомлений</p>
                </div>
              )}

              {items?.map((n) => (
                <button
                  key={n.notificationId}
                  onClick={() => !n.isRead && handleMarkRead(n.notificationId)}
                  className="flex w-full items-start gap-2.5 border-b border-[color:var(--admin-border)] px-4 py-3 text-left last:border-b-0 hover:bg-[color:var(--admin-hover)]"
                >
                  <span
                    className={`mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full ${n.isRead ? 'bg-transparent' : 'bg-[color:var(--admin-accent)]'}`}
                    aria-hidden
                  />
                  <span className="min-w-0 flex-1">
                    <span className="mb-0.5 block text-[10px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                      {TYPE_LABEL[n.type] ?? n.type}
                    </span>
                    <span className="block text-[13px] leading-snug text-[color:var(--admin-text)]">{n.message}</span>
                    <span className="mt-1 block text-[11px] text-[color:var(--admin-text-tertiary)]">{timeAgo(n.createdAt)}</span>
                  </span>
                  {n.isRead && <CheckIcon width={13} height={13} className="mt-1 shrink-0 text-[color:var(--admin-text-tertiary)]" />}
                </button>
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
