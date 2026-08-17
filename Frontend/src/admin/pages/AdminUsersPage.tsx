import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Pagination } from '../components/Pagination'
import { Badge } from '../components/Badge'
import { UserDetailPanel } from '../components/UserDetailPanel'
import { AddButton } from '../components/Button'
import { FormModal, FormField } from '../components/FormModal'
import { Select } from '../components/Select'
import { EntityPicker } from '../components/EntityPicker'
import { Toast } from '../components/Toast'
import { SearchIcon, UsersIcon, RefreshIcon } from '../components/icons'
import {
  adminUsersApi,
  adminApi,
  ApiError,
  type AdminUserListItem,
  type AdminStoreListItem,
  type InvitedRole,
  type UserInvitationListItem,
} from '../../lib/api'

const TAKE = 25

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const ROLE_OPTIONS: { value: InvitedRole; label: string }[] = [
  { value: 'User', label: 'User' },
  { value: 'StorePartner', label: 'StorePartner' },
  { value: 'Admin', label: 'Admin' },
]

// Admin console screens are plain-Russian, unlike the bilingual (ru/tg) StorePartner cabinet
// (AdminUsersPage never imported useT — see StaffPage.tsx for the i18n'd sibling), so this modal
// follows the same hardcoded-Russian convention as the rest of the page instead of introducing a
// bilingual island here.
function InviteUserModal({
  open,
  onClose,
  onCreated,
}: {
  open: boolean
  onClose: () => void
  onCreated: (invitation: UserInvitationListItem) => void
}) {
  const [email, setEmail] = useState('')
  const [emailError, setEmailError] = useState('')
  const [role, setRole] = useState<InvitedRole>('User')
  const [store, setStore] = useState<AdminStoreListItem | null>(null)
  const [storeError, setStoreError] = useState('')

  useEffect(() => {
    if (!open) return
    setEmail('')
    setEmailError('')
    setRole('User')
    setStore(null)
    setStoreError('')
  }, [open])

  async function submit() {
    const trimmed = email.trim()
    if (!trimmed) {
      setEmailError('Укажите email')
      throw new Error('Укажите email')
    }
    if (!EMAIL_RE.test(trimmed)) {
      setEmailError('Некорректный формат email')
      throw new Error('Некорректный формат email')
    }
    if (role === 'StorePartner' && !store) {
      setStoreError('Выберите магазин')
      throw new Error('Выберите магазин')
    }

    const res = await adminUsersApi.createUserInvitation(trimmed, role, role === 'StorePartner' ? store!.storeId : undefined)
    if (res.outcome === 'Sent') {
      const now = new Date().toISOString()
      onCreated({
        invitationId: res.invitationId!,
        email: trimmed,
        invitedRole: role,
        storeId: role === 'StorePartner' ? store!.storeId : null,
        storeName: role === 'StorePartner' ? store!.name : null,
        employeeRole: role === 'StorePartner' ? 'Owner' : null,
        status: 'Pending',
        expiresAt: res.expiresAt!,
        createdAt: now,
        lastSentAt: now,
      })
    } else if (res.outcome === 'StoreNotFound') {
      throw new Error('Магазин не найден')
    } else {
      throw new Error('Недостаточно прав для отправки приглашения')
    }
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title="Добавить пользователя"
      isDirty={!!email}
      onSubmit={submit}
      submitLabel="Отправить приглашение"
      submitBusyLabel="Отправляем…"
      cancelLabel="Отмена"
    >
      <FormField label="Email" required error={emailError}>
        <input
          type="email"
          value={email}
          onChange={(e) => {
            setEmail(e.target.value)
            setEmailError('')
          }}
          placeholder="user@example.com"
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label="Роль" required>
        <Select
          value={role}
          onChange={(v) => {
            setRole(v as InvitedRole)
            setStoreError('')
          }}
          options={ROLE_OPTIONS}
          ariaLabel="Роль"
        />
      </FormField>
      {role === 'StorePartner' && (
        <FormField label="Магазин" required error={storeError}>
          <EntityPicker
            value={store}
            onChange={(v) => {
              setStore(v)
              setStoreError('')
            }}
            fetchPage={async ({ search, skip, take }) => {
              const res = await adminApi.getStores({ search: search || undefined, skip, take })
              return { items: res.stores, totalCount: res.totalCount }
            }}
            getId={(s) => s.storeId}
            getLabel={(s) => s.name}
            renderOption={(s) => (
              <div>
                <div className="text-[13px] font-medium text-[color:var(--admin-text)]">{s.name}</div>
                <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">{s.address}</div>
              </div>
            )}
            placeholder="Найти магазин по названию…"
            ariaLabel="Магазин"
          />
        </FormField>
      )}
    </FormModal>
  )
}

