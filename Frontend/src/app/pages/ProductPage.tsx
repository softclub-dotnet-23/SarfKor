import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { favoritesApi, priceAlertsApi, productsApi } from '../../lib/api'
import {
  Button,
  CountUp,
  EmptyState,
  ErrorState,
  LINE,
  MaskReveal,
  Reveal,
  SectionTitle,
  Skeleton,
  Spinner,
  TXT,
  km,
  money,
  useAsync,
} from '../ui'

/**
 * Optional coordinates. The backend takes lat/lng on the scan and uses them to
 * fill DistanceKm, but the whole lookup works without them, so permission is
 * requested once and a refusal is simply a result set with no distances — never
 * a blocked page.
 */
function useGeo() {
  const [coords, setCoords] = useState<{ lat: number; lng: number } | null>(null)
  const [settled, setSettled] = useState(false)
  useEffect(() => {
    if (!navigator.geolocation) {
      setSettled(true)
      return
    }
    let done = false
    navigator.geolocation.getCurrentPosition(
      (p) => {
        if (done) return
        done = true
        setCoords({ lat: p.coords.latitude, lng: p.coords.longitude })
        setSettled(true)
      },
      () => {
        if (done) return
        done = true
        setSettled(true)
      },
      { timeout: 6000, maximumAge: 300000 },
    )
    return () => {
      done = true
    }
  }, [])
  return { coords, settled }
}

export function ProductPage() {
  const { barcode = '' } = useParams()
  const { coords, settled } = useGeo()

  // Waits for the geolocation prompt to settle so the request is made once, with
  // distances if they are available, instead of firing twice.
  const scan = useAsync(
    () =>
      settled
        ? productsApi.scanBarcode(barcode, coords?.lat, coords?.lng)
        : new Promise<never>(() => {}),
    [barcode, settled, coords?.lat, coords?.lng],
  )

  const product = scan.data

  // Analytics only: a failure here must never surface as a failed price lookup.
  useEffect(() => {
    if (product) void productsApi.recordScan(product.productId).catch(() => {})
  }, [product])

  const stores = useMemo(
    () => (product ? [...product.stores].sort((a, b) => a.price - b.price) : []),
    [product],
  )
  const best = stores[0]
  const worst = stores[stores.length - 1]
  const savings = best && worst ? worst.price - best.price : 0

  if (scan.loading) {
    return (
      <>
        <Skeleton h={13} w={110} className="mb-7" />
        <Skeleton h={46} w="72%" className="mb-4" />
        <Skeleton h={46} w="46%" className="mb-14" />
        <Skeleton h={128} className="mb-10 rounded-2xl" />
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="border-b py-5" style={{ borderColor: LINE }}>
            <Skeleton h={14} w={`${40 + i * 12}%`} />
          </div>
        ))}
      </>
    )
  }

  if (scan.error) {
    // A 404 here is not a failure, it is the catalog honestly not knowing this code.
    const notFound = /404|not found/i.test(scan.error)
    return notFound ? (
      <EmptyState
        title="Такого штрихкода пока нет в каталоге"
        body={`Код ${barcode} ещё никто не добавил. Попробуйте другой товар — каталог пополняется покупателями.`}
        action={
          <Link to="/app/scan">
            <Button variant="ghost">Сканировать другой</Button>
          </Link>
        }
      />
    ) : (
      <ErrorState message={scan.error} onRetry={scan.reload} />
    )
  }

  if (!product) return null

  if (stores.length === 0) {
    return (
      <EmptyState
        title={product.productName}
        body="Товар есть в каталоге, но ни один магазин пока не выставил на него цену."
      />
    )
  }

  return (
    <>
      <Reveal>
        <Link
          to="/app/scan"
          className="mb-8 inline-block text-[12px] transition-colors duration-300 hover:text-white"
          style={{ color: TXT.rest }}
        >
          ← Сканировать другой
        </Link>
      </Reveal>

      <header className="mb-12">
        <Reveal i={1}>
          <p
            className="mb-5 text-[10px] font-bold uppercase tracking-[0.24em] tabular-nums"
            style={{ color: TXT.rest }}
          >
            {barcode}
          </p>
        </Reveal>
        <h1 className="text-[clamp(28px,4.4vw,44px)] font-extrabold leading-[1.06] tracking-[-0.035em]">
          <MaskReveal delay={0.12}>{product.productName}</MaskReveal>
        </h1>
      </header>

      {/* ── BEST PRICE ───────────────────────────────────────── */}
      <Reveal i={3}>
        <section
          className="mb-4 rounded-2xl border p-8 sm:p-10"
          style={{
            borderColor: 'rgba(255,255,255,0.16)',
            background:
              'radial-gradient(120% 140% at 12% 0%, rgba(255,255,255,0.055), transparent 62%)',
          }}
        >
          <p
            className="mb-4 text-[10px] font-bold uppercase tracking-[0.24em]"
            style={{ color: TXT.rest }}
          >
            Лучшая цена
          </p>
          <p className="flex items-baseline gap-2.5 text-[clamp(40px,7vw,60px)] font-extrabold leading-none tracking-[-0.045em] tabular-nums">
            <CountUp to={best.price} decimals={Number.isInteger(best.price) ? 0 : 2} />
            <span className="text-[0.4em] font-bold" style={{ color: TXT.tertiary }}>
              {best.currency === 'TJS' ? 'с.' : best.currency}
            </span>
          </p>
          <p className="mt-5 text-[14px]" style={{ color: TXT.secondary }}>
            <span className="font-semibold text-white">{best.storeName}</span>
            {km(best.distanceKm) && <> · {km(best.distanceKm)}</>}
          </p>
          {savings > 0 && (
            <p className="mt-1.5 text-[13.5px]" style={{ color: TXT.rest }}>
              Экономия до {money(savings, best.currency)} против самой дорогой цены
            </p>
          )}
        </section>
      </Reveal>

      <Reveal i={4}>
        <ProductActions productId={product.productId} suggested={best.price} />
      </Reveal>

      {/* ── ALL STORES ───────────────────────────────────────── */}
      <Reveal i={5}>
        <div className="mt-14">
          <SectionTitle
            right={
              <span className="text-[11px] tabular-nums" style={{ color: TXT.rest }}>
                {stores.length}{' '}
                {stores.length === 1 ? 'магазин' : stores.length < 5 ? 'магазина' : 'магазинов'}
              </span>
            }
          >
            Все цены
          </SectionTitle>

          <ul className="flex flex-col">
            {stores.map((s, i) => {
              // Bar length is the store's price as a share of the dearest, floored
              // so the cheapest never collapses to an invisible sliver.
              const share = worst.price > 0 ? s.price / worst.price : 1
              const diff = s.price - best.price
              return (
                <li key={s.priceEntryId} className="border-b py-5" style={{ borderColor: LINE }}>
                  <div className="flex items-baseline justify-between gap-5">
                    <span className="flex min-w-0 items-baseline gap-4">
                      <span
                        className="w-[18px] shrink-0 text-[10px] font-semibold tabular-nums"
                        style={{ color: i === 0 ? '#fff' : 'rgba(255,255,255,0.30)' }}
                      >
                        {String(i + 1).padStart(2, '0')}
                      </span>
                      <span className="min-w-0">
                        <span className="block truncate text-[15px] font-semibold text-white">
                          {s.storeName}
                        </span>
                        <span className="mt-0.5 block text-[12px]" style={{ color: TXT.rest }}>
                          {km(s.distanceKm) ?? 'Расстояние неизвестно'}
                          {diff > 0 && <> · +{money(diff, s.currency)}</>}
                          {i === 0 && <> · дешевле всего</>}
                        </span>
                      </span>
                    </span>
                    <span className="shrink-0 text-[16px] font-bold tabular-nums text-white">
                      {money(s.price, s.currency)}
                    </span>
                  </div>
                  <span
                    aria-hidden
                    className="mt-3.5 block h-[2px] w-full overflow-hidden rounded-full"
                    style={{ background: 'rgba(255,255,255,0.06)' }}
                  >
                    <PriceBar share={share} highlight={i === 0} />
                  </span>
                </li>
              )
            })}
          </ul>
        </div>
      </Reveal>
    </>
  )
}

