import { useCallback, useEffect, useState } from 'react'
import { LineChart } from '../components/LineChart'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { Panel, SectionHeader, Row, RowDivider, EmptyRow } from '../cabinet/components/primitives'
import { AlertIcon, ClockIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { storesApi, salesApi, type StoreDashboard, type ProfitReport, type ReorderAlert, type CashierShift } from '../../lib/api'
import { daysAgo, firstOfMonth, today, weekdayLabel } from '../lib/dates'

const DAILY_GOAL_KEY = 'sarfkor-daily-goal'

// n is null on any report row that isn't the 'Found' outcome (see StoreDashboard/ProfitReport
// comment in lib/api/stores.ts) -- every KpiStat call below reads that as "0", the same way an
// empty/not-yet-loaded number already displays, rather than crashing on Math.round(null).
function fmt(n: number | null) {
  return Math.round(n ?? 0).toLocaleString('ru-RU')
}

interface DashboardData {
  dashboard: StoreDashboard
  profitToday: ProfitReport
  profitMonth: ProfitReport
  week: { day: string; value: number }[]
  alerts: ReorderAlert[]
  shifts: CashierShift[]
}

function useDashboardData(storeId: number) {
  const [data, setData] = useState<DashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      const weekDates = Array.from({ length: 7 }, (_, i) => daysAgo(6 - i))
      const [dashboard, profitToday, profitMonth, weekReports, alertsRes, shiftsRes] = await Promise.all([
        storesApi.getStoreDashboard(storeId),
        storesApi.getProfitReport(storeId, today(), today()),
        storesApi.getProfitReport(storeId, firstOfMonth(), today()),
        Promise.all(weekDates.map((d) => storesApi.getDailySalesReport(storeId, d))),
        storesApi.getReorderAlerts(storeId),
        salesApi.getCashierShifts(storeId),
      ])
      if (dashboard.outcome === 'Forbidden' || dashboard.outcome === 'StoreNotFound') {
        setErrorKind(dashboard.outcome === 'Forbidden' ? 'forbidden' : 'notFound')
        setError(dashboard.outcome === 'Forbidden' ? 'Нет доступа к этому магазину' : 'Магазин не найден')
        setLoading(false)
        return
      }
      setData({
        dashboard,
        profitToday,
        profitMonth,
        week: weekDates.map((d, i) => ({ day: weekdayLabel(d), value: weekReports[i].revenue ?? 0 })),
        alerts: alertsRes.alerts ?? [],
        shifts: shiftsRes.shifts ?? [],
      })
    } catch (err) {
      console.error('Failed to load dashboard data:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить данные')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
    const interval = setInterval(load, 60_000)
    return () => clearInterval(interval)
  }, [load])

  return { data, loading, error, errorKind, reload: load }
}

/** Flat KPI: 32/500 number, 12/400 muted label, no icon, hairline left rule. */
function KpiStat({
  label,
  value,
  suffix,
  sub,
  accentColor,
}: {
  label: string
  value: string
  suffix?: string | null
  sub?: string
  accentColor?: string
}) {
  return (
    <div className="relative pl-4">
      <span
        className="absolute inset-y-1 left-0 w-[2px] rounded-full"
        style={{ background: accentColor ?? 'var(--admin-text-tertiary)' }}
        aria-hidden
      />
      <div className="text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">{label}</div>
      <div className="mt-1.5 flex items-end gap-1.5 leading-none">
        <span className="text-[32px] font-[500] tabular-nums text-[color:var(--admin-text)]">{value}</span>
        {suffix && (
          <span className="mb-0.5 text-[13px] font-[400] text-[color:var(--admin-text-tertiary)]">{suffix}</span>
        )}
      </div>
      {sub && <div className="mt-1 text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">{sub}</div>}
    </div>
  )
}

