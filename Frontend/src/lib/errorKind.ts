import { ApiError } from './api/client'

/**
 * One classification for every failed data-load across the project, instead of each page
 * inventing its own "is this a 403 or a network error" logic ad hoc. `apiFetch` throws a raw
 * (non-ApiError) exception for anything that never reached the server -- offline, DNS failure,
 * CORS -- so `err instanceof ApiError` is the network/server split; `.status` is the rest.
 */
export type ErrorKind = 'forbidden' | 'notFound' | 'server' | 'network' | 'unknown'

export function classifyError(err: unknown): ErrorKind {
  if (err instanceof ApiError) {
    if (err.status === 401 || err.status === 403) return 'forbidden'
    if (err.status === 404) return 'notFound'
    if (err.status >= 500) return 'server'
    return 'unknown'
  }
  // fetch() itself rejects (TypeError, "Failed to fetch"/"NetworkError") when the request never
  // reached a server at all -- offline, DNS, CORS, connection refused.
  if (err instanceof TypeError) return 'network'
  return 'unknown'
}
