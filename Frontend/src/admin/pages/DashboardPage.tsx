import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { LineChart } from '../components/LineChart'
import { RingChart } from '../components/RingChart'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { RevenueIcon, PackageIcon, AlertIcon, ClockIcon } from '../components/icons'
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

function KpiCard({
  label,
  value,
  suffix,
  icon,
  accent,
}: {
  label: string
  value: number
  suffix?: string
  icon: React.ReactNode
  accent: string
}) {
  return (
    <Card className="p-5">
      <div className="mb-3.5 flex items-center justify-between">
        <span className="text-[13px] font-medium text-[color:var(--admin-text-secondary)]">{label}</span>
        <span className="grid h-9 w-9 place-items-center rounded-[10px]" style={{ background: `${accent}22`, color: accent }}>
          {icon}
        </span>
      </div>
      <div className="whitespace-nowrap text-[28px] font-extrabold leading-none tracking-tight text-[color:var(--admin-text)]">
        {fmt(value)}
        {suffix ? <span className="ml-1 text-base font-semibold text-[color:var(--admin-text-tertiary)]">{suffix}</span> : null}
      </div>
    </Card>
  )
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
      <Card>
        <ErrorState message={error || 'Нет данных'} onRetry={reload} />
      </Card>
    )
  }

  const { dashboard, profitToday, profitMonth, week, alerts, shifts } = data
  const marginPct = profitMonth.revenue > 0 ? Math.round((profitMonth.profit / profitMonth.revenue) * 100) : 0
  const goalPct = Math.min(Math.round((dashboard.todaySalesCount / dailyGoal) * 100), 100)
  const recentShifts = [...shifts].sort((a, b) => b.startedAt.localeCompare(a.startedAt)).slice(0, 4)

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6 xl:flex-row">
      <div className="flex min-w-0 flex-1 flex-col gap-6">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <KpiCard label="Продано сегодня" value={dashboard.todaySalesCount} icon={<PackageIcon width={18} height={18} />} accent="#38bdf8" />
          <KpiCard
            label="Себестоимость сегодня"
            value={profitToday.totalCost}
            suffix={profitToday.currency}
            icon={<PackageIcon width={18} height={18} />}
            accent="#fbbf24"
          />
          <KpiCard
            label="Выручка сегодня"
            value={dashboard.todayRevenue}
            suffix={dashboard.currency}
            icon={<RevenueIcon width={18} height={18} />}
            accent="#34d399"
          />
        </div>

        <Card className="p-6">
          <div className="mb-5 flex items-center justify-between">
            <div>
              <div className="text-[16px] font-bold text-[color:var(--admin-text)]">Выручка за 7 дней</div>
              <div className="mt-0.5 text-xs text-[color:var(--admin-text-tertiary)]">По данным /reports/daily-sales</div>
            </div>
            <button onClick={reload} className="text-xs font-semibold text-[color:var(--admin-accent)] hover:opacity-80">
              Обновить
            </button>
          </div>
          <LineChart data={week} />
        </Card>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Card className="flex flex-col items-center p-6">
            <div className="mb-1 self-start text-[16px] font-bold text-[color:var(--admin-text)]">Маржинальность</div>
            <div className="mb-5 self-start text-xs text-[color:var(--admin-text-tertiary)]">С начала месяца</div>
            <RingChart value={marginPct} label="маржа" colorFrom="#0ea5e9" colorTo="#38bdf8" />
            <div className="mt-4 text-[28px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
              {fmt(profitMonth.profit)} <span className="text-base font-medium text-[color:var(--admin-text-tertiary)]">{profitMonth.currency}</span>
            </div>
            <div className="mt-1 text-xs text-[color:var(--admin-text-tertiary)]">
              Выручка {fmt(profitMonth.revenue)} · себестоимость {fmt(profitMonth.totalCost)}
            </div>
          </Card>
          <Card className="flex flex-col items-center p-6">
            <div className="mb-1 self-start text-[16px] font-bold text-[color:var(--admin-text)]">Цель дня</div>
            <div className="mb-5 self-start text-xs text-[color:var(--admin-text-tertiary)]">
              Настраивается в разделе «Настройки»
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
          </Card>
        </div>
      </div>

      {/* Right panel */}
      <div className="flex w-full flex-col gap-5 xl:w-[300px] xl:shrink-0">
        <Card className="p-5">
          <div className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
            Магазин
          </div>
          <div className="mb-1 text-[16px] font-extrabold text-[color:var(--admin-text)]">ID: {storeId}</div>
          <div className="text-[12px] text-[color:var(--admin-text-tertiary)]">
            {dashboard.productsInStockCount} товаров на складе
          </div>
        </Card>

        <div>
          <div className="mb-3.5 flex items-center justify-between">
            <span className="text-[14px] font-bold text-[color:var(--admin-text)]">Последние смены</span>
          </div>
          <div className="flex flex-col gap-2.5">
            {recentShifts.length === 0 && (
              <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">Ещё не было ни одной смены</p>
            )}
            {recentShifts.map((shift) => (
              <div
                key={shift.cashierShiftId}
                className="flex items-center gap-3 rounded-[14px] bg-[color:var(--admin-card)] p-3 ring-1 ring-[color:var(--admin-border)]"
              >
                <span className="grid h-9 w-9 shrink-0 place-items-center rounded-[10px] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]">
                  <ClockIcon width={16} height={16} />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[12.5px] font-semibold text-[color:var(--admin-text)]">
                    {new Date(shift.startedAt).toLocaleDateString('ru-RU')}
                  </div>
                  <div className="mt-0.5 truncate text-[11px] text-[color:var(--admin-text-tertiary)]">
                    {shift.endedAt
                      ? shift.closingCash !== undefined && shift.expectedCash !== undefined
                        ? `Закрыта · расхождение ${fmt(shift.closingCash - shift.expectedCash)}`
                        : 'Закрыта'
                      : 'Открыта'}
                  </div>
                </div>
                <div className="shrink-0 text-[13px] font-bold text-[color:var(--admin-text)]">
                  {fmt(shift.openingCash)} {shift.currency}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div>
          <div className="mb-3.5 flex items-center justify-between">
            <span className="text-[14px] font-bold text-[color:var(--admin-text)]">Требует внимания</span>
          </div>
          <div className="flex flex-col gap-2.5">
            {alerts.length === 0 && (
              <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">
                Нет товаров ниже порога — либо правила дозаказа ещё не настроены
              </p>
            )}
            {alerts.map((alert) => (
              <div
                key={alert.productId}
                className="flex items-center gap-3 rounded-[14px] bg-[color:var(--admin-card)] p-3 ring-1 ring-[color:var(--admin-border)]"
              >
                <span className="grid h-9 w-9 shrink-0 place-items-center rounded-[10px] bg-[color:var(--admin-warning-dim)] text-[color:var(--admin-warning)]">
                  <AlertIcon width={16} height={16} />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">
                    Товар #{alert.productId}
                  </div>
                  <div className="mt-0.5 text-[11px] font-medium text-[color:var(--admin-warning)]">
                    Осталось {alert.currentQuantity} из {alert.thresholdQuantity}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
