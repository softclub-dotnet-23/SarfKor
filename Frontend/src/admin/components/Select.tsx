import { useEffect, useId, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useIsMobile } from '../../hooks/useMediaQuery'
import { useFloatingPosition } from '../../lib/useFloatingPosition'
import { lockBodyScroll, unlockBodyScroll } from '../../lib/scrollLock'
import { ChevronDownIcon, CheckIcon, XIcon } from './icons'

export interface SelectOption {
  value: string
  label: string
}

const SCHEMES = {
  admin: {
    trigger:
      'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)] focus-visible:border-[color:var(--admin-accent)]',
    placeholder: 'text-[color:var(--admin-text-tertiary)]',
    chevron: 'text-[color:var(--admin-text-tertiary)]',
    // Opaque --admin-sidebar, not the translucent --admin-card "glass" tone -- this panel portals
    // to document.body and floats over arbitrary page content (a table, a form), so it needs a
    // surface that reads as solid regardless of what's behind it. --admin-card at ~4.5% alpha in
    // dark mode is exactly the "Все статусы" bug: the table underneath showed through the panel.
    panel: 'border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] shadow-[var(--admin-shadow)]',
    option: 'text-[color:var(--admin-text)]',
    optionActive: 'bg-[color:var(--admin-hover)]',
    optionSelected: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    empty: 'text-[color:var(--admin-text-tertiary)]',
    faint: 'text-[color:var(--admin-text-tertiary)]',
    border: 'border-[color:var(--admin-border)]',
    sheetBg: 'bg-[color:var(--admin-content)]',
  },
} as const

const SIZES = {
  md: 'rounded-xl px-3.5 py-2.5 text-[13px]',
  sm: 'rounded-lg px-2.5 py-1.5 text-[12.5px]',
} as const

interface SelectProps {
  value: string
  onChange: (value: string) => void
  options: SelectOption[]
  placeholder?: string
  className?: string
  disabled?: boolean
  scheme?: keyof typeof SCHEMES
  size?: keyof typeof SIZES
  ariaLabel?: string
}

/**
 * The one dropdown-select component for the project — every filter, form field, and switcher
 * that picks one value from a short list goes through this (ADMIN task: "приведи ВСЕ выпадающие
 * списки проекта к одному компоненту"). Shares its floating/portal architecture with
 * CategoryPicker/EntityPicker/SectionSelect: portaled to document.body via useFloatingPosition
 * (never clipped by an ancestor's overflow, flips above the trigger when short on room below),
 * and a full-screen sheet with large rows below the mobile breakpoint.
 *
 * Keyboard: the trigger keeps DOM focus the whole time it's open (options are inert `role="option"`
 * rows, not separately focusable) — ArrowUp/Down move the active option, Enter/Space commits it,
 * Escape closes without changing the value, Tab closes and lets focus continue past the trigger.
 */
export function Select({
  value,
  onChange,
  options,
  placeholder = 'Выберите…',
  className = '',
  disabled,
  scheme = 'admin',
  size = 'md',
  ariaLabel,
}: SelectProps) {
  const t = SCHEMES[scheme]
  const isMobile = useIsMobile()
  const listId = useId()
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(() => Math.max(0, options.findIndex((o) => o.value === value)))
  const rootRef = useRef<HTMLDivElement>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const pos = useFloatingPosition(rootRef, open && !isMobile)
  const selected = options.find((o) => o.value === value)

  function openPanel() {
    if (disabled) return
    setActiveIndex(Math.max(0, options.findIndex((o) => o.value === value)))
    setOpen(true)
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
    <div role="listbox" id={listId} aria-label={ariaLabel ?? placeholder} className={large ? 'flex-1 overflow-y-auto p-2' : 'max-h-60 overflow-auto p-1'}>
      {options.length === 0 && <div className={`px-3 py-2 text-[13px] ${t.empty}`}>Нет вариантов</div>}
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
            className={`flex cursor-pointer items-center justify-between gap-2 rounded-lg font-medium transition-colors ${
              large ? 'min-h-12 px-3.5 text-[15px]' : 'px-3 py-2 text-[13px]'
            } ${isSelected ? t.optionSelected : isActive ? t.optionActive : t.option}`}
          >
            <span className="truncate">{o.label}</span>
            {isSelected && <CheckIcon width={large ? 16 : 14} height={large ? 16 : 14} className="shrink-0" />}
          </div>
        )
      })}
    </div>
  )

  return (
    <div ref={rootRef} className={`relative ${className}`}>
      <button
        type="button"
        disabled={disabled}
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        aria-activedescendant={open && options[activeIndex] ? `${listId}-opt-${options[activeIndex].value}` : undefined}
        aria-label={ariaLabel}
        onClick={() => (open ? setOpen(false) : openPanel())}
        onKeyDown={onTriggerKeyDown}
        className={`flex w-full items-center justify-between gap-2 border text-left outline-none transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${SIZES[size]} ${t.trigger}`}
      >
        <span className={`truncate ${selected ? '' : t.placeholder}`}>{selected ? selected.label : placeholder}</span>
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
                style={{ position: 'fixed', left: pos.left, width: pos.width, top: pos.top, bottom: pos.bottom, maxHeight: pos.maxHeight }}
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
                aria-label={ariaLabel ?? placeholder}
              >
                <div className={`flex shrink-0 items-center justify-between border-b p-4 ${t.border}`}>
                  <span className="text-[15px] font-bold text-[color:var(--admin-text)]">{ariaLabel ?? placeholder}</span>
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
