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
  ChevronDownIcon,
} from '../components/icons'

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

const PAGE_META: Record<string, { title: string; subtitle: string }> = {
  '/admin': { title: 'Дашборд', subtitle: 'Обзор магазина за сегодня' },
  '/admin/pos': { title: 'Касса', subtitle: 'Сканируйте штрихкод и оформляйте продажи' },
  '/admin/inventory': { title: 'Склад', subtitle: 'Остатки и приход товаров' },
  '/admin/supply': { title: 'Поставки', subtitle: 'Поставщики, заказы и перемещения' },
  '/admin/marketing': { title: 'Маркетинг', subtitle: 'Акции, наборы и истекающие предложения' },
  '/admin/staff': { title: 'Сотрудники', subtitle: 'Кассиры и смены' },
  '/admin/reports': { title: 'Отчёты', subtitle: 'Выручка, прибыль и динамика продаж' },
  '/admin/settings': { title: 'Настройки', subtitle: 'Магазин, профиль и безопасность' },
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

const EASE = [0.16, 1, 0.3, 1] as const

/** Horizontal nav tab — active state is a 2px underline, not a filled pill. */
function TabItem({
  to,
  label,
  icon: Icon,
  end,
  layoutId,
}: {
  to: string
  label: string
  icon: (p: { width: number; height: number }) => ReactNode
  end?: boolean
  layoutId: string
}) {
  return (
    <NavLink to={to} end={end} className="group relative shrink-0">
      {({ isActive }) => (
        <span className="relative flex items-center gap-2 px-3.5 py-3">
          <Icon width={15} height={15} />
          <span
            className={clsx(
              'whitespace-nowrap text-[13px] tracking-tight transition-colors duration-200',
              isActive
                ? 'font-semibold text-[color:var(--admin-text)]'
                : 'font-medium text-[color:var(--admin-text-tertiary)] group-hover:text-[color:var(--admin-text-secondary)]',
            )}
          >
            {label}
          </span>
          {isActive && (
            <motion.span
              layoutId={layoutId}
              className="absolute inset-x-3 bottom-0 h-[2px] rounded-full bg-[color:var(--admin-accent)]"
              transition={{ type: 'spring', stiffness: 420, damping: 34 }}
            />
          )}
        </span>
      )}
    </NavLink>
  )
}

function ShiftControl() {
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
    } catch {
      setShifts([])
    }
  }, [storeId])

  useEffect(() => { load() }, [load])

  const myOpenShift = shifts?.find((s) => s.cashierUserId === user?.userId && !s.endedAt) ?? null

  async function handleToggle() {
    if (!storeId) return
    setBusy(true)
    setError('')
    try {
      if (myOpenShift) {
        await salesApi.closeCashierShift(myOpenShift.cashierShiftId, Number(amount) || 0)
      } else {
        await salesApi.openCashierShift(storeId, Number(amount) || 0, 'TJS')
      }
      setAmount('')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось обновить смену')
    } finally {
      setBusy(false)
    }
  }

  if (!storeId) return null

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className={clsx(
          'flex items-center gap-2 rounded-full border px-3.5 py-1.5 text-[11.5px] font-semibold transition-all duration-200',
          myOpenShift
            ? 'border-[color:var(--admin-accent)] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-text)]'
            : 'border-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)] hover:border-[color:var(--admin-border-strong)] hover:text-[color:var(--admin-text-secondary)]',
        )}
      >
        <span
          className={clsx(
            'h-1.5 w-1.5 rounded-full',
            myOpenShift ? 'bg-[color:var(--admin-success)]' : 'bg-[color:var(--admin-text-tertiary)]',
          )}
        />
        {myOpenShift ? 'Смена открыта' : 'Закрыта'}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -8, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -8, scale: 0.97 }}
            transition={{ duration: 0.2, ease: EASE }}
            className="absolute right-0 top-[calc(100%+10px)] z-50 w-[260px] rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] p-4"
            style={{ boxShadow: 'var(--admin-shadow-lift)' }}
          >
            <div className="mb-0.5 truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
            <div className="mb-4 text-[11px] text-[color:var(--admin-text-tertiary)]">
              {myOpenShift
                ? `На смене с ${new Date(myOpenShift.startedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}`
                : 'Смена не открыта'}
            </div>
            <input
              type="number"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder={myOpenShift ? 'Сумма в кассе' : 'Начальная сумма, TJS'}
              className="mb-3 w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[12.5px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]"
            />
            {error && <div className="mb-2 text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
            <button
              onClick={handleToggle}
              disabled={busy}
              className={clsx(
                'w-full rounded-xl py-2.5 text-[12.5px] font-bold transition-opacity disabled:opacity-40',
                myOpenShift
                  ? 'bg-[color:var(--admin-danger)] text-white'
                  : 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]',
              )}
            >
              {busy ? '…' : myOpenShift ? 'Закрыть смену' : 'Открыть смену'}
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

