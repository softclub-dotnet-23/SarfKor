import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/**
 * Gates the platform moderation console (/console) behind the Admin role. Distinct from
 * RequireStore — Admin has nothing to do with owning a store, it's a separate, unrelated
 * capability (content moderation), so a StorePartner without Admin still bounces here.
 */
export function RequireAdmin() {
  const { hasRole } = useAuth()

  if (!hasRole('Admin')) {
    return <Navigate to="/admin" replace />
  }

  return <Outlet />
}
