import { useEffect, useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { ChevronDownIcon, CheckIcon } from './icons'

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
    panel: 'border-[color:var(--admin-border)] bg-[color:var(--admin-card)] shadow-[var(--admin-shadow)]',
    option: 'text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]',
    optionSelected: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    empty: 'text-[color:var(--admin-text-tertiary)]',
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
}

export function Select({
  value,
  onChange,
  options,
  placeholder = 'Выберите…',
  className = '',
  disabled,
  scheme = 'admin',
  size = 'md',
}: SelectProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const t = SCHEMES[scheme]
  const selected = options.find((o) => o.value === value)

  useEffect(() => {
    if (!open) return
    function onDocClick(e: MouseEvent) {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onDocClick)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDocClick)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  return (
    <div ref={rootRef} className={`relative ${className}`}>
      <button
        type="button"
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
        className={`flex w-full items-center justify-between gap-2 border text-left outline-none transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${SIZES[size]} ${t.trigger}`}
      >
        <span className={`truncate ${selected ? '' : t.placeholder}`}>{selected ? selected.label : placeholder}</span>
        <ChevronDownIcon width={14} height={14} className={`shrink-0 transition-transform ${t.chevron} ${open ? 'rotate-180' : ''}`} />
      </button>
      <AnimatePresence>
        {open && (
          <motion.div
            role="listbox"
            initial={{ opacity: 0, y: -6, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.15, ease: 'easeOut' }}
            className={`absolute z-30 mt-1.5 max-h-60 w-full min-w-fit overflow-auto rounded-xl border p-1 ${t.panel}`}
          >
            {options.length === 0 && <div className={`px-3 py-2 text-[13px] ${t.empty}`}>Нет вариантов</div>}
            {options.map((o) => (
              <button
                key={o.value}
                type="button"
                role="option"
                aria-selected={o.value === value}
                onClick={() => {
                  onChange(o.value)
                  setOpen(false)
                }}
                className={`flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-left text-[13px] font-medium transition-colors ${
                  o.value === value ? t.optionSelected : t.option
                }`}
              >
                <span className="truncate">{o.label}</span>
                {o.value === value && <CheckIcon width={14} height={14} className="shrink-0" />}
              </button>
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