/** Relative-price bar: this store's price as a share of the dearest one. */
function PriceBar({ share, highlight }: { share: number; highlight: boolean }) {
  return (
    <span
      className="block h-full rounded-full"
      style={{
        width: `${Math.max(8, share * 100)}%`,
        background: highlight ? '#fff' : 'rgba(255,255,255,0.28)',
      }}
    />
  )
}

/** Favourite toggle + price alert. Both are real endpoints. */
function ProductActions({ productId, suggested }: { productId: number; suggested: number }) {
  const favs = useAsync(() => favoritesApi.getFavorites(), [])
  const [busy, setBusy] = useState(false)
  const [openAlert, setOpenAlert] = useState(false)
  const [target, setTarget] = useState(String(Math.floor(suggested)))
  const [alertMsg, setAlertMsg] = useState('')

  const fav = favs.data?.favorites.find((f) => f.type === 'Product' && f.entityId === productId)

  async function toggleFav() {
    setBusy(true)
    try {
      if (fav) await favoritesApi.removeFavorite('Product', productId)
      else await favoritesApi.addFavorite('Product', productId)
      favs.reload()
    } catch {
      /* the button simply does not change state */
    } finally {
      setBusy(false)
    }
  }

  async function createAlert(e: React.FormEvent) {
    e.preventDefault()
    const value = Number(target)
    if (!Number.isFinite(value) || value <= 0) return
    setBusy(true)
    setAlertMsg('')
    try {
      await priceAlertsApi.createPriceAlert(productId, value)
      setAlertMsg('Уведомим, когда цена опустится ниже.')
      setOpenAlert(false)
    } catch {
      setAlertMsg('Не удалось создать отслеживание.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <div className="flex flex-wrap gap-3">
        <Button variant="ghost" onClick={toggleFav} disabled={busy || favs.loading}>
          {busy && <Spinner />}
          {fav ? 'В избранном' : 'В избранное'}
        </Button>
        <Button variant="ghost" onClick={() => setOpenAlert((v) => !v)}>
          Следить за ценой
        </Button>
      </div>

      {openAlert && (
        <form onSubmit={createAlert} className="mt-5 flex items-end gap-4">
          <label className="flex-1">
            <span
              className="mb-2 block text-[10px] font-bold uppercase tracking-[0.16em]"
              style={{ color: TXT.rest }}
            >
              Сообщить, когда цена ниже
            </span>
            <input
              value={target}
              onChange={(e) => setTarget(e.target.value.replace(/[^\d.]/g, ''))}
              inputMode="decimal"
              className="w-full border-0 bg-transparent pb-3 text-[16px] tabular-nums text-white caret-white"
              style={{ borderBottom: `1px solid ${LINE}` }}
            />
          </label>
          <Button type="submit" disabled={busy}>
            {busy && <Spinner dark />}
            Сохранить
          </Button>
        </form>
      )}

      {alertMsg && (
        <p className="mt-4 text-[13px]" style={{ color: TXT.secondary }}>
          {alertMsg}
        </p>
      )}
    </div>
  )
}
