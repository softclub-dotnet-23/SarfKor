import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Select } from '../components/Select'
import { Pagination } from '../components/Pagination'
import { DateField } from '../components/DateField'
import { AuditLogRow } from '../components/AuditLogRow'
import { ClockIcon } from '../components/icons'
import { adminApi, type AuditLogEntry } from '../../lib/api'

const TAKE = 30

const ENTITY_TYPES = [
  'Store',
  'StoreSubscription',
  'SubscriptionPayment',
  'ApplicationUser',
  'ContributorTrustScore',
  'Brand',
  'Product',
  'StoreOwnerInvitation',
  'AdminInvitation',
  'PendingAssistantAction',
]

export function AdminAuditLogPage() {
  const [params, setParams] = useSearchParams()
  const entityType = params.get('entityType') ?? ''
  const action = params.get('action') ?? ''
  const performedByUserId = params.get('performedByUserId') ?? ''
  const from = params.get('from') ?? ''
  const to = params.get('to') ?? ''
  const skip = Number(params.get('skip') ?? '0')

  const [actionInput, setActionInput] = useState(action)
  const [entries, setEntries] = useState<AuditLogEntry[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getAuditLog({
        skip,
        take: TAKE,
        entityType: entityType || undefined,
        action: action || undefined,
        performedByUserId: performedByUserId || undefined,
        from: from || undefined,
        to: to || undefined,
      })
      setEntries(res.entries)
      setTotalCount(res.totalCount)
    } catch (err) {
      console.error('Failed to load audit log:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить журнал действий')
    }
  }, [skip, entityType, action, performedByUserId, from, to])

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

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 flex flex-wrap items-center gap-2.5">
        <Select
          scheme="admin"
          className="min-w-[190px]"
          value={entityType}
          onChange={(v) => updateParam('entityType', v)}
          placeholder="Все объекты"
          options={ENTITY_TYPES.map((t) => ({ value: t, label: t }))}
        />
        <form
          onSubmit={(e) => {
            e.preventDefault()
            updateParam('action', actionInput.trim())
          }}
          className="min-w-[200px] flex-1"
        >
          <input
            value={actionInput}
            onChange={(e) => setActionInput(e.target.value)}
            onBlur={() => updateParam('action', actionInput.trim())}
            placeholder="Тип действия, напр. Store.Suspended"
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </form>
        <DateField value={from} onChange={(iso) => updateParam('from', iso)} title="С даты" />
        <DateField value={to} onChange={(iso) => updateParam('to', iso)} title="По дату" />
        {performedByUserId && (
          <button
            onClick={() => updateParam('performedByUserId', '')}
            className="rounded-xl border border-[color:var(--admin-accent)] bg-[color:var(--admin-accent-soft)] px-3.5 py-2.5 text-[12px] font-bold text-[color:var(--admin-accent)]"
          >
            Только {entries?.find((e) => e.performedByUserId === performedByUserId)?.performedByEmail ?? 'этот админ'} ✕
          </button>
        )}
      </div>

      <Card scheme="admin" className="p-2">
        {entries === null && !error && <Loading scheme="admin" />}
        {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
        {entries && entries.length === 0 && (
          <EmptyState scheme="admin" icon={<ClockIcon width={22} height={22} />} title="Записей не найдено" body="Измените фильтры или период." />
        )}
        {entries && entries.length > 0 && (
          <div className="flex flex-col gap-1.5 p-2">
            {entries.map((e) => (
              <AuditLogRow
                key={e.auditLogId}
                entry={e}
                extra={
                  !performedByUserId && (
                    <button
                      onClick={(ev) => {
                        ev.stopPropagation()
                        updateParam('performedByUserId', e.performedByUserId)
                      }}
                      className="shrink-0 text-[10.5px] font-semibold text-[color:var(--admin-text-secondary)] opacity-0 hover:text-[color:var(--admin-text)] hover:underline group-hover:opacity-100"
                    >
                      только он
                    </button>
                  )
                }
              />
            ))}
          </div>
        )}
        {entries && entries.length > 0 && (
          <div className="px-3 pb-3">
            <Pagination skip={skip} take={TAKE} totalCount={totalCount} onChange={(s) => updateParam('skip', String(s))} />
          </div>
        )}
      </Card>
    </div>
  )
}
