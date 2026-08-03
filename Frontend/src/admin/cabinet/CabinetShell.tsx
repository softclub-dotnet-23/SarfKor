import { useCallback, useEffect, useId, useRef, useState, type ReactNode } from 'react'
import { NavLink, Outlet, Link, useLocation } from 'react-router-dom'
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import clsx from 'clsx'
import { LogoMark } from '../../components/Logo'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { salesApi, ApiError, type CashierShift } from '../../lib/api'
import {
  GridIcon,
  RegisterIcon,
  PackageIcon,
  UsersIcon,
  ReportIcon,
  SettingsIcon,
  LogOutIcon,
  TruckIcon,
  TagIcon,
} from '../components/icons'
import { CommandPalette } from '../components/CommandPalette'
import { NotificationBell } from '../components/NotificationBell'
import { AssistantPanel } from '../components/AssistantPanel'

// ─── collapsed-state persistence ─────────────────────────────────────────────
const COLLAPSED_KEY = 'sarfkor-sidebar-collapsed'

function readCollapsed(): boolean {
  try { return localStorage.getItem(COLLAPSED_KEY) === '1' } catch { return false }
}
function saveCollapsed(v: boolean) {
  try { localStorage.setItem(COLLAPSED_KEY, v ? '1' : '0') } catch {}
}

// ─── ChevronLeft icon (not in shared icons) ──────────────────────────────────
function ChevronLeftIcon({ width = 16, height = 16 }: { width?: number; height?: number }) {
  return (
    <svg width={width} height={height} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <polyline points="15 18 9 12 15 6" />
    </svg>
  )
}

function StoreIcon({ width = 16, height = 16 }: { width?: number; height?: number }) {
  return (
    <svg width={width} height={height} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/>
      <polyline points="9 22 9 12 15 12 15 22"/>
    </svg>
  )
}

function SearchIcon({ width = 15, height = 15 }: { width?: number; height?: number }) {
  return (
    <svg width={width} height={height} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <circle cx="11" cy="11" r="8"/>
      <line x1="21" y1="21" x2="16.65" y2="16.65"/>
    </svg>
  )
}

// ─── Navigation items ─────────────────────────────────────────────────────────
const NAV_ITEMS = [
  { to: '/admin', label: 'Дашборд', icon: GridIcon, end: true, ownerOnly: true },
  { to: '/admin/pos', label: 'Касса', icon: RegisterIcon, ownerOnly: false },
  { to: '/admin/inventory', label: 'Склад', icon: PackageIcon, ownerOnly: false },
  { to: '/admin/supply', label: 'Поставки', icon: TruckIcon, ownerOnly: true },
  { to: '/admin/marketing', label: 'Маркетинг', icon: TagIcon, ownerOnly: true },
  { to: '/admin/staff', label: 'Сотрудники', icon: UsersIcon, ownerOnly: true },
  { to: '/admin/reports', label: 'Отчёты', icon: ReportIcon, ownerOnly: true },
  { to: '/admin/settings', label: 'Настройки', icon: SettingsIcon, ownerOnly: true },
]

// ─── Shift control popover ────────────────────────────────────────────────────
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

const EASE = [0.16, 1, 0.3, 1] as const

