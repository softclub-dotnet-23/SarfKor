import { useCallback, useEffect, useState } from 'react'
import { LineChart } from '../components/LineChart'
import { RingChart } from '../components/RingChart'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { Panel, SectionHeader, Stat, Row, RowDivider, EmptyRow } from '../cabinet/components/primitives'
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
        setError(
          dashboard.outcome === 'Forbidden'
            ? 'У вас нет доступа к этому магазину'
            : 'Магазин не найден — возможно, ID устарел',
        )
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
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить данные дашборда')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
    // A light auto-refresh keeps KPIs current without hammering the API on
    // every render — 60s is a reasonable "feels live" cadence for a
    // real backend that isn't rate-limited on these read endpoints, but
    // shouldn't be polled aggressively either.
    const interval = setInterval(load, 60000)
    return () => clearInterval(interval)
  }, [load])

  return { data, loading, error, reload: load }
}

export function DashboardPage() {
  const { storeId } = useAuth()
  const { data, loading, error, reload } = useDashboardData(storeId!)
  const [dailyGoal] = useState(() => Number(localStorage.getItem(DAILY_GOAL_KEY)) || 150)

  if (loading) {
    return <Loading label="Загружаем данные магазина…" />
  }

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
      <div className="flex min-w-0 flex-1 flex-col gap-5">
        <Panel className="grid grid-cols-1 gap-6 sm:grid-cols-3">
          <Stat label="Продано сегодня" value={fmt(dashboard.todaySalesCount)} accent="#38bdf8" />
          <Stat label="Себестоимость сегодня" value={fmt(profitToday.totalCost)} suffix={profitToday.currency} accent="#fbbf24" />
          <Stat label="Выручка сегодня" value={fmt(dashboard.todayRevenue)} suffix={dashboard.currency} accent="#34d399" />
        </Panel>

        <Panel>
          <SectionHeader
            eyebrow="/reports/daily-sales"
            title="Выручка за 7 дней"
            action={
              <button onClick={reload} className="text-[11.5px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80">
                Обновить
              </button>
            }
          />
          <LineChart data={week} />
        </Panel>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Panel className="flex flex-col items-center">
            <div className="w-full">
              <SectionHeader eyebrow="С начала месяца" title="Маржинальность" />
            </div>
            <RingChart value={marginPct} label="маржа" colorFrom="#0ea5e9" colorTo="#38bdf8" />
            <div className="mt-4 text-[28px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
              {fmt(profitMonth.profit)} <span className="text-base font-medium text-[color:var(--admin-text-tertiary)]">{profitMonth.currency}</span>
            </div>
            <div className="mt-1 text-xs text-[color:var(--admin-text-tertiary)]">
              Выручка {fmt(profitMonth.revenue)} · себестоимость {fmt(profitMonth.totalCost)}
            </div>
          </Panel>
          <Panel className="flex flex-col items-center">
            <div className="w-full">
              <SectionHeader eyebrow="Настраивается в «Настройках»" title="Цель дня" />
            </div>
            <RingChart value={goalPct} label="выполнено" colorFrom="#38bdf8" colorTo="#818cf8" />
            <div className="mt-4 flex items-center gap-4">
              <div className="text-center">
                <div className="text-[22px] font-extrabold text-[color:var(--admin-text)]">{dashboard.todaySalesCount}</div>
                <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">продано</div>
              </div>
              <div className="h-8 w-px bg-[color:var(--admin-border)]" />
              <div className="text-center">
                <div className="text-[22px] font-extrabold text-[color:var(--admin-text-tertiary)]">{dailyGoal}</div>
                <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">цель</div>
              </div>
            </div>
          </Panel>
        </div>
      </div>

      {/* Right panel */}
      <div className="flex w-full flex-col gap-5 xl:w-[300px] xl:shrink-0">
        <Panel>
          <div className="mb-1 text-[10.5px] font-bold uppercase tracking-[0.14em] text-[color:var(--admin-text-tertiary)]">
            Магазин
          </div>
          <div className="mb-1 text-[16px] font-extrabold text-[color:var(--admin-text)]">ID: {storeId}</div>
          <div className="text-[12px] text-[color:var(--admin-text-tertiary)]">
            {dashboard.productsInStockCount} товаров на складе
          </div>
        </Panel>

        <Panel>
          <SectionHeader title="Последние смены" />
          {recentShifts.length === 0 && <EmptyRow>Ещё не было ни одной смены</EmptyRow>}
          {recentShifts.map((shift, i) => (
            <div key={shift.cashierShiftId}>
              {i > 0 && <RowDivider />}
              <Row
                icon={<ClockIcon width={15} height={15} />}
                iconTone="accent"
                title={new Date(shift.startedAt).toLocaleDateString('ru-RU')}
                subtitle={
                  shift.endedAt
                    ? shift.closingCash !== undefined && shift.expectedCash !== undefined
                      ? `Закрыта · расхождение ${fmt(shift.closingCash - shift.expectedCash)}`
                      : 'Закрыта'
                    : 'Открыта'
                }
                trailing={`${fmt(shift.openingCash)} ${shift.currency}`}
              />
            </div>
          ))}
        </Panel>

        <Panel>
          <SectionHeader title="Требует внимания" />
          {alerts.length === 0 && <EmptyRow>Нет товаров ниже порога — либо правила дозаказа ещё не настроены</EmptyRow>}
          {alerts.map((alert, i) => (
            <div key={alert.productId}>
              {i > 0 && <RowDivider />}
              <Row
                icon={<AlertIcon width={15} height={15} />}
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
