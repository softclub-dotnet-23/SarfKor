import { useCallback, useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useAuth } from '../auth/AuthContext'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { useProfile, ProfileProvider } from '../lib/useProfile'
import { useAvatarUrl } from '../lib/useAvatarUrl'
import { SunIcon, MoonIcon } from '../components/icons'
import { notificationsApi, type Notification } from '../lib/api'
import { AppStyles, EASE, LINE, TXT } from './ui'

/**
 * Shell for the consumer half of the product.
 *
 * The navigation is numbered rather than icon-led because that is the landing's
 * own device — ACT 00 counts its words 001/002/003 — so the app reads as the next
 * reel of the same film instead of a dashboard bolted onto it. On desktop it is a
 * quiet rail; below lg it becomes a bottom bar, which is where a barcode-scanning
 * product actually gets used.
 */
const NAV = [
  { to: '/app', label: 'Главная', end: true },
  { to: '/app/scan', label: 'Сканировать', end: false },
  { to: '/app/lists', label: 'Списки', end: false },
  { to: '/app/favorites', label: 'Избранное', end: false },
  { to: '/app/alerts', label: 'Отслеживание', end: false },
]

const ROLE_LABEL: Record<string, string> = {
  Admin: 'Администратор',
  StorePartner: 'Партнёр',
  User: 'Покупатель',
}

function initials(email: string, name?: string | null) {
  const src = (name || email || '').trim()
  if (!src) return '—'
  const parts = src.split(/[\s@._-]+/).filter(Boolean)
  return (parts[0]?.[0] ?? '').concat(parts[1]?.[0] ?? '').toUpperCase() || src[0].toUpperCase()
}

function NavItem({
  to,
  label,
  end,
  index,
  onNavigate,
}: {
  to: string
  label: string
  end: boolean
  index: number
  onNavigate?: () => void
}) {
  return (
    <NavLink to={to} end={end} onClick={onNavigate} className="group relative block">
      {({ isActive }) => (
        <span className="relative flex items-baseline gap-3.5 py-2.5">
          {isActive && (
            <motion.span
              layoutId="sk-nav-active"
              aria-hidden
              className="absolute -left-8 top-1/2 h-[18px] w-px -translate-y-1/2"
              style={{ background: TXT.primary }}
              transition={{ duration: 0.45, ease: EASE }}
            />
          )}
          <span
            className="w-[18px] shrink-0 text-[10px] font-semibold tabular-nums tracking-[0.1em] transition-colors duration-500"
            style={{ color: isActive ? TXT.rest : 'var(--app-line)' }}
          >
            {String(index + 1).padStart(2, '0')}
          </span>
          <span
            className="text-[14.5px] font-semibold tracking-tight transition-colors duration-500"
            style={{ color: isActive ? TXT.primary : TXT.rest }}
          >
            {label}
          </span>
        </span>
      )}
    </NavLink>
  )
}

function BellIcon() {
  return (
    <svg width={15} height={15} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9" />
      <path d="M13.73 21a2 2 0 01-3.46 0" />
    </svg>
  )
}

function useOutsideDismiss(open: boolean, onDismiss: () => void) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    const onPtr = (e: PointerEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) onDismiss()
    }
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onDismiss() }
    document.addEventListener('pointerdown', onPtr)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('pointerdown', onPtr)
      document.removeEventListener('keydown', onKey)
    }
  }, [open, onDismiss])
  return ref
}

