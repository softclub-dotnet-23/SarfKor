import type { CSSProperties, ReactNode } from 'react'
import clsx from 'clsx'

/**
 * New surface primitive for the cabinet redesign. Deliberately not `Card`:
 * no ring border, no flat single shadow — a soft top hairline (light catching
 * the upper edge) plus an ambient shadow, 28px radius matching the landing's
 * own card scale. This is the one surface every cabinet composition below
 * builds on.
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
        boxShadow: '0 1px 0 0 color-mix(in srgb, var(--admin-text) 6%, transparent) inset, var(--admin-shadow)',
        ...style,
      }}
    >
      {children}
    </div>
  )
}

/** Eyebrow + title + optional trailing action — the header every panel
 *  section below opens with, instead of ad hoc `text-[16px] font-bold` runs
 *  repeated per page. */
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
          <div className="mb-1 text-[10.5px] font-bold uppercase tracking-[0.14em] text-[color:var(--admin-text-tertiary)]">
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
 * KPI presentation, replacing the boxed-icon `KpiCard`: number leads at full
 * weight, label sits above it as an eyebrow, and a thin left rule carries the
 * accent colour instead of a tinted icon badge — the number itself is the
 * hero, not an icon next to it.
 */
export function Stat({
  label,
  value,
  suffix,
  accent = 'var(--admin-accent)',
}: {
  label: string
  value: string | number
  suffix?: string
  accent?: string
}) {
  return (
    <div className="relative pl-4">
      <span className="absolute inset-y-0.5 left-0 w-[3px] rounded-full" style={{ background: accent }} aria-hidden />
      <div className="text-[11px] font-semibold uppercase tracking-[0.1em] text-[color:var(--admin-text-tertiary)]">
        {label}
      </div>
      <div className="mt-2 whitespace-nowrap text-[30px] font-extrabold leading-none tracking-tight text-[color:var(--admin-text)]">
        {value}
        {suffix ? <span className="ml-1.5 text-[15px] font-semibold text-[color:var(--admin-text-tertiary)]">{suffix}</span> : null}
      </div>
    </div>
  )
}

/** A single line in a list of records (shift, alert, order…) — borderless,
 *  separated by hairlines from its siblings rather than boxed individually,
 *  so a list of these reads as one continuous panel instead of stacked
 *  cards-within-a-card. */
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
  const toneVar = {
    neutral: 'var(--admin-text-tertiary)',
    accent: 'var(--admin-accent)',
    warning: 'var(--admin-warning)',
    danger: 'var(--admin-danger)',
  }[iconTone]
  return (
    <div className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
      {icon && (
        <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full" style={{ color: toneVar, background: `color-mix(in srgb, ${toneVar} 14%, transparent)` }}>
          {icon}
        </span>
      )}
      <div className="min-w-0 flex-1">
        <div className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{title}</div>
        {subtitle && <div className="mt-0.5 truncate text-[11.5px] text-[color:var(--admin-text-tertiary)]">{subtitle}</div>}
      </div>
      {trailing && <div className="shrink-0 text-[13px] font-bold text-[color:var(--admin-text)]">{trailing}</div>}
    </div>
  )
}

/** Divider between `Row`s inside a `Panel` list — a single hairline shared
 *  by the list rather than a border baked into each row. */
export function RowDivider() {
  return <div className="h-px bg-[color:var(--admin-border)]" />
}

export function EmptyRow({ children }: { children: ReactNode }) {
  return <p className="py-3 text-[12.5px] text-[color:var(--admin-text-tertiary)]">{children}</p>
}
