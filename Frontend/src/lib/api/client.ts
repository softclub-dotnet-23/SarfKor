// Thin fetch wrapper for the real Sarfkor backend (Backend/src/WebApi).
//
// Base URL comes from VITE_API_BASE_URL (see .env.example) — defaults to the
// backend's documented local http profile (Backend/src/WebApi/Properties/
// launchSettings.json). The backend's CORS policy is config-driven and reads
// allowed origins from `Cors:AllowedOrigins`, not hardcoded — that must
// include this dev server's origin (e.g. http://localhost:5173) on the
// backend side or every request here will fail as a CORS error in the
// browser regardless of what this code does.
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5135'

const ACCESS_TOKEN_KEY = 'sarfkor-access-token'
const REFRESH_TOKEN_KEY = 'sarfkor-refresh-token'

export interface AuthTokens {
  accessToken: string
  refreshToken: string
}

export function getTokens(): AuthTokens | null {
  const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY)
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
  if (!accessToken || !refreshToken) return null
  return { accessToken, refreshToken }
}

export function setTokens(tokens: AuthTokens) {
  localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken)
}

export function clearTokens() {
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
}

export class ApiError extends Error {
  status: number
  body?: unknown

  constructor(status: number, message: string, body?: unknown) {
    super(message)
    this.status = status
    this.body = body
    this.name = 'ApiError'
  }
}

// No request against this backend should ever hang the UI indefinitely — a submit button that
// never comes back is indistinguishable from a frozen app. 15s is generous for anything actually
// backed by a DB query (email sends no longer block the request at all, see WORKLOG); past that,
// something is genuinely stuck and the caller should see a clear, retryable error instead of a
// spinner forever. `ApiError(0, ...)` is the timeout's own sentinel status — classifyError below
// maps it to 'network', the same retryable bucket as "request never reached the server".
const DEFAULT_TIMEOUT_MS = 15000

async function fetchWithTimeout(input: string, init: RequestInit, timeoutMs = DEFAULT_TIMEOUT_MS): Promise<Response> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)
  try {
    return await fetch(input, { ...init, signal: controller.signal })
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError') {
      throw new ApiError(0, 'Превышено время ожидания сервера — попробуйте ещё раз', undefined)
    }
    throw err
  } finally {
    clearTimeout(timer)
  }
}

// The access token expires in 15 minutes (server-side constant) and the
// refresh token is single-use/rotated on every call. If two requests 401 at
// the same moment, they must share one refresh attempt — a second call with
// the same (now-consumed) refresh token would just fail — so concurrent
// refreshes are collapsed into a single in-flight promise.
// Exported because AuthContext's session-restore path needs the *same* collapse:
// calling authApi.refresh() directly from there raced this one (two callers, one
// single-use token — the loser 401s and the session is dropped even though it was
// valid). Every refresh in the app must go through this function.
let refreshPromise: Promise<AuthTokens | null> | null = null

