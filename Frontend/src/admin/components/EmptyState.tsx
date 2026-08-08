import type { ReactNode } from 'react'
import { CheckIcon } from './icons'

// Promoted from ModerationPage's local EmptyState (previously the only
// polished version in the admin codebase) so every page's "no data" state
// gets the same icon-badge treatment instead of a bare line of text.
const SCHEMES = {
  admin: {
    title: 'text-[color:var(--admin-text)]',
    body: 'text-[color:var(--admin-text-tertiary)]',
  },
} as const

// 'success' (the default CheckIcon-in-a-green-circle look) reads as "done/all clear" — right for
// "Явных дубликатов не найдено", wrong for "nothing exists here yet" (e.g. an empty category
// tree, which isn't an achievement). 'neutral' swaps the circle to the same muted tone used for
// tertiary text/icons everywhere else, no color implying success either way.
const TONES = {
  success: 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]',
  neutral: 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-tertiary)]',
} as const

export function EmptyState({
  title,
  body,
  icon,
  action,
  scheme = 'admin',
  tone = 'success',
}: {
  title: string
  body?: string
  icon?: ReactNode
  action?: ReactNode
  scheme?: keyof typeof SCHEMES
  tone?: keyof typeof TONES
}) {
  const t = SCHEMES[scheme]
  return (
    <div className="flex flex-col items-center justify-center gap-4 px-6 py-16 text-center">
      <span className={`grid h-14 w-14 place-items-center rounded-full ${TONES[tone]}`}>
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