function AppNotificationBell({ direction = 'up' }: { direction?: 'up' | 'down' }) {
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<Notification[]>([])
  const ref = useOutsideDismiss(open, useCallback(() => setOpen(false), []))

  const load = useCallback(async () => {
    try {
      const res = await notificationsApi.getNotifications()
      setItems(res.notifications ?? [])
    } catch { /* non-critical */ }
  }, [])

  useEffect(() => {
    load()
    const t = setInterval(load, 60_000)
    return () => clearInterval(t)
  }, [load])

  async function markRead(id: number) {
    try {
      await notificationsApi.markNotificationAsRead(id)
      setItems((prev) => prev.map((n) => n.notificationId === id ? { ...n, isRead: true } : n))
    } catch { /* */ }
  }

  const unread = items.filter((n) => !n.isRead).length

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label={`Уведомления${unread > 0 ? ` (${unread})` : ''}`}
        className="relative grid h-7 w-7 place-items-center rounded-lg transition-colors duration-300"
        style={{ color: TXT.rest }}
      >
        <BellIcon />
        {unread > 0 && (
          <span
            className="absolute right-0.5 top-0.5 flex h-3.5 min-w-3.5 items-center justify-center rounded-full px-[2px] text-[7.5px] font-bold leading-none text-white"
            style={{ background: 'var(--app-text-primary)' }}
          >
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: direction === 'down' ? -6 : 6, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, scale: 0.97 }}
            transition={{ duration: 0.18, ease: EASE }}
            className={`absolute z-50 w-[280px] overflow-hidden rounded-2xl border ${
              direction === 'down' ? 'top-full left-0 mt-2' : 'bottom-full left-0 mb-2'
            }`}
            style={{
              borderColor: LINE,
              background: 'var(--bg-app)',
              boxShadow: '0 8px 40px rgba(0,0,0,0.18)',
            }}
          >
            <div className="border-b px-4 py-3" style={{ borderColor: LINE }}>
              <span className="text-[11.5px] font-bold" style={{ color: TXT.primary }}>
                Уведомления
              </span>
            </div>
            <div className="max-h-[300px] overflow-y-auto">
              {items.length === 0 && (
                <div className="px-4 py-5 text-center text-[12px]" style={{ color: TXT.rest }}>
                  Новых уведомлений нет
                </div>
              )}
              {items.slice(0, 15).map((n) => (
                <button
                  key={n.notificationId}
                  onClick={() => markRead(n.notificationId)}
                  className="w-full border-b px-4 py-3 text-left last:border-0 transition-colors duration-200 hover:bg-[color:var(--app-line-soft)]"
                  style={{ borderColor: LINE, opacity: n.isRead ? 0.55 : 1 }}
                >
                  <div className="text-[12.5px] font-medium leading-snug" style={{ color: TXT.primary }}>
                    {n.message}
                  </div>
                  <div className="mt-0.5 text-[10.5px]" style={{ color: TXT.rest }}>
                    {new Date(n.createdAt).toLocaleString('ru-RU', {
                      day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit',
                    })}
                  </div>
                </button>
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

export function AppShell() {
  return (
    <ProfileProvider>
      <AppShellInner />
    </ProfileProvider>
  )
}

function AppShellInner() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const { profile } = useProfile()
  const avatarUrl = useAvatarUrl(!!profile?.avatarReference, profile?.avatarReference)
  const isDark = theme === 'dark'

  const role = user?.roles?.includes('Admin')
    ? 'Admin'
    : user?.roles?.includes('StorePartner')
      ? 'StorePartner'
      : 'User'

  function handleLogout() {
    logout() // existing auth logic — clears tokens + store id, resets context
    navigate('/', { replace: true })
  }

  return (
    <div className="sk-app relative min-h-screen bg-[color:var(--bg-app)] text-[color:var(--app-text-primary)]">
      <AppStyles />

      {/* one soft key light, fixed to the viewport so pages scroll through it */}
      <div
        aria-hidden
        className="pointer-events-none fixed inset-0 z-0"
        style={{
          background:
            'radial-gradient(90vmax 70vmax at 78% -10%, color-mix(in srgb, var(--app-text-primary) 5.5%, transparent), transparent 60%)',
        }}
      />

      <div className="relative z-10 lg:grid lg:grid-cols-[264px_1fr]">
        {/* ── RAIL (desktop) ───────────────────────────────────── */}
        <aside
          className="sticky top-0 hidden h-screen flex-col justify-between border-r px-8 py-10 lg:flex"
          style={{ borderColor: LINE }}
        >
          <div>
            <NavLink to="/" className="mb-16 inline-flex items-center gap-3">
              <span className="grid h-8 w-8 place-items-center rounded-[10px] bg-[color:var(--app-text-primary)] text-[15px] font-extrabold tracking-tight text-[color:var(--bg-app)]">
                S
              </span>
              <span className="text-[16px] font-bold tracking-tight">Sarfkor</span>
            </NavLink>

            <nav className="flex flex-col">
              {NAV.map((n, i) => (
                <NavItem key={n.to} {...n} index={i} />
              ))}
            </nav>
          </div>

          <div>
            <div className="mb-5 h-px w-full" style={{ background: LINE }} />
            <NavLink to="/app/profile" className="mb-4 flex items-center gap-3">
              <span
                className="grid h-9 w-9 shrink-0 place-items-center overflow-hidden rounded-full text-[12px] font-bold"
                style={{ background: 'var(--app-line)', color: TXT.primary }}
              >
                {avatarUrl ? <img src={avatarUrl} alt="" className="h-full w-full object-cover" /> : initials(user?.email ?? '')}
              </span>
              <span className="min-w-0">
                <span className="block truncate text-[13px] font-semibold" style={{ color: TXT.primary }}>
                  {profile?.displayName || user?.email || '—'}
                </span>
                <span className="block text-[11px]" style={{ color: TXT.rest }}>
                  {ROLE_LABEL[role]}
                </span>
              </span>
            </NavLink>
            <div className="flex items-center gap-3 text-[12px]">
              <NavLink
                to="/app/settings"
                className="transition-colors duration-300 hover:text-[color:var(--app-text-primary)]"
                style={{ color: TXT.rest }}
              >
                Настройки
              </NavLink>
              <button
                onClick={handleLogout}
                className="transition-colors duration-300 hover:text-[color:var(--app-text-primary)]"
                style={{ color: TXT.rest }}
              >
                Выйти
              </button>
              <div className="ml-auto flex items-center gap-1.5">
                <AppNotificationBell />
                <button
                  onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
                  aria-label="Переключить тему"
                  className="grid h-7 w-7 shrink-0 place-items-center rounded-lg transition-colors duration-300"
                  style={{ color: TXT.rest }}
                >
                  {isDark ? <SunIcon width={14} height={14} /> : <MoonIcon width={14} height={14} />}
                </button>
              </div>
            </div>
          </div>
        </aside>

        {/* ── CONTENT ──────────────────────────────────────────── */}
        <main className="min-w-0 pb-28 lg:pb-0">
          {/* mobile top bar */}
          <div
            className="flex items-center justify-between border-b px-6 py-5 lg:hidden"
            style={{ borderColor: LINE }}
          >
            <NavLink to="/" className="flex items-center gap-2.5">
              <span className="grid h-7 w-7 place-items-center rounded-[9px] bg-[color:var(--app-text-primary)] text-[13px] font-extrabold text-[color:var(--bg-app)]">
                S
              </span>
              <span className="text-[15px] font-bold tracking-tight">Sarfkor</span>
            </NavLink>
            <div className="flex items-center gap-1.5">
              <AppNotificationBell direction="down" />
              <button
                onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
                aria-label="Переключить тему"
                className="grid h-8 w-8 shrink-0 place-items-center rounded-full"
                style={{ color: TXT.rest }}
              >
                {isDark ? <SunIcon width={15} height={15} /> : <MoonIcon width={15} height={15} />}
              </button>
              <NavLink
                to="/app/profile"
                className="grid h-8 w-8 place-items-center overflow-hidden rounded-full text-[11px] font-bold"
                style={{ background: 'var(--app-line)' }}
              >
                {avatarUrl ? <img src={avatarUrl} alt="" className="h-full w-full object-cover" /> : initials(user?.email ?? '')}
              </NavLink>
            </div>
          </div>

          <motion.div
            key={location.pathname}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.4, ease: EASE }}
            className="mx-auto w-full max-w-[880px] px-6 py-10 sm:px-9 lg:px-14 lg:py-16"
          >
            <Outlet />
          </motion.div>
        </main>
      </div>

      {/* ── BOTTOM BAR (mobile) ────────────────────────────────── */}
      <nav
        className="fixed inset-x-0 bottom-0 z-20 border-t px-2 pb-[env(safe-area-inset-bottom)] lg:hidden"
        style={{
          borderColor: LINE,
          background: 'color-mix(in srgb, var(--bg-app) 92%, transparent)',
          backdropFilter: 'blur(16px)',
        }}
      >
        <div className="flex items-stretch justify-around">
          {NAV.map((n) => (
            <NavLink key={n.to} to={n.to} end={n.end} className="min-w-0 flex-1">
              {({ isActive }) => (
                <span className="relative flex flex-col items-center gap-1.5 px-1 py-3">
                  {isActive && (
                    <motion.span
                      layoutId="sk-nav-active-m"
                      aria-hidden
                      className="absolute inset-x-3 top-0 h-px"
                      style={{ background: TXT.primary }}
                      transition={{ duration: 0.4, ease: EASE }}
                    />
                  )}
                  <span
                    className="truncate text-[10.5px] font-semibold tracking-tight transition-colors duration-300"
                    style={{ color: isActive ? TXT.primary : TXT.rest }}
                  >
                    {n.label}
                  </span>
                </span>
              )}
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  )
}
