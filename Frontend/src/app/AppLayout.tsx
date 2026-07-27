import { useState } from 'react'
import { NavLink, Outlet, Link } from 'react-router-dom'
import clsx from 'clsx'
import { LogoMark } from '../components/Logo'
import { useAuth } from '../auth/AuthContext'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { SunIcon, MoonIcon, ScanIcon, HeartIcon, ListIcon, BellIcon, UserIcon, ReceiptIcon } from '../components/icons'

const NAV_ITEMS = [
  { to: '/app', label: 'Сканировать', icon: ScanIcon, end: true },
  { to: '/app/favorites', label: 'Избранное', icon: HeartIcon },
  { to: '/app/lists', label: 'Списки покупок', icon: ListIcon },
  { to: '/app/alerts', label: 'Уведомления о цене', icon: BellIcon },
  { to: '/app/notifications', label: 'Оповещения', icon: BellIcon },
  { to: '/app/receipts', label: 'Чеки', icon: ReceiptIcon },
  { to: '/app/account', label: 'Профиль', icon: UserIcon },
]

export function AppLayout() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const { user, logout } = useAuth()
  const isDark = theme === 'dark'

  return (
    <div className="min-h-screen bg-[color:var(--bg-app)] text-[color:var(--text-primary)]">
      <header className="sticky top-0 z-40 border-b border-[color:var(--border-subtle)] bg-[color:var(--bg-app)]/90 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-6xl items-center gap-4 px-4 sm:px-6">
          <Link to="/app" className="flex shrink-0 items-center gap-2.5">
            <LogoMark size={28} />
            <span className="text-[17px] font-extrabold tracking-tight">Sarfkor</span>
          </Link>

          <nav className="hidden flex-1 items-center gap-1 lg:flex">
            {NAV_ITEMS.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  clsx(
                    'flex items-center gap-2 rounded-full px-3.5 py-2 text-[13.5px] font-semibold transition-colors',
                    isActive
                      ? 'bg-[color:var(--bg-section)] text-[color:var(--text-primary)]'
                      : 'text-[color:var(--text-secondary)] hover:text-[color:var(--text-primary)]',
                  )
                }
              >
                <item.icon width={16} height={16} />
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="ml-auto flex shrink-0 items-center gap-2">
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              aria-label="Переключить тему"
              className="grid h-9 w-9 place-items-center rounded-full text-[color:var(--text-secondary)] hover:bg-[color:var(--bg-section)]"
            >
              {isDark ? <SunIcon width={17} height={17} /> : <MoonIcon width={17} height={17} />}
            </button>
            <div className="hidden items-center gap-2.5 border-l border-[color:var(--border-subtle)] pl-3 sm:flex">
              <div className="grid h-9 w-9 place-items-center rounded-full bg-[color:var(--color-brand-light)] text-[13px] font-bold text-[color:var(--color-brand)]">
                {user?.email?.charAt(0).toUpperCase() ?? '?'}
              </div>
              <button
                onClick={logout}
                className="text-[13px] font-semibold text-[color:var(--text-secondary)] hover:text-[color:var(--text-primary)]"
              >
                Выйти
              </button>
            </div>
            <button
              onClick={() => setMobileNavOpen((v) => !v)}
              aria-label="Меню"
              className="grid h-9 w-9 place-items-center rounded-full text-[color:var(--text-secondary)] hover:bg-[color:var(--bg-section)] lg:hidden"
            >
              <span className="flex w-4 flex-col gap-[4px]">
                <span className="block h-[1.5px] w-full bg-current" />
                <span className="block h-[1.5px] w-full bg-current" />
                <span className="block h-[1.5px] w-full bg-current" />
              </span>
            </button>
          </div>
        </div>

        {mobileNavOpen && (
          <nav className="flex flex-col gap-1 border-t border-[color:var(--border-subtle)] px-4 py-3 lg:hidden">
            {NAV_ITEMS.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                onClick={() => setMobileNavOpen(false)}
                className={({ isActive }) =>
                  clsx(
                    'flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-[14px] font-semibold',
                    isActive ? 'bg-[color:var(--bg-section)] text-[color:var(--text-primary)]' : 'text-[color:var(--text-secondary)]',
                  )
                }
              >
                <item.icon width={17} height={17} />
                {item.label}
              </NavLink>
            ))}
            <button onClick={logout} className="mt-1 px-3 py-2.5 text-left text-[14px] font-semibold text-[color:var(--text-secondary)]">
              Выйти ({user?.email})
            </button>
          </nav>
        )}
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <Outlet />
      </main>
    </div>
  )
}
