import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  authApi,
  meApi,
  ApiError,
  decodeJwt,
  rolesFromToken,
  mustChangePasswordFromToken,
  getTokens,
  setTokens,
  clearTokens,
  refreshTokens,
  type MyStore,
} from '../lib/api'

export interface AuthUser {
  userId: string
  email: string
  roles: string[]
}

// `roles` on success lets the caller (LoginPage) decide where to redirect without racing the
// context's own async state update — reading `user` right after awaiting login() would still see
// the pre-login value, since setUser() hasn't flushed a re-render yet in this same closure.
type AuthResult = { ok: true; roles: string[] } | { ok: false; error: string }

// Login can fail because the account is real but its registration email was never confirmed —
// that's not "wrong password," it's "go enter/resend your code," so the caller (LoginPage) needs
// to tell the two apart to route correctly instead of just showing an error.
type LoginResult = { ok: true; roles: string[] } | { ok: false; error: string; requiresEmailConfirmation?: boolean; email?: string }

// Register never logs the caller in directly anymore — every self-registration needs the emailed
// code confirmed first (confirmEmail below completes the login step once that happens).
type RegisterResult = { ok: true; requiresEmailConfirmation: true; email: string } | { ok: false; error: string }

interface AuthContextValue {
  user: AuthUser | null
  storeId: number | null
  /** Stores the caller owns or is a registered employee of — null until resolved at least once, [] once resolved with none found. */
  myStores: MyStore[] | null
  /** The caller's role in the currently-selected store, from `myStores` — null until `myStores`/`storeId` resolve. */
  currentStoreRole: MyStore['role'] | null
  /** True only while resolving the session on first load (deciding whether a stored token is still valid). */
  loading: boolean
  /** True when someone else (a store owner) set this account's password — every route but the
   *  forced change-password screen should be blocked until clearMustChangePassword() runs. */
  mustChangePassword: boolean
  /** Call once ChangePasswordCommand actually succeeds — the JWT claim itself stays stale until
   *  the token naturally rotates, so this is the real signal the app acts on. */
  clearMustChangePassword: () => void
  login: (email: string, password: string) => Promise<LoginResult>
  register: (email: string, password: string) => Promise<RegisterResult>
  /** Confirms the 6-digit code emailed on register and logs the caller in on success. */
  confirmEmail: (email: string, code: string) => Promise<AuthResult>
  /** Applies a token pair issued outside login/register (e.g. accepting a store invite) — same
   *  setTokens -> decode -> fetchAndApplyMyStores sequence login/register already do. */
  applyAuthResult: (tokens: { accessToken: string; refreshToken: string }) => Promise<{ roles: string[] }>
  logout: () => void
  setStoreId: (id: number) => void
  /** Drops a locally-cached storeId that no longer resolves (store deleted, or stale from a different account that once shared this browser) — sends the caller back to the picker instead of a dead retry loop. */
  clearStoreId: () => void
  /** Re-pulls the JWT after an action that changes the caller's roles server-side (e.g. creating a store grants StorePartner) — the previously-issued token doesn't carry the new role until this runs. */
  refreshRoles: () => Promise<void>
  /** Re-fetches the owned/employed store list — call after creating a store or being added as an employee. */
  refreshMyStores: () => Promise<void>
  hasRole: (role: string) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)
const STORE_ID_KEY = 'sarfkor-store-id'

function userFromAccessToken(accessToken: string): AuthUser | null {
  const decoded = decodeJwt(accessToken)
  if (!decoded) return null
  return { userId: decoded.sub, email: decoded.email ?? '', roles: rolesFromToken(accessToken) }
}

