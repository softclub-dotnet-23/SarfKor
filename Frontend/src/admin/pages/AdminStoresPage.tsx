import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Select } from '../components/Select'
import { Pagination } from '../components/Pagination'
import { DateField } from '../components/DateField'
import { StoreStatusBadge, SubscriptionStatusBadge } from '../components/StatusBadge'
import { StoreDetailPanel } from '../components/StoreDetailPanel'
import { SearchIcon, StoreIcon, AlertIcon } from '../components/icons'
import {
  adminApi,
  metricsApi,
  ApiError,
  type AdminStoreListItem,
  type StoreStatus,
  type SubscriptionStatus,
  type PlatformMetrics,
} from '../../lib/api'

const STATUS_OPTIONS: { value: StoreStatus; label: string }[] = [
  { value: 'PendingApproval', label: 'Ожидает подтверждения' },
  { value: 'Active', label: 'Активен' },
  { value: 'Suspended', label: 'Приостановлен' },
  { value: 'Blocked', label: 'Заблокирован' },
  { value: 'Archived', label: 'Архивирован' },
  { value: 'Rejected', label: 'Отклонён' },
]

const SUBSCRIPTION_STATUS_OPTIONS: { value: SubscriptionStatus; label: string }[] = [
  { value: 'Trial', label: 'Пробный период' },
  { value: 'Active', label: 'Активна' },
  { value: 'PastDue', label: 'Просрочена' },
  { value: 'Suspended', label: 'Приостановлена' },
  { value: 'Cancelled', label: 'Отменена' },
]

const FLAG_LABEL: Record<string, string> = {
  problem: 'Проблемные магазины (много жалоб)',
  silent: 'Молчащие магазины (нет продаж 30+ дней)',
  'no-sales': 'Магазины без единой продажи',
}

const TAKE = 25

/** Overview's attention blocks link here with ?flag=... -- these three lists come from the
 *  platform-metrics aggregate (not a filterable GetStores facet), so this reads that endpoint and
 *  renders a focused list rather than trying to fake a matching server-side filter. */
function FlaggedStoresView({ flag, onOpenStore }: { flag: string; onOpenStore: (id: number) => void }) {
  const [metrics, setMetrics] = useState<PlatformMetrics | null>(null)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      setMetrics(await metricsApi.getPlatformMetrics())
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить список')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  if (metrics === null && !error) return <Loading scheme="mod" />
  if (error) return <ErrorState scheme="mod" message={error} onRetry={load} />
  if (!metrics) return null

  const rows =
    flag === 'problem'
      ? metrics.problemStores.map((s) => ({ id: s.storeId, name: s.storeName, meta: `${s.reportCount} жалоб` }))
      : flag === 'silent'
        ? metrics.silentStores.map((s) => ({ id: s.storeId, name: s.storeName, meta: `последняя продажа ${new Date(s.lastSaleAt).toLocaleDateString('ru-RU')}` }))
        : metrics.storesWithNoSales.map((s) => ({ id: s.storeId, name: s.storeName, meta: `подключён ${new Date(s.connectedAt).toLocaleDateString('ru-RU')}` }))

  return (
    <Card scheme="mod" className="p-5">
      <div className="mb-4 flex items-center gap-2 text-[13.5px] font-bold text-[color:var(--mod-text)]">
        <AlertIcon width={16} height={16} className="text-[color:var(--mod-warn)]" />
        {FLAG_LABEL[flag] ?? flag}
      </div>
      {rows.length === 0 ? (
        <EmptyState scheme="mod" title="Пусто" body="Сейчас магазинов в этом списке нет." />
      ) : (
        <div className="flex flex-col gap-1">
          {rows.map((r) => (
            <button
              key={r.id}
              onClick={() => onOpenStore(r.id)}
              className="flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2.5 text-left transition-colors hover:bg-[color:var(--mod-panel2)]"
            >
              <span className="truncate text-[13px] font-semibold text-[color:var(--mod-text)]">{r.name}</span>
              <span className="shrink-0 text-[11.5px] text-[color:var(--mod-faint)]">{r.meta}</span>
            </button>
          ))}
        </div>
      )}
    </Card>
  )
}

