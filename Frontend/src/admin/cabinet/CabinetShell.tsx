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
  SearchIcon,
  TruckIcon,
  TagIcon,
  ClockIcon,
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

const PAGE_TITLES: Record<string, { title: string; subtitle: string }> = {
  '/admin': { title: 'Дашборд', subtitle: 'Обзор магазина за сегодня' },
  '/admin/pos': { title: 'Касса', subtitle: 'Сканируйте штрихкод и оформляйте продажи' },
  '/admin/inventory': { title: 'Склад', subtitle: 'Остатки и приход товаров' },
  '/admin/supply': { title: 'Поставки', subtitle: 'Поставщики, заказы и перемещения между магазинами' },
  '/admin/marketing': { title: 'Маркетинг', subtitle: 'Акции, наборы товаров, скоро истекает и ответы на отзывы' },
  '/admin/staff': { title: 'Сотрудники', subtitle: 'Сотрудники магазина и кассовые смены' },
  '/admin/reports': { title: 'Отчёты', subtitle: 'Выручка, прибыль и динамика продаж' },
  '/admin/settings': { title: 'Настройки', subtitle: 'Магазин, профиль и безопасность' },
}

/** Closes a popover/menu on an outside pointer event or Escape — the one
 *  piece of plumbing every floating panel below needs. */
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

/** A horizontal tab, not a sidebar row: the active mark is a 2px underline
 *  riding the baseline rather than a filled pill behind the label, so the
 *  nav reads as wayfinding chrome rather than a list of destinations. */
function TabItem({
  to,
  label,
  icon: Icon,
  end,
  layoutId,
  onNavigate,
}: {
  to: string
  label: string
  icon: (props: { width: number; height: number }) => ReactNode
  end?: boolean
  layoutId: string
  onNavigate?: () => void
}) {
  return (
    <NavLink to={to} end={end} onClick={onNavigate} className="group relative shrink-0">
      {({ isActive }) => (
        <span className="relative flex items-center gap-2 px-3.5 py-2.5">
          <Icon width={16} height={16} />
          <span
            className={clsx(
              'whitespace-nowrap text-[13.5px] tracking-tight transition-colors duration-200',
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

/** Same shift open/close logic as before, presented as a status pill that
 *  opens a small popover instead of a permanently-visible sidebar block —
 *  chrome real estate now belongs to navigation, not a standing widget. */
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

  useEffect(() => {
    load()
  }, [load])

  const myOpenShift = shifts?.find((s) => s.cashierUserId === user?.userId && !s.endedAt) ?? null

  async function handleToggleShift() {
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
          'flex items-center gap-2 rounded-full border px-3.5 py-2 text-[12px] font-semibold transition-colors duration-200',
          myOpenShift
            ? 'border-[color:var(--admin-accent)]/30 bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]'
            : 'border-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]',
        )}
      >
        <ClockIcon width={14} height={14} />
        {myOpenShift ? 'Смена открыта' : 'Смена закрыта'}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -6, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.18, ease: [0.16, 1, 0.3, 1] }}
            className="absolute right-0 top-[calc(100%+10px)] z-50 w-[260px] rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] p-4 [box-shadow:var(--admin-shadow-lift)]"
          >
            <div className="mb-1 truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
            <div className="mb-3 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
              {myOpenShift
                ? `На смене с ${new Date(myOpenShift.startedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}`
                : 'Смена не открыта'}
            </div>
            <input
              type="number"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder={myOpenShift ? 'Сумма в кассе' : 'Начальная сумма'}
              className="mb-2 w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[12px] text-[color:var(--admin-text)] outline-none transition-[border-color,box-shadow] duration-200 focus:border-[color:var(--admin-accent)] focus:shadow-[0_0_0_3px_var(--admin-accent-soft)]"
            />
            {error && <div className="mb-2 text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
            <button
              onClick={handleToggleShift}
              disabled={busy}
              className={clsx(
                'w-full rounded-xl py-2 text-[12px] font-semibold text-white transition-opacity disabled:opacity-50',
                myOpenShift ? 'bg-[color:var(--admin-danger)]' : 'bg-[color:var(--admin-accent)]',
              )}
            >
              {busy ? 'Секунду…' : myOpenShift ? 'Закрыть смену' : 'Открыть смену'}
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

/** Replaces the old sidebar-footer identity block: a click target that opens
 *  a small menu with role, email and sign-out, rather than a static row that
 *  was always on screen taking up rail space. */
function AccountMenu() {
  const { user, logout, currentStoreRole } = useAuth()
  const [open, setOpen] = useState(false)
  const ref = useDismiss(open, () => setOpen(false))
  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'
  const roleLabel = currentStoreRole === 'Cashier' ? 'Кассир' : user?.roles.includes('StorePartner') ? 'Владелец' : 'Пользователь'

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 rounded-full py-1 pl-1 pr-2.5 transition-colors duration-200 hover:bg-[color:var(--admin-hover)]"
      >
        <span
          className="grid h-8 w-8 place-items-center rounded-full text-[13px] font-bold text-white"
          style={{ background: 'linear-gradient(135deg, var(--admin-accent), color-mix(in srgb, var(--admin-accent) 65%, black))' }}
        >
          {initial}
        </span>
        <ChevronDownIcon
          width={14}
          height={14}
          className={clsx('hidden text-[color:var(--admin-text-tertiary)] transition-transform duration-200 sm:block', open && 'rotate-180')}
        />
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -6, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.18, ease: [0.16, 1, 0.3, 1] }}
            className="absolute right-0 top-[calc(100%+10px)] z-50 w-[220px] overflow-hidden rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] [box-shadow:var(--admin-shadow-lift)]"
          >
            <div className="border-b border-[color:var(--admin-border)] px-4 py-3">
              <div className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
              <div className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">{roleLabel}</div>
            </div>
            <button
              onClick={logout}
              className="flex w-full items-center gap-2.5 px-4 py-3 text-left text-[13px] font-medium text-[color:var(--admin-text-secondary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]"
            >
              <LogOutIcon width={16} height={16} />
              Выйти
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

/** Cross-fade + settle between pages. */
function PageTransition({ pathKey, children }: { pathKey: string; children: ReactNode }) {
  const reduce = useReducedMotion()
  if (reduce) return <>{children}</>
  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={pathKey}
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -6 }}
        transition={{ duration: 0.35, ease: [0.16, 1, 0.3, 1] }}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  )
}