function ShiftBadge({ collapsed }: { collapsed: boolean }) {
  const { storeId, user } = useAuth()
  const [open, setOpen] = useState(false)
  const [shifts, setShifts] = useState<CashierShift[] | null>(null)
  const [amount, setAmount] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const ref = useDismiss(open, () => setOpen(false))

  const load = useCallback(async () => {
    if (!storeId) return
    try {
      const res = await salesApi.getCashierShifts(storeId)
      setShifts(res.shifts ?? [])
    } catch { setShifts([]) }
  }, [storeId])

  useEffect(() => { load() }, [load])

  const myOpenShift = shifts?.find((s) => s.cashierUserId === user?.userId && !s.endedAt) ?? null
  const isOpen = !!myOpenShift

  async function handleToggle() {
    if (!storeId) return
    setBusy(true); setError('')
    try {
      if (myOpenShift) {
        await salesApi.closeCashierShift(myOpenShift.cashierShiftId, Number(amount) || 0)
      } else {
        await salesApi.openCashierShift(storeId, Number(amount) || 0, 'TJS')
      }
      setAmount(''); await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось обновить смену')
    } finally { setBusy(false) }
  }

  if (!storeId) return null

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        title={collapsed ? (isOpen ? 'Смена открыта' : 'Смена закрыта') : undefined}
        className={clsx(
          'flex items-center gap-2 rounded-xl border transition-all duration-200',
          collapsed ? 'h-9 w-9 justify-center' : 'w-full px-3 py-2',
          isOpen
            ? 'border-[color:var(--admin-success-dim)] bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]'
            : 'border-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)] hover:border-[color:var(--admin-border-strong)] hover:text-[color:var(--admin-text-secondary)]',
        )}
      >
        <span className={clsx('h-1.5 w-1.5 shrink-0 rounded-full', isOpen ? 'bg-[color:var(--admin-success)]' : 'bg-current opacity-40')} />
        {!collapsed && (
          <span className="text-[11.5px] font-semibold">{isOpen ? 'Смена открыта' : 'Смена закрыта'}</span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, x: collapsed ? 8 : 0, y: collapsed ? 0 : 8, scale: 0.97 }}
            animate={{ opacity: 1, x: 0, y: 0, scale: 1 }}
            exit={{ opacity: 0, scale: 0.97 }}
            transition={{ duration: 0.18, ease: EASE }}
            className={clsx(
              'absolute z-50 w-[260px] rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] p-4',
              collapsed ? 'left-full ml-3 top-0' : 'bottom-full left-0 mb-2',
            )}
            style={{ boxShadow: 'var(--admin-shadow-lift)' }}
          >
            <div className="mb-0.5 text-[12px] font-bold uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">Управление сменой</div>
            <div className="mb-4 text-[12px] text-[color:var(--admin-text-secondary)]">
              {isOpen
                ? `Открыта в ${new Date(myOpenShift!.startedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}`
                : 'Смена не открыта'}
            </div>
            <input
              type="number"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder={isOpen ? 'Итоговая сумма в кассе' : 'Начальная сумма, TJS'}
              className="mb-3 w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[12.5px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]"
            />
            {error && <div className="mb-2 text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
            <button
              onClick={handleToggle}
              disabled={busy}
              className={clsx(
                'w-full rounded-xl py-2.5 text-[12.5px] font-bold transition-opacity disabled:opacity-40',
                isOpen ? 'bg-[color:var(--admin-danger)] text-white' : 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]',
              )}
            >
              {busy ? '…' : isOpen ? 'Закрыть смену' : 'Открыть смену'}
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

// ─── Sidebar nav item ─────────────────────────────────────────────────────────
function SideNavItem({
  to,
  label,
  icon: Icon,
  end,
  collapsed,
}: {
  to: string
  label: string
  icon: (p: { width: number; height: number }) => ReactNode
  end?: boolean
  collapsed: boolean
}) {
  return (
    <NavLink to={to} end={end} className="group relative block">
      {({ isActive }) => (
        <span
          title={collapsed ? label : undefined}
          className={clsx(
            'relative flex items-center gap-3 rounded-xl transition-all duration-150',
            collapsed ? 'h-9 w-9 justify-center' : 'h-9 px-3',
            isActive
              ? 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-text)]'
              : 'text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text-secondary)]',
          )}
        >
          {isActive && (
            <span className="absolute left-0 top-1/2 h-4 w-0.5 -translate-y-1/2 rounded-full bg-[color:var(--admin-accent)]" aria-hidden />
          )}
          <span className="shrink-0"><Icon width={16} height={16} /></span>
          {!collapsed && (
            <span className={clsx('truncate text-[13px]', isActive ? 'font-semibold' : 'font-medium')}>{label}</span>
          )}

          {/* Tooltip for collapsed state */}
          {collapsed && (
            <span
              aria-hidden
              className="pointer-events-none absolute left-full z-50 ml-3 whitespace-nowrap rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] px-2.5 py-1.5 text-[12px] font-semibold text-[color:var(--admin-text)] opacity-0 shadow-lg transition-opacity group-hover:opacity-100"
              style={{ boxShadow: 'var(--admin-shadow)' }}
            >
              {label}
            </span>
          )}
        </span>
      )}
    </NavLink>
  )
}

// ─── Page transition ──────────────────────────────────────────────────────────
function PageTransition({ pathKey, children }: { pathKey: string; children: ReactNode }) {
  const reduce = useReducedMotion()
  if (reduce) return <>{children}</>
  return (
    <AnimatePresence mode="wait" initial={false}>
      <motion.div
        key={pathKey}
        initial={{ opacity: 0, y: 6 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -3 }}
        transition={{ duration: 0.22, ease: EASE }}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  )
}

