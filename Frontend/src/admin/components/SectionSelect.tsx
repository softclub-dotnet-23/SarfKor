import { useEffect, useId, useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useIsMobile } from '../../hooks/useMediaQuery'
import { useFloatingPosition } from '../../lib/useFloatingPosition'
import { lockBodyScroll, unlockBodyScroll } from '../../lib/scrollLock'
import { ChevronDownIcon, CheckIcon, XIcon } from './icons'

const SCHEMES = {
  admin: {
    trigger: 'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)] hover:bg-[color:var(--admin-border)] focus-visible:border-[color:var(--admin-accent)]',
    chevron: 'text-[color:var(--admin-text-tertiary)]',
    // Opaque --admin-sidebar, not the translucent --admin-card "glass" tone -- this panel portals
    // to document.body and floats over arbitrary page content, so it needs a surface that reads
    // as solid regardless of what's behind it.
    panel: 'border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] shadow-[var(--admin-shadow)]',
    option: 'text-[color:var(--admin-text)]',
    optionActive: 'bg-[color:var(--admin-hover)]',
    optionSelected: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    faint: 'text-[color:var(--admin-text-tertiary)]',
    border: 'border-[color:var(--admin-border)]',
    // The mobile sheet replaces the whole screen, so it needs the page's own OPAQUE background
    // (--admin-content) -- --admin-card is a translucent "glass" tone (alpha 0.045 in dark mode)
    // meant for a card floating over that background, not a full-screen surface. Using --admin-card
    // here made the sheet nearly see-through, with the page's own content bleeding through it.
    sheetBg: 'bg-[color:var(--admin-content)]',
  },
} as const

export interface SectionOption<T extends string> {
  value: T
  label: string
  icon?: ReactNode
}

interface SectionSelectProps<T extends string> {
  value: T
  onChange: (value: T) => void
  options: readonly SectionOption<T>[]
  scheme?: keyof typeof SCHEMES
  className?: string
  ariaLabel?: string
}

/**
 * Replaces a horizontal row of tabs with one dropdown selector — trigger shows the current
 * section, opening it lists every section with a checkmark on the active one. Built as the
 * single reusable "page has multiple sections" control (ADMIN task: "один переиспользуемый
 * компонент" — no per-page copies of a tab bar anymore).
 *
 * Shares its floating/portal architecture with EntityPicker/CategoryPicker: a desktop dropdown
 * portaled via useFloatingPosition (so it's never clipped by an ancestor's overflow, e.g. a
 * SidePanel's scroll body), and a full-screen sheet with large rows below the mobile breakpoint.
 *
 * Keyboard: the trigger button keeps DOM focus the entire time it's open (options are inert
 * `role="option"` rows, not separately focusable) — ArrowUp/Down move the active option,
 * Enter/Space commits it, Escape closes without changing the value, Tab closes and lets focus
 * continue past the trigger as normal. This mirrors the W3C "listbox button" pattern
 * (aria-activedescendant on the trigger) rather than moving focus into the portal.
 */
