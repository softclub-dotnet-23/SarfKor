import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/** Gates owner-only cabinet pages (Дашборд, Поставки, Маркетинг, Сотрудники, Отчёты, Настройки) behind
 *  Owner — a Cashier StoreEmployee gets sent to Касса instead (CLAUDE.md §7's cashier sub-role). */
export function RequireOwner() {
  const { currentStoreRole } = useAuth()

  if (currentStoreRole === 'Cashier') {
    return <Navigate to="/admin/pos" replace />
  }

  return <Outlet />
}