// ─── CabinetShell ─────────────────────────────────────────────────────────────
export function CabinetShell() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'
  const location = useLocation()
  const { user, logout, storeId, currentStoreRole } = useAuth()
  const bottomTabLayoutId = useId()

  // Collapsed state — remember across sessions, start collapsed on tablet
  const [collapsed, setCollapsed] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false
    return window.innerWidth < 1280 ? true : readCollapsed()
  })

  function toggleCollapsed() {
    setCollapsed((v) => { saveCollapsed(!v); return !v })
  }

  const visibleNavItems = NAV_ITEMS.filter((item) => currentStoreRole !== 'Cashier' || !item.ownerOnly)

  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'
  const roleLabel = currentStoreRole === 'Cashier' ? 'Кассир' : user?.roles.includes('Admin') ? 'Администратор' : user?.roles.includes('StorePartner') ? 'Владелец' : 'Пользователь'

  const SIDEBAR_W = collapsed ? 64 : 240

  return (
    <div className="cabinet-shell admin-shell flex h-screen w-full overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <CommandPalette />

      {/* ──────────────────────────────────────────────────────────────────────
          LEFT SIDEBAR — desktop/tablet only (hidden on mobile via CSS)
      ─────────────────────────────────────────────────────────────────────── */}
      <aside
        className="relative hidden shrink-0 flex-col border-r border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] md:flex"
        style={{
          width: SIDEBAR_W,
          transition: 'width 0.25s cubic-bezier(0.16,1,0.3,1)',
          overflow: 'hidden',
        }}
      >
        {/* ── Header: logo + collapse toggle ── */}
        <div
          className="flex shrink-0 items-center border-b border-[color:var(--admin-border)] px-3"
          style={{ height: 56, minWidth: SIDEBAR_W }}
        >
          <Link to="/" className={clsx('flex items-center gap-2.5', collapsed ? 'justify-center w-full' : '')}>
            <LogoMark size={22} />
            {!collapsed && (
              <span className="whitespace-nowrap text-[14.5px] font-extrabold tracking-tight">Sarfkor</span>
            )}
          </Link>
          {!collapsed && (
            <button
              onClick={toggleCollapsed}
              title="Свернуть"
              className="ml-auto grid h-7 w-7 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]"
            >
              <ChevronLeftIcon width={15} height={15} />
            </button>
          )}
        </div>

        {/* ── Expand button when collapsed ── */}
        {collapsed && (
          <button
            onClick={toggleCollapsed}
            title="Развернуть"
            className="absolute right-0 top-[72px] z-10 grid h-6 w-3 translate-x-full items-center justify-center rounded-r-md border border-l-0 border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]"
            style={{ cursor: 'e-resize' }}
          >
            <span className="text-[8px]">›</span>
          </button>
        )}

        {/* ── Navigation ── */}
        <nav className="flex flex-1 flex-col gap-0.5 overflow-y-auto overflow-x-hidden p-3">
          {/* Quick search hint */}
          {!collapsed && (
            <button
              className="mb-2 flex w-full items-center gap-2 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-left text-[12px] text-[color:var(--admin-text-tertiary)] transition-colors hover:border-[color:var(--admin-border-strong)] hover:text-[color:var(--admin-text-secondary)]"
              onClick={() => document.dispatchEvent(new CustomEvent('sarfkor:open-palette'))}
            >
              <SearchIcon width={13} height={13} />
              <span className="flex-1">Поиск…</span>
              <kbd className="rounded border border-[color:var(--admin-border)] px-1 py-0.5 text-[10px]">⌘K</kbd>
            </button>
          )}
          {collapsed && (
            <button
              title="Поиск (⌘K)"
              className="mb-2 grid h-9 w-9 place-items-center rounded-xl text-[color:var(--admin-text-tertiary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text-secondary)]"
              onClick={() => document.dispatchEvent(new CustomEvent('sarfkor:open-palette'))}
            >
              <SearchIcon width={16} height={16} />
            </button>
          )}

          {visibleNavItems.map((item) => (
            <SideNavItem
              key={item.to}
              to={item.to}
              label={item.label}
              icon={item.icon}
              end={item.end}
              collapsed={collapsed}
            />
          ))}
        </nav>

        {/* ── Footer: store info + user + theme ── */}
        <div className="shrink-0 border-t border-[color:var(--admin-border)] p-3">
          {/* Store info */}
          {storeId && (
            <div
              className={clsx(
                'mb-2 flex items-center gap-2.5 rounded-xl bg-[color:var(--admin-hover)] transition-all',
                collapsed ? 'h-9 w-9 justify-center' : 'px-3 py-2.5',
              )}
              title={collapsed ? `Магазин #${storeId}` : undefined}
            >
              <span className="shrink-0 text-[color:var(--admin-text-tertiary)]"><StoreIcon width={15} height={15} /></span>
              {!collapsed && (
                <div className="min-w-0">
                  <div className="truncate text-[11px] font-bold text-[color:var(--admin-text)]">Магазин #{storeId}</div>
                  <div className="text-[10px] text-[color:var(--admin-text-tertiary)]">{roleLabel}</div>
                </div>
              )}
            </div>
          )}

          {/* Shift control */}
          <div className={clsx('mb-2', collapsed && 'flex justify-center')}>
            <ShiftBadge collapsed={collapsed} />
          </div>

          {/* User row */}
          <div
            className={clsx(
              'mb-2 flex items-center gap-2.5 rounded-xl transition-all',
              collapsed ? 'h-9 w-9 justify-center' : 'px-2 py-1.5',
            )}
          >
            <span
              className="grid h-7 w-7 shrink-0 place-items-center rounded-full text-[12px] font-bold"
              style={{
                background: 'color-mix(in srgb, var(--admin-text) 14%, transparent)',
                color: 'var(--admin-text)',
              }}
            >
              {initial}
            </span>
            {!collapsed && (
              <div className="min-w-0 flex-1">
                <div className="truncate text-[12px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
              </div>
            )}
          </div>

          {/* Notification + Theme + Logout row */}
          <div className={clsx('flex gap-1', collapsed ? 'flex-col items-center' : 'items-center')}>
            <NotificationBell collapsed={collapsed} />
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              title={isDark ? 'Светлая тема' : 'Тёмная тема'}
              className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text-secondary)]"
            >
              {isDark ? <SunIcon width={15} height={15} /> : <MoonIcon width={15} height={15} />}
            </button>
            <button
              onClick={logout}
              title="Выйти"
              className="flex h-8 w-8 items-center justify-center rounded-lg text-[color:var(--admin-text-tertiary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-danger)]"
            >
              <LogOutIcon width={15} height={15} />
            </button>
          </div>
        </div>
      </aside>

      {/* ──────────────────────────────────────────────────────────────────────
          MAIN CONTENT AREA
      ─────────────────────────────────────────────────────────────────────── */}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        {/* Top bar — mobile only (md+ uses sidebar) */}
        <header
          className="flex shrink-0 items-center justify-between gap-3 border-b border-[color:var(--admin-border)] px-4 md:hidden"
          style={{
            height: 52,
            background: 'color-mix(in srgb, var(--admin-sidebar) 92%, transparent)',
            backdropFilter: 'blur(20px)',
            WebkitBackdropFilter: 'blur(20px)',
          }}
        >
          <Link to="/" className="flex items-center gap-2">
            <LogoMark size={20} />
            <span className="text-[14px] font-extrabold tracking-tight">Sarfkor</span>
          </Link>
          <div className="flex items-center gap-1.5">
            <ShiftBadge collapsed />
            <NotificationBell collapsed />
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              aria-label="Переключить тему"
              className="grid h-8 w-8 place-items-center rounded-full text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
            >
              {isDark ? <SunIcon width={14} height={14} /> : <MoonIcon width={14} height={14} />}
            </button>
            <button
              onClick={logout}
              aria-label="Выйти"
              className="grid h-8 w-8 place-items-center rounded-full text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-danger)]"
            >
              <LogOutIcon width={14} height={14} />
            </button>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto px-4 py-5 pb-20 sm:px-6 sm:py-6 md:pb-6 lg:px-8 lg:py-7">
          <PageTransition pathKey={location.pathname}>
            <Outlet />
          </PageTransition>
        </main>
      </div>

      {/* ──────────────────────────────────────────────────────────────────────
          MOBILE BOTTOM NAV — visible only on <md
      ─────────────────────────────────────────────────────────────────────── */}
      <nav
        className="fixed inset-x-0 bottom-0 z-30 flex items-center justify-around border-t border-[color:var(--admin-border)] px-1 pb-[max(6px,env(safe-area-inset-bottom))] pt-1 md:hidden"
        style={{
          background: 'color-mix(in srgb, var(--admin-sidebar) 95%, transparent)',
          backdropFilter: 'blur(24px)',
          WebkitBackdropFilter: 'blur(24px)',
        }}
      >
        {visibleNavItems.slice(0, 5).map((item) => (
          <NavLink key={item.to} to={item.to} end={item.end} className="relative flex flex-1 flex-col items-center gap-0.5 py-1.5">
            {({ isActive }) => (
              <>
                {isActive && (
                  <motion.span
                    layoutId={bottomTabLayoutId}
                    className="absolute top-0 h-[2px] w-8 rounded-full bg-[color:var(--admin-accent)]"
                    transition={{ type: 'spring', stiffness: 440, damping: 36 }}
                  />
                )}
                <item.icon width={19} height={19} />
                <span
                  className={clsx(
                    'text-[9px] font-semibold leading-none',
                    isActive ? 'text-[color:var(--admin-text)]' : 'text-[color:var(--admin-text-tertiary)]',
                  )}
                >
                  {item.label}
                </span>
              </>
            )}
          </NavLink>
        ))}
      </nav>

      <AssistantPanel />
    </div>
  )
}