export function SectionSelect<T extends string>({ value, onChange, options, scheme = 'admin', className = '', ariaLabel }: SectionSelectProps<T>) {
  const t = SCHEMES[scheme]
  const isMobile = useIsMobile()
  const listId = useId()
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(() => Math.max(0, options.findIndex((o) => o.value === value)))
  const rootRef = useRef<HTMLDivElement>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const pos = useFloatingPosition(rootRef, open && !isMobile)

  const selected = options.find((o) => o.value === value) ?? options[0]

  function openPanel() {
    setActiveIndex(Math.max(0, options.findIndex((o) => o.value === value)))
    setOpen(true)
  }

  function closePanel() {
    setOpen(false)
    triggerRef.current?.focus()
  }

  function commit(index: number) {
    const opt = options[index]
    if (opt) onChange(opt.value)
    setOpen(false)
  }

  useEffect(() => {
    if (!open) return
    function onDocClick(e: MouseEvent) {
      const target = e.target as Node
      if (rootRef.current?.contains(target) || panelRef.current?.contains(target)) return
      setOpen(false)
    }
    document.addEventListener('mousedown', onDocClick)
    return () => document.removeEventListener('mousedown', onDocClick)
  }, [open])

  useEffect(() => {
    if (!open || !isMobile) return
    lockBodyScroll()
    return unlockBodyScroll
  }, [open, isMobile])

  function onTriggerKeyDown(e: React.KeyboardEvent) {
    if (!open) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp' || e.key === 'Enter' || e.key === ' ') {
        e.preventDefault()
        openPanel()
      }
      return
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActiveIndex((i) => Math.min(i + 1, options.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActiveIndex((i) => Math.max(i - 1, 0))
    } else if (e.key === 'Home') {
      e.preventDefault()
      setActiveIndex(0)
    } else if (e.key === 'End') {
      e.preventDefault()
      setActiveIndex(options.length - 1)
    } else if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      commit(activeIndex)
    } else if (e.key === 'Escape') {
      e.preventDefault()
      setOpen(false)
    } else if (e.key === 'Tab') {
      setOpen(false)
    }
  }

  const optionRows = (large: boolean) => (
    <div role="listbox" id={listId} aria-label={ariaLabel} className={large ? 'flex-1 overflow-y-auto p-2' : 'max-h-[min(60vh,360px)] overflow-y-auto p-1'}>
      {options.map((o, idx) => {
        const isSelected = o.value === value
        const isActive = idx === activeIndex
        return (
          <div
            key={o.value}
            id={`${listId}-opt-${o.value}`}
            role="option"
            aria-selected={isSelected}
            onMouseEnter={() => setActiveIndex(idx)}
            onClick={() => commit(idx)}
            className={`flex cursor-pointer items-center gap-2.5 rounded-lg transition-colors ${large ? 'min-h-12 px-3.5 text-[15px]' : 'min-h-9 px-2.5 text-[13px]'} font-medium ${
              isSelected ? t.optionSelected : isActive ? t.optionActive : t.option
            }`}
          >
            {o.icon && <span className="shrink-0">{o.icon}</span>}
            <span className="min-w-0 flex-1 truncate">{o.label}</span>
            {isSelected && <CheckIcon width={large ? 16 : 14} height={large ? 16 : 14} className="shrink-0" />}
          </div>
        )
      })}
    </div>
  )

  return (
    <div ref={rootRef} className={`relative ${className}`}>
      <button
        ref={triggerRef}
        type="button"
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        aria-activedescendant={open && options[activeIndex] ? `${listId}-opt-${options[activeIndex].value}` : undefined}
        aria-label={ariaLabel}
        onClick={() => (open ? closePanel() : openPanel())}
        onKeyDown={onTriggerKeyDown}
        className={`flex items-center gap-2 rounded-xl border py-2.5 pl-3.5 pr-3 text-left text-[13.5px] font-bold outline-none transition-colors ${t.trigger}`}
      >
        {selected?.icon && <span className="shrink-0">{selected.icon}</span>}
        <span className="truncate">{selected?.label}</span>
        <ChevronDownIcon width={14} height={14} className={`shrink-0 transition-transform ${t.chevron} ${open ? 'rotate-180' : ''}`} />
      </button>

      {!isMobile &&
        createPortal(
          <AnimatePresence>
            {open && pos && (
              <motion.div
                ref={panelRef}
                initial={{ opacity: 0, y: -6, scale: 0.98 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: -6, scale: 0.98 }}
                transition={{ duration: 0.15, ease: 'easeOut' }}
                style={{ position: 'fixed', left: pos.left, width: Math.max(pos.width, 220), top: pos.top, bottom: pos.bottom, maxHeight: pos.maxHeight }}
                // admin-shell: portaled to document.body, outside the page's own .admin-shell
                // wrapper, so every --admin-* custom property is undefined at this node without
                // re-declaring the scope here -- see CategoryPicker.tsx for the full explanation.
                className={`admin-shell z-popover overflow-hidden rounded-xl border ${t.panel}`}
              >
                {optionRows(false)}
              </motion.div>
            )}
          </AnimatePresence>,
          document.body,
        )}

      {isMobile &&
        createPortal(
          <AnimatePresence>
            {open && (
              <motion.div
                initial={{ opacity: 0, y: '100%' }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: '100%' }}
                transition={{ type: 'spring', stiffness: 380, damping: 36 }}
                className={`admin-shell fixed inset-0 z-modal flex flex-col ${t.sheetBg}`}
                role="dialog"
                aria-modal="true"
                aria-label={ariaLabel}
              >
                <div className={`flex shrink-0 items-center justify-between border-b p-4 ${t.border}`}>
                  <span className="text-[15px] font-bold text-[color:var(--admin-text)]">{ariaLabel ?? 'Раздел'}</span>
                  <button type="button" onClick={() => setOpen(false)} aria-label="Закрыть" className={`grid h-10 w-10 place-items-center rounded-xl ${t.faint}`}>
                    <XIcon width={17} height={17} />
                  </button>
                </div>
                {optionRows(true)}
              </motion.div>
            )}
          </AnimatePresence>,
          document.body,
        )}
    </div>
  )
}
