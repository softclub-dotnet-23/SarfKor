import { motion } from 'framer-motion'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { MoonIcon, SunIcon } from './icons'

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'

  function handleToggle(e: React.MouseEvent<HTMLButtonElement>) {
    runThemeTransition(e.currentTarget, toggleTheme)
  }

  return (
    <button
      type="button"
      onClick={handleToggle}
      aria-label="Переключить тему"
      aria-pressed={isDark}
      className="relative flex h-9 w-[68px] items-center rounded-full bg-[color:var(--bg-section)] p-1 ring-1 ring-inset ring-[color:var(--border-subtle)] transition-colors"
    >
      <motion.span
        layout
        transition={{ type: 'spring', stiffness: 500, damping: 32 }}
        className="absolute top-1 flex h-7 w-7 items-center justify-center rounded-full bg-[color:var(--bg-inverse)] shadow-md"
        style={{ left: isDark ? 'calc(100% - 30px)' : '4px' }}
      >
        {isDark ? (
          <MoonIcon width={15} height={15} className="text-white" />
        ) : (
          <SunIcon width={15} height={15} className="text-[color:var(--bg-app)]" />
        )}
      </motion.span>
      <SunIcon width={14} height={14} className="relative z-0 ml-1 text-[color:var(--text-secondary)]" />
      <MoonIcon width={14} height={14} className="relative z-0 ml-auto mr-1 text-[color:var(--text-secondary)]" />
    </button>
  )
}
