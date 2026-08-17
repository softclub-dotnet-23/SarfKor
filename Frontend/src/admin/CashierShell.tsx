import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import clsx from 'clsx'
import { LogoMark } from '../components/Logo'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../components/icons'
import { useAuth } from '../auth/AuthContext'
import { salesApi, ApiError, type CashierShift } from '../lib/api'
import { RegisterIcon, PackageIcon, LogOutIcon } from './components/icons'
import { LanguageSwitcher } from './components/LanguageSwitcher'
import { RouteErrorBoundary } from '../components/RouteErrorBoundary'
import { FormModal, FormField } from './components/FormModal'
import { useT } from '../i18n/translations'
import { useLocaleFormat } from '../i18n/format'

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

// Was a permanently-open card (amount input + button always on screen, pushing the actual POS
// content down) — same "постоянно открытая форма -> кнопка + модалка" complaint ADMIN_PROMPT's
// own follow-up raised about the old admin add-forms, just never applied here since this predates
// that pass. Now: a single-line status strip that's just a status dot + a button; the amount
// input only exists inside the FormModal it opens, on demand.
function ShiftBar() {
  const { storeId, user } = useAuth()
  const t = useT()
  const { time } = useLocaleFormat()
  const [shifts, setShifts] = useState<CashierShift[] | null>(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [amount, setAmount] = useState('')

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

  function openModal() {
    setAmount('')
    setModalOpen(true)
  }

  async function handleSubmit() {
    if (!storeId) return
    try {
      if (myOpenShift) {
        await salesApi.closeCashierShift(myOpenShift.cashierShiftId, Number(amount) || 0)
      } else {
        await salesApi.openCashierShift(storeId, Number(amount) || 0, 'TJS')
      }
      await load()
      setModalOpen(false)
    } catch (err) {
      throw err instanceof ApiError ? err : new Error(t('partner.shift.updateError'))
    }
  }

  return (
    <>
      <div className="mt-2.5 flex items-center justify-between gap-2 rounded-xl bg-[color:var(--admin-hover)] px-3 py-2">
        <div className="flex min-w-0 items-center gap-2">
          <span
            aria-hidden
            className={clsx('h-2 w-2 shrink-0 rounded-full', myOpenShift ? 'bg-[color:var(--admin-success)]' : 'bg-[color:var(--admin-text-tertiary)] opacity-50')}
          />
          <span className="truncate text-[12.5px] font-medium text-[color:var(--admin-text-secondary)]">
            {myOpenShift ? t('partner.shift.onSince', { time: time(myOpenShift.startedAt) }) : t('partner.shift.notOpen')}
          </span>
        </div>
        <button
          onClick={openModal}
          disabled={!storeId}
          className={clsx(
            'shrink-0 rounded-lg px-3 py-1.5 text-[12px] font-semibold transition-opacity disabled:opacity-50',
            myOpenShift ? 'bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)]' : 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]',
          )}
        >
          {myOpenShift ? t('partner.shift.close') : t('partner.shift.open')}
        </button>
      </div>

      <FormModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title={myOpenShift ? t('partner.shift.close') : t('partner.shift.open')}
        isDirty={!!amount}
        onSubmit={handleSubmit}
        submitLabel={myOpenShift ? t('partner.shift.close') : t('partner.shift.open')}
        submitBusyLabel={t('common.saving')}
        scheme="admin"
      >
        <FormField label={myOpenShift ? t('partner.shift.amountInDrawer') : t('partner.shift.openingAmount')} scheme="admin">
          <input
            type="number"
            autoFocus
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="0"
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </FormField>
      </FormModal>
    </>
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

      {/* Shift open/close — the one thing a cashier must do before selling anything, so its
          status rides along at the top of the scroll area rather than being buried in a menu.
          Compact status strip only; the actual open/close form lives in the modal the button
          opens, not inline (matches the button->modal convention every other admin form uses). */}
      <div className="shrink-0 border-b border-[color:var(--admin-border)] bg-[color:var(--admin-content)] px-3 pb-3">
        <ShiftBar />
      </div>

      <main className="min-h-0 flex-1 overflow-y-auto px-3 pb-3 pt-3">
        <PageTransition pathKey={location.pathname}>
          <RouteErrorBoundary key={location.pathname}>
            <Outlet />
          </RouteErrorBoundary>
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
