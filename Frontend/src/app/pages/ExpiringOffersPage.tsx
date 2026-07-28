import { useEffect, useState } from 'react'
import { getExpiringOffersNearby, type ExpiringOffer } from '../../lib/api/expiringOffers'
import { ApiError } from '../../lib/api'
import { ClockIcon, MapPinIcon } from '../../components/icons'

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function timeRemaining(expiresAt: string): string {
  const diffMs = new Date(expiresAt).getTime() - Date.now()
  if (diffMs <= 0) return 'Истёк'
  const totalMinutes = Math.floor(diffMs / 60000)
  const days = Math.floor(totalMinutes / (60 * 24))
  const hours = Math.floor((totalMinutes % (60 * 24)) / 60)
  if (days > 0) return `Осталось ${days} дн.`
  if (hours > 0) return `Осталось ${hours} ч.`
  return `Осталось ${totalMinutes} мин.`
}

export function ExpiringOffersPage() {
  const [offers, setOffers] = useState<ExpiringOffer[] | null>(null)
  const [error, setError] = useState('')
  const [locStatus, setLocStatus] = useState<'idle' | 'requesting' | 'granted' | 'denied'>('idle')

  useEffect(() => {
    let cancelled = false

    async function load(lat?: number, lng?: number) {
      setError('')
      try {
        const res = await getExpiringOffersNearby(lat, lng)
        if (!cancelled) setOffers(res.offers)
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiError ? err.message : 'Не удалось загрузить предложения')
      }
    }

    if (!('geolocation' in navigator)) {
      load()
      return
    }

    setLocStatus('requesting')
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        if (cancelled) return
        setLocStatus('granted')
        load(pos.coords.latitude, pos.coords.longitude)
      },
      () => {
        if (cancelled) return
        setLocStatus('denied')
        load()
      },
      { timeout: 8000 },
    )

    return () => {
      cancelled = true
    }
  }, [])

  const sorted = offers ? [...offers].sort((a, b) => (a.distanceKm ?? Infinity) - (b.distanceKm ?? Infinity)) : null

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-1 text-[22px] font-extrabold tracking-tight">Скоро истекает</h1>
      <p className="mb-2 text-[14px] text-[color:var(--text-secondary)]">Товары со скидкой из-за истекающего срока годности</p>
      {locStatus === 'denied' && (
        <p className="mb-4 text-[12.5px] text-[color:var(--text-tertiary)]">
          Доступ к геолокации не предоставлен — показаны предложения без сортировки по расстоянию.
        </p>
      )}

      {offers === null && !error && (
        <p className="py-10 text-center text-[13px] text-[color:var(--text-tertiary)]">
          {locStatus === 'requesting' ? 'Определяем местоположение…' : 'Загрузка…'}
        </p>
      )}
      {error && <p className="py-10 text-center text-[14px] text-[color:var(--text-secondary)]">{error}</p>}
      {sorted && sorted.length === 0 && (
        <div className="rounded-3xl bg-[color:var(--bg-card)] p-10 text-center shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">
          <ClockIcon width={28} height={28} className="mx-auto mb-3 text-[color:var(--text-tertiary)]" />
          <p className="text-[14px] font-semibold">Сейчас нет предложений с истекающим сроком</p>
        </div>
      )}
      {sorted && sorted.length > 0 && (
        <div className="flex flex-col gap-2.5">
          {sorted.map((o) => (
            <div
              key={o.offerId}
              className="flex items-center justify-between gap-3 rounded-2xl bg-[color:var(--bg-card)] p-4 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]"
            >
              <div className="flex min-w-0 items-center gap-3">
                <span className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-[color:var(--color-brand-light)] text-[color:var(--color-brand)]">
                  <ClockIcon width={18} height={18} />
                </span>
                <div className="min-w-0">
                  <div className="truncate text-[14px] font-bold">{o.productName}</div>
                  <div className="flex items-center gap-1 text-[12px] text-[color:var(--text-tertiary)]">
                    <MapPinIcon width={12} height={12} />
                    <span className="truncate">{o.storeName}</span>
                    {o.distanceKm != null && <span>· {o.distanceKm.toFixed(1)} км</span>}
                  </div>
                </div>
              </div>
              <div className="shrink-0 text-right">
                <div className="text-[15px] font-extrabold">
                  {fmt(o.discountedPrice)} <span className="text-[12px] font-semibold text-[color:var(--text-tertiary)]">{o.currency}</span>
                </div>
                <div className="text-[11.5px] text-[color:var(--text-tertiary)] line-through">{fmt(o.originalPrice)}</div>
                <div className="mt-0.5 text-[11px] font-semibold text-[color:var(--color-brand)]">{timeRemaining(o.expiresAt)}</div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
