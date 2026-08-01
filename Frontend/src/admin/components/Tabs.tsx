import { useId } from 'react'
import { motion } from 'framer-motion'

// Replaces the three near-identical segmented-control reimplementations in
// MarketingPage/SupplyPage/ReportsPage. The sliding pill echoes the same
// layoutId-driven indicator the landing/consumer nav already uses, so this
// reads as the same interaction language rather than a fourth bespoke one.
const SCHEMES = {
  admin: {
    track: 'bg-[color:var(--admin-hover)]',
    pill: 'bg-[color:var(--admin-card)] shadow-[var(--admin-shadow)]',
    activeText: 'text-[color:var(--admin-text)]',
    inactiveText: 'text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]',
  },
  mod: {
    track: 'bg-[color:var(--mod-panel2)]',
    pill: 'bg-[color:var(--mod-elev)] shadow-[var(--mod-shadow)]',
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
  // Unique per mount so two Tabs on the same page never share a layoutId
  // (framer-motion matches layoutId globally, not per-component-instance).
  const layoutId = useId()
  return (
    <div className={`inline-flex items-center gap-1 rounded-full p-1 ${t.track} ${className}`} role="tablist">
      {options.map((o) => {
        const active = o.value === value
        return (
          <button
            key={o.value}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() => onChange(o.value)}
            className={`relative rounded-full px-3.5 py-1.5 text-[12.5px] font-semibold transition-colors duration-300 ${active ? t.activeText : t.inactiveText}`}
          >
            {active && (
              <motion.span
                layoutId={layoutId}
                className={`absolute inset-0 -z-10 rounded-full ${t.pill}`}
                transition={{ type: 'spring', stiffness: 380, damping: 32 }}
              />
            )}
            {o.label}
          </button>
        )
      })}
    </div>
  )
}