function invitedRoleLabel(invitation: UserInvitationListItem) {
  if (invitation.invitedRole === 'StorePartner') return `StorePartner${invitation.storeName ? ` · ${invitation.storeName}` : ''}`
  return invitation.invitedRole
}

function InvitationTableRow({ invitation, onChanged, showActionsColumn }: { invitation: UserInvitationListItem; onChanged: () => void; showActionsColumn: boolean }) {
  const [busy, setBusy] = useState<'resend' | 'revoke' | null>(null)
  const [justResent, setJustResent] = useState(false)
  const [rowError, setRowError] = useState('')

  async function handleResend() {
    setBusy('resend')
    setRowError('')
    try {
      await adminUsersApi.resendUserInvitation(invitation.invitationId)
      setJustResent(true)
      setTimeout(() => setJustResent(false), 2500)
      onChanged()
    } catch (err) {
      setRowError(err instanceof ApiError ? err.message : 'Не удалось отправить повторно')
    } finally {
      setBusy(null)
    }
  }

  async function handleRevoke() {
    if (!window.confirm(`Отозвать приглашение для ${invitation.email}?`)) return
    setBusy('revoke')
    setRowError('')
    try {
      await adminUsersApi.revokeUserInvitation(invitation.invitationId)
      onChanged()
    } catch (err) {
      setRowError(err instanceof ApiError ? err.message : 'Не удалось отозвать приглашение')
      setBusy(null)
    }
  }

  return (
    <tr
      className="border-b border-[color:var(--admin-border)] last:border-0"
      style={{ background: 'color-mix(in srgb, var(--admin-accent) 5%, transparent)' }}
    >
      <td className="px-4 py-3 font-semibold text-[color:var(--admin-text)]">{invitation.email}</td>
      <td className="px-4 py-3 text-[color:var(--admin-text-tertiary)]">—</td>
      <td className="px-4 py-3 text-[color:var(--admin-text-secondary)]">{invitedRoleLabel(invitation)}</td>
      <td className="px-4 py-3">
        <div className="flex flex-col items-start gap-1">
          <Badge scheme="admin" variant="accent" size="sm">Приглашение отправлено</Badge>
          <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">до {fmtDate(invitation.expiresAt)}</span>
          {rowError && <span className="text-[11px] font-medium text-[color:var(--admin-danger)]">{rowError}</span>}
        </div>
      </td>
      {showActionsColumn && (
        <td className="px-4 py-3">
          <div className="flex items-center gap-1.5">
            <button
              onClick={handleResend}
              disabled={busy !== null}
              className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-hover)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] disabled:opacity-50"
            >
              <RefreshIcon width={13} height={13} />
              {justResent ? 'Отправлено' : 'Отправить повторно'}
            </button>
            <button
              onClick={handleRevoke}
              disabled={busy !== null}
              className="rounded-lg bg-[color:var(--admin-danger-dim)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-danger)] hover:opacity-80 disabled:opacity-50"
            >
              Отозвать
            </button>
          </div>
        </td>
      )}
    </tr>
  )
}

