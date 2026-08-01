import type { CSSProperties, ReactNode } from 'react'
import clsx from 'clsx'

interface CardProps {
  children: ReactNode
  className?: string
  style?: CSSProperties
}

export function Card({ children, className, style }: CardProps) {
  return (
    <div
      className={clsx(
        'rounded-[18px] bg-[color:var(--admin-card)] ring-1 ring-[color:var(--admin-border)] [box-shadow:var(--admin-shadow)]',
        className,
      )}
      style={style}
    >
      {children}
    </div>
  )
}
