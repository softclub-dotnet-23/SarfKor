import { useState } from 'react'
import { priceAlertsApi } from '../../lib/api'
import { EmptyState, ErrorState, LINE, Reveal, SectionTitle, Skeleton, TXT, money, useAsync } from '../ui'

/**
 * Price alerts. The backend has no delete — an alert is deactivated, not removed —
 * so inactive rows stay visible and are shown as switched off rather than hidden,
 * which is also what stops the list from looking like the action silently failed.
 */
export function AlertsPage() {
  const alerts = useAsync(() => priceAlertsApi.getPriceAlerts(), [])
  const [busy, setBusy] = useState<number | null>(null)

  async function deactivate(id: number) {
    setBusy(id)
    try {
      await priceAlertsApi.deactivatePriceAlert(id)
      alerts.reload()
    } finally {
      setBusy(null)
    }
  }

  const rows = alerts.data?.alerts ?? []
  const active = rows.filter((a) => a.isActive)
  const off = rows.filter((a) => !a.isActive)

  return (
    <>
      <Reveal>
        <p
          className="mb-5 text-[10px] font-bold uppercase tracking-[0.28em]"
          style={{ color: TXT.rest }}
        >
          Отслеживание цен
        </p>
        <h1 className="mb-12 text-[clamp(30px,4.6vw,46px)] font-extrabold leading-[1.04] tracking-[-0.04em]">
          Ждём снижения
        </h1>
      </Reveal>

      {alerts.loading && (
        <div className="flex flex-col">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="border-b py-5" style={{ borderColor: LINE }}>
              <Skeleton h={14} w={`${42 + i * 8}%`} />
            </div>
          ))}
        </div>
      )}

      {alerts.error && <ErrorState message={alerts.error} onRetry={alerts.reload} />}

      {!alerts.loading && !alerts.error && rows.length === 0 && (
        <EmptyState
          title="Нет активных отслеживаний"
          body="Откройте товар и нажмите «Следить за ценой» — сообщим, когда она опустится ниже вашей планки."
        />
      )}

      {active.length > 0 && (
        <ul className="mb-14 flex flex-col">
          {active.map((a, i) => (
            <Reveal key={a.priceAlertId} i={i}>
              <li
                className="flex items-baseline justify-between gap-5 border-b py-5"
                style={{ borderColor: LINE }}
              >
                <span className="min-w-0">
                  <span className="block text-[15px] font-semibold text-white">
                    Товар #{a.productId}
                  </span>
                  <span className="mt-0.5 block text-[12px]" style={{ color: TXT.rest }}>
                    Цель — {money(a.targetPrice, a.currency)}
                  </span>
                </span>
                <button
                  onClick={() => deactivate(a.priceAlertId)}
                  disabled={busy === a.priceAlertId}
                  className="shrink-0 text-[12px] transition-colors duration-300 hover:text-white disabled:opacity-40"
                  style={{ color: TXT.rest }}
                >
                  Отключить
                </button>
              </li>
            </Reveal>
          ))}
        </ul>
      )}

      {off.length > 0 && (
        <>
          <SectionTitle>Отключённые</SectionTitle>
          <ul className="flex flex-col">
            {off.map((a) => (
              <li
                key={a.priceAlertId}
                className="flex items-baseline justify-between gap-5 border-b py-4"
                style={{ borderColor: LINE }}
              >
                <span className="text-[14px]" style={{ color: TXT.rest }}>
                  Товар #{a.productId}
                </span>
                <span className="text-[12px] tabular-nums" style={{ color: TXT.rest }}>
                  {money(a.targetPrice, a.currency)}
                </span>
              </li>
            ))}
          </ul>
        </>
      )}
    </>
  )
}
