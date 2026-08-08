import type { ReactNode } from 'react'

// Promotes the ~70 raw-hex status pills scattered across admin pages
// (#f87171/#34d399/#fbbf24 for danger/success/warning) onto the semantic
// --admin-* tokens added alongside this component — a fixed status palette used
// everywhere across the StorePartner cabinet and the platform Admin console:
// green=active, amber=needs attention
// (expiring/overdue), red=blocked/stopped, neutral grey=waiting.
const VARIANTS = {
  admin: {
    success: 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]',
    danger: 'bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)]',
    warning: 'bg-[color:var(--admin-warning-dim)] text-[color:var(--admin-warning)]',
    accent: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    neutral: 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-tertiary)]',
  },
} as const

const SIZES = {
  md: 'px-2.5 py-1 text-[11px]',
  sm: 'px-2 py-0.5 text-[10.5px]',
} as const

type Scheme = keyof typeof VARIANTS
type Variant = keyof (typeof VARIANTS)['admin']
type Size = keyof typeof SIZES

export function Badge({
  children,
  variant = 'neutral',
  scheme = 'admin',
  size = 'md',
  className = '',
}: {
  children: ReactNode
  variant?: Variant
  scheme?: Scheme
  size?: Size
  className?: string
}) {
  return (
    <span
      className={`inline-flex w-fit shrink-0 items-center gap-1 rounded-full font-semibold ${SIZES[size]} ${VARIANTS[scheme][variant]} ${className}`}
    >
      {children}
    </span>
  )
}
