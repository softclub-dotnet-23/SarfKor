import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { priceAlertsApi, ApiError, type PriceAlert } from '../../lib/api'
import { BellIcon } from '../../components/icons'

export function PriceAlertsPage() {
  const [items, setItems] = useState<PriceAlert[] | null>(null)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)
  const [productId, setProductId] = useState('')
  const [target, setTarget] = useState('')
  const [creating, setCreating] = useState(false)
  const [formError, setFormError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await priceAlertsApi.getPriceAlerts()).alerts)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить уведомления')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function create(e: FormEvent) {
    e.preventDefault()
    const pid = Number(productId)
    const t = Number(target)
    if (!pid || Number.isNaN(t) || t <= 0) return
    setCreating(true)
    setFormError('')
    try {
      await priceAlertsApi.createPriceAlert(pid, t, 'TJS')
      setProductId('')
      setTarget('')
      await load()
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Не удалось создать уведомление')
    } finally {
      setCreating(false)
    }
  }

  async function deactivate(alertId: number) {
    setBusyId(alertId)
    try {
      await priceAlertsApi.deactivatePriceAlert(alertId)
      setItems((cur) => cur?.map((a) => (a.priceAlertId === alertId ? { ...a, isActive: false } : a)) ?? null)
    } catch {
      await load()
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-1 text-[22px] font-extrabold tracking-tight">Уведомления о цене</h1>
      <p className="mb-6 text-[14px] text-[color:var(--text-secondary)]">Сообщим, когда цена товара упадёт до нужного уровня</p>

      <form onSubmit={create} className="mb-6 flex flex-wrap items-center gap-2 rounded-2xl bg-[color:var(--bg-card)] p-4 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">
        <input
          value={productId}
          onChange={(e) => setProductId(e.target.value)}
          placeholder="ID товара"
          type="number"
          className="w-32 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3 py-2 text-[13.5px] outline-none focus:border-[color:var(--color-brand)]"
        />
        <input
          value={target}
          onChange={(e) => setTarget(e.target.value)}
          placeholder="Желаемая цена, TJS"
          type="number"
          step="0.01"
          min={0}
          className="w-40 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3 py-2 text-[13.5px] outline-none focus:border-[color:var(--color-brand)]"
        />
        <button
          type="submit"
          disabled={creating || !productId || !target}
          className="rounded-lg bg-[color:var(--color-brand)] px-4 py-2 text-[13px] font-bold text-white disabled:opacity-50"
        >
          Создать
        </button>
        {formError && <span className="text-[12px] text-[color:var(--text-secondary)]">{formError}</span>}
      </form>

      {items === null && !error && <p className="py-10 text-center text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p>}
      {error && <p className="py-6 text-center text-[14px] text-[color:var(--text-secondary)]">{error}</p>}
      {items && items.length === 0 && (
        <div className="rounded-3xl bg-[color:var(--bg-card)] p-10 text-center shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">
          <BellIcon width={28} height={28} className="mx-auto mb-3 text-[color:var(--text-tertiary)]" />
          <p className="text-[14px] font-semibold">Уведомлений пока нет</p>
          <p className="mt-1 text-[13px] text-[color:var(--text-secondary)]">Создайте на странице сканирования или вручную выше</p>
        </div>
      )}
      {items && items.length > 0 && (
        <div className="flex flex-col gap-2.5">
          {items.map((a) => (
            <div
              key={a.priceAlertId}
              className="flex items-center justify-between gap-3 rounded-2xl bg-[color:var(--bg-card)] p-4 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]"
            >
              <div>
                <div className="text-[14px] font-bold">Товар #{a.productId}</div>
                <div className="text-[13px] text-[color:var(--text-secondary)]">
                  цель: {a.targetPrice.toFixed(2)} {a.currency}
                </div>
              </div>
              {a.isActive ? (
                <button
                  onClick={() => deactivate(a.priceAlertId)}
                  disabled={busyId === a.priceAlertId}
                  className="rounded-full border border-[color:var(--border-subtle)] px-3.5 py-1.5 text-[12.5px] font-semibold text-[color:var(--text-secondary)] hover:border-[color:var(--border-strong)] disabled:opacity-40"
                >
                  Отключить
                </button>
              ) : (
                <span className="rounded-full bg-[color:var(--bg-section)] px-3.5 py-1.5 text-[12.5px] font-semibold text-[color:var(--text-tertiary)]">
                  Отключено
                </span>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
