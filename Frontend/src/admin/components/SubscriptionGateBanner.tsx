import { Link, useLocation } from 'react-router-dom'
import { useT } from '../../i18n/translations'
import { useLocaleFormat } from '../../i18n/format'
import { useSubscriptionGate } from '../../lib/subscriptionGate'
import { CardIcon } from './icons'

/**
 * The section-level state ADMIN_PROMPT asked for: instead of letting the cashier/owner fill out a
 * form that can't be submitted and only finding out at the end, this shows the "почему" up front,
 * on every page under CabinetShell/CashierShell (mounted once there, not copy-pasted per page) --
 * so it's visible no matter which section they're in.
 *
 * Non-dismissible on purpose: this reflects a real, persistent billing state, not a one-off notice
 * -- hiding it wouldn't change that write actions are actually blocked underneath.
 */
export function SubscriptionGateBanner() {
  const { loading, isOperational, info, isOwner } = useSubscriptionGate()
  const t = useT()
  const { date } = useLocaleFormat()
  const location = useLocation()

  if (loading || isOperational) return null

  const onSettingsPage = location.pathname.startsWith('/admin/settings')

  return (
    <div
      role="alert"
      className="mb-4 flex flex-wrap items-center gap-3 rounded-xl border border-[color:var(--admin-danger-dim)] bg-[color:var(--admin-danger-dim)] px-4 py-3"
    >
      <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-[color:var(--admin-danger)] text-[color:var(--admin-danger-fg)]">
        <CardIcon width={16} height={16} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-[13px] font-semibold text-[color:var(--admin-danger)]">
          {isOwner ? t('common.errorSubscriptionInactiveOwner') : t('common.errorSubscriptionInactiveCashier')}
        </p>
        {isOwner && info?.currentPeriodEndsAt && (
          <p className="text-[12px] text-[color:var(--admin-text-secondary)]">
            {t('common.subscriptionExpiredOn', { date: date(info.currentPeriodEndsAt) })}
          </p>
        )}
      </div>
      {isOwner && !onSettingsPage && (
        <Link
          to="/admin/settings"
          className="shrink-0 rounded-lg bg-[color:var(--admin-danger)] px-3.5 py-2 text-[12.5px] font-semibold text-[color:var(--admin-danger-fg)] transition-opacity hover:opacity-90"
        >
          {t('common.subscriptionGoToSettings')}
        </Link>
      )}
    </div>
  )
}
