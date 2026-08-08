import { useCallback, useEffect, useState } from 'react'
import { SidePanel, FieldRow } from './SidePanel'
import { ReasonModal } from './ReasonModal'
import { Loading } from './Loading'
import { ErrorState, classifyError, type ErrorKind } from './ErrorState'
import { EmptyState } from './EmptyState'
import { Badge } from './Badge'
import { ShieldIcon, StoreIcon } from './icons'
import { adminUsersApi, type AdminUserDetail } from '../../lib/api'

function fmtDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

export function UserDetailPanel({ userId, onClose }: { userId: string; onClose: () => void }) {
  const [detail, setDetail] = useState<AdminUserDetail | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [blockOpen, setBlockOpen] = useState(false)
  const [unblockOpen, setUnblockOpen] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      setDetail(await adminUsersApi.getUserDetail(userId))
    } catch (err) {
      console.error('Не удалось загрузить пользователя:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить пользователя')
    }
  }, [userId])

  useEffect(() => {
    setDetail(null)
    load()
  }, [load])

  return (
    <SidePanel
      open
      onClose={onClose}
      title={
        <span className="flex items-center gap-2">
          <ShieldIcon width={16} height={16} className="shrink-0 text-[color:var(--admin-text-tertiary)]" />
          {detail?.email ?? userId}
        </span>
      }
      subtitle={detail?.roles.join(', ')}
    >
      {detail === null && !error && <Loading scheme="admin" />}
      {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
      {detail && detail.outcome === 'NotFound' && <EmptyState scheme="admin" title="Пользователь не найден" />}
      {detail && detail.outcome === 'Found' && (
        <div className="flex flex-col gap-5">
          <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
            <FieldRow label="Email" value={detail.email ?? '—'} />
            <FieldRow label="Регистрация" value={fmtDate(detail.createdAt)} />
            <FieldRow label="Роли" value={detail.roles.join(', ') || '—'} />
            <FieldRow
              label="Статус"
              value={
                detail.isBlocked ? (
                  <Badge scheme="admin" variant="danger" size="sm">Заблокирован</Badge>
                ) : (
                  <Badge scheme="admin" variant="success" size="sm">Активен</Badge>
                )
              }
            />
            {detail.isBlocked && detail.blockedReason && <FieldRow label="Причина блокировки" value={detail.blockedReason} />}
            <FieldRow label="Отправлено цен" value={detail.priceSubmissionsTotal} />
            <FieldRow label="Из них подтверждено" value={detail.priceSubmissionsVerified} />
            <FieldRow label="Жалоб за 90 дней" value={detail.reportsAgainstLast90Days} />
          </div>

          <div>
            {detail.isBlocked ? (
              <button
                onClick={() => setUnblockOpen(true)}
                className="rounded-xl border border-[color:var(--admin-border)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]"
              >
                Разблокировать
              </button>
            ) : (
              <button
                onClick={() => setBlockOpen(true)}
                className="rounded-xl border border-[color:var(--admin-danger)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--admin-danger)] hover:bg-[color:var(--admin-danger-dim)]"
              >
                Заблокировать
              </button>
            )}
          </div>

          <div className="border-t border-[color:var(--admin-border)] pt-4">
            <div className="mb-2 flex items-center gap-2 text-[11.5px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
              <StoreIcon width={13} height={13} />
              Магазины
            </div>
            {detail.stores.length === 0 && <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">Не связан ни с одним магазином</p>}
            {detail.stores.length > 0 && (
              <div className="flex flex-col gap-1.5">
                {detail.stores.map((s) => (
                  <div key={s.storeId} className="flex items-center justify-between gap-2 rounded-lg bg-[color:var(--admin-hover)] px-3 py-2 text-[12.5px]">
                    <span className="truncate font-semibold text-[color:var(--admin-text)]">{s.storeName}</span>
                    <span className="shrink-0 text-[11px] text-[color:var(--admin-text-tertiary)]">{s.relationship}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      <ReasonModal
        open={blockOpen}
        onClose={() => setBlockOpen(false)}
        title="Заблокировать пользователя"
        description="Пользователь не сможет войти, а все активные сессии завершатся немедленно."
        confirmLabel="Заблокировать"
        danger
        onConfirm={async (reason) => {
          await adminUsersApi.blockUser(userId, reason)
          load()
        }}
      />
      <ReasonModal
        open={unblockOpen}
        onClose={() => setUnblockOpen(false)}
        title="Разблокировать пользователя"
        confirmLabel="Разблокировать"
        onConfirm={async (reason) => {
          await adminUsersApi.unblockUser(userId, reason)
          load()
        }}
      />
    </SidePanel>
  )
}
