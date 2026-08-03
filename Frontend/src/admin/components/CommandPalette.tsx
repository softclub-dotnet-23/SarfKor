import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useReducedMotion } from 'framer-motion'
import {
  GridIcon,
  RegisterIcon,
  PackageIcon,
  UsersIcon,
  ReportIcon,
  SettingsIcon,
  TruckIcon,
  TagIcon,
} from './icons'

const EASE = [0.16, 1, 0.3, 1] as const

interface PaletteItem {
  id: string
  label: string
  sub?: string
  icon: (p: { width: number; height: number }) => React.ReactNode
  action: () => void
  keys?: string[]
}

function SearchIcon() {
  return (
    <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <circle cx="11" cy="11" r="8"/>
      <line x1="21" y1="21" x2="16.65" y2="16.65"/>
    </svg>
  )
}

function ArrowReturnIcon() {
  return (
    <svg width={12} height={12} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round">
      <polyline points="9 10 4 15 9 20"/>
      <path d="M20 4v7a4 4 0 01-4 4H4"/>
    </svg>
  )
}

function useFocusTrap(ref: React.RefObject<HTMLElement | null>, active: boolean) {
  useEffect(() => {
    if (!active) return
    const el = ref.current
    if (!el) return
    const focusable = el.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
    )
    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    function onKey(e: KeyboardEvent) {
      if (e.key !== 'Tab') return
      if (e.shiftKey) {
        if (document.activeElement === first) { e.preventDefault(); last?.focus() }
      } else {
        if (document.activeElement === last) { e.preventDefault(); first?.focus() }
      }
    }
    el.addEventListener('keydown', onKey)
    return () => el.removeEventListener('keydown', onKey)
  }, [active, ref])
}

