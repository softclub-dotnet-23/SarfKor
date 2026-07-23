import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

/**
 * Gates the real admin pages behind having a known store. The backend has no
 * "list stores I own" endpoint, so the only way this app knows which store
 * you run is if it created one for you and remembered the id locally — if
 * that memory is gone (new browser, cleared storage) there is currently no
 * way to recover it from the API, only to create another store.
 */
export function RequireStore() {
  const { hasRole, storeId } = useAuth()
  const location = useLocation()

  if (location.pathname === '/admin/onboarding') {
    return <Outlet />
  }

  if (!hasRole('StorePartner') || storeId === null) {
    return <Navigate to="/admin/onboarding" replace />
  }

  return <Outlet />
}
