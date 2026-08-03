import type { CSSProperties, ReactNode } from 'react'
import clsx from 'clsx'

/**
 * Film-dark surface primitive. Glass panel on dark, frosted on light.
 * 28px radius, subtle inset top-edge highlight, cinematic shadow.
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
      className={clsx('relative rounded-[28px] bg-[color:var(--admin-card)]', padded && 'p-6', className)}
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

/** Eyebrow (tracking-widest uppercase) + title + optional trailing action. */
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
          <div className="mb-1.5 text-[10px] font-bold uppercase tracking-[0.16em] text-[color:var(--admin-text-tertiary)]">
            {eyebrow}
          </div>
        )}
        <h2 className="text-[17px] font-extrabold tracking-tight text-[color:var(--admin-text)]">{title}</h2>
      </div>
      {action}
    </div>
  )
}

/**
 * KPI stat: number leads at full weight, label is a small eyebrow above it.
 * A 3px left rule in the accent color is the only visual affordance — the
 * number itself is the hero.
 */
export function Stat({
  label,
  value,
  suffix,
  accent = 'var(--admin-text-tertiary)',
}: {
  label: string
  value: string | number
  suffix?: string
  accent?: string
}) {
  return (
    <div className="relative pl-4">
      <span className="absolute inset-y-1 left-0 w-[2px] rounded-full" style={{ background: accent }} aria-hidden />
      <div className="text-[10.5px] font-bold uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">
        {label}
      </div>
      <div className="mt-2 whitespace-nowrap text-[32px] font-black leading-none tracking-tighter text-[color:var(--admin-text)]">
        {value}
        {suffix ? (
          <span className="ml-2 text-[14px] font-semibold text-[color:var(--admin-text-tertiary)]">{suffix}</span>
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
            background: `color-mix(in srgb, ${toneColor} 13%, transparent)`,
          }}
        >
          {icon}
        </span>
      )}
      <div className="min-w-0 flex-1">
        <div className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{title}</div>
        {subtitle && (
          <div className="mt-0.5 truncate text-[11.5px] text-[color:var(--admin-text-tertiary)]">{subtitle}</div>
        )}
      </div>
      {trailing && (
        <div className="shrink-0 text-[13px] font-bold tabular-nums text-[color:var(--admin-text)]">{trailing}</div>
      )}
    </div>
  )
}

export function RowDivider() {
  return <div className="h-px bg-[color:var(--admin-border)]" />
}

export function EmptyRow({ children }: { children: ReactNode }) {
  return <p className="py-3 text-[13px] text-[color:var(--admin-text-tertiary)]">{children}</p>
}

/** Primary CTA button — uses accent as background, accent-fg as text so both
 *  dark (white button, black text) and light (black button, white text) modes
 *  read correctly without hardcoding a color in the component. */
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
        'rounded-2xl px-5 py-2.5 text-[13.5px] font-bold transition-all duration-200',
        'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]',
        'hover:opacity-90 active:scale-[0.98] disabled:pointer-events-none disabled:opacity-40',
        className,
      )}
    >
      {children}
    </button>
  )
}

/** Ghost button — transparent with a subtle border. */
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
        'rounded-2xl border border-[color:var(--admin-border)] px-5 py-2.5 text-[13.5px] font-semibold',
        'text-[color:var(--admin-text-secondary)] transition-all duration-200',
        'hover:border-[color:var(--admin-border-strong)] hover:text-[color:var(--admin-text)]',
        'active:scale-[0.98] disabled:pointer-events-none disabled:opacity-40',
        className,
      )}
    >
      {children}
    </button>
  )
}
