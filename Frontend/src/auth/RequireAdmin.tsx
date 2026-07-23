import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/** Gates the platform moderation screens behind the Admin role (CLAUDE.md §7 — moderation, not store ownership). */
export function RequireAdmin() {
  const { hasRole } = useAuth()

  if (!hasRole('Admin')) {
    return <Navigate to="/admin" replace />
  }

  return <Outlet />
}
