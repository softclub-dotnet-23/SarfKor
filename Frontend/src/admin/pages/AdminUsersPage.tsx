import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Pagination } from '../components/Pagination'
import { Badge } from '../components/Badge'
import { UserDetailPanel } from '../components/UserDetailPanel'
import { SearchIcon, UsersIcon, ChevronDownIcon } from '../components/icons'
import { adminUsersApi, type AdminUserListItem } from '../../lib/api'

const TAKE = 25

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

export function AdminUsersPage() {
  const [params, setParams] = useSearchParams()
  const userId = params.get('userId')
  const search = params.get('search') ?? ''
  const skip = Number(params.get('skip') ?? '0')
  const sortDir = params.get('sort') === 'asc' ? 'asc' : params.get('sort') === 'desc' ? 'desc' : null

  const [searchInput, setSearchInput] = useState(search)
  const [users, setUsers] = useState<AdminUserListItem[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminUsersApi.getUsers({ skip, take: TAKE, search: search || undefined })
      setUsers(res.users)
      setTotalCount(res.totalCount)
    } catch (err) {
      console.error('Failed to load users list:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить пользователей')
    }
  }, [skip, search])

  useEffect(() => {
    load()
  }, [load])

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'skip') next.delete('skip')
    setParams(next, { replace: true })
  }

  function toggleSort() {
    updateParam('sort', sortDir === 'desc' ? 'asc' : 'desc')
  }

  function openUser(id: string) {
    const next = new URLSearchParams(params)
    next.set('userId', id)
    setParams(next)
  }

  function closeUser() {
    const next = new URLSearchParams(params)
    next.delete('userId')
    setParams(next)
  }

  // Sort is client-side over the current page only (getUsers doesn't take a sortBy) -- the
  // dedicated GetTrustScores endpoint sorts server-side across the whole table, but this list
  // needs search/roles/blocked status too, so it stays the primary "Пользователи" table.
  const sortedUsers =
    sortDir && users
      ? [...users].sort((a, b) => ((a.trustScore ?? -1) - (b.trustScore ?? -1)) * (sortDir === 'asc' ? 1 : -1))
      : users

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          updateParam('search', searchInput.trim())
        }}
        className="relative mb-4 max-w-md"
      >
        <SearchIcon width={15} height={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)]" />
        <input
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          onBlur={() => updateParam('search', searchInput.trim())}
          placeholder="Поиск по email…"
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] py-2.5 pl-9 pr-3.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </form>

      <Card scheme="admin" className="overflow-hidden">
        {users === null && !error && <Loading scheme="admin" />}
        {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
        {users && users.length === 0 && <EmptyState scheme="admin" icon={<UsersIcon width={22} height={22} />} title="Пользователей не найдено" body="Измените поисковый запрос." />}
        {sortedUsers && sortedUsers.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-[13px]">
              <thead>
                <tr className="border-b border-[color:var(--admin-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                  <th className="px-4 py-3">Email</th>
                  <th className="px-4 py-3">Регистрация</th>
                  <th className="px-4 py-3">Роли</th>
                  <th className="px-4 py-3">
                    <button onClick={toggleSort} className="flex items-center gap-1 hover:text-[color:var(--admin-text)]">
                      Рейтинг
                      <ChevronDownIcon width={11} height={11} className={sortDir === 'asc' ? 'rotate-180' : ''} />
                    </button>
                  </th>
                  <th className="px-4 py-3">Статус</th>
                </tr>
              </thead>
              <tbody>
                {sortedUsers.map((u) => (
                  <tr
                    key={u.userId}
                    onClick={() => openUser(u.userId)}
                    className="cursor-pointer border-b border-[color:var(--admin-border)] transition-colors last:border-0 hover:bg-[color:var(--admin-hover)]"
                  >
                    <td className="px-4 py-3 font-semibold text-[color:var(--admin-text)]">{u.email ?? u.userId}</td>
                    <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--admin-text-tertiary)]">{fmtDate(u.createdAt)}</td>
                    <td className="px-4 py-3 text-[color:var(--admin-text-secondary)]">{u.roles.join(', ') || '—'}</td>
                    <td className="px-4 py-3 font-[JetBrains_Mono,monospace] font-bold text-[color:var(--admin-text)]">{u.trustScore ?? '—'}</td>
                    <td className="px-4 py-3">
                      {u.isBlocked ? (
                        <Badge scheme="admin" variant="danger" size="sm">Заблокирован</Badge>
                      ) : (
                        <Badge scheme="admin" variant="success" size="sm">Активен</Badge>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {sortedUsers && sortedUsers.length > 0 && (
          <div className="px-4 pb-4">
            <Pagination skip={skip} take={TAKE} totalCount={totalCount} onChange={(s) => updateParam('skip', String(s))} />
          </div>
        )}
      </Card>

      {userId && <UserDetailPanel userId={userId} onClose={closeUser} />}
    </div>
  )
}
