import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { LineChart } from '../components/LineChart'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { DownloadIcon, RevenueIcon, PackageIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { storesApi, productsApi, ApiError, type ProfitReport } from '../../lib/api'
import { daysAgo, today, weekdayLabel } from '../lib/dates'

type Range = 'today' | 'week' | 'month'

const RANGE_LABEL: Record<Range, string> = {
  today: 'Сегодня',
  week: '7 дней',
  month: '30 дней',
}

const RANGE_DAYS: Record<Range, number> = {
  today: 0,
  week: 6,
  month: 29,
}

interface TopProduct {
  productId: number
  productName: string
  totalQuantity: number
}

function fmt(n: number) {
  return Math.round(n).toLocaleString('ru-RU')
}

function downloadCsv(rows: (string | number)[][], filename: string) {
  const csv = rows.map((r) => r.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(',')).join('\n')
  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

export function ReportsPage() {
  const { storeId } = useAuth()
  const [range, setRange] = useState<Range>('week')
  const [profit, setProfit] = useState<ProfitReport | null>(null)
  const [chartData, setChartData] = useState<{ day: string; value: number }[]>([])
  const [topProducts, setTopProducts] = useState<TopProduct[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(
    async (r: Range) => {
      if (!storeId) return
      setError('')
      try {
        const from = daysAgo(RANGE_DAYS[r])
        const chartDates = Array.from({ length: 14 }, (_, i) => daysAgo(13 - i))
        const [profitRes, chartReports, topRes] = await Promise.all([
          storesApi.getProfitReport(storeId, from, today()),
          Promise.all(chartDates.map((d) => storesApi.getDailySalesReport(storeId, d))),
          productsApi.getTopSellingProducts(storeId, 8),
        ])
        setProfit(profitRes)
        setChartData(chartDates.map((d, i) => ({ day: weekdayLabel(d), value: chartReports[i].revenue })))
        setTopProducts(topRes.products ?? [])
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Не удалось загрузить отчёты')
      } finally {
        setLoading(false)
      }
    },
    [storeId],
  )

  useEffect(() => {
    setLoading(true)
    load(range)
  }, [load, range])

  function exportReport() {
    if (!profit) return
    const marginPct = profit.revenue > 0 ? Math.round((profit.profit / profit.revenue) * 100) : 0
    const rows: (string | number)[][] = [
      ['Отчёт', RANGE_LABEL[range]],
      ['Период', `${profit.fromDate} — ${profit.toDate}`],
      ['Выручка', fmt(profit.revenue)],
      ['Себестоимость', fmt(profit.totalCost)],
      ['Прибыль', fmt(profit.profit)],
      ['Маржа, %', marginPct],
      [],
      ['Товар', 'Продано, шт'],
      ...topProducts.map((p) => [p.productName, p.totalQuantity]),
    ]
    downloadCsv(rows, `sarfkor-report-${range}.csv`)
  }

  if (loading) {
    return <Loading label="Загружаем отчёты…" />
  }

  if (error || !profit) {
    return (
      <Card>
        <ErrorState
          message={error || 'Нет данных'}
          onRetry={() => {
            setLoading(true)
            load(range)
          }}
        />
      </Card>
    )
  }

  const marginPct = profit.revenue > 0 ? Math.round((profit.profit / profit.revenue) * 100) : 0

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex gap-2">
          {(Object.keys(RANGE_LABEL) as Range[]).map((r) => (
            <button
              key={r}
              onClick={() => setRange(r)}
              className={`rounded-lg px-3.5 py-1.5 text-[12px] font-semibold transition-colors ${
                range === r
                  ? 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]'
                  : 'text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text-secondary)]'
              }`}
            >
              {RANGE_LABEL[r]}
            </button>
          ))}
        </div>
        <button
          onClick={exportReport}
          className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[13px] font-semibold text-white hover:opacity-90"
        >
          <DownloadIcon width={15} height={15} />
          Экспорт CSV
        </button>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Выручка</div>
          <div className="mt-2 text-[24px] font-extrabold text-[color:var(--admin-text)]">
            {fmt(profit.revenue)} {profit.currency}
          </div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Себестоимость</div>
          <div className="mt-2 text-[24px] font-extrabold text-[color:var(--admin-text)]">
            {fmt(profit.totalCost)} {profit.currency}
          </div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Прибыль</div>
          <div className="mt-2 text-[24px] font-extrabold text-[color:var(--admin-success)]">
            {fmt(profit.profit)} {profit.currency}
          </div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Маржинальность</div>
          <div className="mt-2 text-[24px] font-extrabold text-[color:var(--admin-text)]">{marginPct}%</div>
        </Card>
      </div>

      <Card className="p-6">
        <div className="mb-1 flex items-center gap-2">
          <RevenueIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Динамика продаж за 14 дней</span>
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          График всегда показывает последние 14 дней, независимо от периода выше
        </p>
        <LineChart data={chartData} />
      </Card>

      <Card className="p-6">
        <div className="mb-5 flex items-center gap-2">
          <PackageIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Топ товаров по продажам</span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[420px] border-collapse text-left text-[13px]">
            <thead>
              <tr className="text-[11px] uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                <th className="pb-3 font-semibold">Товар</th>
                <th className="pb-3 font-semibold">Продано, шт</th>
              </tr>
            </thead>
            <tbody>
              {topProducts.map((p) => (
                <tr key={p.productId} className="border-t border-[color:var(--admin-border)]">
                  <td className="py-3 pr-3 font-semibold text-[color:var(--admin-text)]">{p.productName}</td>
                  <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{p.totalQuantity}</td>
                </tr>
              ))}
              {topProducts.length === 0 && (
                <tr>
                  <td colSpan={2} className="py-10 text-center text-[color:var(--admin-text-tertiary)]">
                    Пока нет данных о продажах
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  )
}