export function DashboardPage() {
  const { storeId } = useAuth()
  const { data, loading, error, errorKind, reload } = useDashboardData(storeId!)
  const [dailyGoal] = useState(() => Number(localStorage.getItem(DAILY_GOAL_KEY)) || 150)

  if (loading) return <Loading label="Загружаем данные магазина…" />
  if (error || !data) {
    return (
      <Panel>
        <ErrorState message={error || 'Нет данных'} kind={error ? errorKind : 'notFound'} onRetry={reload} />
      </Panel>
    )
  }

  const { dashboard, profitToday, profitMonth, week, alerts, shifts } = data
  const monthRevenue = profitMonth.revenue ?? 0
  const monthProfit = profitMonth.profit ?? 0
  const marginPct = monthRevenue > 0 ? Math.round((monthProfit / monthRevenue) * 100) : 0
  const goalPct = Math.min(Math.round(((dashboard.todaySalesCount ?? 0) / dailyGoal) * 100), 100)
  const todayRevenue = profitToday.revenue ?? 0
  const todayProfit = profitToday.profit ?? 0
  const todayMarginPct = todayRevenue > 0 ? Math.round((todayProfit / todayRevenue) * 100) : null
  const recentShifts = [...shifts].sort((a, b) => b.startedAt.localeCompare(a.startedAt)).slice(0, 4)

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-5 xl:flex-row">
      {/* Left column */}
      <div className="flex min-w-0 flex-1 flex-col gap-5">

        {/* Hero KPI strip — today's numbers */}
        <Panel>
          <div className="grid grid-cols-1 gap-8 sm:grid-cols-3">
            <KpiStat
              label="Продано сегодня"
              value={fmt(dashboard.todaySalesCount)}
              sub="позиций"
              accentColor="var(--admin-accent)"
            />
            <KpiStat
              label="Выручка сегодня"
              value={fmt(dashboard.todayRevenue)}
              suffix={dashboard.currency}
              sub={`себестоимость ${fmt(profitToday.totalCost)}`}
              accentColor="#38bdf8"
            />
            <KpiStat
              label="Прибыль сегодня"
              value={fmt(profitToday.profit)}
              suffix={profitToday.currency}
              sub={todayMarginPct !== null ? `маржа ${todayMarginPct}%` : undefined}
              accentColor="var(--admin-success)"
            />
          </div>
        </Panel>

        {/* Revenue chart */}
        <Panel>
          <SectionHeader
            eyebrow="Ежедневные продажи"
            title="Выручка за 7 дней"
            action={
              <button
                onClick={reload}
                className="text-[12px] font-[400] text-[color:var(--admin-text-tertiary)] transition-colors hover:text-[color:var(--admin-text)]"
              >
                Обновить
              </button>
            }
          />
          <LineChart data={week} />
        </Panel>

        {/* Month overview */}
        <Panel>
          <SectionHeader eyebrow="Текущий месяц" title="Финансовый итог" />
          <div className="grid grid-cols-2 gap-8 sm:grid-cols-4">
            <KpiStat label="Выручка" value={fmt(profitMonth.revenue)} suffix={profitMonth.currency} />
            <KpiStat label="Себестоимость" value={fmt(profitMonth.totalCost)} suffix={profitMonth.currency} />
            <KpiStat label="Прибыль" value={fmt(profitMonth.profit)} suffix={profitMonth.currency} accentColor="var(--admin-success)" />
            <KpiStat
              label="Маржинальность"
              value={`${marginPct}`}
              suffix="%"
              accentColor={marginPct >= 20 ? 'var(--admin-success)' : 'var(--admin-text-tertiary)'}
            />
          </div>

          {/* Goal progress */}
          <div className="mt-8 border-t border-[color:var(--admin-border)] pt-6">
            <div className="mb-3 flex items-center justify-between">
              <span className="text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">
                Цель дня — {dashboard.todaySalesCount} / {dailyGoal} позиций
              </span>
              <span className="text-[14px] font-[500] text-[color:var(--admin-text)]">{goalPct}%</span>
            </div>
            <div className="h-1 w-full overflow-hidden rounded-full bg-[color:var(--admin-border)]">
              <div
                className="h-full rounded-full bg-[color:var(--admin-accent)] transition-all duration-700"
                style={{ width: `${goalPct}%` }}
              />
            </div>
          </div>
        </Panel>
      </div>

      {/* Right column */}
      <div className="flex w-full flex-col gap-5 xl:w-[300px] xl:shrink-0">
        {/* Store KPI */}
        <Panel>
          <div className="text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">
            Магазин #{storeId} · склад
          </div>
          <div className="mt-2 text-[32px] font-[500] tabular-nums leading-none text-[color:var(--admin-text)]">
            {fmt(dashboard.productsInStockCount)}
          </div>
          <div className="mt-1 text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">позиций в наличии</div>
        </Panel>

        {/* Recent shifts */}
        <Panel>
          <SectionHeader title="Последние смены" />
          {recentShifts.length === 0 && <EmptyRow>Смен ещё не было</EmptyRow>}
          {recentShifts.map((shift, i) => (
            <div key={shift.cashierShiftId}>
              {i > 0 && <RowDivider />}
              <Row
                icon={<ClockIcon width={14} height={14} />}
                iconTone={shift.endedAt ? 'neutral' : 'accent'}
                title={new Date(shift.startedAt).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short' })}
                subtitle={
                  shift.endedAt
                    ? `Закрыта · ${new Date(shift.endedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}`
                    : 'Открыта сейчас'
                }
                trailing={`${fmt(shift.openingCash)} ${shift.currency}`}
              />
            </div>
          ))}
        </Panel>

        {/* Reorder alerts */}
        <Panel>
          <SectionHeader title="Низкий остаток" />
          {alerts.length === 0 && <EmptyRow>Всё в норме</EmptyRow>}
          {alerts.map((alert, i) => (
            <div key={alert.productId}>
              {i > 0 && <RowDivider />}
              <Row
                icon={<AlertIcon width={14} height={14} />}
                iconTone="warning"
                title={alert.productName}
                subtitle={`Осталось ${alert.currentQuantity} из ${alert.thresholdQuantity}`}
              />
            </div>
          ))}
        </Panel>
      </div>
    </div>
  )
}
