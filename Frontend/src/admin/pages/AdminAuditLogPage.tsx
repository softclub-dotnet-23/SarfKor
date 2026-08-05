import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Select } from '../components/Select'
import { Pagination } from '../components/Pagination'
import { AuditLogRow } from '../components/AuditLogRow'
import { ClockIcon } from '../components/icons'
import { adminApi, ApiError, type AuditLogEntry } from '../../lib/api'

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

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getAuditLog({
        skip,
        take: TAKE,
        entityType: entityType || undefined,
        action: action || undefined,
        performedByUserId: performedByUserId || undefined,
        from: from ? new Date(from).toISOString() : undefined,
        to: to ? new Date(to).toISOString() : undefined,
      })
      setEntries(res.entries)
      setTotalCount(res.totalCount)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить журнал действий')
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
          scheme="mod"
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
            className="w-full rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
          />
        </form>
        <input
          type="date"
          value={from}
          onChange={(e) => updateParam('from', e.target.value)}
          title="С даты"
          className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        <input
          type="date"
          value={to}
          onChange={(e) => updateParam('to', e.target.value)}
          title="По дату"
          className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        {performedByUserId && (
          <button
            onClick={() => updateParam('performedByUserId', '')}
            className="rounded-xl border border-[color:var(--mod-accent)] bg-[color:var(--mod-accent-dim)] px-3.5 py-2.5 text-[12px] font-bold text-[color:var(--mod-accent2)]"
          >
            Только {entries?.find((e) => e.performedByUserId === performedByUserId)?.performedByEmail ?? 'этот админ'} ✕
          </button>
        )}
      </div>

      <Card scheme="mod" className="p-2">
        {entries === null && !error && <Loading scheme="mod" />}
        {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
        {entries && entries.length === 0 && (
          <EmptyState scheme="mod" icon={<ClockIcon width={22} height={22} />} title="Записей не найдено" body="Измените фильтры или период." />
        )}
        {entries && entries.length > 0 && (
          <div className="flex flex-col gap-1.5 p-2">
            {entries.map((e) => (
              <div key={e.auditLogId} className="group relative">
                <AuditLogRow entry={e} />
                {!performedByUserId && (
                  <button
                    onClick={() => updateParam('performedByUserId', e.performedByUserId)}
                    className="absolute right-11 top-3 hidden text-[10.5px] font-semibold text-[color:var(--mod-accent2)] hover:underline group-hover:block"
                  >
                    только он
                  </button>
                )}
              </div>
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