export async function refreshTokens(): Promise<AuthTokens | null> {
  const current = getTokens()
  if (!current) return null

  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const res = await fetchWithTimeout(`${API_BASE_URL}/api/auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken: current.refreshToken }),
        })
        if (!res.ok) {
          clearTokens()
          return null
        }
        const data = (await res.json()) as { accessToken: string; refreshToken: string }
        const next = { accessToken: data.accessToken, refreshToken: data.refreshToken }
        setTokens(next)
        return next
      } catch {
        clearTokens()
        return null
      } finally {
        refreshPromise = null
      }
    })()
  }
  return refreshPromise
}

interface ApiFetchOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  query?: Record<string, string | number | boolean | undefined | null | (string | number)[]>
  auth?: boolean // defaults to true — set false for anonymous endpoints
  /** Skip JSON body parsing/serialization (used for multipart uploads). */
  raw?: RequestInit
  /** Overrides DEFAULT_TIMEOUT_MS for this one call. Code review 2026-08-10 finding #3: the
   *  backend's AI assistant client deliberately allows up to 60s for a reply (a real LLM call, not
   *  a DB-backed request), but every apiFetch call used to inherit the same 15s default regardless
   *  — silently killing any assistant reply that took longer than that, even though the backend
   *  would have answered successfully. assistant.ts's chat() is the one caller that sets this. */
  timeoutMs?: number
}

function buildUrl(path: string, query?: ApiFetchOptions['query']) {
  const url = new URL(path, API_BASE_URL)
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null) continue
      if (Array.isArray(value)) {
        for (const item of value) url.searchParams.append(key, String(item))
      } else {
        url.searchParams.set(key, String(value))
      }
    }
  }
  return url.toString()
}

/**
 * Calls the backend and returns the parsed JSON body. Handles JWT attach +
 * one transparent refresh-and-retry on a 401. Throws ApiError for any
 * non-2xx response (after the retry, if a retry was attempted).
 */
export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const { method = 'GET', body, query, auth = true, timeoutMs } = options

  const doFetch = async (): Promise<Response> => {
    const headers: Record<string, string> = {}
    if (body !== undefined) headers['Content-Type'] = 'application/json'
    if (auth) {
      const tokens = getTokens()
      if (tokens) headers['Authorization'] = `Bearer ${tokens.accessToken}`
    }
    return fetchWithTimeout(
      buildUrl(path, query),
      {
        method,
        headers,
        body: body !== undefined ? JSON.stringify(body) : undefined,
      },
      timeoutMs,
    )
  }

  let res = await doFetch()

  if (res.status === 401 && auth && getTokens()) {
    const refreshed = await refreshTokens()
    if (refreshed) {
      res = await doFetch()
    }
  }

  if (!res.ok) {
    let parsedBody: unknown
    try {
      parsedBody = await res.json()
    } catch {
      // no JSON body
    }
    // Two distinct shapes come back from the backend for a non-2xx response:
    // FluentValidation failures serialize as ASP.NET ProblemDetails (an object with `.title`,
    // via ToValidationProblem), while a plain `NotFound("...")`/`Conflict("...")`/`BadRequest("...")`
    // — used all over the controllers for outcome-mapped errors — serializes as a bare JSON string.
    // Missing the second case meant every one of those (e.g. "Store not found.") silently fell back
    // to the generic "404 Not Found"/"409 Conflict" text instead of the real backend message.
    let message: string | undefined
    if (parsedBody && typeof parsedBody === 'object' && 'title' in parsedBody) {
      message = String((parsedBody as { title?: unknown }).title)
    } else if (typeof parsedBody === 'string' && parsedBody.trim()) {
      message = parsedBody
    }
    throw new ApiError(res.status, message ?? `${res.status} ${res.statusText}`, parsedBody)
  }

  if (res.status === 204) return undefined as T
  const text = await res.text()
  return text ? (JSON.parse(text) as T) : (undefined as T)
}

/**
 * Fetches a binary resource (currently just the caller's own avatar) with the same auth-header +
 * one-retry-on-401 handling as apiFetch, but returning a Blob instead of parsed JSON — an <img src>
 * can't carry an Authorization header itself, so callers turn this into an object URL instead.
 * Returns null for any non-2xx response (404 "no avatar set" and network failure look the same to
 * the caller: nothing to show).
 */
export async function apiFetchBlob(path: string): Promise<Blob | null> {
  const doFetch = () => {
    const headers: Record<string, string> = {}
    const tokens = getTokens()
    if (tokens) headers['Authorization'] = `Bearer ${tokens.accessToken}`
    return fetchWithTimeout(buildUrl(path), { headers })
  }

  let res = await doFetch()
  if (res.status === 401 && getTokens()) {
    const refreshed = await refreshTokens()
    if (refreshed) res = await doFetch()
  }
  if (!res.ok) return null
  return res.blob()
}

export async function apiUpload<T>(path: string, formData: FormData): Promise<T> {
  const headers: Record<string, string> = {}
  const tokens = getTokens()
  if (tokens) headers['Authorization'] = `Bearer ${tokens.accessToken}`

  // Longer budget than DEFAULT_TIMEOUT_MS — this carries actual file bytes (product photos,
  // receipt scans), not just a JSON payload, so 15s is unrealistically tight on a slow connection.
  const uploadTimeoutMs = 30000
  let res = await fetchWithTimeout(buildUrl(path), { method: 'POST', headers, body: formData }, uploadTimeoutMs)
  if (res.status === 401 && tokens) {
    const refreshed = await refreshTokens()
    if (refreshed) {
      headers['Authorization'] = `Bearer ${refreshed.accessToken}`
      res = await fetchWithTimeout(buildUrl(path), { method: 'POST', headers, body: formData }, uploadTimeoutMs)
    }
  }
  if (!res.ok) {
    let parsedBody: unknown
    try {
      parsedBody = await res.json()
    } catch {
      // no JSON body
    }
    throw new ApiError(res.status, `${res.status} ${res.statusText}`, parsedBody)
  }
  const text = await res.text()
  return text ? (JSON.parse(text) as T) : (undefined as T)
}
