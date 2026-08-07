import { useState, type FormEvent } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import clsx from 'clsx'
import { LogoMark } from '../components/Logo'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { useT } from '../i18n/translations'
import { SunIcon, MoonIcon } from '../components/icons'
import { LogOutIcon, GridIcon, StoreIcon, CardIcon, UsersIcon, TagIcon, ClockIcon, MailIcon } from './components/icons'
import { AdminModal } from './components/AdminModal'
import { Input } from './components/Input'
import { AssistantPanel } from './components/AssistantPanel'
import { LanguageSwitcher } from './components/LanguageSwitcher'
import { RouteErrorBoundary } from '../components/RouteErrorBoundary'
import { useAuth } from '../auth/AuthContext'
import { adminApi, ApiError } from '../lib/api'

const NAV_ITEMS = [
  { to: '/admin/overview', num: '01', key: 'nav.overview', icon: GridIcon },
  { to: '/admin/stores', num: '02', key: 'nav.stores', icon: StoreIcon },
  { to: '/admin/subscriptions', num: '03', key: 'nav.subscriptions', icon: CardIcon },
  { to: '/admin/users', num: '04', key: 'nav.users', icon: UsersIcon },
  { to: '/admin/reference', num: '05', key: 'nav.reference', icon: TagIcon },
  { to: '/admin/audit-log', num: '06', key: 'nav.auditLog', icon: ClockIcon },
] as const

const PAGE_TITLE_KEYS: Record<string, { title: 'page.overview.title' | 'page.stores.title' | 'page.subscriptions.title' | 'page.users.title' | 'page.reference.title' | 'page.auditLog.title'; subtitle: 'page.overview.subtitle' | 'page.stores.subtitle' | 'page.subscriptions.subtitle' | 'page.users.subtitle' | 'page.reference.subtitle' | 'page.auditLog.subtitle' }> = {
  '/admin/overview': { title: 'page.overview.title', subtitle: 'page.overview.subtitle' },
  '/admin/stores': { title: 'page.stores.title', subtitle: 'page.stores.subtitle' },
  '/admin/subscriptions': { title: 'page.subscriptions.title', subtitle: 'page.subscriptions.subtitle' },
  '/admin/users': { title: 'page.users.title', subtitle: 'page.users.subtitle' },
  '/admin/reference': { title: 'page.reference.title', subtitle: 'page.reference.subtitle' },
  '/admin/audit-log': { title: 'page.auditLog.title', subtitle: 'page.auditLog.subtitle' },
}

function InviteAdminModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT()
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  function handleClose() {
    if (busy) return
    setEmail('')
    setError('')
    setDone(false)
    onClose()
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!email.trim() || busy) return
    setBusy(true)
    setError('')
    try {
      await adminApi.inviteAdmin(email.trim())
      setDone(true)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось отправить приглашение')
    } finally {
      setBusy(false)
    }
  }

  return (
    <AdminModal open={open} onClose={handleClose} title={t('invite.title')} scheme="admin">
      {done ? (
        <div className="py-2">
          <p className="text-[13.5px] text-[color:var(--admin-text)]">
            {t('invite.doneText')} <b>{email}</b>. {t('invite.doneHint')}
          </p>
          <button
            onClick={handleClose}
            className="mt-4 w-full rounded-xl bg-[color:var(--admin-accent)] py-2.5 text-[13px] font-bold text-[color:var(--admin-accent-fg)]"
          >
            {t('invite.doneButton')}
          </button>
        </div>
      ) : (
        <form onSubmit={handleSubmit}>
          <p className="mb-4 text-[13px] leading-relaxed text-[color:var(--admin-text-secondary)]">{t('invite.description')}</p>
          <label className="mb-1.5 block text-[11.5px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
            {t('invite.emailLabel')}
          </label>
          <Input
            scheme="admin"
            type="email"
            required
            autoFocus
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="admin@sarfkor.tj"
          />
          {error && <p className="mt-2 text-[12px] font-medium text-[color:var(--admin-danger)]">{error}</p>}
          <div className="mt-5 flex justify-end gap-2">
            <button
              type="button"
              onClick={handleClose}
              className="rounded-xl border border-[color:var(--admin-border)] px-4 py-2.5 text-[13px] font-semibold text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]"
            >
              {t('invite.cancel')}
            </button>
            <button
              type="submit"
              disabled={busy || !email.trim()}
              className="rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[13px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:brightness-110 active:scale-95 disabled:opacity-50"
            >
              {busy ? t('invite.busy') : t('invite.submit')}
            </button>
          </div>
        </form>
      )}
    </AdminModal>
  )
}

function PageTransition({ pathKey, children }: { pathKey: string; children: React.ReactNode }) {
  const reduce = useReducedMotion()
  if (reduce) return <>{children}</>
  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={pathKey}
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -6 }}
        transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  )
}

