import { Badge } from './Badge'
import type { StoreStatus, SubscriptionStatus } from '../../lib/api'

// One status->color mapping per status type, reused everywhere it appears (store list, store
// card, subscriptions page, metrics) so the same status never looks different in two places
// (ADMIN_PROMPT.md §4): neutral=waiting, green=active, amber=needs attention, red=blocked/stopped.

const STORE_STATUS: Record<StoreStatus, { label: string; variant: 'neutral' | 'success' | 'warning' | 'danger' }> = {
  PendingApproval: { label: 'Ожидает подтверждения', variant: 'neutral' },
  Active: { label: 'Активен', variant: 'success' },
  Suspended: { label: 'Приостановлен', variant: 'warning' },
  Blocked: { label: 'Заблокирован', variant: 'danger' },
  Archived: { label: 'Архивирован', variant: 'neutral' },
  Rejected: { label: 'Отклонён', variant: 'danger' },
}

const SUBSCRIPTION_STATUS: Record<SubscriptionStatus, { label: string; variant: 'neutral' | 'success' | 'warning' | 'danger' }> = {
  Trial: { label: 'Пробный период', variant: 'neutral' },
  Active: { label: 'Активна', variant: 'success' },
  PastDue: { label: 'Просрочена', variant: 'warning' },
  Suspended: { label: 'Приостановлена', variant: 'danger' },
  Cancelled: { label: 'Отменена', variant: 'neutral' },
}

export function StoreStatusBadge({ status, size = 'md' }: { status: StoreStatus; size?: 'md' | 'sm' }) {
  const s = STORE_STATUS[status]
  return (
    <Badge scheme="mod" variant={s.variant} size={size}>
      {s.label}
    </Badge>
  )
}

export function SubscriptionStatusBadge({ status, size = 'md' }: { status: SubscriptionStatus; size?: 'md' | 'sm' }) {
  const s = SUBSCRIPTION_STATUS[status]
  return (
    <Badge scheme="mod" variant={s.variant} size={size}>
      {s.label}
    </Badge>
  )
}
