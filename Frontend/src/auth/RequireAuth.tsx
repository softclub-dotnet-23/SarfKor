import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function RequireAuth() {
  const { user, loading } = useAuth()
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

  return <Outlet />
}
