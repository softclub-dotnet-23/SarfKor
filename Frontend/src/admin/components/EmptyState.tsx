import type { ReactNode } from 'react'
import { CheckIcon } from './icons'

// Promoted from ModerationPage's local EmptyState (previously the only
// polished version in the admin codebase) so every page's "no data" state
// gets the same icon-badge treatment instead of a bare line of text.
const SCHEMES = {
  admin: {
    icon: 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]',
    title: 'text-[color:var(--admin-text)]',
    body: 'text-[color:var(--admin-text-tertiary)]',
  },
} as const

export function EmptyState({
  title,
  body,
  icon,
  action,
  scheme = 'admin',
}: {
  title: string
  body?: string
  icon?: ReactNode
  action?: ReactNode
  scheme?: keyof typeof SCHEMES
}) {
  const t = SCHEMES[scheme]
  return (
    <div className="flex flex-col items-center justify-center gap-4 px-6 py-16 text-center">
      <span className={`grid h-14 w-14 place-items-center rounded-full ${t.icon}`}>
        {icon ?? <CheckIcon width={26} height={26} strokeWidth={2.5} />}
      </span>
      <div>
        <div className={`text-[15px] font-bold ${t.title}`}>{title}</div>
        {body && <div className={`mt-1 max-w-xs text-[13px] leading-relaxed ${t.body}`}>{body}</div>}
      </div>
      {action && <div className="mt-1">{action}</div>}
    </div>
  )
}
