import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { NavLink, Outlet, Link, useLocation } from 'react-router-dom'
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import clsx from 'clsx'
import { LogoMark } from '../components/Logo'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../components/icons'
import { useAuth } from '../auth/AuthContext'
import { salesApi, ApiError, type CashierShift } from '../lib/api'
import { AssistantPanel } from './components/AssistantPanel'
import {
  GridIcon,
  RegisterIcon,
  PackageIcon,
  UsersIcon,
  ReportIcon,
  SettingsIcon,
  RefreshIcon,
  LogOutIcon,
  SearchIcon,
  TruckIcon,
  TagIcon,
} from './components/icons'

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

function ShiftCard() {
  const { storeId, user } = useAuth()
  const [shifts, setShifts] = useState<CashierShift[] | null>(null)
  const [amount, setAmount] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

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

  return (
    <div className="mt-3 rounded-2xl bg-[color:var(--admin-hover)] p-4">
      <div className="mb-2.5 flex items-center justify-between">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
          Смена
        </span>
        <button
          onClick={load}
          aria-label="Обновить"
          className="grid h-7 w-7 place-items-center rounded-lg bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]"
        >
          <RefreshIcon width={14} height={14} />
        </button>
      </div>
      <div className="mb-1 truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
      <div className="mb-3 text-xs text-[color:var(--admin-text-tertiary)]">
        {myOpenShift ? `На смене с ${new Date(myOpenShift.startedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}` : 'Смена не открыта'}
      </div>
      <input
        type="number"
        value={amount}
        onChange={(e) => setAmount(e.target.value)}
        placeholder={myOpenShift ? 'Сумма в кассе' : 'Начальная сумма'}
        className="mb-2 w-full rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[12px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
      />
      {error && <div className="mb-2 text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
      <button
        onClick={handleToggleShift}
        disabled={busy || !storeId}
        className={clsx(
          'w-full rounded-lg py-1.5 text-[12px] font-semibold text-white transition-opacity disabled:opacity-50',
          myOpenShift ? 'bg-[color:var(--admin-danger)]' : 'bg-[color:var(--admin-accent)]',
        )}
      >
        {busy ? 'Секунду…' : myOpenShift ? 'Закрыть смену' : 'Открыть смену'}
      </button>
    </div>
  )
}

/** Cross-fade + settle between pages — the same idle-free reveal language as
 *  the landing/consumer app, applied once here rather than per admin page. */
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

export function AdminLayout() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'
  const location = useLocation()
  const { user, logout, currentStoreRole } = useAuth()
  const page = PAGE_TITLES[location.pathname] ?? PAGE_TITLES['/admin']
  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'
  const visibleNavItems = NAV_ITEMS.filter((item) => currentStoreRole !== 'Cashier' || !item.ownerOnly)

  // RequireAuth (the parent route) redirects to /login the moment `user`
  // goes null, so logging out doesn't need its own navigate() — a manual one
  // here raced against that redirect and got clobbered by it anyway.
  function handleLogout() {
    logout()
  }

  return (
    <div className="admin-shell flex h-screen w-full overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      {/* Sidebar */}
      <aside
        className={clsx(
          'fixed inset-y-0 left-0 z-40 flex w-[240px] shrink-0 flex-col border-r border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] px-4 pb-5 pt-6 transition-transform duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] [box-shadow:var(--admin-shadow)] lg:static lg:shadow-none lg:translate-x-0',
          mobileNavOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <Link to="/" className="mb-8 flex items-center gap-2.5 px-2">
          <LogoMark size={28} />
          <span className="text-[19px] font-extrabold tracking-tight text-[color:var(--admin-text)]">Sarfkor</span>
        </Link>

        <nav className="flex flex-1 flex-col gap-1">
          {visibleNavItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              onClick={() => setMobileNavOpen(false)}
              className={({ isActive }) =>
                clsx(
                  'flex items-center gap-3 rounded-xl px-3.5 py-2.5 text-[14px] font-medium transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)]',
                  isActive
                    ? 'bg-[color:var(--admin-accent-soft)] font-semibold text-[color:var(--admin-accent)]'
                    : 'text-[color:var(--admin-text-secondary)] hover:translate-x-0.5 hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]',
                )
              }
            >
              <item.icon width={18} height={18} />
              {item.label}
            </NavLink>
          ))}
        </nav>

        <ShiftCard />

        <button
          onClick={handleLogout}
          className="mt-2 flex items-center gap-3 rounded-xl px-3.5 py-2.5 text-left text-[14px] font-medium text-[color:var(--admin-text-tertiary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]"
        >
          <LogOutIcon width={18} height={18} />
          Выйти
        </button>
      </aside>

      <AnimatePresence>
        {mobileNavOpen && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
            className="fixed inset-0 z-30 bg-black/45 backdrop-blur-sm lg:hidden"
            onClick={() => setMobileNavOpen(false)}
            aria-hidden
          />
        )}
      </AnimatePresence>

      {/* Content */}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header className="relative z-10 flex shrink-0 items-center gap-4 border-b border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] px-6 py-4 [box-shadow:0_1px_0_var(--admin-border),0_4px_16px_-8px_rgba(0,0,0,0.12)]">
          <button
            onClick={() => setMobileNavOpen(true)}
            aria-label="Меню"
            className="grid h-9 w-9 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)] lg:hidden"
          >
            <span className="flex w-4 flex-col gap-[4px]">
              <span className="block h-[1.5px] w-full bg-current" />
              <span className="block h-[1.5px] w-full bg-current" />
              <span className="block h-[1.5px] w-full bg-current" />
            </span>
          </button>

          <div className="min-w-0 flex-1">
            <h1 className="truncate text-[17px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
              {page.title}
            </h1>
            <p className="truncate text-[13px] text-[color:var(--admin-text-tertiary)]">{page.subtitle}</p>
          </div>

          <div className="relative hidden w-64 shrink-0 md:block">
            <SearchIcon
              width={16}
              height={16}
              className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)]"
            />
            <input
              type="text"
              placeholder="Поиск..."
              className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] py-2 pl-9 pr-3 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
            />
          </div>

          <button
            onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
            aria-label="Переключить тему"
            className="grid h-9 w-9 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
          >
            {isDark ? <SunIcon width={17} height={17} /> : <MoonIcon width={17} height={17} />}
          </button>

          <div className="hidden shrink-0 items-center gap-2.5 border-l border-[color:var(--admin-border)] pl-4 sm:flex">
            <div
              className="grid h-9 w-9 place-items-center rounded-xl text-[15px] font-bold text-white [box-shadow:var(--admin-shadow)]"
              style={{
                background:
                  'linear-gradient(135deg, var(--admin-accent), color-mix(in srgb, var(--admin-accent) 65%, black))',
              }}
            >
              {initial}
            </div>
            <div className="hidden lg:block">
              <div className="max-w-[160px] truncate text-[13px] font-semibold leading-tight text-[color:var(--admin-text)]">
                {user?.email}
              </div>
              <div className="text-[11px] leading-tight text-[color:var(--admin-text-tertiary)]">
                {currentStoreRole === 'Cashier' ? 'Кассир' : user?.roles.includes('StorePartner') ? 'Владелец' : 'Пользователь'}
              </div>
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-6">
          <PageTransition pathKey={location.pathname}>
            <Outlet />
          </PageTransition>
        </main>
      </div>

      <AssistantPanel />
    </div>
  )
}
