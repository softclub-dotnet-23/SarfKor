import { Link, Outlet } from 'react-router-dom'
import { LogoMark } from '../components/Logo'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../components/icons'
import { useAuth } from '../auth/AuthContext'
import { ShieldIcon, LogOutIcon } from './components/icons'

// Deliberately not AdminLayout — that sidebar assumes a store (POS/inventory/staff nav, cashier
// shift widget). Admin moderation has nothing to do with owning a store, so it gets its own,
// much simpler shell instead of shoehorning it into store-shaped navigation.
export function ConsoleLayout() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'
  const { user, logout } = useAuth()

  return (
    <div className="admin-shell min-h-screen bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <header className="flex items-center gap-4 border-b border-[color:var(--admin-border)] px-6 py-4">
        <Link to="/" className="flex items-center gap-2.5">
          <LogoMark size={26} />
          <span className="text-[17px] font-extrabold tracking-tight">Sarfkor</span>
        </Link>

        <div className="flex items-center gap-2 rounded-full bg-[color:var(--admin-accent-soft)] px-3 py-1 text-[12px] font-semibold text-[color:var(--admin-accent)]">
          <ShieldIcon width={14} height={14} />
          Admin console
        </div>

        <div className="ml-auto flex items-center gap-3">
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
            aria-label="Переключить тему"
            className="grid h-9 w-9 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
          >
            {isDark ? <SunIcon width={17} height={17} /> : <MoonIcon width={17} height={17} />}
          </button>

          <div className="hidden text-right sm:block">
            <div className="text-[13px] font-semibold leading-tight">{user?.email}</div>
            <div className="text-[11px] leading-tight text-[color:var(--admin-text-tertiary)]">Admin</div>
          </div>

          <button
            onClick={logout}
            aria-label="Выйти"
            className="grid h-9 w-9 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
          >
            <LogOutIcon width={17} height={17} />
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-[1100px] p-6">
        <Outlet />
      </main>
    </div>
  )
}
