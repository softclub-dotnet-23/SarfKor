import { motion } from 'framer-motion'
import { useId } from 'react'

const SCHEMES = {
  admin: {
    track: 'border-b border-[color:var(--admin-border)]',
    indicator: 'bg-[color:var(--admin-accent)]',
    activeText: 'text-[color:var(--admin-text)]',
    inactiveText: 'text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]',
  },
  mod: {
    track: 'border-b border-[color:var(--mod-border)]',
    indicator: 'bg-[color:var(--mod-accent)]',
    activeText: 'text-[color:var(--mod-text)]',
    inactiveText: 'text-[color:var(--mod-muted)] hover:text-[color:var(--mod-text)]',
  },
} as const

interface TabsProps<T extends string> {
  value: T
  onChange: (value: T) => void
  options: readonly { value: T; label: string }[]
  scheme?: keyof typeof SCHEMES
  className?: string
}

export function Tabs<T extends string>({ value, onChange, options, scheme = 'admin', className = '' }: TabsProps<T>) {
  const t = SCHEMES[scheme]
  const layoutId = useId()
  return (
    <div className={`flex items-end gap-1 ${t.track} ${className}`} role="tablist">
      {options.map((o) => {
        const active = o.value === value
        return (
          <button
            key={o.value}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() => onChange(o.value)}
            className={`relative px-3.5 pb-2.5 pt-1.5 text-[13px] font-[500] transition-colors duration-150 ${active ? t.activeText : t.inactiveText}`}
          >
            {o.label}
            {active && (
              <motion.span
                layoutId={layoutId}
                className={`absolute bottom-0 left-0 right-0 h-[2px] ${t.indicator}`}
                transition={{ type: 'spring', stiffness: 380, damping: 32 }}
              />
            )}
          </button>
        )
      })}
    </div>
  )
}
