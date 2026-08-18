import { ApiError } from './api/client'
import type { useT } from '../i18n/translations'

/**
 * One classification for every failed data-load across the project, instead of each page
 * inventing its own "is this a 403 or a network error" logic ad hoc. `apiFetch` throws a raw
 * (non-ApiError) exception for anything that never reached the server -- offline, DNS failure,
 * CORS -- so `err instanceof ApiError` is the network/server split; `.status` is the rest.
 *
 * subscriptionInactive/conflict/validation were previously folded into 'unknown', which is why a
 * 402 (subscription inactive, e.g. RecordStockReceipt) and every other non-2xx status past 404
 * all rendered the exact same "Что-то пошло не так" -- the one thing the backend's error message
 * never says, since it always explains the specific reason.
 */
export type ErrorKind = 'forbidden' | 'notFound' | 'server' | 'network' | 'subscriptionInactive' | 'conflict' | 'validation' | 'unknown'

export function classifyError(err: unknown): ErrorKind {
  if (err instanceof ApiError) {
    // status 0 is fetchWithTimeout's own sentinel for "aborted after DEFAULT_TIMEOUT_MS" — the
    // request never got a real HTTP response, so it belongs in the same retryable bucket as a
    // request that never reached the server at all.
    if (err.status === 0) return 'network'
    if (err.status === 401 || err.status === 403) return 'forbidden'
    if (err.status === 404) return 'notFound'
    if (err.status === 402) return 'subscriptionInactive'
    if (err.status === 409) return 'conflict'
    if (err.status === 422) return 'validation'
    if (err.status >= 500) return 'server'
    return 'unknown'
  }
  // fetch() itself rejects (TypeError, "Failed to fetch"/"NetworkError") when the request never
  // reached a server at all -- offline, DNS, CORS, connection refused.
  if (err instanceof TypeError) return 'network'
  return 'unknown'
}

type TFunc = ReturnType<typeof useT>

/**
 * The one function every catch block in the project should call to put an error on screen.
 *
 * Deliberately never shows `ApiError.message` -- every backend controller writes its outcome text
 * in English (`Conflict("This email is already registered")`, `StatusCode(402, "Subscription is
 * not active — ...")`, ...), because it's meant for the API contract/Swagger, not a RU/TG-speaking
 * store owner. Printing that text verbatim was found leaking onto multiple screens in a row (the
 * "Новое перемещение" modal, cashier shift errors, staff management, ...) -- for an `ApiError`
 * specifically, this function is the fix at the root instead of translating each occurrence as
 * it's noticed: it looks up a translated, kind-specific line from the i18n dictionaries instead,
 * unconditionally.
 *
 * A plain (non-`ApiError`) `Error` is a different case and IS shown via `.message` -- that's not
 * raw server text, it's a message the frontend itself authored (client-side field validation, or
 * an already-translated outcome-specific line built by the caller, e.g.
 * `describeInitiateTransferOutcome(result.outcome)`). FormModal's whole "throw to show a server
 * error" contract depends on that distinction: collapsing "Укажите название"/"Проверьте строки
 * заказа" into the same generic fallback as an actual backend failure would be a regression in the
 * exact opposite direction -- replacing a specific, correct message with a vague one.
 *
 * `err.message`/`err.status` are still logged to the console by the caller (or captured by
 * RouteErrorBoundary for render crashes) for actual debugging -- this function only decides what
 * the *screen* shows for a real ApiError.
 *
 * `isOwner` only matters for the 402 case: an owner sees the billing-specific line (they're the
 * only one who can act on it), a cashier/employee sees a no-financial-detail "ask the owner"
 * line instead. Defaults to the owner copy so a call site that hasn't threaded role through yet
 * still gets a correct, translated (if slightly over-informative) message rather than raw text.
 */
export function describeError(err: unknown, t: TFunc, opts?: { isOwner?: boolean }): string {
  if (err instanceof ApiError) {
    const kind = classifyError(err)
    if (kind === 'subscriptionInactive') {
      return opts?.isOwner === false ? t('common.errorSubscriptionInactiveCashier') : t('common.errorSubscriptionInactiveOwner')
    }
    const dict: Record<Exclude<ErrorKind, 'subscriptionInactive'>, string> = {
      forbidden: t('common.errorForbidden'),
      notFound: t('common.errorNotFound'),
      server: t('common.errorServer'),
      network: t('common.errorNetwork'),
      conflict: t('common.errorConflict'),
      validation: t('common.errorValidation'),
      unknown: t('common.errorUnknown'),
    }
    return dict[kind]
  }
  if (err instanceof Error && err.message) return err.message
  return t('common.errorUnknown')
}

const RU_KIND_TEXT: Record<Exclude<ErrorKind, 'subscriptionInactive'>, string> = {
  forbidden: 'Нет доступа',
  notFound: 'Не найдено',
  server: 'Ошибка сервера. Мы уже знаем и разбираемся.',
  network: 'Нет соединения с сервером. Проверьте интернет.',
  conflict: 'Действие конфликтует с текущим состоянием.',
  validation: 'Проверьте введённые данные.',
  unknown: 'Что-то пошло не так',
}

/**
 * Same fix as {@link describeError}, for the platform-Admin console specifically -- those screens
 * are deliberately plain-Russian, not run through useT() (see AdminUsersPage.tsx's own comment:
 * "unlike the bilingual (ru/tg) StorePartner cabinet ... follows the same hardcoded-Russian
 * convention as the rest of the page instead of introducing a bilingual island here"). Pulling in
 * useT() just for this would be exactly that bilingual island, so this hardcodes the same RU text
 * describeError's dictionary lookup would produce, without a language dependency. Admin-only
 * actions never actually hit 'subscriptionInactive' (store subscription checks don't gate the
 * platform console), so there's no owner/cashier split to carry here.
 */
export function describeErrorRu(err: unknown): string {
  if (err instanceof ApiError) {
    const kind = classifyError(err)
    return kind === 'subscriptionInactive' ? RU_KIND_TEXT.conflict : RU_KIND_TEXT[kind]
  }
  if (err instanceof Error && err.message) return err.message
  return RU_KIND_TEXT.unknown
}