/**
 * The StorePartner cabinet's shell, rebuilt around a command-bar
 * architecture instead of a fixed left rail: brand, horizontal tabs, and
 * utilities all live in one sticky bar, and the page title/subtitle run as
 * a second, thinner strip beneath it. On narrow screens the tabs collapse
 * into a horizontally-scrolling strip and the utility cluster shrinks to
 * icon-only — there is no drawer, no overlay, no slide-in panel.
 */
export function CabinetShell() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'
  const location = useLocation()
  const { currentStoreRole } = useAuth()
  const page = PAGE_TITLES[location.pathname] ?? PAGE_TITLES['/admin']
  const visibleNavItems = NAV_ITEMS.filter((item) => currentStoreRole !== 'Cashier' || !item.ownerOnly)
  const tabLayoutId = useId()
  const bottomTabLayoutId = useId()
  const [searchOpen, setSearchOpen] = useState(false)
  const searchRef = useDismiss(searchOpen, () => setSearchOpen(false))

  return (
    <div className="cabinet-shell admin-shell flex h-screen w-full flex-col overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <style>{`.cabinet-tabs::-webkit-scrollbar { display: none; } .cabinet-tabs { scrollbar-width: none; }`}</style>
      {/* Row 1: brand · nav tabs · utilities */}
      <header
        className="relative z-20 flex shrink-0 items-center gap-5 border-b border-[color:var(--admin-border)] px-5 backdrop-blur-xl"
        style={{ background: 'color-mix(in srgb, var(--admin-sidebar) 86%, transparent)', height: '60px' }}
      >
        <Link to="/" className="flex shrink-0 items-center gap-2.5">
          <LogoMark size={26} />
          <span className="hidden text-[16px] font-extrabold tracking-tight sm:inline">Sarfkor</span>
        </Link>

        <div className="mx-1 hidden h-6 w-px shrink-0 bg-[color:var(--admin-border)] lg:block" />

        <nav className="cabinet-tabs hidden min-w-0 flex-1 items-center gap-0.5 overflow-x-auto lg:flex">
          {visibleNavItems.map((item) => (
            <TabItem key={item.to} to={item.to} label={item.label} icon={item.icon} end={item.end} layoutId={tabLayoutId} />
          ))}
        </nav>
        <div className="min-w-0 flex-1 lg:hidden" />

        <div className="flex shrink-0 items-center gap-2.5">
          <div ref={searchRef} className="relative hidden sm:block">
            <AnimatePresence initial={false} mode="wait">
              {searchOpen ? (
                <motion.div
                  key="input"
                  initial={{ width: 36, opacity: 0 }}
                  animate={{ width: 220, opacity: 1 }}
                  exit={{ width: 36, opacity: 0 }}
                  transition={{ duration: 0.22, ease: [0.16, 1, 0.3, 1] }}
                  className="overflow-hidden"
                >
                  <input
                    autoFocus
                    type="text"
                    placeholder="Поиск..."
                    className="w-full rounded-full border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] py-2 pl-3.5 pr-3.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
                  />
                </motion.div>
              ) : (
                <motion.button
                  key="icon"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  onClick={() => setSearchOpen(true)}
                  aria-label="Поиск"
                  className="grid h-9 w-9 place-items-center rounded-full text-[color:var(--admin-text-tertiary)] transition-colors hover:bg-[color:var(--admin-hover)]"
                >
                  <SearchIcon width={16} height={16} />
                </motion.button>
              )}
            </AnimatePresence>
          </div>

          <div className="hidden md:block">
            <ShiftControl />
          </div>

          <button
            onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
            aria-label="Переключить тему"
            className="grid h-9 w-9 shrink-0 place-items-center rounded-full text-[color:var(--admin-text-secondary)] transition-colors duration-300 hover:bg-[color:var(--admin-hover)]"
          >
            {isDark ? <SunIcon width={16} height={16} /> : <MoonIcon width={16} height={16} />}
          </button>

          <AccountMenu />
        </div>
      </header>

      {/* Row 2: page title/subtitle — a thin strip, not a section of the sidebar. */}
      <div className="hidden shrink-0 items-baseline gap-3 border-b border-[color:var(--admin-border)] px-5 py-3.5 lg:flex">
        <h1 className="text-[17px] font-extrabold tracking-tight text-[color:var(--admin-text)]">{page.title}</h1>
        <p className="truncate text-[12.5px] text-[color:var(--admin-text-tertiary)]">{page.subtitle}</p>
      </div>

      <main className="flex-1 overflow-y-auto px-5 py-6 pb-24 sm:px-7 sm:py-7 lg:pb-7">
        <div className="mb-5 lg:hidden">
          <h1 className="text-[19px] font-extrabold tracking-tight text-[color:var(--admin-text)]">{page.title}</h1>
          <p className="text-[13px] text-[color:var(--admin-text-tertiary)]">{page.subtitle}</p>
        </div>
        <PageTransition pathKey={location.pathname}>
          <Outlet />
        </PageTransition>
      </main>

      {/* Mobile: a fixed icon-only tab bar instead of a slide-in drawer. */}
      <nav
        className="fixed inset-x-0 bottom-0 z-20 flex items-center justify-around border-t border-[color:var(--admin-border)] px-1 pb-[max(6px,env(safe-area-inset-bottom))] pt-1.5 backdrop-blur-xl lg:hidden"
        style={{ background: 'color-mix(in srgb, var(--admin-sidebar) 92%, transparent)' }}
      >
        {visibleNavItems.map((item) => (
          <NavLink key={item.to} to={item.to} end={item.end} className="relative flex flex-1 flex-col items-center gap-1 py-1.5">
            {({ isActive }) => (
              <>
                {isActive && (
                  <motion.span
                    layoutId={bottomTabLayoutId}
                    className="absolute top-0 h-[2px] w-8 rounded-full bg-[color:var(--admin-accent)]"
                    transition={{ type: 'spring', stiffness: 420, damping: 34 }}
                  />
                )}
                <item.icon width={19} height={19} />
                <span
                  className={clsx(
                    'text-[9.5px] font-medium leading-none',
                    isActive ? 'text-[color:var(--admin-accent)]' : 'text-[color:var(--admin-text-tertiary)]',
                  )}
                  style={isActive ? { fontWeight: 700 } : undefined}
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
