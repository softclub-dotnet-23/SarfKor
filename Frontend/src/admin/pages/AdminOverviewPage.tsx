import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import {
  StoreIcon,
  CardIcon,
  RevenueIcon,
  RegisterIcon,
  TagIcon,
  UsersIcon,
  AlertIcon,
  ClockIcon,
} from '../components/icons'
import { metricsApi, ApiError, type PlatformMetrics, type MetricsDay } from '../../lib/api'

function fmtMoney(amount: number, currency: string) {
  return `${Math.round(amount).toLocaleString('ru-RU')} ${currency}`
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short' })
}

/* ---------- KPI tiles ---------- */

function KpiTile({ label, value, icon, to }: { label: string; value: string | number; icon: React.ReactNode; to?: string }) {
  const body = (
    <Card scheme="mod" interactive={!!to} className="p-5">
      <div className="flex items-start justify-between gap-2">
        <span className="text-[11.5px] font-semibold leading-tight text-[color:var(--mod-muted)]">{label}</span>
        <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[color:var(--mod-accent-dim)] text-[color:var(--mod-accent2)]">
          {icon}
        </span>
      </div>
      <div className="mt-3 font-[JetBrains_Mono,monospace] text-[27px] font-bold tracking-tight text-[color:var(--mod-text)]">
        {value}
      </div>
    </Card>
  )
  return to ? <Link to={to}>{body}</Link> : body
}

/* ---------- attention block: a title + a small clickable row list ---------- */

function AttentionBlock<T>({
  title,
  icon,
  items,
  emptyText,
  to,
  renderRow,
}: {
  title: string
  icon: React.ReactNode
  items: T[]
  emptyText: string
  to: string
  renderRow: (item: T) => { key: string | number; label: string; meta: string }
}) {
  return (
    <Card scheme="mod" className="p-5">
      <div className="mb-3 flex items-center justify-between">
        <div className="flex items-center gap-2 text-[13.5px] font-bold text-[color:var(--mod-text)]">
          <span className="grid h-7 w-7 place-items-center rounded-lg bg-[color:var(--mod-warn-dim)] text-[color:var(--mod-warn)]">
            {icon}
          </span>
          {title}
        </div>
        {items.length > 0 && (
          <Link to={to} className="text-[11.5px] font-semibold text-[color:var(--mod-accent2)] hover:underline">
            Все ({items.length})
          </Link>
        )}
      </div>
      {items.length === 0 ? (
        <p className="py-3 text-[12.5px] text-[color:var(--mod-faint)]">{emptyText}</p>
      ) : (
        <div className="flex flex-col gap-0.5">
          {items.slice(0, 5).map((item) => {
            const row = renderRow(item)
            return (
              <Link
                key={row.key}
                to={to}
                className="flex items-center justify-between gap-2 rounded-lg px-2 py-2 transition-colors hover:bg-[color:var(--mod-panel2)]"
              >
                <span className="truncate text-[12.5px] font-semibold text-[color:var(--mod-text)]">{row.label}</span>
                <span className="shrink-0 font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--mod-faint)]">{row.meta}</span>
              </Link>
            )
          })}
        </div>
      )}
    </Card>
  )
}

/* ---------- time-series chart (plain SVG bars, no charting dependency) ---------- */

const RANGE_OPTIONS = [7, 30, 90] as const

function SalesChart() {
  const [rangeDays, setRangeDays] = useState<(typeof RANGE_OPTIONS)[number]>(7)
  const [days, setDays] = useState<MetricsDay[] | null>(null)
  const [error, setError] = useState('')

  const load = useCallback(async (range: number) => {
    setError('')
    try {
      const to = new Date()
      const from = new Date()
      from.setDate(from.getDate() - (range - 1))
      const res = await metricsApi.getMetricsTimeSeries(from.toISOString().slice(0, 10), to.toISOString().slice(0, 10))
      setDays(res.days)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить график')
    }
  }, [])

  useEffect(() => {
    setDays(null)
    load(rangeDays)
  }, [rangeDays, load])

  const max = Math.max(1, ...(days ?? []).map((d) => d.sales))
  const newStoresTotal = (days ?? []).reduce((sum, d) => sum + d.newStores, 0)
  const salesTotal = (days ?? []).reduce((sum, d) => sum + d.sales, 0)
  const labelEvery = rangeDays <= 7 ? 1 : rangeDays <= 30 ? 5 : 15

  return (
    <Card scheme="mod" className="p-5">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div className="text-[14px] font-bold text-[color:var(--mod-text)]">Продажи по платформе</div>
        <div className="flex gap-1 rounded-lg bg-[color:var(--mod-panel2)] p-1">
          {RANGE_OPTIONS.map((r) => (
            <button
              key={r}
              onClick={() => setRangeDays(r)}
              className={`rounded-md px-3 py-1.5 text-[12px] font-bold transition-colors ${
                rangeDays === r ? 'bg-[color:var(--mod-accent)] text-white' : 'text-[color:var(--mod-muted)] hover:text-[color:var(--mod-text)]'
              }`}
            >
              {r} дн.
            </button>
          ))}
        </div>
      </div>

      {days === null && !error && <Loading scheme="mod" />}
      {error && <ErrorState scheme="mod" message={error} onRetry={() => load(rangeDays)} />}
      {days && (
        <>
          <div className="flex items-end gap-[2px]" style={{ height: 140 }}>
            {days.map((d) => (
              <div key={d.date} className="group relative flex flex-1 flex-col items-center justify-end" style={{ height: '100%' }}>
                <div
                  className="w-full rounded-t-[3px] bg-[color:var(--mod-accent)] opacity-80 transition-opacity group-hover:opacity-100"
                  style={{ height: `${Math.max(2, (d.sales / max) * 100)}%` }}
                  title={`${fmtDate(d.date)}: ${d.sales} продаж`}
                />
              </div>
            ))}
          </div>
          <div className="mt-1.5 flex justify-between font-[JetBrains_Mono,monospace] text-[10px] text-[color:var(--mod-faint)]">
            {days
              .filter((_, i) => i % labelEvery === 0 || i === days.length - 1)
              .map((d) => (
                <span key={d.date}>{fmtDate(d.date)}</span>
              ))}
          </div>
          <div className="mt-4 flex gap-6 border-t border-[color:var(--mod-border)] pt-3">
            <div>
              <div className="font-[JetBrains_Mono,monospace] text-[16px] font-bold text-[color:var(--mod-text)]">{salesTotal}</div>
              <div className="text-[11px] text-[color:var(--mod-faint)]">продаж за период</div>
            </div>
            <div>
              <div className="font-[JetBrains_Mono,monospace] text-[16px] font-bold text-[color:var(--mod-text)]">{newStoresTotal}</div>
              <div className="text-[11px] text-[color:var(--mod-faint)]">новых магазинов</div>
            </div>
          </div>
        </>
      )}
    </Card>
  )
}