export function AdminStoresPage() {
  const [params, setParams] = useSearchParams()
  const flag = params.get('flag')
  const storeId = params.get('storeId')
  const status = (params.get('status') as StoreStatus | null) ?? ''
  const subscriptionStatus = (params.get('subscriptionStatus') as SubscriptionStatus | null) ?? ''
  const connectedFrom = params.get('connectedFrom') ?? ''
  const connectedTo = params.get('connectedTo') ?? ''
  const search = params.get('search') ?? ''
  const skip = Number(params.get('skip') ?? '0')

  const [searchInput, setSearchInput] = useState(search)
  const [stores, setStores] = useState<AdminStoreListItem[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getStores({
        skip,
        take: TAKE,
        status: status || undefined,
        subscriptionStatus: subscriptionStatus || undefined,
        connectedFrom: connectedFrom || undefined,
        connectedTo: connectedTo || undefined,
        search: search || undefined,
      })
      setStores(res.stores)
      setTotalCount(res.totalCount)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить список магазинов')
    }
  }, [skip, status, subscriptionStatus, connectedFrom, connectedTo, search])

  useEffect(() => {
    if (!flag) load()
  }, [flag, load])

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'skip') next.delete('skip')
    setParams(next, { replace: true })
  }

  function openStore(id: number) {
    const next = new URLSearchParams(params)
    next.delete('flag')
    next.set('storeId', String(id))
    setParams(next)
  }

  function closeStore() {
    const next = new URLSearchParams(params)
    next.delete('storeId')
    setParams(next)
  }

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      {flag && (
        <div className="mb-4">
          <button onClick={() => updateParam('flag', '')} className="mb-3 text-[12px] font-semibold text-[color:var(--mod-muted)] hover:text-[color:var(--mod-text)] hover:underline">
            ← Все магазины
          </button>
          <FlaggedStoresView flag={flag} onOpenStore={openStore} />
        </div>
      )}

      {!flag && (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-2.5">
            <form
              onSubmit={(e) => {
                e.preventDefault()
                updateParam('search', searchInput.trim())
              }}
              className="relative flex-1 min-w-[220px]"
            >
              <SearchIcon width={15} height={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[color:var(--mod-faint)]" />
              <input
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                onBlur={() => updateParam('search', searchInput.trim())}
                placeholder="Название, адрес или email владельца…"
                className="w-full rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] py-2.5 pl-9 pr-3.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
              />
            </form>
            <Select
              scheme="mod"
              className="min-w-[190px]"
              value={status}
              onChange={(v) => updateParam('status', v)}
              placeholder="Все статусы"
              options={STATUS_OPTIONS}
            />
            <Select
              scheme="mod"
              className="min-w-[190px]"
              value={subscriptionStatus}
              onChange={(v) => updateParam('subscriptionStatus', v)}
              placeholder="Любая подписка"
              options={SUBSCRIPTION_STATUS_OPTIONS}
            />
            <DateField value={connectedFrom} onChange={(iso) => updateParam('connectedFrom', iso)} title="Подключён с" />
            <DateField value={connectedTo} onChange={(iso) => updateParam('connectedTo', iso)} title="Подключён по" />
          </div>

          <Card scheme="mod" className="overflow-hidden">
            {stores === null && !error && <Loading scheme="mod" />}
            {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
            {stores && stores.length === 0 && (
              <EmptyState scheme="mod" icon={<StoreIcon width={22} height={22} />} title="Магазинов не найдено" body="Измените фильтры или поисковый запрос." />
            )}
            {stores && stores.length > 0 && (
              <div className="overflow-x-auto">
                <table className="w-full border-collapse text-[13px]">
                  <thead>
                    <tr className="border-b border-[color:var(--mod-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--mod-faint)]">
                      <th className="px-4 py-3 font-bold">Магазин</th>
                      <th className="px-4 py-3 font-bold">Владелец</th>
                      <th className="px-4 py-3 font-bold">Статус</th>
                      <th className="px-4 py-3 font-bold">Подписка</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stores.map((s) => (
                      <tr
                        key={s.storeId}
                        onClick={() => openStore(s.storeId)}
                        className="cursor-pointer border-b border-[color:var(--mod-border)] transition-colors last:border-0 hover:bg-[color:var(--mod-panel2)]"
                      >
                        <td className="px-4 py-3">
                          <div className="font-semibold text-[color:var(--mod-text)]">{s.name}</div>
                          <div className="truncate text-[11.5px] text-[color:var(--mod-faint)]">{s.address}</div>
                        </td>
                        <td className="px-4 py-3 text-[color:var(--mod-muted)]">{s.ownerEmail ?? '—'}</td>
                        <td className="px-4 py-3">
                          <StoreStatusBadge status={s.status} size="sm" />
                        </td>
                        <td className="px-4 py-3">
                          {s.subscriptionStatus ? (
                            <div className="flex items-center gap-1.5">
                              <SubscriptionStatusBadge status={s.subscriptionStatus} size="sm" />
                              <span className="text-[11.5px] text-[color:var(--mod-faint)]">{s.subscriptionPlanName}</span>
                            </div>
                          ) : (
                            <span className="text-[color:var(--mod-faint)]">—</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            {stores && stores.length > 0 && (
              <div className="px-4 pb-4">
                <Pagination skip={skip} take={TAKE} totalCount={totalCount} onChange={(s) => updateParam('skip', String(s))} />
              </div>
            )}
          </Card>
        </>
      )}

      {storeId && <StoreDetailPanel storeId={Number(storeId)} onClose={closeStore} onNavigateToStore={openStore} />}
    </div>
  )
}
