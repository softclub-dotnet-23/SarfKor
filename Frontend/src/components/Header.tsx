import { useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Link } from 'react-router-dom'
import clsx from 'clsx'
import { Logo } from './Logo'
import { ThemeToggle } from './ThemeToggle'
import { useActiveSection } from '../hooks/useActiveSection'
import { useScrolled } from '../hooks/useScrolled'

const NAV_ITEMS = [
  { id: 'hero', label: 'Главная' },
  { id: 'how-it-works', label: 'Возможности' },
  { id: 'stats', label: 'Как это работает' },
  { id: 'stores', label: 'Магазины' },
  { id: 'testimonials', label: 'О нас' },
  { id: 'faq', label: 'Контакты' },
]

function scrollToSection(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

export function Header() {
  const scrolled = useScrolled()
  const active = useActiveSection(NAV_ITEMS.map((i) => i.id))
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <header
      className={clsx(
        'fixed inset-x-0 top-0 z-50 transition-all duration-300',
        scrolled
          ? 'bg-[color:var(--bg-app)]/80 shadow-[0_1px_0_var(--border-subtle)] backdrop-blur-xl'
          : 'bg-transparent',
      )}
    >
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-6 lg:px-10">
        <button onClick={() => scrollToSection('hero')} className="shrink-0">
          <Logo />
        </button>

        <nav className="hidden items-center gap-1 lg:flex">
          {NAV_ITEMS.map((item) => (
            <button
              key={item.id}
              onClick={() => scrollToSection(item.id)}
              className={clsx(
                'relative rounded-full px-4 py-2 text-[14px] font-medium transition-colors',
                active === item.id
                  ? 'text-[color:var(--text-primary)]'
                  : 'text-[color:var(--text-secondary)] hover:text-[color:var(--text-primary)]',
              )}
            >
              {active === item.id && (
                <motion.span
                  layoutId="nav-pill"
                  className="absolute inset-0 rounded-full bg-[color:var(--bg-section)]"
                  transition={{ type: 'spring', stiffness: 500, damping: 35 }}
                />
              )}
              <span className="relative">{item.label}</span>
            </button>
          ))}
        </nav>

        <div className="flex items-center gap-3">
          <div className="hidden sm:block">
            <ThemeToggle />
          </div>
          <Link
            to="/login"
            className="hidden shrink-0 rounded-full bg-[color:var(--bg-inverse)] px-5 py-2 text-[14px] font-semibold text-[color:var(--bg-app)] transition-transform hover:scale-[1.03] active:scale-[0.97] sm:block"
          >
            Войти
          </Link>
          <button
            className="grid h-9 w-9 place-items-center rounded-full ring-1 ring-inset ring-[color:var(--border-subtle)] lg:hidden"
            aria-label="Меню"
            onClick={() => setMobileOpen((v) => !v)}
          >
            <span className="relative block h-3 w-4">
              <motion.span
                animate={{ rotate: mobileOpen ? 45 : 0, y: mobileOpen ? 5 : 0 }}
                className="absolute left-0 top-0 h-[1.5px] w-4 bg-[color:var(--text-primary)]"
              />
              <motion.span
                animate={{ opacity: mobileOpen ? 0 : 1 }}
                className="absolute left-0 top-1/2 h-[1.5px] w-4 -translate-y-1/2 bg-[color:var(--text-primary)]"
              />
              <motion.span
                animate={{ rotate: mobileOpen ? -45 : 0, y: mobileOpen ? -5 : 0 }}
                className="absolute bottom-0 left-0 h-[1.5px] w-4 bg-[color:var(--text-primary)]"
              />
            </span>
          </button>
        </div>
      </div>

      <AnimatePresence>
        {mobileOpen && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.25, ease: 'easeInOut' }}
            className="overflow-hidden border-t border-[color:var(--border-subtle)] bg-[color:var(--bg-app)] lg:hidden"
          >
            <nav className="flex flex-col gap-1 px-6 py-4">
              {NAV_ITEMS.map((item) => (
                <button
                  key={item.id}
                  onClick={() => {
                    scrollToSection(item.id)
                    setMobileOpen(false)
                  }}
                  className={clsx(
                    'rounded-lg px-3 py-2.5 text-left text-[15px] font-medium',
                    active === item.id
                      ? 'bg-[color:var(--bg-section)] text-[color:var(--text-primary)]'
                      : 'text-[color:var(--text-secondary)]',
                  )}
                >
                  {item.label}
                </button>
              ))}
              <Link
                to="/login"
                onClick={() => setMobileOpen(false)}
                className="rounded-lg bg-[color:var(--bg-inverse)] px-3 py-2.5 text-center text-[15px] font-semibold text-[color:var(--bg-app)] sm:hidden"
              >
                Войти
              </Link>
              <div className="px-3 pt-2 sm:hidden">
                <ThemeToggle />
              </div>
            </nav>
          </motion.div>
        )}
      </AnimatePresence>
    </header>
  )
}