function AccountMenu() {
  const { user, logout, currentStoreRole } = useAuth()
  const [open, setOpen] = useState(false)
  const ref = useDismiss(open, () => setOpen(false))
  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'
  const roleLabel =
    currentStoreRole === 'Cashier' ? 'Кассир' : user?.roles.includes('StorePartner') ? 'Владелец' : 'Пользователь'

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 rounded-full py-1 pl-1 pr-2.5 transition-colors duration-200 hover:bg-[color:var(--admin-hover)]"
      >
        <span
          className="grid h-8 w-8 place-items-center rounded-full text-[13px] font-bold"
          style={{
            background: 'color-mix(in srgb, var(--admin-text) 12%, transparent)',
            color: 'var(--admin-text)',
          }}
        >
          {initial}
        </span>
        <ChevronDownIcon
          width={13}
          height={13}
          className={clsx(
            'hidden text-[color:var(--admin-text-tertiary)] transition-transform duration-200 sm:block',
            open && 'rotate-180',
          )}
        />
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -8, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -8, scale: 0.97 }}
            transition={{ duration: 0.2, ease: EASE }}
            className="absolute right-0 top-[calc(100%+10px)] z-50 w-[220px] overflow-hidden rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)]"
            style={{ boxShadow: 'var(--admin-shadow-lift)' }}
          >
            <div className="border-b border-[color:var(--admin-border)] px-4 py-3">
              <div className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">{roleLabel}</div>
            </div>
            <button
              onClick={logout}
              className="flex w-full items-center gap-2.5 px-4 py-3 text-left text-[13px] font-medium text-[color:var(--admin-text-secondary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]"
            >
              <LogOutIcon width={15} height={15} />
              Выйти
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

function PageTransition({ pathKey, children }: { pathKey: string; children: ReactNode }) {
  const reduce = useReducedMotion()
  if (reduce) return <>{children}</>
  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={pathKey}
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -4 }}
        transition={{ duration: 0.3, ease: EASE }}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  )
}

/**
 * StorePartner cabinet shell — film-dark monochrome identity.
 *
 * Command-bar architecture: brand → horizontal nav tabs → utilities in one
 * sticky strip. Below it a thin page-title band. Below that the content.
 * Mobile: same bar collapses to brand + icons; a fixed bottom tab bar takes
 * over navigation.
 */
