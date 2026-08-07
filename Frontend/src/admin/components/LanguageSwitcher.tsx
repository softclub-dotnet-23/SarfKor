import { useEffect, useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import clsx from 'clsx'
import { useLanguage } from '../../i18n/LanguageProvider'
import { useT } from '../../i18n/translations'
import { ChevronDownIcon } from './icons'

const SCHEMES = {
  admin: {
    trigger: 'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)]',
    panel: 'border-[color:var(--admin-border)] bg-[color:var(--admin-card)] shadow-[var(--admin-shadow-lift)]',
    optionSelected: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    option: 'text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]',
  },
} as const

/** Same control, same place in the header, in both cabinets — Admin's original and StorePartner's
 *  copy are literally this one component now, not two independent implementations. */
export function LanguageSwitcher({ scheme = 'admin' }: { scheme?: keyof typeof SCHEMES }) {
  const t = SCHEMES[scheme]
  const { language, setLanguage, options } = useLanguage()
  const tr = useT()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    function onDocClick(e: MouseEvent) {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onDocClick)
    return () => document.removeEventListener('mousedown', onDocClick)
  }, [open])

  return (
    <div ref={rootRef} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label={tr('shell.toggleLanguage')}
        aria-haspopup="listbox"
        aria-expanded={open}
        className={clsx('flex h-9 items-center gap-1 rounded-[10px] border px-2.5 font-[JetBrains_Mono,monospace] text-[11px] font-bold', t.trigger)}
      >
        {language.toUpperCase()}
        <ChevronDownIcon width={11} height={11} className={clsx('transition-transform', open && 'rotate-180')} />
      </button>
      <AnimatePresence>
        {open && (
          <motion.div
            role="listbox"
            initial={{ opacity: 0, y: -6, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.15, ease: 'easeOut' }}
            className={clsx('absolute right-0 z-30 mt-1.5 w-28 overflow-hidden rounded-xl border p-1', t.panel)}
          >
            {options.map((o) => (
              <button
                key={o.value}
                role="option"
                aria-selected={o.value === language}
                onClick={() => {
                  setLanguage(o.value)
                  setOpen(false)
                }}
                className={clsx(
                  'flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left font-[JetBrains_Mono,monospace] text-[12px] font-bold transition-colors',
                  o.value === language ? t.optionSelected : t.option,
                )}
              >
                {o.label}
              </button>
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
