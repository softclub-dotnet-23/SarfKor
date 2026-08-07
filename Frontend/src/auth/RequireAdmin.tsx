import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/** Gates the platform Admin console (stores/subscriptions/users/reference/audit — a platform
 *  operator role, not store ownership or content moderation; see ADMIN_PROMPT.md). */
export function RequireAdmin() {
  const { hasRole } = useAuth()

  if (!hasRole('Admin')) {
    return <Navigate to="/admin" replace />
  }

  return <Outlet />
}
