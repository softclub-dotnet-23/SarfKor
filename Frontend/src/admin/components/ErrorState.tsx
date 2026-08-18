import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useT } from '../../i18n/translations'
import { useLocaleFormat } from '../../i18n/format'
import { useAuth } from '../../auth/AuthContext'
import { storesApi, type MyStoreSubscriptionStatus } from '../../lib/api'
import { classifyError, type ErrorKind } from '../../lib/errorKind'
import { AlertIcon, LockIcon, SearchIcon, WifiOffIcon, CardIcon } from './icons'

// Promoted from ModerationPage's local ErrorState, replacing the bespoke error Card +
// "Повторить" button each page previously hand-rolled -- now the one error screen for every
// failed data-load in the project (admin console + StorePartner/Cashier cabinets). `message` is
// always the caller's specific "what failed to load" line (ADMIN_PROMPT: "внятная формулировка,
// не абстрактная «произошла ошибка»"), e.g. "Не удалось загрузить список сотрудников" -- never
// the raw exception text, which callers log to the console themselves (or, for render crashes,
// RouteErrorBoundary does it for them) instead of showing it to the user.
//
// `kind` (from classifyError(err), or passed explicitly) picks the icon, the secondary "why"
// line, and whether Retry is even offered -- retrying a 403 just 403s again, so 'forbidden' and
// 'notFound' get no retry button, unlike 'server'/'network'/'unknown' which are plausibly
// transient. Defaults to 'unknown' so every existing call site keeps working unchanged; passing
// `kind={classifyError(err)}` from the catch block is what turns on the full distinction.
const ICONS: Record<ErrorKind, typeof AlertIcon> = {
  forbidden: LockIcon,
  notFound: SearchIcon,
  server: AlertIcon,
  network: WifiOffIcon,
  subscriptionInactive: CardIcon,
  conflict: AlertIcon,
  validation: AlertIcon,
  unknown: AlertIcon,
}

/**
 * The "к кому обратиться" half of a 402 that a bare status code can't carry on its own -- fetches
 * the store's own subscription status once (lazy, only for this one kind) and renders the period-
 * end date plus, for an owner specifically, a link to where they'd actually go do something about
 * it. A cashier sees the same date but a "ask your manager" line instead of a dead-end link, since
 * subscription management isn't part of the Cashier cabinet.
 */
function SubscriptionInactiveHint({ storeId }: { storeId: number | null }) {
  const t = useT()
  const { date } = useLocaleFormat()
  const [info, setInfo] = useState<MyStoreSubscriptionStatus | null>(null)

  useEffect(() => {
    if (!storeId) return
    let cancelled = false
    storesApi
      .getMySubscriptionStatus(storeId)
      .then((res) => { if (!cancelled) setInfo(res) })
      .catch(() => {
        // Best-effort enrichment -- if even this fails, the plain 402 detail text above already
        // told the user what's wrong, just without the date/link.
      })
    return () => { cancelled = true }
  }, [storeId])

  if (!info || info.outcome !== 'Found') return null

  return (
    <div className="mt-1 flex flex-col items-center gap-1.5">
      {info.currentPeriodEndsAt && (
        <p className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">
          {t('common.subscriptionExpiredOn', { date: date(info.currentPeriodEndsAt) })}
        </p>
      )}
      {info.isOwner ? (
        <Link
          to="/admin/settings"
          className="text-[12.5px] font-semibold text-[color:var(--admin-accent)] hover:opacity-70"
        >
          {t('common.subscriptionGoToSettings')}
        </Link>
      ) : (
        <p className="max-w-xs text-[12px] leading-relaxed text-[color:var(--admin-text-tertiary)]">
          {t('common.subscriptionContactAdmin')}
        </p>
      )}
    </div>
  )
}

export function ErrorState({
  message,
  kind = 'unknown',
  onRetry,
}: {
  /** The specific "what failed" line -- e.g. "Не удалось загрузить список сотрудников". */
  message: string
  kind?: ErrorKind
  onRetry?: () => void
  /** @deprecated no longer needed -- there is only one scheme. Kept so existing call sites
   *  passing scheme="admin" don't need touching; any value is accepted and ignored. */
  scheme?: string
}) {
  const t = useT()
  const { storeId } = useAuth()
  const Icon = ICONS[kind]
  const detail = {
    forbidden: t('common.errorForbidden'),
    notFound: t('common.errorNotFound'),
    server: t('common.errorServer'),
    network: t('common.errorNetwork'),
    subscriptionInactive: t('common.errorSubscriptionInactive'),
    conflict: t('common.errorConflict'),
    validation: t('common.errorValidation'),
    unknown: t('common.errorUnknown'),
  }[kind]
  // Retrying "no access", "not found", or "subscription inactive" reruns the exact same request
  // with the exact same outcome -- only offer the button for kinds that are plausibly transient
  // (plus 'unknown', so every pre-existing call site that doesn't pass `kind` keeps its button).
  const canRetry = onRetry && kind !== 'forbidden' && kind !== 'notFound' && kind !== 'subscriptionInactive'

  return (
    <div role="alert" className="flex flex-col items-center gap-3 px-6 py-16 text-center">
      <span className="grid h-12 w-12 place-items-center rounded-full bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)]">
        <Icon width={22} height={22} />
      </span>
      <div>
        <p className="text-[14px] font-semibold text-[color:var(--admin-text)]">{message}</p>
        <p className="mt-1 max-w-xs text-[12.5px] leading-relaxed text-[color:var(--admin-text-tertiary)]">{detail}</p>
      </div>
      {kind === 'subscriptionInactive' && <SubscriptionInactiveHint storeId={storeId} />}
      {canRetry && (
        <button
          onClick={onRetry}
          className="mt-1 rounded-xl bg-[color:var(--admin-accent)] px-5 py-2.5 text-[13px] font-bold text-[color:var(--admin-accent-fg)] transition-transform duration-300 hover:brightness-110 active:scale-95"
        >
          {t('common.retry')}
        </button>
      )}
    </div>
  )
}

export { classifyError }
export type { ErrorKind }