export function CabinetShell() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'
  const location = useLocation()
  const { currentStoreRole } = useAuth()
  const page = PAGE_META[location.pathname] ?? PAGE_META['/admin']
  const visibleNavItems = NAV_ITEMS.filter((item) => currentStoreRole !== 'Cashier' || !item.ownerOnly)
  const tabLayoutId = useId()
  const bottomTabLayoutId = useId()

  return (
    <div className="cabinet-shell admin-shell flex h-screen w-full flex-col overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <style>{`.cabinet-tabs::-webkit-scrollbar{display:none}.cabinet-tabs{scrollbar-width:none}`}</style>

      {/* ── Command bar ── */}
      <header
        className="relative z-20 flex shrink-0 items-center gap-4 border-b border-[color:var(--admin-border)] px-5"
        style={{
          height: '56px',
          background: 'color-mix(in srgb, var(--admin-sidebar) 90%, transparent)',
          backdropFilter: 'blur(20px)',
          WebkitBackdropFilter: 'blur(20px)',
        }}
      >
        {/* Brand */}
        <Link to="/" className="flex shrink-0 items-center gap-2.5 py-1">
          <LogoMark size={24} />
          <span className="hidden text-[15px] font-extrabold tracking-tight sm:inline">Sarfkor</span>
        </Link>

        <div className="mx-0.5 hidden h-5 w-px shrink-0 bg-[color:var(--admin-border)] lg:block" />

        {/* Horizontal nav — visible on desktop */}
        <nav className="cabinet-tabs hidden min-w-0 flex-1 items-center overflow-x-auto lg:flex">
          {visibleNavItems.map((item) => (
            <TabItem key={item.to} to={item.to} label={item.label} icon={item.icon} end={item.end} layoutId={tabLayoutId} />
          ))}
        </nav>
        <div className="min-w-0 flex-1 lg:hidden" />

        {/* Utility cluster */}
        <div className="flex shrink-0 items-center gap-2">
          <div className="hidden md:block">
            <ShiftControl />
          </div>

          <button
            onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
            aria-label="Переключить тему"
            className="grid h-9 w-9 shrink-0 place-items-center rounded-full text-[color:var(--admin-text-secondary)] transition-colors duration-300 hover:bg-[color:var(--admin-hover)]"
          >
            {isDark ? <SunIcon width={15} height={15} /> : <MoonIcon width={15} height={15} />}
          </button>

          <AccountMenu />
        </div>
      </header>

      {/* ── Page title strip ── */}
      <div
        className="hidden shrink-0 items-baseline gap-3 border-b border-[color:var(--admin-border)] px-6 py-3 lg:flex"
        style={{ background: 'color-mix(in srgb, var(--admin-sidebar) 60%, transparent)' }}
      >
        <h1 className="text-[15px] font-extrabold tracking-tight text-[color:var(--admin-text)]">{page.title}</h1>
        <span className="text-[12px] text-[color:var(--admin-text-tertiary)]">{page.subtitle}</span>
      </div>

      {/* ── Page content ── */}
      <main className="flex-1 overflow-y-auto px-5 py-6 pb-24 sm:px-6 sm:py-7 lg:pb-7">
        <div className="mb-5 lg:hidden">
          <h1 className="text-[20px] font-extrabold tracking-tight text-[color:var(--admin-text)]">{page.title}</h1>
          <p className="text-[13px] text-[color:var(--admin-text-tertiary)]">{page.subtitle}</p>
        </div>
        <PageTransition pathKey={location.pathname}>
          <Outlet />
        </PageTransition>
      </main>

      {/* ── Mobile bottom tab bar ── */}
      <nav
        className="fixed inset-x-0 bottom-0 z-20 flex items-center justify-around border-t border-[color:var(--admin-border)] px-1 pb-[max(8px,env(safe-area-inset-bottom))] pt-1.5 lg:hidden"
        style={{
          background: 'color-mix(in srgb, var(--admin-sidebar) 94%, transparent)',
          backdropFilter: 'blur(20px)',
        }}
      >
        {visibleNavItems.map((item) => (
          <NavLink key={item.to} to={item.to} end={item.end} className="relative flex flex-1 flex-col items-center gap-1 py-1.5">
            {({ isActive }) => (
              <>
                {isActive && (
                  <motion.span
                    layoutId={bottomTabLayoutId}
                    className="absolute top-0 h-[2px] w-7 rounded-full bg-[color:var(--admin-accent)]"
                    transition={{ type: 'spring', stiffness: 420, damping: 34 }}
                  />
                )}
                <item.icon width={18} height={18} />
                <span
                  className={clsx(
                    'text-[9.5px] font-semibold leading-none',
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
    </div>
  )
}
