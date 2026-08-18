import { ApiError } from './api/client'

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

/**
 * The one rule this file exists to enforce project-wide: never show a made-up generic phrase when
 * the server already sent a specific reason. Every ApiError's `.message` already IS that specific
 * reason (see client.ts's ProblemDetails/plain-string parsing) -- controllers write real sentences
 * ("Subscription is not active — inventory operations are closed until the store's subscription
 * is current.", "This email is already registered", ...), not codes. `fallback` only fires for a
 * genuinely-unlabelled failure (network drop, an unparsed 500) where there is no server text to
 * show at all.
 */
export function errorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiError && err.message && !/^\d{3} /.test(err.message)) return err.message
  return fallback
}
