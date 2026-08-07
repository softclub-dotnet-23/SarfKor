import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { NavLink, Outlet, Link, useLocation, useNavigate } from 'react-router-dom'
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import clsx from 'clsx'
import { LogoMark } from '../components/Logo'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../components/icons'
import { useAuth } from '../auth/AuthContext'
import { salesApi, ApiError, type CashierShift } from '../lib/api'
import { useProfile, ProfileProvider } from '../lib/useProfile'
import { useAvatarUrl } from '../lib/useAvatarUrl'
import { AssistantPanel } from './components/AssistantPanel'
import { NotificationBell } from './components/NotificationBell'
import { CommandPalette, type CommandPaletteItem } from './components/CommandPalette'
import { LanguageSwitcher } from './components/LanguageSwitcher'
import { CashierShell } from './CashierShell'
import { useT } from '../i18n/translations'
import { useLocaleFormat } from '../i18n/format'
import {
  GridIcon,
  RegisterIcon,
  PackageIcon,
  UsersIcon,
  ReportIcon,
  SettingsIcon,
  RefreshIcon,
  LogOutIcon,
  TruckIcon,
  TagIcon,
  ChevronLeftIcon,
} from './components/icons'

const SIDEBAR_COLLAPSED_KEY = 'sarfkor-sidebar-collapsed'

const NAV_ITEMS = [
  { to: '/admin', num: '01', key: 'partner.nav.dashboard', icon: GridIcon, end: true, ownerOnly: true },
  { to: '/admin/pos', num: '02', key: 'partner.nav.pos', icon: RegisterIcon, end: false, ownerOnly: false },
  { to: '/admin/inventory', num: '03', key: 'partner.nav.inventory', icon: PackageIcon, end: false, ownerOnly: false },
  { to: '/admin/supply', num: '04', key: 'partner.nav.supply', icon: TruckIcon, end: false, ownerOnly: true },
  { to: '/admin/marketing', num: '05', key: 'partner.nav.marketing', icon: TagIcon, end: false, ownerOnly: true },
  { to: '/admin/staff', num: '06', key: 'partner.nav.staff', icon: UsersIcon, end: false, ownerOnly: true },
  { to: '/admin/reports', num: '07', key: 'partner.nav.reports', icon: ReportIcon, end: false, ownerOnly: true },
  { to: '/admin/settings', num: '08', key: 'partner.nav.settings', icon: SettingsIcon, end: false, ownerOnly: true },
] as const

const PAGE_TITLE_KEYS: Record<string, { title: 'partner.page.dashboard.title' | 'partner.page.pos.title' | 'partner.page.inventory.title' | 'partner.page.supply.title' | 'partner.page.marketing.title' | 'partner.page.staff.title' | 'partner.page.reports.title' | 'partner.page.settings.title'; subtitle: 'partner.page.dashboard.subtitle' | 'partner.page.pos.subtitle' | 'partner.page.inventory.subtitle' | 'partner.page.supply.subtitle' | 'partner.page.marketing.subtitle' | 'partner.page.staff.subtitle' | 'partner.page.reports.subtitle' | 'partner.page.settings.subtitle' }> = {
  '/admin': { title: 'partner.page.dashboard.title', subtitle: 'partner.page.dashboard.subtitle' },
  '/admin/pos': { title: 'partner.page.pos.title', subtitle: 'partner.page.pos.subtitle' },
  '/admin/inventory': { title: 'partner.page.inventory.title', subtitle: 'partner.page.inventory.subtitle' },
  '/admin/supply': { title: 'partner.page.supply.title', subtitle: 'partner.page.supply.subtitle' },
  '/admin/marketing': { title: 'partner.page.marketing.title', subtitle: 'partner.page.marketing.subtitle' },
  '/admin/staff': { title: 'partner.page.staff.title', subtitle: 'partner.page.staff.subtitle' },
  '/admin/reports': { title: 'partner.page.reports.title', subtitle: 'partner.page.reports.subtitle' },
  '/admin/settings': { title: 'partner.page.settings.title', subtitle: 'partner.page.settings.subtitle' },
}