/* ---------- page ---------- */

export function AdminOverviewPage() {
  const [metrics, setMetrics] = useState<PlatformMetrics | null>(null)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      setMetrics(await metricsApi.getPlatformMetrics())
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить метрики платформы')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  if (metrics === null && !error) return <Loading scheme="mod" />
  if (error) return <ErrorState scheme="mod" message={error} onRetry={load} />
  if (!metrics) return null

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 grid grid-cols-1 gap-3.5 sm:grid-cols-2 xl:grid-cols-4">
        <KpiTile
          label="Активных магазинов"
          value={metrics.storesByStatus.Active ?? 0}
          icon={<StoreIcon width={18} height={18} />}
          to="/admin/stores?status=Active"
        />
        <KpiTile
          label="Активных подписок"
          value={metrics.activeSubscriptionsTotal}
          icon={<CardIcon width={18} height={18} />}
          to="/admin/subscriptions"
        />
        <KpiTile
          label="Прогноз выручки / мес"
          value={fmtMoney(metrics.estimatedMonthlyRevenue, metrics.revenueCurrency)}
          icon={<RevenueIcon width={18} height={18} />}
          to="/admin/subscriptions"
        />
        <KpiTile
          label="Продаж за 7 дней"
          value={metrics.salesLast7Days}
          icon={<RegisterIcon width={18} height={18} />}
        />
        <KpiTile
          label="Новых цен за 7 дней"
          value={metrics.newPriceEntriesLast7Days}
          icon={<TagIcon width={18} height={18} />}
        />
        <KpiTile
          label="Покупателей за 30 дней"
          value={metrics.activeConsumersLast30Days}
          icon={<UsersIcon width={18} height={18} />}
        />
        <KpiTile
          label="Триалы истекают на неделе"
          value={metrics.trialsEndingThisWeek}
          icon={<ClockIcon width={18} height={18} />}
          to="/admin/subscriptions?tab=expiring"
        />
        <KpiTile
          label="Просроченные подписки"
          value={metrics.pastDueCount}
          icon={<AlertIcon width={18} height={18} />}
          to="/admin/subscriptions?tab=pastdue"
        />
      </div>

      <div className="mb-4">
        <SalesChart />
      </div>

      <div className="grid grid-cols-1 gap-3.5 lg:grid-cols-3">
        <AttentionBlock
          title="Проблемные магазины"
          icon={<AlertIcon width={15} height={15} />}
          items={metrics.problemStores}
          emptyText="Нет магазинов с накопленными жалобами"
          to="/admin/stores?flag=problem"
          renderRow={(s) => ({ key: s.storeId, label: s.storeName, meta: `${s.reportCount} жалоб` })}
        />
        <AttentionBlock
          title="Молчащие магазины"
          icon={<ClockIcon width={15} height={15} />}
          items={metrics.silentStores}
          emptyText="Все подключённые магазины продают"
          to="/admin/stores?flag=silent"
          renderRow={(s) => ({ key: s.storeId, label: s.storeName, meta: fmtDate(s.lastSaleAt) })}
        />
        <AttentionBlock
          title="Без единой продажи"
          icon={<StoreIcon width={15} height={15} />}
          items={metrics.storesWithNoSales}
          emptyText="Все подключённые магазины хоть раз продавали"
          to="/admin/stores?flag=no-sales"
          renderRow={(s) => ({ key: s.storeId, label: s.storeName, meta: `с ${fmtDate(s.connectedAt)}` })}
        />
      </div>
    </div>
  )
}