export function CommandPalette() {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [active, setActive] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const reduce = useReducedMotion()

  const allItems: PaletteItem[] = [
    { id: 'dash', label: 'Дашборд', sub: 'Главная страница кабинета', icon: GridIcon, action: () => navigate('/admin') },
    { id: 'pos', label: 'Касса', sub: 'Открыть POS-кассу', icon: RegisterIcon, action: () => navigate('/admin/pos') },
    { id: 'inv', label: 'Склад', sub: 'Инвентаризация и остатки', icon: PackageIcon, action: () => navigate('/admin/inventory') },
    { id: 'sup', label: 'Поставки', sub: 'Поставщики и закупки', icon: TruckIcon, action: () => navigate('/admin/supply') },
    { id: 'mkt', label: 'Маркетинг', sub: 'Акции и лояльность', icon: TagIcon, action: () => navigate('/admin/marketing') },
    { id: 'staff', label: 'Сотрудники', sub: 'Управление персоналом', icon: UsersIcon, action: () => navigate('/admin/staff') },
    { id: 'rep', label: 'Отчёты', sub: 'Продажи, прибыль, аналитика', icon: ReportIcon, action: () => navigate('/admin/reports') },
    { id: 'set', label: 'Настройки', sub: 'Магазин, пароль, тема', icon: SettingsIcon, action: () => navigate('/admin/settings') },
  ]

  const q = query.trim().toLowerCase()
  const items = q
    ? allItems.filter((it) => it.label.toLowerCase().includes(q) || it.sub?.toLowerCase().includes(q))
    : allItems

  function close() {
    setOpen(false)
    setQuery('')
    setActive(0)
  }

  function confirm(idx: number) {
    const item = items[idx]
    if (!item) return
    item.action()
    close()
  }

  // Open on custom event (sidebar button) + Ctrl/Cmd+K
  useEffect(() => {
    function onEvent() { setOpen(true) }
    function onKey(e: KeyboardEvent) {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault()
        setOpen((v) => !v)
      }
      if (e.key === 'Escape') close()
    }
    document.addEventListener('sarfkor:open-palette', onEvent as EventListener)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('sarfkor:open-palette', onEvent as EventListener)
      document.removeEventListener('keydown', onKey)
    }
  }, [])

  // Focus input when palette opens
  useEffect(() => {
    if (open) {
      setActive(0)
      setTimeout(() => inputRef.current?.focus(), 50)
    }
  }, [open])

  // Clamp active when results change
  useEffect(() => {
    setActive((v) => Math.min(v, Math.max(items.length - 1, 0)))
  }, [items.length])

  useFocusTrap(containerRef, open)

  function onKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'ArrowDown') { e.preventDefault(); setActive((v) => (v + 1) % Math.max(items.length, 1)) }
    if (e.key === 'ArrowUp') { e.preventDefault(); setActive((v) => (v - 1 + Math.max(items.length, 1)) % Math.max(items.length, 1)) }
    if (e.key === 'Enter') { e.preventDefault(); confirm(active) }
  }

  return (
    <AnimatePresence>
      {open && (
        <>
          {/* Backdrop */}
          <motion.div
            key="backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: reduce ? 0 : 0.18 }}
            className="fixed inset-0 z-[200]"
            style={{ background: 'color-mix(in srgb, var(--admin-content) 70%, transparent)', backdropFilter: 'blur(8px)' }}
            onClick={close}
          />

          {/* Panel */}
          <motion.div
            key="panel"
            ref={containerRef}
            initial={{ opacity: 0, y: reduce ? 0 : -16, scale: reduce ? 1 : 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: reduce ? 0 : -8, scale: reduce ? 1 : 0.97 }}
            transition={{ duration: reduce ? 0 : 0.22, ease: EASE }}
            role="dialog"
            aria-modal
            aria-label="Командная палитра"
            className="fixed left-1/2 top-[15vh] z-[201] w-full max-w-[520px] -translate-x-1/2 overflow-hidden rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)]"
            style={{ boxShadow: '0 24px 80px -8px rgba(0,0,0,0.35), 0 0 0 1px var(--admin-border)' }}
            onKeyDown={onKeyDown}
          >
            {/* Search row */}
            <div className="flex items-center gap-3 border-b border-[color:var(--admin-border)] px-4 py-3.5">
              <span className="shrink-0 text-[color:var(--admin-text-tertiary)]"><SearchIcon /></span>
              <input
                ref={inputRef}
                value={query}
                onChange={(e) => { setQuery(e.target.value); setActive(0) }}
                placeholder="Перейти в раздел…"
                className="min-w-0 flex-1 bg-transparent text-[14px] text-[color:var(--admin-text)] placeholder:text-[color:var(--admin-text-tertiary)] outline-none"
              />
              {query && (
                <button
                  onClick={() => setQuery('')}
                  className="shrink-0 text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text-secondary)]"
                  tabIndex={-1}
                >
                  <svg width={14} height={14} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.5} strokeLinecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
              )}
            </div>

            {/* Results */}
            <div className="overflow-y-auto" style={{ maxHeight: 360 }}>
              {items.length === 0 && (
                <div className="px-4 py-8 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
                  Ничего не найдено
                </div>
              )}
              {items.map((item, i) => {
                const isActive = i === active
                return (
                  <button
                    key={item.id}
                    onClick={() => confirm(i)}
                    onMouseEnter={() => setActive(i)}
                    className="flex w-full items-center gap-3 px-4 py-2.5 text-left transition-colors"
                    style={{
                      background: isActive ? 'var(--admin-accent-soft)' : 'transparent',
                    }}
                  >
                    <span
                      className="grid h-7 w-7 shrink-0 place-items-center rounded-lg"
                      style={{
                        background: isActive ? 'color-mix(in srgb, var(--admin-accent) 14%, transparent)' : 'var(--admin-hover)',
                        color: isActive ? 'var(--admin-text)' : 'var(--admin-text-tertiary)',
                      }}
                    >
                      <item.icon width={14} height={14} />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span
                        className="block text-[13.5px] font-semibold leading-tight"
                        style={{ color: isActive ? 'var(--admin-text)' : 'var(--admin-text-secondary)' }}
                      >
                        {item.label}
                      </span>
                      {item.sub && (
                        <span className="block text-[11.5px] leading-tight text-[color:var(--admin-text-tertiary)]">
                          {item.sub}
                        </span>
                      )}
                    </span>
                    {isActive && (
                      <span className="shrink-0 text-[color:var(--admin-text-tertiary)]">
                        <ArrowReturnIcon />
                      </span>
                    )}
                  </button>
                )
              })}
            </div>

            {/* Footer hint */}
            <div className="flex items-center gap-4 border-t border-[color:var(--admin-border)] px-4 py-2.5">
              <span className="flex items-center gap-1.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
                <kbd className="rounded border border-[color:var(--admin-border)] px-1 py-0.5 text-[9px] font-bold">↑↓</kbd>
                выбрать
              </span>
              <span className="flex items-center gap-1.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
                <kbd className="rounded border border-[color:var(--admin-border)] px-1 py-0.5 text-[9px] font-bold">↵</kbd>
                перейти
              </span>
              <span className="flex items-center gap-1.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
                <kbd className="rounded border border-[color:var(--admin-border)] px-1 py-0.5 text-[9px] font-bold">Esc</kbd>
                закрыть
              </span>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  )
}
