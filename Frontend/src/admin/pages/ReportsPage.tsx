import { useCallback, useEffect, useState } from 'react'
import { LineChart } from '../components/LineChart'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { Panel, SectionHeader, Stat, Row, RowDivider, EmptyRow } from '../cabinet/components/primitives'
import { DownloadIcon } from '../components/icons'
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
      <Panel>
        <ErrorState
          message={error || 'Нет данных'}
          onRetry={() => {
            setLoading(true)
            load(range)
          }}
        />
      </Panel>
    )
  }

  const marginPct = profit.revenue > 0 ? Math.round((profit.profit / profit.revenue) * 100) : 0

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex gap-5 border-b border-[color:var(--admin-border)]">
          {(Object.keys(RANGE_LABEL) as Range[]).map((r) => (
            <button
              key={r}
              onClick={() => setRange(r)}
              className={`-mb-px border-b-2 pb-2.5 text-[12px] font-semibold transition-colors ${
                range === r
                  ? 'border-[color:var(--admin-text)] text-[color:var(--admin-text)]'
                  : 'border-transparent text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text-secondary)]'
              }`}
            >
              {RANGE_LABEL[r]}
            </button>
          ))}
        </div>
        <button
          onClick={exportReport}
          className="flex items-center justify-center gap-2 rounded-[8px] bg-[color:var(--admin-accent)] px-5 py-[10px] text-[14px] font-[500] text-[color:var(--admin-accent-fg)] transition-all duration-150 ease-out hover:scale-[1.01] hover:shadow-[0_4px_16px_rgba(0,0,0,0.18)]"
        >
          <DownloadIcon width={14} height={14} />
          Экспорт CSV
        </button>
      </div>

      <Panel className="grid grid-cols-1 gap-6 sm:grid-cols-2 xl:grid-cols-4">
        <Stat label="Выручка" value={fmt(profit.revenue)} suffix={profit.currency} accent="#38bdf8" />
        <Stat label="Себестоимость" value={fmt(profit.totalCost)} suffix={profit.currency} accent="#fbbf24" />
        <Stat label="Прибыль" value={fmt(profit.profit)} suffix={profit.currency} accent="var(--admin-success)" />
        <Stat label="Маржинальность" value={marginPct} suffix="%" accent="#818cf8" />
      </Panel>

      <Panel>
        <SectionHeader eyebrow="Последние 14 дней, вне зависимости от периода выше" title="Динамика продаж" />
        <LineChart data={chartData} />
      </Panel>

      <Panel>
        <SectionHeader title="Топ товаров по продажам" />
        {topProducts.length === 0 && <EmptyRow>Пока нет данных о продажах</EmptyRow>}
        {topProducts.map((p, i) => (
          <div key={p.productId}>
            {i > 0 && <RowDivider />}
            <Row title={p.productName} trailing={`${p.totalQuantity} шт`} />
          </div>
        ))}
      </Panel>
    </div>
  )
}