export function AdminConsoleLayout() {
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const t = useT()
  const location = useLocation()
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [inviteOpen, setInviteOpen] = useState(false)
  const isDark = theme === 'dark'
  const pageKeys = PAGE_TITLE_KEYS[location.pathname] ?? PAGE_TITLE_KEYS['/admin/overview']

  return (
    <div className="admin-shell flex h-screen w-full overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      {/* Sidebar */}
      <aside
        className={clsx(
          'fixed inset-y-0 left-0 z-40 flex w-[246px] shrink-0 flex-col overflow-y-auto border-r border-[color:var(--admin-border)] bg-[color:var(--admin-card)] transition-transform duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] lg:static lg:translate-x-0',
          mobileNavOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex items-center gap-2.5 px-5 pb-4 pt-5">
          <div className="grid h-[34px] w-[34px] shrink-0 place-items-center rounded-[9px] bg-[color:var(--admin-accent)]">
            <LogoMark size={20} mono />
          </div>
          <div className="flex flex-col leading-none">
            <span className="text-[16px] font-extrabold tracking-tight">Sarfkor</span>
            <span className="mt-1 w-fit rounded-[5px] bg-[color:var(--admin-accent-soft)] px-1.5 py-0.5 font-[JetBrains_Mono,monospace] text-[9.5px] font-semibold uppercase tracking-[.12em] text-[color:var(--admin-accent)]">
              Admin
            </span>
          </div>
        </div>

        <nav className="flex flex-1 flex-col gap-1 px-3 py-2">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={() => setMobileNavOpen(false)}
              className={({ isActive }) =>
                clsx(
                  'relative flex items-center gap-3 rounded-[11px] px-3 py-2.5 text-[13.5px] font-semibold transition-colors duration-150',
                  isActive
                    ? // Current page: solid pill background + full-contrast text only — must stay visually
                      // distinct from hover on every other row, or you can't tell which page you're on
                      // (bug report: hovering a different item made it indistinguishable from the active one).
                      'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-text)]'
                    : 'text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-accent-soft)]/50 hover:text-[color:var(--admin-text)]',
                )
              }
            >
              <span className="shrink-0 font-[JetBrains_Mono,monospace] text-[11px] font-bold text-[color:var(--admin-text-tertiary)]">{item.num}</span>
              <item.icon width={17} height={17} className="shrink-0 opacity-90" />
              <span className="truncate">{t(item.key)}</span>
            </NavLink>
          ))}
        </nav>

        <div className="px-3 pb-2">
          <button
            onClick={() => setInviteOpen(true)}
            className="flex w-full items-center gap-3 rounded-[11px] px-3 py-2.5 text-left text-[13px] font-semibold text-[color:var(--admin-text-secondary)] transition-colors hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]"
          >
            <MailIcon width={17} height={17} className="shrink-0" />
            {t('shell.inviteAdmin')}
          </button>
        </div>

        <div className="border-t border-[color:var(--admin-border)] p-3">
          <div className="flex items-center gap-2.5 rounded-[11px] bg-[color:var(--admin-hover)] px-2.5 py-2.5">
            <div className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-[color:var(--admin-accent-soft)] text-[13px] font-bold text-[color:var(--admin-accent)]">
              {user?.email?.charAt(0).toUpperCase() ?? '?'}
            </div>
            <div className="min-w-0 flex-1 leading-tight">
              <div className="truncate text-[13px] font-bold">{user?.email}</div>
              <div className="text-[11px] font-semibold text-[color:var(--admin-accent)]">{t('shell.administrator')}</div>
            </div>
            <button
              onClick={logout}
              aria-label={t('shell.logout')}
              className="flex shrink-0 items-center justify-center text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]"
            >
              <LogOutIcon width={16} height={16} />
            </button>
          </div>
        </div>
      </aside>

      <AnimatePresence>
        {mobileNavOpen && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-30 bg-black/45 backdrop-blur-sm lg:hidden"
            onClick={() => setMobileNavOpen(false)}
            aria-hidden
          />
        )}
      </AnimatePresence>

      {/* Main */}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header className="flex h-[62px] shrink-0 items-center justify-between border-b border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-6">
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
            <h1 className="truncate text-[18px] font-extrabold tracking-tight">{t(pageKeys.title)}</h1>
            <span className="hidden truncate text-[12px] font-medium text-[color:var(--admin-text-secondary)] sm:inline">{t(pageKeys.subtitle)}</span>
          </div>
          <div className="flex shrink-0 items-center gap-3">
            <div className="hidden items-center gap-1.5 rounded-[9px] border border-[color:var(--admin-border)] px-2.5 py-1.5 font-[JetBrains_Mono,monospace] text-[11px] font-semibold text-[color:var(--admin-text-secondary)] sm:flex">
              <span className="h-[7px] w-[7px] rounded-full bg-[color:var(--admin-success)]" style={{ animation: 'mod-live-ping 1.6s ease-in-out infinite' }} />
              LIVE
            </div>
            <LanguageSwitcher scheme="admin" />
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              aria-label={t('shell.toggleTheme')}
              className="grid h-9 w-9 place-items-center rounded-[10px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)]"
            >
              {isDark ? <SunIcon width={16} height={16} /> : <MoonIcon width={16} height={16} />}
            </button>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto px-6 py-6">
          <PageTransition pathKey={location.pathname}>
            <RouteErrorBoundary key={location.pathname}>
              <Outlet />
            </RouteErrorBoundary>
          </PageTransition>
        </main>
      </div>

      <AssistantPanel />
      <InviteAdminModal open={inviteOpen} onClose={() => setInviteOpen(false)} />
    </div>
  )
}
