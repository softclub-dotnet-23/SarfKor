import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi, ApiError, decodeJwt, rolesFromToken, getTokens, setTokens, clearTokens } from '../lib/api'

export interface AuthUser {
  userId: string
  email: string
  roles: string[]
}

type AuthResult = { ok: true } | { ok: false; error: string }

interface AuthContextValue {
  user: AuthUser | null
  storeId: number | null
  /** True only while resolving the session on first load (deciding whether a stored token is still valid). */
  loading: boolean
  login: (email: string, password: string) => Promise<AuthResult>
  register: (email: string, password: string) => Promise<AuthResult>
  logout: () => void
  setStoreId: (id: number) => void
  /** Re-pulls the JWT after an action that changes the caller's roles server-side (e.g. creating a store grants StorePartner) — the previously-issued token doesn't carry the new role until this runs. */
  refreshRoles: () => Promise<void>
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
  const [loading, setLoading] = useState(true)

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
        setUser(userFromAccessToken(tokens.accessToken))
        setLoading(false)
        return
      }
      // Access token is stale on load (15 min lifetime) — proactively refresh
      // once before rendering anything gated, instead of flashing protected
      // content and then failing every API call.
      try {
        const result = await authApi.refresh(tokens.refreshToken)
        if (cancelled) return
        setTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken })
        setUser(userFromAccessToken(result.accessToken))
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
      loading,
      login: async (email, password) => {
        try {
          const result = await authApi.login(email, password)
          setTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken })
          setUser(userFromAccessToken(result.accessToken))
          return { ok: true }
        } catch (err) {
          return { ok: false, error: friendlyAuthError(err, 'Не удалось войти') }
        }
      },
      register: async (email, password) => {
        try {
          const result = await authApi.register(email, password)
          setTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken })
          setUser(userFromAccessToken(result.accessToken))
          return { ok: true }
        } catch (err) {
          return { ok: false, error: friendlyAuthError(err, 'Не удалось зарегистрироваться') }
        }
      },
      logout: () => {
        clearTokens()
        localStorage.removeItem(STORE_ID_KEY)
        setUser(null)
        setStoreIdState(null)
      },
      setStoreId: (id: number) => {
        localStorage.setItem(STORE_ID_KEY, String(id))
        setStoreIdState(id)
      },
      refreshRoles: async () => {
        const tokens = getTokens()
        if (!tokens) return
        const result = await authApi.refresh(tokens.refreshToken)
        setTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken })
        setUser(userFromAccessToken(result.accessToken))
      },
      hasRole: (role: string) => user?.roles.includes(role) ?? false,
    }),
    [user, storeId, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
