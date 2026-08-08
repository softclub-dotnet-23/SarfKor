import { useEffect, useId, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { AnimatePresence, motion } from 'framer-motion'
import clsx from 'clsx'
import { useLanguage } from '../../i18n/LanguageProvider'
import { useT } from '../../i18n/translations'
import { useIsMobile } from '../../hooks/useMediaQuery'
import { useFloatingPosition } from '../../lib/useFloatingPosition'
import { lockBodyScroll, unlockBodyScroll } from '../../lib/scrollLock'
import { ChevronDownIcon, CheckIcon, XIcon } from './icons'

const SCHEMES = {
  admin: {
    trigger: 'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)]',
    // Opaque --admin-sidebar, not the translucent --admin-card "glass" tone -- this panel portals
    // to document.body and floats over arbitrary page content, so it needs a surface that reads
    // as solid regardless of what's behind it. Same fix as Select.tsx/CategoryPicker.tsx.
    panel: 'border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] shadow-[var(--admin-shadow-lift)]',
    optionSelected: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    optionActive: 'bg-[color:var(--admin-hover)]',
    option: 'text-[color:var(--admin-text)]',
    faint: 'text-[color:var(--admin-text-tertiary)]',
    border: 'border-[color:var(--admin-border)]',
    sheetBg: 'bg-[color:var(--admin-content)]',
  },
} as const

/** Same control, same place in the header, in both cabinets — Admin's original and StorePartner's
 *  copy are literally this one component now, not two independent implementations. Built on the
 *  same portal/floating-position/keyboard-nav architecture as Select.tsx (its trigger is just a
 *  compact "RU ⌄" pill instead of a full-width field, so it doesn't delegate to Select itself). */
export function LanguageSwitcher({ scheme = 'admin' }: { scheme?: keyof typeof SCHEMES }) {
  const t = SCHEMES[scheme]
  const { language, setLanguage, options } = useLanguage()
  const tr = useT()
  const isMobile = useIsMobile()
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(() => Math.max(0, options.findIndex((o) => o.value === language)))
  const rootRef = useRef<HTMLDivElement>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const listId = useId()
  const pos = useFloatingPosition(rootRef, open && !isMobile)

  function openPanel() {
    setActiveIndex(Math.max(0, options.findIndex((o) => o.value === language)))
    setOpen(true)
  }

  function commit(index: number) {
    const opt = options[index]
    if (opt) setLanguage(opt.value)
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

  const optionRows = (large: boolean) => (
    <div role="listbox" id={listId} aria-label={tr('shell.toggleLanguage')} className={large ? 'flex-1 overflow-y-auto p-2' : undefined}>
      {options.map((o, idx) => (
        <div
          key={o.value}
          id={`${listId}-opt-${o.value}`}
          role="option"
          aria-selected={o.value === language}
          onMouseEnter={() => setActiveIndex(idx)}
          onClick={() => commit(idx)}
          className={clsx(
            'flex cursor-pointer items-center justify-between gap-2 rounded-lg text-left font-[JetBrains_Mono,monospace] font-bold transition-colors',
            large ? 'min-h-12 px-3.5 text-[15px]' : 'px-3 py-2 text-[12px]',
            o.value === language ? t.optionSelected : idx === activeIndex ? t.optionActive : t.option,
          )}
        >
          {o.label}
          {o.value === language && <CheckIcon width={large ? 16 : 12} height={large ? 16 : 12} className="shrink-0" />}
        </div>
      ))}
    </div>
  )

  function onKeyDown(e: React.KeyboardEvent) {
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

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        onClick={() => (open ? setOpen(false) : openPanel())}
        onKeyDown={onKeyDown}
        aria-label={tr('shell.toggleLanguage')}
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        aria-activedescendant={open && options[activeIndex] ? `${listId}-opt-${options[activeIndex].value}` : undefined}
        className={clsx('flex h-9 items-center gap-1 rounded-[10px] border px-2.5 font-[JetBrains_Mono,monospace] text-[11px] font-bold outline-none', t.trigger)}
      >
        {language.toUpperCase()}
        <ChevronDownIcon width={11} height={11} className={clsx('transition-transform', open && 'rotate-180')} />
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
                style={{ position: 'fixed', left: pos.left, width: Math.max(pos.width, 112), top: pos.top, bottom: pos.bottom, maxHeight: pos.maxHeight }}
                // admin-shell: portaled to document.body, outside the page's own .admin-shell
                // wrapper -- see Select.tsx/CategoryPicker.tsx for the full explanation.
                className={clsx('admin-shell z-popover overflow-auto rounded-xl border p-1', t.panel)}
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
                className={clsx('admin-shell fixed inset-0 z-modal flex flex-col', t.sheetBg)}
                role="dialog"
                aria-modal="true"
                aria-label={tr('shell.toggleLanguage')}
              >
                <div className={clsx('flex shrink-0 items-center justify-between border-b p-4', t.border)}>
                  <span className="text-[15px] font-bold text-[color:var(--admin-text)]">{tr('shell.toggleLanguage')}</span>
                  <button type="button" onClick={() => setOpen(false)} aria-label="Закрыть" className={clsx('grid h-10 w-10 place-items-center rounded-xl', t.faint)}>
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
