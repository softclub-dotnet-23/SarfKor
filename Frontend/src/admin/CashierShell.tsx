import { type ReactNode } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import clsx from 'clsx'
import { LogoMark } from '../components/Logo'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../components/icons'
import { useAuth } from '../auth/AuthContext'
import { RegisterIcon, PackageIcon, LogOutIcon } from './components/icons'
import { ShiftCard } from './AdminLayout'
import { LanguageSwitcher } from './components/LanguageSwitcher'
import { useT } from '../i18n/translations'

// Cashier's own shell — deliberately NOT a restyle of the desktop
// StorePartner sidebar. Same tokens/typography/status palette as the rest of
// the design system, but a different scenario entirely: a phone in one hand,
// standing at a register, in a hurry. No sidebar; a thin top bar for
// identity/theme/logout, and a fixed two-item bottom tab bar sized for a
// thumb (Cashier only ever has two destinations — pos/inventory — see
// main.tsx's route tree, RequireOwner gates everything else out).
const TABS = [
  { to: '/admin/pos', key: 'partner.nav.pos', icon: RegisterIcon, titleKey: 'partner.page.pos.title' },
  { to: '/admin/inventory', key: 'partner.nav.inventory', icon: PackageIcon, titleKey: 'partner.page.inventory.title' },
] as const

function PageTransition({ pathKey, children }: { pathKey: string; children: ReactNode }) {
  const reduce = useReducedMotion()
  if (reduce) return <>{children}</>
  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={pathKey}
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.2, ease: [0.16, 1, 0.3, 1] }}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  )
}

export function CashierShell() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const { logout } = useAuth()
  const location = useLocation()
  const t = useT()
  const isDark = theme === 'dark'
  const page = TABS.find((tab) => location.pathname.startsWith(tab.to)) ?? TABS[0]

  return (
    <div className="admin-shell flex h-screen w-full flex-col overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      {/* Top bar — identity + language + theme + logout only. No menu button:
          there is nothing to open. */}
      <header className="flex h-14 shrink-0 items-center justify-between gap-2 border-b border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] px-3">
        <div className="flex min-w-0 items-center gap-2">
          <div className="grid h-8 w-8 shrink-0 place-items-center rounded-[8px] bg-[color:var(--admin-accent)]">
            <LogoMark size={16} mono />
          </div>
          <div className="min-w-0 leading-tight">
            <div className="truncate text-[14px] font-extrabold tracking-tight">{t(page.titleKey)}</div>
            <div className="truncate text-[10.5px] font-medium text-[color:var(--admin-text-tertiary)]">{t('partner.shell.cashier')}</div>
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-1">
          <LanguageSwitcher scheme="admin" />
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
            aria-label={t('shell.toggleTheme')}
            className="grid h-12 w-12 place-items-center rounded-[10px] text-[color:var(--admin-text-secondary)] active:bg-[color:var(--admin-hover)]"
          >
            {isDark ? <SunIcon width={19} height={19} /> : <MoonIcon width={19} height={19} />}
          </button>
          <button
            onClick={logout}
            aria-label={t('shell.logout')}
            className="grid h-12 w-12 place-items-center rounded-[10px] text-[color:var(--admin-text-secondary)] active:bg-[color:var(--admin-danger-dim)] active:text-[color:var(--admin-danger)]"
          >
            <LogOutIcon width={19} height={19} />
          </button>
        </div>
      </header>

      {/* Shift open/close — the one thing a cashier must do before selling
          anything, so it rides along at the top of the scroll area rather
          than being buried in a menu. Same component/logic StorePartner's
          sidebar uses, just full-width instead of a narrow column. */}
      <div className="shrink-0 border-b border-[color:var(--admin-border)] bg-[color:var(--admin-content)] px-3 pb-3">
        <ShiftCard collapsed={false} />
      </div>

      <main className="min-h-0 flex-1 overflow-y-auto px-3 pb-3 pt-3">
        <PageTransition pathKey={location.pathname}>
          <Outlet />
        </PageTransition>
      </main>

      {/* Bottom tab bar — the only navigation Cashier has. Large targets
          (full-width flex-1 columns, comfortably over the 48×48 floor) sit
          in the thumb zone; safe-area padding keeps them clear of a phone's
          home-indicator gesture strip. */}
      <nav
        className="grid shrink-0 grid-cols-2 border-t border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)]"
        style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
      >
        {TABS.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) =>
              clsx(
                'flex flex-col items-center justify-center gap-1 py-2.5 text-[12px] font-semibold transition-colors',
                isActive ? 'text-[color:var(--admin-accent)]' : 'text-[color:var(--admin-text-tertiary)]',
              )
            }
          >
            {({ isActive }) => (
              <>
                <span
                  className={clsx(
                    'grid h-9 w-9 place-items-center rounded-full transition-colors',
                    isActive && 'bg-[color:var(--admin-accent-soft)]',
                  )}
                >
                  <tab.icon width={20} height={20} />
                </span>
                {t(tab.key)}
              </>
            )}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
