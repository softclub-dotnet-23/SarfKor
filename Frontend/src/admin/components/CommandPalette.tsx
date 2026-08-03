import { useEffect, useMemo, useRef, useState, type ComponentType, type KeyboardEvent as ReactKeyboardEvent } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { createPortal } from 'react-dom'
import { SearchIcon, CommandIcon, type IconProps } from './icons'

export interface CommandPaletteItem {
  id: string
  label: string
  hint?: string
  icon: ComponentType<IconProps>
  action: () => void
}

/**
 * Header search box + the Cmd/Ctrl-K palette it opens — one component instead of two, since the
 * header's search field was previously a static, non-functional placeholder input (typing into it
 * did nothing). Filters `items` client-side; nothing here fabricates data, `items` is always
 * built by the caller from the same NAV_ITEMS/role list already driving the sidebar.
 */
export function CommandPalette({ items }: { items: CommandPaletteItem[] }) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [activeIndex, setActiveIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return items
    return items.filter((i) => i.label.toLowerCase().includes(q))
  }, [items, query])

  useEffect(() => {
    function onGlobalKey(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setOpen((v) => !v)
      }
    }
    document.addEventListener('keydown', onGlobalKey)
    return () => document.removeEventListener('keydown', onGlobalKey)
  }, [])

  useEffect(() => {
    if (!open) return
    setQuery('')
    setActiveIndex(0)
    // Portal content isn't mounted on the same tick this effect runs.
    const id = requestAnimationFrame(() => inputRef.current?.focus())
    document.body.style.overflow = 'hidden'
    return () => {
      cancelAnimationFrame(id)
      document.body.style.overflow = ''
    }
  }, [open])

  useEffect(() => {
    setActiveIndex(0)
  }, [query])

  function runItem(item: CommandPaletteItem | undefined) {
    if (!item) return
    setOpen(false)
    item.action()
  }

  function onKeyDown(e: ReactKeyboardEvent) {
    if (e.key === 'Escape') {
      setOpen(false)
    } else if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActiveIndex((i) => Math.min(i + 1, filtered.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActiveIndex((i) => Math.max(i - 1, 0))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      runItem(filtered[activeIndex])
    }
  }

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="flex w-full items-center gap-2.5 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] py-2 pl-3.5 pr-3 text-left text-[13px] text-[color:var(--admin-text-tertiary)] transition-colors hover:border-[color:var(--admin-accent)]"
      >
        <SearchIcon width={16} height={16} className="shrink-0" />
        <span className="flex-1 truncate">Поиск...</span>
        <span className="hidden shrink-0 items-center gap-0.5 rounded-md bg-[color:var(--admin-card)] px-1.5 py-0.5 text-[10px] font-semibold text-[color:var(--admin-text-tertiary)] ring-1 ring-[color:var(--admin-border)] sm:flex">
          <CommandIcon width={10} height={10} />K
        </span>
      </button>

      {createPortal(
        <AnimatePresence>
          {open && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.15 }}
              className="admin-shell fixed inset-0 z-[100] flex items-start justify-center bg-black/50 px-4 pt-[12vh] backdrop-blur-sm"
              onClick={() => setOpen(false)}
            >
              <motion.div
                initial={{ opacity: 0, y: -12, scale: 0.98 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: -8, scale: 0.98 }}
                transition={{ type: 'spring', stiffness: 380, damping: 32 }}
                onClick={(e) => e.stopPropagation()}
                onKeyDown={onKeyDown}
                className="w-full max-w-[540px] overflow-hidden rounded-2xl bg-[color:var(--admin-card)] ring-1 ring-[color:var(--admin-border)] [box-shadow:var(--admin-shadow-lift)]"
              >
                <div className="flex items-center gap-3 border-b border-[color:var(--admin-border)] px-4 py-3.5">
                  <SearchIcon width={17} height={17} className="shrink-0 text-[color:var(--admin-text-tertiary)]" />
                  <input
                    ref={inputRef}
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    placeholder="Перейти на страницу..."
                    className="w-full border-0 bg-transparent text-[15px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)]"
                  />
                  <kbd className="shrink-0 rounded-md bg-[color:var(--admin-hover)] px-1.5 py-0.5 text-[10px] font-semibold text-[color:var(--admin-text-tertiary)]">
                    Esc
                  </kbd>
                </div>

                <div className="max-h-[360px] overflow-y-auto p-2">
                  {filtered.length === 0 && (
                    <p className="px-3 py-8 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">Ничего не найдено</p>
                  )}
                  {filtered.map((item, i) => (
                    <button
                      key={item.id}
                      onMouseEnter={() => setActiveIndex(i)}
                      onClick={() => runItem(item)}
                      className={`flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-[13.5px] font-medium transition-colors ${
                        i === activeIndex
                          ? 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]'
                          : 'text-[color:var(--admin-text-secondary)]'
                      }`}
                    >
                      <item.icon width={16} height={16} className="shrink-0" />
                      <span className="flex-1 truncate">{item.label}</span>
                      {item.hint && <span className="shrink-0 text-[11px] text-[color:var(--admin-text-tertiary)]">{item.hint}</span>}
                    </button>
                  ))}
                </div>
              </motion.div>
            </motion.div>
          )}
        </AnimatePresence>,
        document.body,
      )}
    </>
  )
}
