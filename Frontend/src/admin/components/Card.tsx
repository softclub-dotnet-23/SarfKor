import type { CSSProperties, ReactNode } from 'react'
import clsx from 'clsx'

const SCHEMES = {
  admin: {
    surface: 'rounded-2xl bg-[color:var(--admin-card)] ring-1 ring-[color:var(--admin-border)] [box-shadow:var(--admin-shadow)]',
    lift: 'hover:[box-shadow:var(--admin-shadow-lift)]',
  },
  mod: {
    surface: 'rounded-2xl bg-[color:var(--mod-panel)] ring-1 ring-[color:var(--mod-border)] [box-shadow:var(--mod-shadow)]',
    lift: 'hover:[box-shadow:var(--mod-shadow-lift)]',
  },
} as const

interface CardProps {
  children: ReactNode
  className?: string
  style?: CSSProperties
  scheme?: keyof typeof SCHEMES
  /** Adds a hover-lift toward the scheme's elevated shadow, for cards that act
   *  as a trigger (nav to a detail view, open a modal) rather than static display. */
  interactive?: boolean
}

export function Card({ children, className, style, scheme = 'admin', interactive }: CardProps) {
  const t = SCHEMES[scheme]
  return (
    <div
      className={clsx(
        t.surface,
        interactive && `transition-[box-shadow,transform] duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] hover:-translate-y-0.5 ${t.lift}`,
        className,
      )}
      style={style}
    >
      {children}
    </div>
  )
}