function friendlyAuthError(err: unknown, fallback: string, mode: 'login' | 'register' | 'confirm' = 'login'): string {
  // TypeError from fetch() itself — server down, CORS blocked, or no network.
  // Must be checked before ApiError because TypeError is not an ApiError.
  if (err instanceof TypeError && err.message.toLowerCase().includes('fetch')) {
    return 'Не удаётся связаться с сервером'
  }
  if (err instanceof ApiError) {
    if (err.status === 401) return 'Неверный email или пароль'
    if (err.status === 429) return 'Слишком много попыток. Подождите немного и попробуйте снова'
    if (err.status === 0 || err.message.includes('fetch')) return 'Не удаётся связаться с сервером'
    // 409/400 in this shape only ever come from /api/auth/register -- login's only realistic
    // failure is 401, so a 400 there falls through to `fallback` rather than a register-specific lie.
    if (mode === 'register') {
      if (err.status === 409) return 'Этот email уже зарегистрирован. Войдите или восстановите пароль'
      if (err.status === 400)
        return 'Пароль должен содержать минимум 8 символов: заглавную и строчную буквы, цифру и спецсимвол'
    }
    if (mode === 'confirm' && err.status === 400) {
      return err.message?.includes('Too many attempts')
        ? 'Слишком много попыток. Зарегистрируйтесь заново, чтобы получить новый код'
        : 'Неверный или истёкший код'
    }
  }
  return fallback
}

interface EmailConfirmationRequiredBody {
  requiresEmailConfirmation: true
  email: string
}

