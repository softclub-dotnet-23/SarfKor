import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi, meApi, ApiError, decodeJwt, rolesFromToken, getTokens, setTokens, clearTokens, refreshTokens, type MyStore } from '../lib/api'

export interface AuthUser {
  userId: string
  email: string
  roles: string[]
}

// `roles` on success lets the caller (LoginPage) decide where to redirect without racing the
// context's own async state update — reading `user` right after awaiting login() would still see
// the pre-login value, since setUser() hasn't flushed a re-render yet in this same closure.
type AuthResult = { ok: true; roles: string[] } | { ok: false; error: string }

interface AuthContextValue {
  user: AuthUser | null
  storeId: number | null
  /** Stores the caller owns or is a registered employee of — null until resolved at least once, [] once resolved with none found. */
  myStores: MyStore[] | null
  /** The caller's role in the currently-selected store, from `myStores` — null until `myStores`/`storeId` resolve. */
  currentStoreRole: MyStore['role'] | null
  /** True only while resolving the session on first load (deciding whether a stored token is still valid). */
  loading: boolean
  login: (email: string, password: string) => Promise<AuthResult>
  register: (email: string, password: string) => Promise<AuthResult>
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

function friendlyAuthError(err: unknown, fallback: string): string {
  if (err instanceof ApiError) {
    if (err.status === 401) return 'Неверный email или пароль'
    if (err.status === 400) return 'Не удалось зарегистрироваться — проверьте email и пароль (минимум 8 символов)'
    if (err.status === 429) return 'Слишком много попыток. Подождите немного и попробуйте снова'
    if (err.status === 0 || err.message.includes('fetch')) return 'Не удаётся связаться с сервером'
  }
  return fallback
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [storeId, setStoreIdState] = useState<number | null>(() => {
    const raw = localStorage.getItem(STORE_ID_KEY)
    return raw ? Number(raw) : null
  })
  const [myStores, setMyStores] = useState<MyStore[] | null>(null)
  const [loading, setLoading] = useState(true)

  // Fetches the caller's owned/employed stores and, if none is cached locally yet and there's
  // exactly one, adopts it automatically — this is what recovers a StorePartner's/cashier's store
  // on a fresh browser instead of stranding them on the onboarding screen (GET /api/me/stores).
  async function fetchAndApplyMyStores(roles: string[]): Promise<MyStore[]> {
    if (!roles.includes('StorePartner')) {
      setMyStores([])
      return []
    }
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
    await fetchAndApplyMyStores(nextUser?.roles ?? [])
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
        await fetchAndApplyMyStores(nextUser?.roles ?? [])
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
        await fetchAndApplyMyStores(nextUser?.roles ?? [])
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
      login: async (email, password) => {
        try {
          const result = await authApi.login(email, password)
          const { roles } = await applyAuthResult(result)
          return { ok: true, roles }
        } catch (err) {
          return { ok: false, error: friendlyAuthError(err, 'Не удалось войти') }
        }
      },
      register: async (email, password) => {
        try {
          const result = await authApi.register(email, password)
          setTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken })
          const nextUser = userFromAccessToken(result.accessToken)
          setUser(nextUser)
          return { ok: true, roles: nextUser?.roles ?? [] }
        } catch (err) {
          return { ok: false, error: friendlyAuthError(err, 'Не удалось зарегистрироваться') }
        }
      },
      applyAuthResult,
      logout: () => {
        clearTokens()
        localStorage.removeItem(STORE_ID_KEY)
        setUser(null)
        setStoreIdState(null)
        setMyStores(null)
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
        await fetchAndApplyMyStores(user?.roles ?? [])
      },
      hasRole: (role: string) => user?.roles.includes(role) ?? false,
    }),
    [user, storeId, myStores, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