export function ShiftCard({ collapsed }: { collapsed: boolean }) {
  const { storeId, user } = useAuth()
  const t = useT()
  const { time } = useLocaleFormat()
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
      setError(err instanceof ApiError ? err.message : t('partner.shift.updateError'))
    } finally {
      setBusy(false)
    }
  }

  if (collapsed) return null

  return (
    <div className="mt-3 rounded-2xl bg-[color:var(--admin-hover)] p-4">
      <div className="mb-2.5 flex items-center justify-between">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
          {t('partner.shift.title')}
        </span>
        <button
          onClick={load}
          aria-label={t('partner.shell.refresh')}
          className="grid h-7 w-7 place-items-center rounded-lg bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]"
        >
          <RefreshIcon width={14} height={14} />
        </button>
      </div>
      <div className="mb-1 truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
      <div className="mb-3 text-xs text-[color:var(--admin-text-tertiary)]">
        {myOpenShift ? t('partner.shift.onSince', { time: time(myOpenShift.startedAt) }) : t('partner.shift.notOpen')}
      </div>
      <input
        type="number"
        value={amount}
        onChange={(e) => setAmount(e.target.value)}
        placeholder={myOpenShift ? t('partner.shift.amountInDrawer') : t('partner.shift.openingAmount')}
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
        {busy ? t('common.saving') : myOpenShift ? t('partner.shift.close') : t('partner.shift.open')}
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
  const { currentStoreRole } = useAuth()
  // Cashier: phone-in-hand, standing, one route (pos/inventory) at a time — a
  // desktop sidebar shell is the wrong tool entirely, not just a restyle of
  // this one. StorePartner (Owner) keeps the dense desktop console below.
  if (currentStoreRole === 'Cashier') {
    return (
      <ProfileProvider>
        <CashierShell />
      </ProfileProvider>
    )
  }
  return (
    <ProfileProvider>
      <AdminLayoutInner />
    </ProfileProvider>
  )
}

