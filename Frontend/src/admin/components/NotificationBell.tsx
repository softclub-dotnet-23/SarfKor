import { useCallback, useEffect, useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { notificationsApi, type Notification } from '../../lib/api'

const EASE = [0.16, 1, 0.3, 1] as const
const POLL_MS = 60_000

const TYPE_LABEL: Record<string, string> = {
  LowStock: 'Низкий остаток',
  PriceDrop: 'Снижение цены',
  ReportResolved: 'Жалоба разрешена',
  ExpiringOfferPublished: 'Акция опубликована',
}

function BellIcon({ size = 16 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 01-3.46 0"/>
    </svg>
  )
}

function useDismiss(open: boolean, onDismiss: () => void) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    function onPointer(e: PointerEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) onDismiss()
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') onDismiss()
    }
    document.addEventListener('pointerdown', onPointer)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('pointerdown', onPointer)
      document.removeEventListener('keydown', onKey)
    }
  }, [open, onDismiss])
  return ref
}

export function NotificationBell({ collapsed }: { collapsed: boolean }) {
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<Notification[]>([])
  const [markingId, setMarkingId] = useState<number | null>(null)
  const ref = useDismiss(open, () => setOpen(false))

  const load = useCallback(async () => {
    try {
      const res = await notificationsApi.getNotifications()
      setItems(res.notifications ?? [])
    } catch { /* non-critical; stay silent */ }
  }, [])

  useEffect(() => {
    load()
    const interval = setInterval(load, POLL_MS)
    return () => clearInterval(interval)
  }, [load])

  const unread = items.filter((n) => !n.isRead).length

  async function markRead(notificationId: number) {
    setMarkingId(notificationId)
    try {
      await notificationsApi.markNotificationAsRead(notificationId)
      setItems((prev) => prev.map((n) => n.notificationId === notificationId ? { ...n, isRead: true } : n))
    } finally {
      setMarkingId(null)
    }
  }

  async function markAllRead() {
    const unreadItems = items.filter((n) => !n.isRead)
    await Promise.allSettled(unreadItems.map((n) => notificationsApi.markNotificationAsRead(n.notificationId)))
    setItems((prev) => prev.map((n) => ({ ...n, isRead: true })))
  }

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        title={collapsed ? `Уведомления${unread > 0 ? ` (${unread})` : ''}` : undefined}
        className={`relative flex items-center justify-center rounded-xl transition-colors duration-150 ${
          collapsed ? 'h-9 w-9' : 'h-9 w-9'
        } text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text-secondary)]`}
      >
        <BellIcon />
        {unread > 0 && (
          <span className="absolute right-1.5 top-1.5 flex h-[14px] min-w-[14px] items-center justify-center rounded-full bg-[color:var(--admin-danger)] px-[3px] text-[8px] font-bold text-white leading-none">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, x: collapsed ? 8 : 0, y: collapsed ? 0 : -8, scale: 0.97 }}
            animate={{ opacity: 1, x: 0, y: 0, scale: 1 }}
            exit={{ opacity: 0, scale: 0.97 }}
            transition={{ duration: 0.18, ease: EASE }}
            className={`absolute z-50 w-[300px] overflow-hidden rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] ${
              collapsed ? 'left-full ml-3 bottom-0' : 'bottom-full left-0 mb-2'
            }`}
            style={{ boxShadow: 'var(--admin-shadow-lift)' }}
          >
            {/* Header */}
            <div className="flex items-center justify-between border-b border-[color:var(--admin-border)] px-4 py-3">
              <span className="text-[12px] font-bold text-[color:var(--admin-text)]">Уведомления</span>
              {unread > 0 && (
                <button
                  onClick={markAllRead}
                  className="text-[11px] text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text-secondary)] transition-colors"
                >
                  Прочитать все
                </button>
              )}
            </div>

            {/* Items */}
            <div className="max-h-[320px] overflow-y-auto">
              {items.length === 0 && (
                <div className="px-4 py-6 text-center text-[12.5px] text-[color:var(--admin-text-tertiary)]">
                  Новых уведомлений нет
                </div>
              )}
              {items.slice(0, 20).map((n) => (
                <div
                  key={n.notificationId}
                  className={`border-b border-[color:var(--admin-border)] px-4 py-3 last:border-0 transition-colors ${
                    !n.isRead ? 'bg-[color:var(--admin-accent-soft)]' : ''
                  }`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <div className="text-[10.5px] font-bold uppercase tracking-[0.1em] text-[color:var(--admin-text-tertiary)]">
                        {TYPE_LABEL[n.type] ?? n.type}
                      </div>
                      <div className="mt-0.5 text-[12.5px] text-[color:var(--admin-text-secondary)] leading-snug">
                        {n.message}
                      </div>
                      <div className="mt-1 text-[10.5px] text-[color:var(--admin-text-tertiary)]">
                        {new Date(n.createdAt).toLocaleString('ru-RU', {
                          day: 'numeric',
                          month: 'short',
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </div>
                    </div>
                    {!n.isRead && (
                      <button
                        onClick={() => markRead(n.notificationId)}
                        disabled={markingId === n.notificationId}
                        className="mt-0.5 h-4 w-4 shrink-0 rounded-full bg-[color:var(--admin-accent)] opacity-70 transition-opacity hover:opacity-100 disabled:opacity-30"
                        title="Отметить прочитанным"
                      />
                    )}
                  </div>
                </div>
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
