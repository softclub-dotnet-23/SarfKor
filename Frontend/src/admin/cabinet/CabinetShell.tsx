import { useCallback, useEffect, useId, useRef, useState, type CSSProperties, type ReactNode } from 'react'
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

// ─── Tooltip state ────────────────────────────────────────────────────────────
interface TooltipState { label: string; y: number }

// ─── Icons ────────────────────────────────────────────────────────────────────
function ChevronLeftIcon() {
  return (
    <svg width={13} height={13} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.2} strokeLinecap="round" strokeLinejoin="round">
      <polyline points="15 18 9 12 15 6" />
    </svg>
  )
}
function ChevronRightIcon() {
  return (
    <svg width={13} height={13} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.2} strokeLinecap="round" strokeLinejoin="round">
      <polyline points="9 18 15 12 9 6" />
    </svg>
  )
}
function StoreIcon() {
  return (
    <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/>
      <polyline points="9 22 9 12 15 12 15 22"/>
    </svg>
  )
}
function SearchIcon() {
  return (
    <svg width={15} height={15} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <circle cx="11" cy="11" r="8"/>
      <line x1="21" y1="21" x2="16.65" y2="16.65"/>
    </svg>
  )
}

// ─── Nav items config ─────────────────────────────────────────────────────────
const NAV_ITEMS = [
  { to: '/admin',           label: 'Дашборд',   icon: GridIcon,     end: true,  ownerOnly: true  },
  { to: '/admin/pos',       label: 'Касса',      icon: RegisterIcon, end: false, ownerOnly: false },
  { to: '/admin/inventory', label: 'Склад',      icon: PackageIcon,  end: false, ownerOnly: false },
  { to: '/admin/supply',    label: 'Поставки',   icon: TruckIcon,    end: false, ownerOnly: true  },
  { to: '/admin/marketing', label: 'Маркетинг',  icon: TagIcon,      end: false, ownerOnly: true  },
  { to: '/admin/staff',     label: 'Сотрудники', icon: UsersIcon,    end: false, ownerOnly: true  },
  { to: '/admin/reports',   label: 'Отчёты',     icon: ReportIcon,   end: false, ownerOnly: true  },
  { to: '/admin/settings',  label: 'Настройки',  icon: SettingsIcon, end: false, ownerOnly: true  },
]

// ─── useDismiss ───────────────────────────────────────────────────────────────
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
// Sidebar width constants — match index.css fallback and inline style
const W_EXPANDED = 240
const W_COLLAPSED = 64
// Left edge of tooltip (sidebar width + gap)
const TOOLTIP_LEFT = W_COLLAPSED + 12