function isEmailConfirmationRequired(body: unknown): body is EmailConfirmationRequiredBody {
  return typeof body === 'object' && body !== null && (body as { requiresEmailConfirmation?: unknown }).requiresEmailConfirmation === true
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [storeId, setStoreIdState] = useState<number | null>(() => {
    const raw = localStorage.getItem(STORE_ID_KEY)
    return raw ? Number(raw) : null
  })
  const [myStores, setMyStores] = useState<MyStore[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [mustChangePassword, setMustChangePassword] = useState(false)

  // Fetches the caller's owned/employed stores and, if none is cached locally yet and there's
  // exactly one, adopts it automatically — this is what recovers a StorePartner's/cashier's store
  // on a fresh browser instead of stranding them on the onboarding screen (GET /api/me/stores).
  //
  // Deliberately does NOT gate on the caller's role: an earlier version skipped the request
  // unless `roles` already included 'StorePartner', reading `roles` from `user` in the closure.
  // That's a stale-closure trap — e.g. StoreOnboardingPage's handleCreate calls refreshRoles()
  // (which grants StorePartner server-side and setUser()s a fresh token) immediately followed by
  // refreshMyStores(), but the setUser() from refreshRoles() hasn't re-rendered yet, so
  // refreshMyStores() still closes over the pre-creation `user` without the new role and silently
  // no-ops to an empty list — RequireStore then sees the new storeId with an empty myStores,
  // calls it stale, and bounces back to onboarding. The endpoint itself is safe to call
  // unconditionally (GET /api/me/stores just returns [] for a plain User, keyed off the JWT
  // subject, not the role) — the one extra request for a non-partner account is cheaper than
  // this whole class of staleness bug.
  async function fetchAndApplyMyStores(): Promise<MyStore[]> {
    try {
      const res = await meApi.getMyStores()
      setMyStores(res.stores)
      if (!localStorage.getItem(STORE_ID_KEY) && res.stores.length === 1) {
        const onlyStoreId = res.stores[0].storeId
        localStorage.setItem(STORE_ID_KEY, String(onlyStoreId))
        setStoreIdState(onlyStoreId)
      }
      return res.stores
    } catch {
      setMyStores([])
      return []
    }
  }

  async function applyAuthResult(tokens: { accessToken: string; refreshToken: string }): Promise<{ roles: string[] }> {
    setTokens(tokens)
    const nextUser = userFromAccessToken(tokens.accessToken)
    setUser(nextUser)
    setMustChangePassword(mustChangePasswordFromToken(tokens.accessToken))
    await fetchAndApplyMyStores()
    return { roles: nextUser?.roles ?? [] }
  }

  useEffect(() => {
    let cancelled = false
    async function resolveSession() {
      const tokens = getTokens()
      if (!tokens) {
        setLoading(false)
        return
      }
      const decoded = decodeJwt(tokens.accessToken)
      const isExpired = !decoded || decoded.exp * 1000 < Date.now() + 5000
      if (!isExpired) {
        const nextUser = userFromAccessToken(tokens.accessToken)
        setUser(nextUser)
        setMustChangePassword(mustChangePasswordFromToken(tokens.accessToken))
        await fetchAndApplyMyStores()
        if (!cancelled) setLoading(false)
        return
      }
      // Access token is stale on load (15 min lifetime) — proactively refresh
      // once before rendering anything gated, instead of flashing protected
      // content and then failing every API call.
      //
      // Must go through refreshTokens(), not authApi.refresh(): refresh tokens are
      // single-use, and this effect can run twice (StrictMode) or alongside a 401
      // retry. Two direct calls would spend the same token, and the loser's 401
      // would clear a session that is actually still good.
      try {
        const refreshed = await refreshTokens()
        if (cancelled) return
        if (!refreshed) {
          setUser(null)
          return
        }
        const nextUser = userFromAccessToken(refreshed.accessToken)
        setUser(nextUser)
        setMustChangePassword(mustChangePasswordFromToken(refreshed.accessToken))
        await fetchAndApplyMyStores()
      } catch {
        if (cancelled) return
        clearTokens()
        setUser(null)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    resolveSession()
    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      storeId,
      myStores,
      currentStoreRole: myStores?.find((s) => s.storeId === storeId)?.role ?? null,
      loading,
      mustChangePassword,
      clearMustChangePassword: () => setMustChangePassword(false),
      login: async (email, password) => {
        try {
          const result = await authApi.login(email, password)
          const { roles } = await applyAuthResult(result)
          return { ok: true, roles }
        } catch (err) {
          if (err instanceof ApiError && err.status === 403 && isEmailConfirmationRequired(err.body)) {
            return { ok: false, error: 'Подтвердите email — введите код из письма', requiresEmailConfirmation: true, email: err.body.email }
          }
          return { ok: false, error: friendlyAuthError(err, 'Не удалось войти') }
        }
      },
      register: async (email, password) => {
        try {
          const result = await authApi.register(email, password)
          return { ok: true, requiresEmailConfirmation: true, email: result.email }
        } catch (err) {
          return { ok: false, error: friendlyAuthError(err, 'Не удалось зарегистрироваться', 'register') }
        }
      },
      confirmEmail: async (email, code) => {
        try {
          const result = await authApi.confirmEmail(email, code)
          const { roles } = await applyAuthResult(result)
          return { ok: true, roles }
        } catch (err) {
          return { ok: false, error: friendlyAuthError(err, 'Не удалось подтвердить email', 'confirm') }
        }
      },
      applyAuthResult,
      logout: () => {
        clearTokens()
        localStorage.removeItem(STORE_ID_KEY)
        setUser(null)
        setStoreIdState(null)
        setMyStores(null)
        setMustChangePassword(false)
      },
      setStoreId: (id: number) => {
        localStorage.setItem(STORE_ID_KEY, String(id))
        setStoreIdState(id)
      },
      clearStoreId: () => {
        localStorage.removeItem(STORE_ID_KEY)
        setStoreIdState(null)
      },
      refreshRoles: async () => {
        // Same single-use constraint as the restore path above — share the collapse.
        const refreshed = await refreshTokens()
        if (!refreshed) return
        setUser(userFromAccessToken(refreshed.accessToken))
      },
      refreshMyStores: async () => {
        await fetchAndApplyMyStores()
      },
      hasRole: (role: string) => user?.roles.includes(role) ?? false,
    }),
    [user, storeId, myStores, loading, mustChangePassword],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
