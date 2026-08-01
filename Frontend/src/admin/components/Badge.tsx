import type { ReactNode } from 'react'

// Promotes the ~70 raw-hex status pills scattered across admin pages
// (#f87171/#34d399/#fbbf24 for danger/success/warning) onto the semantic
// --admin-* tokens added alongside this component. 'mod' reuses the
// moderation shell's existing --mod-ok/-warn/-danger, which are deliberately
// calmer (warn is grey, not amber) — that's the platform-Admin surface's
// existing restrained palette, not an oversight, so it's left as-is here.
const VARIANTS = {
  admin: {
    success: 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]',
    danger: 'bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)]',
    warning: 'bg-[color:var(--admin-warning-dim)] text-[color:var(--admin-warning)]',
    accent: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    neutral: 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-tertiary)]',
  },
  mod: {
    success: 'bg-[color:var(--mod-ok-dim)] text-[color:var(--mod-ok)]',
    danger: 'bg-[color:var(--mod-danger-dim)] text-[color:var(--mod-danger)]',
    warning: 'bg-[color:var(--mod-warn-dim)] text-[color:var(--mod-warn)]',
    accent: 'bg-[color:var(--mod-accent-dim)] text-[color:var(--mod-accent2)]',
    neutral: 'bg-[color:var(--mod-panel2)] text-[color:var(--mod-faint)]',
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