function AdminLayoutInner() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1')
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const t = useT()
  const isDark = theme === 'dark'
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const pageKeys = PAGE_TITLE_KEYS[location.pathname] ?? PAGE_TITLE_KEYS['/admin']

  function toggleCollapsed() {
    setCollapsed((c) => {
      localStorage.setItem(SIDEBAR_COLLAPSED_KEY, c ? '0' : '1')
      return !c
    })
  }

  // RequireAuth (the parent route) redirects to /login the moment `user`
  // goes null, so logging out doesn't need its own navigate() — a manual one
  // here raced against that redirect and got clobbered by it anyway.
  function handleLogout() {
    logout()
  }

  const paletteItems: CommandPaletteItem[] = [
    ...NAV_ITEMS.map((item) => ({
      id: item.to,
      label: t(item.key),
      icon: item.icon,
      action: () => navigate(item.to),
    })),
    {
      id: 'toggle-theme',
      label: isDark ? t('common.lightTheme') : t('common.darkTheme'),
      icon: isDark ? SunIcon : MoonIcon,
      hint: t('common.appearance'),
      action: () => toggleTheme(),
    },
    {
      id: 'logout',
      label: t('shell.logout'),
      icon: LogOutIcon,
      action: handleLogout,
    },
  ]

  return (
    <div className="admin-shell flex h-screen w-full overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      {/* Sidebar — same structure as the platform Admin console: logo plate +
          role chip, numbered nav rows, feature block, user card at the
          bottom. The collapse toggle is a StorePartner-only feature (the
          reference shell doesn't have one) kept for functional parity with
          before, just restyled to match. */}
      <aside
        className={clsx(
          'fixed inset-y-0 left-0 z-40 flex w-[246px] shrink-0 flex-col overflow-y-auto overflow-x-hidden border-r border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] transition-[transform,width] duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] lg:static lg:translate-x-0',
          mobileNavOpen ? 'translate-x-0' : '-translate-x-full',
          collapsed ? 'lg:w-[76px]' : 'lg:w-[246px]',
        )}
      >
        <div className={clsx('flex items-center gap-2.5 pb-4 pt-5', collapsed ? 'flex-col px-3' : 'px-5')}>
          <Link to="/" className="flex shrink-0 items-center gap-2.5 overflow-hidden">
            <div className="grid h-[34px] w-[34px] shrink-0 place-items-center rounded-[9px] bg-[color:var(--admin-accent)]">
              <LogoMark size={20} mono />
            </div>
            {!collapsed && (
              <div className="flex flex-col leading-none">
                <span className="whitespace-nowrap text-[16px] font-extrabold tracking-tight text-[color:var(--admin-text)]">Sarfkor</span>
                <span className="mt-1 w-fit rounded-[5px] bg-[color:var(--admin-accent-soft)] px-1.5 py-0.5 font-[JetBrains_Mono,monospace] text-[9.5px] font-semibold uppercase tracking-[.12em] text-[color:var(--admin-accent)]">
                  {t('partner.shell.owner')}
                </span>
              </div>
            )}
          </Link>
          <button
            onClick={toggleCollapsed}
            aria-label={collapsed ? t('partner.shell.expandMenu') : t('partner.shell.collapseMenu')}
            aria-expanded={!collapsed}
            className={clsx(
              'hidden h-7 w-7 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)] lg:grid',
              !collapsed && 'ml-auto',
            )}
          >
            <ChevronLeftIcon width={14} height={14} className={clsx('transition-transform duration-300', collapsed && 'rotate-180')} />
          </button>
        </div>

        <nav className="flex flex-1 flex-col gap-1 px-3 py-2">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              title={collapsed ? t(item.key) : undefined}
              onClick={() => setMobileNavOpen(false)}
              className={({ isActive }) =>
                clsx(
                  'relative flex items-center gap-3 rounded-[11px] px-3 py-2.5 text-[13.5px] font-semibold transition-colors duration-150',
                  collapsed && 'justify-center px-0',
                  isActive
                    ? 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-text)]'
                    : 'text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-accent-soft)]/50 hover:text-[color:var(--admin-text)]',
                )
              }
            >
              {!collapsed && (
                <span className="shrink-0 font-[JetBrains_Mono,monospace] text-[11px] font-bold text-[color:var(--admin-text-tertiary)]">{item.num}</span>
              )}
              <item.icon width={17} height={17} className="shrink-0 opacity-90" />
              {!collapsed && <span className="truncate">{t(item.key)}</span>}
            </NavLink>
          ))}
        </nav>

        <ShiftCard collapsed={collapsed} />

        <div className="border-t border-[color:var(--admin-border)] p-3">
          <Link
            to="/admin/settings"
            className={clsx(
              'flex items-center gap-2.5 rounded-[11px] bg-[color:var(--admin-hover)] px-2.5 py-2.5',
              collapsed && 'flex-col gap-1.5 px-1',
            )}
          >
            <UserAvatar collapsed={collapsed} />
            {!collapsed && (
              <div className="min-w-0 flex-1 leading-tight">
                <div className="truncate text-[13px] font-bold text-[color:var(--admin-text)]">{user?.email}</div>
                <div className="text-[11px] font-semibold text-[color:var(--admin-accent)]">{t('partner.shell.owner')}</div>
              </div>
            )}
            <button
              onClick={(e) => {
                e.preventDefault()
                handleLogout()
              }}
              aria-label={t('shell.logout')}
              title={t('shell.logout')}
              className="flex shrink-0 items-center justify-center text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]"
            >
              <LogOutIcon width={16} height={16} />
            </button>
          </Link>
        </div>
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
        <header className="flex h-[62px] shrink-0 items-center justify-between gap-3 border-b border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] px-6">
          <div className="flex min-w-0 items-center gap-3">
            <button
              onClick={() => setMobileNavOpen(true)}
              aria-label={t('shell.menu')}
              className="grid h-9 w-9 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)] lg:hidden"
            >
              <span className="flex w-4 flex-col gap-[4px]">
                <span className="block h-[1.5px] w-full bg-current" />
                <span className="block h-[1.5px] w-full bg-current" />
                <span className="block h-[1.5px] w-full bg-current" />
              </span>
            </button>
            <h1 className="truncate text-[18px] font-extrabold tracking-tight text-[color:var(--admin-text)]">{t(pageKeys.title)}</h1>
            <span className="hidden truncate text-[12px] font-medium text-[color:var(--admin-text-tertiary)] sm:inline">{t(pageKeys.subtitle)}</span>
          </div>

          <div className="flex shrink-0 items-center gap-3">
            <div className="hidden w-56 xl:block">
              <CommandPalette items={paletteItems} />
            </div>
            <NotificationBell />
            <LanguageSwitcher scheme="admin" />
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              aria-label={t('shell.toggleTheme')}
              className="grid h-9 w-9 shrink-0 place-items-center rounded-[10px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)]"
            >
              {isDark ? <SunIcon width={16} height={16} /> : <MoonIcon width={16} height={16} />}
            </button>
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

function UserAvatar({ collapsed }: { collapsed: boolean }) {
  const { user } = useAuth()
  const { profile } = useProfile()
  const avatarUrl = useAvatarUrl(!!profile?.avatarReference, profile?.avatarReference)
  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'
  return (
    <div
      className={clsx(
        'grid shrink-0 place-items-center overflow-hidden rounded-full bg-[color:var(--admin-accent-soft)] text-[13px] font-bold text-[color:var(--admin-accent)]',
        collapsed ? 'h-8 w-8' : 'h-9 w-9',
      )}
    >
      {avatarUrl ? <img src={avatarUrl} alt="" className="h-full w-full object-cover" /> : initial}
    </div>
  )
}
