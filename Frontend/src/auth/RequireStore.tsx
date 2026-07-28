import { useEffect } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

/**
 * Gates the real admin pages behind having a known, still-valid store.
 * GET /api/me/stores (AuthContext's `myStores`) is the source of truth —
 * a cached storeId that isn't in that list (store deleted, or a stale value
 * left over from a different account that once shared this browser) gets
 * dropped so the caller lands back on the picker instead of a dead
 * "404 Not Found / Повторить" loop on every admin page.
 */
export function RequireStore() {
  const { hasRole, storeId, myStores, clearStoreId } = useAuth()
  const location = useLocation()

  const storeIdIsStale = storeId !== null && myStores !== null && !myStores.some((s) => s.storeId === storeId)

  useEffect(() => {
    if (storeIdIsStale) clearStoreId()
  }, [storeIdIsStale, clearStoreId])

  if (location.pathname === '/admin/onboarding') {
    return <Outlet />
  }

  if (!hasRole('StorePartner') || storeId === null || storeIdIsStale) {
    // A platform Admin has no reason to own a store — CLAUDE.md §7 scopes
    // that role to moderation, not the storefront cabinet — so route them
    // to their own surface instead of funneling every non-store account
    // into "create a store."
    if (hasRole('Admin')) {
      return <Navigate to="/admin/moderation" replace />
    }
    return <Navigate to="/admin/onboarding" replace />
  }

  return <Outlet />
}
