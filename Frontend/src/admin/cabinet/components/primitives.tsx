import { type CSSProperties, type ReactNode } from 'react'
import clsx from 'clsx'

/**
 * Film-dark surface primitive. Glass panel on dark, frosted on light.
 * 20px radius, subtle inset top-edge highlight, cinematic shadow.
 */
export function Panel({
  children,
  className,
  style,
  padded = true,
}: {
  children: ReactNode
  className?: string
  style?: CSSProperties
  padded?: boolean
}) {
  return (
    <div
      className={clsx('relative rounded-[20px] bg-[color:var(--admin-card)]', padded && 'p-6', className)}
      style={{
        boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.06), var(--admin-shadow)',
        border: '1px solid var(--admin-border)',
        ...style,
      }}
    >
      {children}
    </div>
  )
}

/** Page-level heading (24/600) + optional action. */
export function PageHeader({
  title,
  action,
}: {
  title: string
  action?: ReactNode
}) {
  return (
    <div className="mb-6 flex items-center justify-between gap-4">
      <h1 className="text-[24px] font-[600] tracking-tight text-[color:var(--admin-text)]">{title}</h1>
      {action}
    </div>
  )
}

/** Section header (18/500) + optional eyebrow + trailing action. */
export function SectionHeader({
  eyebrow,
  title,
  action,
}: {
  eyebrow?: string
  title: string
  action?: ReactNode
}) {
  return (
    <div className="mb-5 flex items-start justify-between gap-4">
      <div>
        {eyebrow && (
          <div className="mb-1 text-[11px] font-[400] uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">
            {eyebrow}
          </div>
        )}
        <h2 className="text-[18px] font-[500] tracking-tight text-[color:var(--admin-text)]">{title}</h2>
      </div>
      {action}
    </div>
  )
}

/**
 * KPI stat: flat cinematic surface. 32/500 number, 12/400 muted label.
 * Hairline left rule in accent color — the number is the hero.
 */
export function Stat({
  label,
  value,
  suffix,
  accent = 'var(--admin-text-tertiary)',
}: {
  label: string
  value: string | number
  suffix?: string | null
  accent?: string
}) {
  return (
    <div className="relative pl-4">
      <span className="absolute inset-y-1 left-0 w-[2px] rounded-full" style={{ background: accent }} aria-hidden />
      <div className="text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">
        {label}
      </div>
      <div className="mt-1.5 whitespace-nowrap text-[32px] font-[500] leading-none tabular-nums text-[color:var(--admin-text)]">
        {value}
        {suffix ? (
          <span className="ml-1.5 text-[14px] font-[400] text-[color:var(--admin-text-tertiary)]">{suffix}</span>
        ) : null}
      </div>
    </div>
  )
}

/** Single record row inside a Panel list — no card border of its own. */
export function Row({
  icon,
  iconTone = 'neutral',
  title,
  subtitle,
  trailing,
}: {
  icon?: ReactNode
  iconTone?: 'neutral' | 'accent' | 'warning' | 'danger'
  title: ReactNode
  subtitle?: ReactNode
  trailing?: ReactNode
}) {
  const toneColor = {
    neutral: 'var(--admin-text-tertiary)',
    accent: 'var(--admin-accent)',
    warning: 'var(--admin-warning)',
    danger: 'var(--admin-danger)',
  }[iconTone]

  return (
    <div className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
      {icon && (
        <span
          className="grid h-8 w-8 shrink-0 place-items-center rounded-full"
          style={{
            color: toneColor,
            background: `color-mix(in srgb, ${toneColor} 12%, transparent)`,
          }}
        >
          {icon}
        </span>
      )}
      <div className="min-w-0 flex-1">
        <div className="truncate text-[14px] font-[400] text-[color:var(--admin-text)]">{title}</div>
        {subtitle && (
          <div className="mt-0.5 truncate text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">{subtitle}</div>
        )}
      </div>
      {trailing && (
        <div className="shrink-0 text-[14px] font-[400] tabular-nums text-[color:var(--admin-text-secondary)]">{trailing}</div>
      )}
    </div>
  )
}

export function RowDivider() {
  return <div className="h-px bg-[color:var(--admin-border)]" />
}

export function EmptyRow({ children }: { children: ReactNode }) {
  return <p className="py-3 text-[14px] font-[400] text-[color:var(--admin-text-tertiary)]">{children}</p>
}

/**
 * Primary CTA — white bg / black text in dark mode, black bg / white text in light.
 * 8px radius, 10px×20px padding, 500 weight. Hover: scale(1.01) + shadow, 150ms.
 */
export function PrimaryButton({
  children,
  onClick,
  type = 'button',
  disabled,
  className,
}: {
  children: ReactNode
  onClick?: () => void
  type?: 'button' | 'submit'
  disabled?: boolean
  className?: string
}) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={clsx(
        'inline-flex items-center justify-center gap-2',
        'rounded-[8px] px-5 py-[10px] text-[14px] font-[500]',
        'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]',
        'transition-all duration-150 ease-out',
        'hover:scale-[1.01] hover:shadow-[0_4px_16px_rgba(0,0,0,0.18)]',
        'active:scale-[0.99] disabled:pointer-events-none disabled:opacity-40',
        className,
      )}
    >
      {children}
    </button>
  )
}

/** Ghost — transparent, hairline border, 8px radius. */
export function GhostButton({
  children,
  onClick,
  type = 'button',
  disabled,
  className,
}: {
  children: ReactNode
  onClick?: () => void
  type?: 'button' | 'submit'
  disabled?: boolean
  className?: string
}) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={clsx(
        'inline-flex items-center justify-center gap-2',
        'rounded-[8px] border border-[color:var(--admin-border)] px-5 py-[10px] text-[14px] font-[500]',
        'text-[color:var(--admin-text-secondary)] transition-all duration-150 ease-out',
        'hover:border-[color:var(--admin-border-strong)] hover:text-[color:var(--admin-text)] hover:scale-[1.01]',
        'active:scale-[0.99] disabled:pointer-events-none disabled:opacity-40',
        className,
      )}
    >
      {children}
    </button>
  )
}