// ─── ShiftBadge ───────────────────────────────────────────────────────────────
function ShiftBadge({ collapsed }: { collapsed: boolean }) {
  const { storeId, user } = useAuth()
  const [open, setOpen] = useState(false)
  const [shifts, setShifts] = useState<CashierShift[] | null>(null)
  const [amount, setAmount] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const ref = useDismiss(open, () => setOpen(false))
  const btnRef = useRef<HTMLButtonElement>(null)
  // Fixed-position coordinates used when collapsed (popover must escape overflow:hidden)
  const [fixedStyle, setFixedStyle] = useState<CSSProperties>({})

  const load = useCallback(async () => {
    if (!storeId) return
    try {
      const res = await salesApi.getCashierShifts(storeId)
      setShifts(res.shifts ?? [])
    } catch { setShifts([]) }
  }, [storeId])

  useEffect(() => { load() }, [load])
  useEffect(() => { if (!open) setError('') }, [open])

  const myOpenShift = shifts?.find((s) => s.cashierUserId === user?.userId && !s.endedAt) ?? null
  const isOpen = !!myOpenShift

  function handleOpen() {
    if (collapsed && btnRef.current) {
      const rect = btnRef.current.getBoundingClientRect()
      // Anchor popover's top to button's top; sidebar edge + gap gives left
      setFixedStyle({ position: 'fixed', top: rect.top, left: TOOLTIP_LEFT, zIndex: 9999 })
    }
    setOpen((v) => !v)
  }

  async function handleToggle() {
    if (!storeId) return
    setBusy(true); setError('')
    try {
      if (myOpenShift) {
        await salesApi.closeCashierShift(myOpenShift.cashierShiftId, Number(amount) || 0)
      } else {
        await salesApi.openCashierShift(storeId, Number(amount) || 0, 'TJS')
      }
      setAmount(''); await load(); setOpen(false)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось обновить смену')
    } finally { setBusy(false) }
  }

  if (!storeId) return null

  return (
    <div ref={ref} className="relative">
      <button
        ref={btnRef}
        onClick={handleOpen}
        aria-label={isOpen ? 'Смена открыта — управление сменой' : 'Смена закрыта — управление сменой'}
        title={isOpen ? 'Смена открыта' : 'Смена закрыта'}
        className={clsx(
          'flex items-center gap-2 rounded-[6px] border transition-colors duration-150',
          collapsed ? 'h-9 w-9 justify-center' : 'w-full px-3 py-2',
          isOpen
            ? 'border-[color:var(--admin-success-dim)] bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]'
            : 'border-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)] hover:border-[color:var(--admin-border-strong)] hover:text-[color:var(--admin-text-secondary)]',
        )}
      >
        <span className={clsx('h-1.5 w-1.5 shrink-0 rounded-full', isOpen ? 'bg-[color:var(--admin-success)]' : 'bg-current opacity-40')} aria-hidden />
        {!collapsed && (
          <span className="text-[12px] font-[500]">{isOpen ? 'Смена открыта' : 'Смена закрыта'}</span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, scale: 0.97, y: collapsed ? 0 : 8 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.97 }}
            transition={{ duration: 0.18, ease: EASE }}
            // In collapsed mode: position:fixed escapes overflow:hidden on <aside>.
            // In expanded mode: position:absolute, bottom-full.
            className={clsx(
              'w-[260px] rounded-[12px] border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] p-4',
              !collapsed && 'absolute bottom-full left-0 mb-2',
            )}
            style={{
              boxShadow: 'var(--admin-shadow-lift)',
              ...(collapsed ? fixedStyle : {}),
            }}
          >
            <div className="mb-1 text-[11px] font-[600] uppercase tracking-[0.1em] text-[color:var(--admin-text-tertiary)]">Управление сменой</div>
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
              className="mb-3 w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]"
            />
            {error && <div className="mb-2 text-[11px] font-[500] text-[color:var(--admin-danger)]">{error}</div>}
            <button
              onClick={handleToggle}
              disabled={busy}
              className={clsx(
                'w-full rounded-[8px] py-2.5 text-[13px] font-[500] transition-opacity disabled:opacity-40',
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

// ─── SideNavItem ──────────────────────────────────────────────────────────────
// Tooltips are NOT rendered here — they come out of overflow:hidden via fixed
// position managed by CabinetShell. Only routing logic lives here.
function SideNavItem({
  to,
  label,
  icon: Icon,
  end,
  collapsed,
  onHover,
  onLeave,
}: {
  to: string
  label: string
  icon: (p: { width: number; height: number }) => ReactNode
  end?: boolean
  collapsed: boolean
  onHover: (label: string, y: number) => void
  onLeave: () => void
}) {
  function captureY(el: HTMLElement) {
    const r = el.getBoundingClientRect()
    onHover(label, r.top + r.height / 2)
  }

  return (
    <NavLink
      to={to}
      end={end}
      // Accessible name for collapsed mode — the visible label is hidden
      aria-label={collapsed ? label : undefined}
      className="group relative block"
      onMouseEnter={collapsed ? (e) => captureY(e.currentTarget as HTMLElement) : undefined}
      onMouseLeave={collapsed ? onLeave : undefined}
      onFocus={collapsed ? (e) => captureY(e.currentTarget as HTMLElement) : undefined}
      onBlur={collapsed ? onLeave : undefined}
    >
      {({ isActive }) => (
        <div
          className={clsx(
            'relative flex h-12 items-center transition-colors duration-150',
            collapsed ? 'w-full justify-center' : 'gap-3 px-4',
            isActive
              ? 'bg-[rgba(0,0,0,0.05)] text-[color:var(--admin-text)] dark:bg-[rgba(255,255,255,0.05)]'
              : 'text-[color:var(--admin-text-tertiary)] hover:bg-[rgba(0,0,0,0.04)] hover:text-[color:var(--admin-text-secondary)] dark:hover:bg-[rgba(255,255,255,0.04)]',
          )}
        >
          {/* 2px left accent — active only, full row height */}
          {isActive && (
            <span
              className="absolute inset-y-0 left-0 w-[2px] bg-[color:var(--admin-accent)]"
              aria-hidden
            />
          )}
          <span className="shrink-0" aria-hidden>
            <Icon width={18} height={18} />
          </span>
          {!collapsed && (
            <span className="truncate text-[14px] font-[500] leading-none">
              {label}
            </span>
          )}
        </div>
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

// ─── IconBtn ─────────────────────────────────────────────────────────────────
function IconBtn({
  onClick,
  title,
  danger,
  children,
}: {
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void
  title?: string
  danger?: boolean
  children: ReactNode
}) {
  return (
    <button
      onClick={onClick}
      title={title}
      aria-label={title}
      className={clsx(
        'grid h-9 w-9 place-items-center rounded-[6px] transition-colors duration-150',
        danger
          ? 'text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-danger-dim)] hover:text-[color:var(--admin-danger)]'
          : 'text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text-secondary)]',
      )}
    >
      {children}
    </button>
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

  const [collapsed, setCollapsed] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false
    return window.innerWidth < 1280 ? true : readCollapsed()
  })

  // Fixed-position tooltip for collapsed nav items — escapes overflow:hidden
  const [tooltip, setTooltip] = useState<TooltipState | null>(null)

  function toggleCollapsed() {
    setTooltip(null) // clear any showing tooltip before animating
    setCollapsed((v) => { saveCollapsed(!v); return !v })
  }

  // Safety-clear tooltip whenever collapsed state changes
  useEffect(() => { setTooltip(null) }, [collapsed])

  const visibleNavItems = NAV_ITEMS.filter((item) => currentStoreRole !== 'Cashier' || !item.ownerOnly)
  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'
  const roleLabel =
    currentStoreRole === 'Cashier' ? 'Кассир'
    : user?.roles.includes('Admin') ? 'Администратор'
    : user?.roles.includes('StorePartner') ? 'Владелец'
    : 'Пользователь'

  // Shared tooltip handlers — any element can call these
  const showTooltip = (label: string, y: number) => setTooltip({ label, y })
  const hideTooltip = () => setTooltip(null)

  return (
    <div className="cabinet-shell admin-shell flex h-screen w-full overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <CommandPalette />

      {/* ── FIXED-POSITION TOOLTIP ─────────────────────────────────────────
          Rendered outside <aside> so overflow:hidden cannot clip it.
          position:fixed escapes ALL overflow constraints.
      ──────────────────────────────────────────────────────────────────── */}
      <AnimatePresence>
        {collapsed && tooltip && (
          <motion.div
            key="sidebar-tooltip"
            initial={{ opacity: 0, x: -4 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: -4 }}
            transition={{ duration: 0.15, ease: 'easeOut' }}
            style={{
              position: 'fixed',
              left: TOOLTIP_LEFT,
              top: tooltip.y,
              transform: 'translateY(-50%)',
              zIndex: 9999,
              pointerEvents: 'none',
              boxShadow: 'var(--admin-shadow)',
            }}
            className="whitespace-nowrap rounded-[6px] border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] px-3 py-2 text-[12px] font-[500] text-[color:var(--admin-text)]"
            role="tooltip"
          >
            {tooltip.label}
          </motion.div>
        )}
      </AnimatePresence>

      {/* ── LEFT SIDEBAR ──────────────────────────────────────────────────── */}
      <aside
        aria-label="Боковая панель"
        className="relative hidden shrink-0 flex-col border-r border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] md:flex"
        style={{
          width: collapsed ? W_COLLAPSED : W_EXPANDED,
          transition: 'width 200ms ease-out',
          overflow: 'hidden',
          willChange: 'width',
        }}
      >
        {/* ── Header ──────────────────────────────────────────────────────── */}
        <div
          className="flex shrink-0 items-center border-b border-[color:var(--admin-border)]"
          style={{ height: 56 }}
        >
          {collapsed ? (
            // Collapsed: the entire header is the expand button.
            // Makes the affordance impossible to miss and keeps the logo visible.
            <button
              onClick={toggleCollapsed}
              title="Развернуть панель"
              aria-label="Развернуть боковую панель"
              aria-expanded={false}
              className="flex h-full w-full flex-col items-center justify-center gap-1 text-[color:var(--admin-text-tertiary)] transition-colors duration-150 hover:text-[color:var(--admin-text-secondary)]"
            >
              <LogoMark size={20} />
              <ChevronRightIcon />
            </button>
          ) : (
            // Expanded: logo (link to home) on left, collapse button on right
            <>
              <Link
                to="/"
                className="flex items-center gap-2.5 pl-4 text-[color:var(--admin-text)] transition-opacity duration-150 hover:opacity-80"
              >
                <LogoMark size={20} />
                <span className="whitespace-nowrap text-[14px] font-[600] tracking-tight">
                  Sarfkor
                </span>
              </Link>
              <button
                onClick={toggleCollapsed}
                title="Свернуть"
                aria-label="Свернуть боковую панель"
                aria-expanded={true}
                className="ml-auto mr-3 grid h-7 w-7 place-items-center rounded-[6px] text-[color:var(--admin-text-tertiary)] transition-colors duration-150 hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text-secondary)]"
              >
                <ChevronLeftIcon />
              </button>
            </>
          )}
        </div>

        {/* ── Search ──────────────────────────────────────────────────────── */}
        <div className={clsx('shrink-0', collapsed ? 'flex justify-center px-3 pt-3' : 'px-4 pt-3')}>
          {collapsed ? (
            <button
              aria-label="Поиск"
              onClick={() => document.dispatchEvent(new CustomEvent('sarfkor:open-palette'))}
              onMouseEnter={(e) => {
                const r = (e.currentTarget as HTMLElement).getBoundingClientRect()
                showTooltip('Поиск  ⌘K', r.top + r.height / 2)
              }}
              onMouseLeave={hideTooltip}
              onFocus={(e) => {
                const r = (e.currentTarget as HTMLElement).getBoundingClientRect()
                showTooltip('Поиск  ⌘K', r.top + r.height / 2)
              }}
              onBlur={hideTooltip}
              className="grid h-9 w-9 place-items-center rounded-[6px] text-[color:var(--admin-text-tertiary)] transition-colors duration-150 hover:bg-[rgba(0,0,0,0.04)] hover:text-[color:var(--admin-text-secondary)] dark:hover:bg-[rgba(255,255,255,0.04)]"
            >
              <SearchIcon />
            </button>
          ) : (
            <button
              aria-label="Открыть поиск"
              onClick={() => document.dispatchEvent(new CustomEvent('sarfkor:open-palette'))}
              className="flex w-full items-center gap-2.5 rounded-[6px] border border-[color:var(--admin-border)] px-3 py-2 text-left transition-colors duration-150 hover:border-[color:var(--admin-border-strong)] hover:bg-[color:var(--admin-hover)]"
            >
              <span className="text-[color:var(--admin-text-tertiary)]" aria-hidden><SearchIcon /></span>
              <span className="flex-1 text-[13px] font-[400] text-[color:var(--admin-text-tertiary)]">Поиск…</span>
              <kbd className="rounded-[4px] border border-[color:var(--admin-border)] px-1.5 py-0.5 text-[10px] font-[500] text-[color:var(--admin-text-tertiary)]">⌘K</kbd>
            </button>
          )}
        </div>

        {/* ── Navigation ──────────────────────────────────────────────────── */}
        <nav aria-label="Основная навигация" className="mt-2 flex flex-1 flex-col overflow-y-auto overflow-x-hidden">
          {visibleNavItems.map((item) => (
            <SideNavItem
              key={item.to}
              to={item.to}
              label={item.label}
              icon={item.icon}
              end={item.end}
              collapsed={collapsed}
              onHover={showTooltip}
              onLeave={hideTooltip}
            />
          ))}
        </nav>

        {/* ── Footer ──────────────────────────────────────────────────────── */}
        <div className="shrink-0 border-t border-[color:var(--admin-border)] p-3">
          {/* Store row */}
          {storeId && (
            <div
              className={clsx(
                'mb-2 flex items-center gap-2.5 rounded-[6px]',
                collapsed ? 'h-9 w-9 justify-center' : 'px-2 py-2',
              )}
              onMouseEnter={collapsed ? (e) => {
                const r = (e.currentTarget as HTMLElement).getBoundingClientRect()
                showTooltip(`Магазин #${storeId}`, r.top + r.height / 2)
              } : undefined}
              onMouseLeave={collapsed ? hideTooltip : undefined}
            >
              <span className="shrink-0 text-[color:var(--admin-text-tertiary)]" aria-hidden><StoreIcon /></span>
              {!collapsed && (
                <div className="min-w-0">
                  <div className="truncate text-[12px] font-[500] text-[color:var(--admin-text)]">Магазин #{storeId}</div>
                  <div className="text-[11px] font-[400] text-[color:var(--admin-text-tertiary)]">{roleLabel}</div>
                </div>
              )}
            </div>
          )}

          {/* Shift control */}
          <div className={clsx('mb-2', collapsed && 'flex justify-center')}>
            <ShiftBadge collapsed={collapsed} />
          </div>

          {/* User avatar + email */}
          <div
            className={clsx(
              'mb-2 flex items-center gap-2.5',
              collapsed ? 'justify-center' : 'px-1 py-1',
            )}
          >
            <span
              aria-label={`Пользователь: ${user?.email ?? ''}`}
              className="grid h-7 w-7 shrink-0 place-items-center rounded-full text-[12px] font-[600]"
              style={{
                background: 'color-mix(in srgb, var(--admin-text) 14%, transparent)',
                color: 'var(--admin-text)',
              }}
            >
              {initial}
            </span>
            {!collapsed && (
              <div className="min-w-0 flex-1">
                <div className="truncate text-[13px] font-[500] text-[color:var(--admin-text)]">{user?.email}</div>
              </div>
            )}
          </div>

          {/* Actions: notifications / theme / logout */}
          <div className={clsx('flex gap-1', collapsed ? 'flex-col items-center' : 'items-center')}>
            <NotificationBell collapsed={collapsed} />
            <IconBtn
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              title={isDark ? 'Светлая тема' : 'Тёмная тема'}
            >
              {isDark ? <SunIcon width={15} height={15} /> : <MoonIcon width={15} height={15} />}
            </IconBtn>
            <IconBtn onClick={logout} title="Выйти" danger>
              <LogOutIcon width={15} height={15} />
            </IconBtn>
          </div>
        </div>
      </aside>

      {/* ── MAIN CONTENT ──────────────────────────────────────────────────── */}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        {/* Mobile top bar */}
        <header
          className="flex shrink-0 items-center justify-between gap-3 border-b border-[color:var(--admin-border)] px-4 md:hidden"
          style={{
            height: 52,
            background: 'color-mix(in srgb, var(--admin-sidebar) 92%, transparent)',
            backdropFilter: 'blur(20px)',
            WebkitBackdropFilter: 'blur(20px)',
          }}
        >
          <Link to="/" className="flex items-center gap-2.5">
            <LogoMark size={20} />
            <span className="text-[14px] font-[600] tracking-tight">Sarfkor</span>
          </Link>
          <div className="flex items-center gap-1.5">
            <ShiftBadge collapsed />
            <NotificationBell collapsed />
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              aria-label={isDark ? 'Светлая тема' : 'Тёмная тема'}
              className="grid h-8 w-8 place-items-center rounded-[6px] text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
            >
              {isDark ? <SunIcon width={15} height={15} /> : <MoonIcon width={15} height={15} />}
            </button>
            <button
              onClick={logout}
              aria-label="Выйти"
              className="grid h-8 w-8 place-items-center rounded-[6px] text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-danger)]"
            >
              <LogOutIcon width={15} height={15} />
            </button>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto px-4 py-5 pb-20 sm:px-6 sm:py-6 md:pb-6 lg:px-8 lg:py-7">
          <PageTransition pathKey={location.pathname}>
            <Outlet />
          </PageTransition>
        </main>
      </div>

      {/* ── MOBILE BOTTOM NAV ─────────────────────────────────────────────── */}
      <nav
        aria-label="Мобильная навигация"
        className="fixed inset-x-0 bottom-0 z-30 flex items-center justify-around border-t border-[color:var(--admin-border)] px-1 pb-[max(6px,env(safe-area-inset-bottom))] pt-1 md:hidden"
        style={{
          background: 'color-mix(in srgb, var(--admin-sidebar) 95%, transparent)',
          backdropFilter: 'blur(24px)',
          WebkitBackdropFilter: 'blur(24px)',
        }}
      >
        {visibleNavItems.slice(0, 5).map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className="relative flex flex-1 flex-col items-center gap-0.5 py-1.5"
          >
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
                    'text-[9px] font-[500] leading-none',
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
