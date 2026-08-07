import { useT } from '../../i18n/translations'
import { classifyError, type ErrorKind } from '../../lib/errorKind'
import { AlertIcon, LockIcon, SearchIcon, WifiOffIcon } from './icons'

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
  unknown: AlertIcon,
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
  const Icon = ICONS[kind]
  const detail = {
    forbidden: t('common.errorForbidden'),
    notFound: t('common.errorNotFound'),
    server: t('common.errorServer'),
    network: t('common.errorNetwork'),
    unknown: t('common.errorUnknown'),
  }[kind]
  // Retrying "no access" or "not found" reruns the exact same request with the exact same
  // outcome -- only offer the button for the two kinds that are plausibly transient (plus
  // 'unknown', so every pre-existing call site that doesn't pass `kind` keeps its retry button).
  const canRetry = onRetry && kind !== 'forbidden' && kind !== 'notFound'

  return (
    <div role="alert" className="flex flex-col items-center gap-3 px-6 py-16 text-center">
      <span className="grid h-12 w-12 place-items-center rounded-full bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)]">
        <Icon width={22} height={22} />
      </span>
      <div>
        <p className="text-[14px] font-semibold text-[color:var(--admin-text)]">{message}</p>
        <p className="mt-1 max-w-xs text-[12.5px] leading-relaxed text-[color:var(--admin-text-tertiary)]">{detail}</p>
      </div>
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
