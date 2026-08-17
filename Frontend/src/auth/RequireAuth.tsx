import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function RequireAuth() {
  const { user, loading, mustChangePassword } = useAuth()
  const location = useLocation()

  // Resolving whether a stored refresh token is still valid — redirecting to
  // /login here would flash the login page even for a returning, still-valid
  // session.
  if (loading) {
    return (
      <div className="admin-shell grid h-screen place-items-center bg-[color:var(--admin-content)] text-[color:var(--admin-text-tertiary)]">
        Загрузка…
      </div>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  // Owner-created cashier with a still-temporary password (or a just-reset one) — blocks every
  // authenticated route except the change-password screen itself, in both /app and /admin, since
  // this is the one shared gate both trees already go through.
  if (mustChangePassword && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />
  }

  return <Outlet />
}