export function AdminUsersPage() {
  const [params, setParams] = useSearchParams()
  const userId = params.get('userId')
  const search = params.get('search') ?? ''
  const skip = Number(params.get('skip') ?? '0')

  const [searchInput, setSearchInput] = useState(search)
  const [users, setUsers] = useState<AdminUserListItem[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const [invitations, setInvitations] = useState<UserInvitationListItem[]>([])
  const [inviteOpen, setInviteOpen] = useState(false)
  const [toastMessage, setToastMessage] = useState('')

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

  const loadInvitations = useCallback(async () => {
    try {
      const res = await adminUsersApi.getUserInvitations()
      setInvitations(res.invitations.filter((i) => i.status === 'Pending'))
    } catch (err) {
      // Non-fatal — the users table above is the primary content; the pending-invites rows just
      // won't show if this fails.
      console.error('Failed to load user invitations:', err)
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    loadInvitations()
  }, [loadInvitations])

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'skip') next.delete('skip')
    setParams(next, { replace: true })
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

  function handleInvitationCreated(invitation: UserInvitationListItem) {
    // Optimistic insert -- the newly-invited row appears immediately, no refetch of the users list.
    setInvitations((prev) => [invitation, ...prev])
    setToastMessage('Приглашение отправлено')
    setTimeout(() => setToastMessage(''), 3200)
  }

  const hasInvitations = invitations.length > 0

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <form
          onSubmit={(e) => {
            e.preventDefault()
            updateParam('search', searchInput.trim())
          }}
          className="relative w-full max-w-md flex-1"
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
        <AddButton onClick={() => setInviteOpen(true)}>Добавить пользователя</AddButton>
      </div>

      <Card scheme="admin" className="overflow-hidden">
        {users === null && !error && <Loading scheme="admin" />}
        {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
        {users && users.length === 0 && !hasInvitations && (
          <EmptyState scheme="admin" icon={<UsersIcon width={22} height={22} />} title="Пользователей не найдено" body="Измените поисковый запрос." />
        )}
        {users && (users.length > 0 || hasInvitations) && (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-[13px]">
              <thead>
                <tr className="border-b border-[color:var(--admin-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                  <th className="px-4 py-3">Email</th>
                  <th className="px-4 py-3">Регистрация</th>
                  <th className="px-4 py-3">Роли</th>
                  <th className="px-4 py-3">Статус</th>
                  {hasInvitations && <th className="px-4 py-3" />}
                </tr>
              </thead>
              <tbody>
                {invitations.map((inv) => (
                  <InvitationTableRow key={`inv-${inv.invitationId}`} invitation={inv} onChanged={loadInvitations} showActionsColumn={hasInvitations} />
                ))}
                {users.map((u) => (
                  <tr
                    key={u.userId}
                    onClick={() => openUser(u.userId)}
                    className="cursor-pointer border-b border-[color:var(--admin-border)] transition-colors last:border-0 hover:bg-[color:var(--admin-hover)]"
                  >
                    <td className="px-4 py-3 font-semibold text-[color:var(--admin-text)]">{u.email ?? u.userId}</td>
                    <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--admin-text-tertiary)]">{fmtDate(u.createdAt)}</td>
                    <td className="px-4 py-3 text-[color:var(--admin-text-secondary)]">{u.roles.join(', ') || '—'}</td>
                    <td className="px-4 py-3">
                      {u.isBlocked ? (
                        <Badge scheme="admin" variant="danger" size="sm">Заблокирован</Badge>
                      ) : (
                        <Badge scheme="admin" variant="success" size="sm">Активен</Badge>
                      )}
                    </td>
                    {hasInvitations && <td className="px-4 py-3" />}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {users && users.length > 0 && (
          <div className="px-4 pb-4">
            <Pagination skip={skip} take={TAKE} totalCount={totalCount} onChange={(s) => updateParam('skip', String(s))} />
          </div>
        )}
      </Card>

      {userId && <UserDetailPanel userId={userId} onClose={closeUser} />}

      <InviteUserModal open={inviteOpen} onClose={() => setInviteOpen(false)} onCreated={handleInvitationCreated} />
      <Toast open={!!toastMessage} variant="success" scheme="admin">
        {toastMessage}
      </Toast>
    </div>
  )
}
