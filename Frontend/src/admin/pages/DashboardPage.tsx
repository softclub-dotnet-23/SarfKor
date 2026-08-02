import { useCallback, useEffect, useState } from 'react'
import { LineChart } from '../components/LineChart'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { Panel, SectionHeader, Row, RowDivider, EmptyRow } from '../cabinet/components/primitives'
import { AlertIcon, ClockIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { storesApi, salesApi, ApiError, type StoreDashboard, type ProfitReport, type ReorderAlert, type CashierShift } from '../../lib/api'
import { daysAgo, firstOfMonth, today, weekdayLabel } from '../lib/dates'

const DAILY_GOAL_KEY = 'sarfkor-daily-goal'

function fmt(n: number) {
  return Math.round(n).toLocaleString('ru-RU')
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
        setError(dashboard.outcome === 'Forbidden' ? 'Нет доступа к этому магазину' : 'Магазин не найден')
        setLoading(false)
        return
      }
      setData({
        dashboard,
        profitToday,
        profitMonth,
        week: weekDates.map((d, i) => ({ day: weekdayLabel(d), value: weekReports[i].revenue })),
        alerts: alertsRes.alerts ?? [],
        shifts: shiftsRes.shifts ?? [],
      })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить данные')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
    const interval = setInterval(load, 60_000)
    return () => clearInterval(interval)
  }, [load])

  return { data, loading, error, reload: load }
}

/** Large editorial KPI block — the number dominates, label is a small eyebrow. */
function HeroStat({
  label,
  value,
  suffix,
  sub,
}: {
  label: string
  value: string
  suffix?: string
  sub?: string
}) {
  return (
    <div className="flex flex-col gap-1">
      <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-[color:var(--admin-text-tertiary)]">
        {label}
      </div>
      <div className="flex items-end gap-2 leading-none">
        <span className="text-[44px] font-black tracking-tighter text-[color:var(--admin-text)]">{value}</span>
        {suffix && (
          <span className="mb-1.5 text-[15px] font-semibold text-[color:var(--admin-text-tertiary)]">{suffix}</span>
        )}
      </div>
      {sub && <div className="text-[12px] text-[color:var(--admin-text-tertiary)]">{sub}</div>}
    </div>
  )
}

export function DashboardPage() {
  const { storeId } = useAuth()
  const { data, loading, error, reload } = useDashboardData(storeId!)
  const [dailyGoal] = useState(() => Number(localStorage.getItem(DAILY_GOAL_KEY)) || 150)

  if (loading) return <Loading label="Загружаем данные магазина…" />
  if (error || !data) {
    return (
      <Panel>
        <ErrorState message={error || 'Нет данных'} onRetry={reload} />
      </Panel>
    )
  }

  const { dashboard, profitToday, profitMonth, week, alerts, shifts } = data
  const marginPct = profitMonth.revenue > 0 ? Math.round((profitMonth.profit / profitMonth.revenue) * 100) : 0
  const goalPct = Math.min(Math.round((dashboard.todaySalesCount / dailyGoal) * 100), 100)
  const recentShifts = [...shifts].sort((a, b) => b.startedAt.localeCompare(a.startedAt)).slice(0, 4)

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-5 xl:flex-row">
      {/* Left column */}
      <div className="flex min-w-0 flex-1 flex-col gap-5">

        {/* Hero KPI strip */}
        <Panel>
          <div className="grid grid-cols-1 gap-8 sm:grid-cols-3">
            <HeroStat
              label="Продано сегодня"
              value={fmt(dashboard.todaySalesCount)}
              sub="позиций"
            />
            <HeroStat
              label="Выручка сегодня"
              value={fmt(dashboard.todayRevenue)}
              suffix={dashboard.currency}
              sub={`себестоимость ${fmt(profitToday.totalCost)}`}
            />
            <HeroStat
              label="Прибыль сегодня"
              value={fmt(profitToday.profit)}
              suffix={profitToday.currency}
              sub={profitToday.revenue > 0 ? `маржа ${Math.round((profitToday.profit / profitToday.revenue) * 100)}%` : undefined}
            />
          </div>
        </Panel>

        {/* Revenue chart */}
        <Panel>
          <SectionHeader
            eyebrow="/reports/daily-sales"
            title="Выручка за 7 дней"
            action={
              <button
                onClick={reload}
                className="text-[11.5px] font-semibold text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)] transition-colors"
              >
                Обновить
              </button>
            }
          />
          <LineChart data={week} />
        </Panel>

        {/* Month overview */}
        <Panel>
          <SectionHeader eyebrow="Месяц" title="Финансовый итог" />
          <div className="grid grid-cols-2 gap-6 sm:grid-cols-4">
            <div>
              <div className="text-[10.5px] font-bold uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">Выручка</div>
              <div className="mt-1.5 text-[24px] font-black tracking-tight text-[color:var(--admin-text)]">
                {fmt(profitMonth.revenue)}
                <span className="ml-1.5 text-[12px] font-semibold text-[color:var(--admin-text-tertiary)]">{profitMonth.currency}</span>
              </div>
            </div>
            <div>
              <div className="text-[10.5px] font-bold uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">Себестоимость</div>
              <div className="mt-1.5 text-[24px] font-black tracking-tight text-[color:var(--admin-text)]">
                {fmt(profitMonth.totalCost)}
                <span className="ml-1.5 text-[12px] font-semibold text-[color:var(--admin-text-tertiary)]">{profitMonth.currency}</span>
              </div>
            </div>
            <div>
              <div className="text-[10.5px] font-bold uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">Прибыль</div>
              <div className="mt-1.5 text-[24px] font-black tracking-tight text-[color:var(--admin-text)]">
                {fmt(profitMonth.profit)}
                <span className="ml-1.5 text-[12px] font-semibold text-[color:var(--admin-text-tertiary)]">{profitMonth.currency}</span>
              </div>
            </div>
            <div>
              <div className="text-[10.5px] font-bold uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">Маржа</div>
              <div className="mt-1.5 text-[24px] font-black tracking-tight" style={{ color: marginPct >= 20 ? 'var(--admin-success)' : 'var(--admin-text)' }}>
                {marginPct}%
              </div>
            </div>
          </div>

          {/* Goal progress bar */}
          <div className="mt-6 border-t border-[color:var(--admin-border)] pt-5">
            <div className="mb-2 flex items-center justify-between">
              <span className="text-[11px] font-bold uppercase tracking-[0.12em] text-[color:var(--admin-text-tertiary)]">
                Цель дня — {dashboard.todaySalesCount} / {dailyGoal}
              </span>
              <span className="text-[13px] font-black text-[color:var(--admin-text)]">{goalPct}%</span>
            </div>
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-[color:var(--admin-hover)]">
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
        {/* Store info */}
        <Panel>
          <div className="text-[10px] font-bold uppercase tracking-[0.16em] text-[color:var(--admin-text-tertiary)]">
            Магазин #{storeId}
          </div>
          <div className="mt-2 text-[32px] font-black tracking-tighter text-[color:var(--admin-text)]">
            {fmt(dashboard.productsInStockCount)}
          </div>
          <div className="text-[12px] text-[color:var(--admin-text-tertiary)]">товаров на складе</div>
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
                title={`Товар #${alert.productId}`}
                subtitle={`Осталось ${alert.currentQuantity} из ${alert.thresholdQuantity}`}
              />
            </div>
          ))}
        </Panel>
      </div>
    </div>
  )
}
