import { useCallback, useEffect, useRef, useState, type FormEvent, type SVGProps } from 'react'
import {
  productsApi,
  favoritesApi,
  priceAlertsApi,
  shoppingListsApi,
  pricingApi,
  ApiError,
  type ScanBarcodeResult,
  type ShoppingList,
} from '../../lib/api'
import { getReviews, submitReview, type Review } from '../../lib/api/reviews'
import { reportOutOfStock } from '../../lib/api/feedback'
import { raisePriceEntryDispute } from '../../lib/api/pricing'
import { getMostScannedProducts, type MostScannedProduct } from '../../lib/api/products'
import { BarcodeIcon, MapPinIcon, HeartIcon, BellIcon, ListIcon, CloseIcon, StarIcon } from '../../components/icons'

function FlagIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={13} height={13} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z" />
      <line x1="4" y1="22" x2="4" y2="15" />
    </svg>
  )
}

function AlertCircleIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={13} height={13} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <circle cx="12" cy="12" r="10" />
      <line x1="12" y1="8" x2="12" y2="12" />
      <line x1="12" y1="16" x2="12.01" y2="16" />
    </svg>
  )
}

function TrendingIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" />
      <polyline points="17 6 23 6 23 12" />
    </svg>
  )
}

const BARCODE_FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39', 'itf']

type CameraState = 'idle' | 'requesting' | 'active' | 'denied' | 'unsupported'

function CameraScanner({ onDetected }: { onDetected: (code: string) => void }) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const rafRef = useRef<number>(0)
  const [state, setState] = useState<CameraState>('idle')

  function stop() {
    if (rafRef.current) cancelAnimationFrame(rafRef.current)
    streamRef.current?.getTracks().forEach((t) => t.stop())
    streamRef.current = null
    setState('idle')
  }

  // Release the camera if the user navigates away mid-scan — a stream left
  // running after unmount keeps the camera's hardware indicator lit and the
  // device locked for other tabs/apps until a full page reload.
  useEffect(() => () => stop(), [])

  async function start() {
    if (!('BarcodeDetector' in window)) {
      setState('unsupported')
      return
    }
    setState('requesting')
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } })
      streamRef.current = stream
      if (videoRef.current) {
        videoRef.current.srcObject = stream
        await videoRef.current.play()
      }
      setState('active')

      const detector = new BarcodeDetector({ formats: BARCODE_FORMATS })
      const tick = async () => {
        if (!videoRef.current) return
        try {
          const codes = await detector.detect(videoRef.current)
          if (codes.length > 0) {
            const value = codes[0].rawValue
            stop()
            onDetected(value)
            return
          }
        } catch {
          // Transient per-frame decode failure (e.g. frame mid-transition) — keep trying.
        }
        rafRef.current = requestAnimationFrame(tick)
      }
      rafRef.current = requestAnimationFrame(tick)
    } catch {
      setState('denied')
    }
  }

  if (state === 'unsupported') {
    return (
      <p className="text-[13px] text-[color:var(--text-secondary)]">
        Ваш браузер не поддерживает сканирование камерой. Введите штрихкод вручную ниже.
      </p>
    )
  }

  if (state === 'idle' || state === 'requesting' || state === 'denied') {
    return (
      <div>
        <button
          onClick={start}
          disabled={state === 'requesting'}
          className="flex w-full items-center justify-center gap-2 rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] py-3 text-[14px] font-bold text-[color:var(--text-primary)] transition-colors hover:border-[color:var(--border-strong)] disabled:opacity-60"
        >
          <BarcodeIcon width={18} height={18} />
          {state === 'requesting' ? 'Запрашиваем доступ к камере…' : 'Сканировать камерой'}
        </button>
        {state === 'denied' && (
          <p className="mt-2 text-[12.5px] text-[color:var(--text-secondary)]">
            Доступ к камере не предоставлен. Введите штрихкод вручную ниже или разрешите доступ в настройках браузера.
          </p>
        )}
      </div>
    )
  }

  return (
    <div className="relative overflow-hidden rounded-xl bg-black">
      {/* eslint-disable-next-line jsx-a11y/media-has-caption */}
      <video ref={videoRef} className="aspect-video w-full object-cover" playsInline muted />
      <div className="pointer-events-none absolute inset-6 rounded-xl border-2 border-white/70" />
      <button
        onClick={stop}
        aria-label="Остановить камеру"
        className="absolute right-3 top-3 grid h-9 w-9 place-items-center rounded-full bg-black/50 text-white"
      >
        <CloseIcon width={16} height={16} />
      </button>
      <p className="absolute inset-x-0 bottom-3 text-center text-[13px] font-medium text-white/90">
        Наведите камеру на штрихкод
      </p>
    </div>
  )
}

function Card({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`rounded-3xl bg-[color:var(--bg-card)] p-6 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)] ${className}`}>
      {children}
    </div>
  )
}

function AddToListMenu({ productId }: { productId: number }) {
  const [open, setOpen] = useState(false)
  const [lists, setLists] = useState<ShoppingList[] | null>(null)
  const [newName, setNewName] = useState('')
  const [status, setStatus] = useState('')

  async function openMenu() {
    setOpen((v) => !v)
    if (lists === null) {
      try {
        setLists((await shoppingListsApi.getShoppingLists()).lists)
      } catch {
        setLists([])
      }
    }
  }

  async function addTo(listId: number) {
    setStatus('')
    try {
      await shoppingListsApi.addShoppingListItem(listId, productId, 1)
      setStatus('Добавлено')
      setTimeout(() => setOpen(false), 700)
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось добавить')
    }
  }

  async function createAndAdd(e: FormEvent) {
    e.preventDefault()
    if (!newName.trim()) return
    try {
      const res = await shoppingListsApi.createShoppingList(newName.trim())
      await addTo(res.shoppingListId)
      setNewName('')
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось создать список')
    }
  }

  return (
    <div className="relative">
      <button
        onClick={openMenu}
        className="flex items-center gap-1.5 rounded-full border border-[color:var(--border-subtle)] px-3.5 py-2 text-[13px] font-semibold text-[color:var(--text-secondary)] hover:border-[color:var(--border-strong)] hover:text-[color:var(--text-primary)]"
      >
        <ListIcon width={15} height={15} />В список
      </button>
      {open && (
        <div className="absolute right-0 top-full z-10 mt-2 w-64 rounded-2xl bg-[color:var(--bg-card)] p-3 shadow-[var(--shadow-lift)] ring-1 ring-[color:var(--border-subtle)]">
          {lists === null && <p className="p-2 text-[13px] text-[color:var(--text-secondary)]">Загрузка…</p>}
          {lists && lists.length > 0 && (
            <div className="mb-2 flex flex-col gap-1">
              {lists.map((l) => (
                <button
                  key={l.shoppingListId}
                  onClick={() => addTo(l.shoppingListId)}
                  className="rounded-lg px-2.5 py-2 text-left text-[13px] font-medium hover:bg-[color:var(--bg-section)]"
                >
                  {l.name} <span className="text-[color:var(--text-tertiary)]">({l.items.length})</span>
                </button>
              ))}
            </div>
          )}
          <form onSubmit={createAndAdd} className="flex gap-1.5 border-t border-[color:var(--border-subtle)] pt-2">
            <input
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Новый список"
              className="min-w-0 flex-1 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-2.5 py-1.5 text-[12.5px] outline-none focus:border-[color:var(--color-brand)]"
            />
            <button type="submit" className="shrink-0 rounded-lg bg-[color:var(--color-brand)] px-3 py-1.5 text-[12px] font-bold text-white">
              +
            </button>
          </form>
          {status && <p className="mt-1.5 text-[11.5px] text-[color:var(--text-secondary)]">{status}</p>}
        </div>
      )}
    </div>
  )
}

function PriceAlertButton({ productId, cheapest }: { productId: number; cheapest?: number }) {
  const [open, setOpen] = useState(false)
  const [target, setTarget] = useState(cheapest ? String(cheapest) : '')
  const [status, setStatus] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    const t = Number(target)
    if (Number.isNaN(t) || t <= 0) return
    try {
      await priceAlertsApi.createPriceAlert(productId, t, 'TJS')
      setStatus('Уведомление создано')
      setTimeout(() => setOpen(false), 900)
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось создать уведомление')
    }
  }

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1.5 rounded-full border border-[color:var(--border-subtle)] px-3.5 py-2 text-[13px] font-semibold text-[color:var(--text-secondary)] hover:border-[color:var(--border-strong)] hover:text-[color:var(--text-primary)]"
      >
        <BellIcon width={15} height={15} />
        Уведомить о цене
      </button>
      {open && (
        <form
          onSubmit={submit}
          className="absolute right-0 top-full z-10 mt-2 w-64 rounded-2xl bg-[color:var(--bg-card)] p-3.5 shadow-[var(--shadow-lift)] ring-1 ring-[color:var(--border-subtle)]"
        >
          <p className="mb-2 text-[12.5px] text-[color:var(--text-secondary)]">Сообщить, когда цена упадёт до:</p>
          <div className="flex gap-1.5">
            <input
              value={target}
              onChange={(e) => setTarget(e.target.value)}
              type="number"
              step="0.01"
              min={0}
              className="min-w-0 flex-1 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-2.5 py-1.5 text-[12.5px] outline-none focus:border-[color:var(--color-brand)]"
            />
            <button type="submit" className="shrink-0 rounded-lg bg-[color:var(--color-brand)] px-3 py-1.5 text-[12px] font-bold text-white">
              OK
            </button>
          </div>
          {status && <p className="mt-1.5 text-[11.5px] text-[color:var(--text-secondary)]">{status}</p>}
        </form>
      )}
    </div>
  )
}

function DisputePriceButton({ priceEntryId }: { priceEntryId: number }) {
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (!reason.trim() || busy) return
    setBusy(true)
    setStatus('')
    try {
      const res = await raisePriceEntryDispute(priceEntryId, reason.trim())
      if (res.outcome === 'Raised') {
        setStatus('Спор отправлен на рассмотрение')
        setReason('')
        setTimeout(() => setOpen(false), 1200)
      } else {
        setStatus('Эта цена больше не существует')
      }
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось отправить спор')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1 text-[11px] font-medium text-[color:var(--text-tertiary)] hover:text-[color:var(--text-primary)]"
      >
        <FlagIcon />
        Оспорить цену
      </button>
      {open && (
        <form
          onSubmit={submit}
          onClick={(e) => e.stopPropagation()}
          className="absolute right-0 top-full z-10 mt-2 w-60 rounded-2xl bg-[color:var(--bg-card)] p-3.5 text-left shadow-[var(--shadow-lift)] ring-1 ring-[color:var(--border-subtle)]"
        >
          <p className="mb-2 text-[12px] font-medium text-[color:var(--text-secondary)]">Почему эта цена неверна?</p>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={2}
            autoFocus
            placeholder="Например: на ценнике другая сумма"
            className="w-full resize-none rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-2.5 py-1.5 text-[12.5px] outline-none focus:border-[color:var(--color-brand)]"
          />
          <button
            type="submit"
            disabled={busy || !reason.trim()}
            className="mt-2 w-full rounded-lg bg-[color:var(--color-brand)] px-3 py-1.5 text-[12px] font-bold text-white disabled:opacity-50"
          >
            {busy ? 'Отправляем…' : 'Отправить'}
          </button>
          {status && <p className="mt-1.5 text-[11.5px] text-[color:var(--text-secondary)]">{status}</p>}
        </form>
      )}
    </div>
  )
}

function OutOfStockButton({ productId, storeId }: { productId: number; storeId: number }) {
  const [open, setOpen] = useState(false)
  const [description, setDescription] = useState('')
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (busy) return
    setBusy(true)
    setStatus('')
    try {
      await reportOutOfStock(productId, description.trim() || 'Нет в наличии', storeId)
      setStatus('Спасибо, сообщили магазину')
      setTimeout(() => setOpen(false), 1200)
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось отправить')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1 text-[11px] font-medium text-[color:var(--text-tertiary)] hover:text-[color:var(--text-primary)]"
      >
        <AlertCircleIcon />
        Нет в наличии
      </button>
      {open && (
        <form
          onSubmit={submit}
          onClick={(e) => e.stopPropagation()}
          className="absolute right-0 top-full z-10 mt-2 w-60 rounded-2xl bg-[color:var(--bg-card)] p-3.5 text-left shadow-[var(--shadow-lift)] ring-1 ring-[color:var(--border-subtle)]"
        >
          <p className="mb-2 text-[12px] font-medium text-[color:var(--text-secondary)]">Сообщить, что товара нет в этом магазине</p>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={2}
            autoFocus
            placeholder="Комментарий (необязательно)"
            className="w-full resize-none rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-2.5 py-1.5 text-[12.5px] outline-none focus:border-[color:var(--color-brand)]"
          />
          <button
            type="submit"
            disabled={busy}
            className="mt-2 w-full rounded-lg bg-[color:var(--color-brand)] px-3 py-1.5 text-[12px] font-bold text-white disabled:opacity-50"
          >
            {busy ? 'Отправляем…' : 'Сообщить'}
          </button>
          {status && <p className="mt-1.5 text-[11.5px] text-[color:var(--text-secondary)]">{status}</p>}
        </form>
      )}
    </div>
  )
}

function MostScannedSection() {
  const [products, setProducts] = useState<MostScannedProduct[] | null>(null)

  useEffect(() => {
    getMostScannedProducts(6)
      .then((res) => setProducts(res.products))
      .catch(() => setProducts([]))
  }, [])

  if (products === null || products.length === 0) return null

  return (
    <Card className="mt-4">
      <div className="mb-3 flex items-center gap-2 text-[14px] font-bold">
        <TrendingIcon />
        Часто сканируют
      </div>
      <div className="flex flex-col divide-y divide-[color:var(--border-subtle)]">
        {products.map((p) => (
          <div key={p.productId} className="flex items-center justify-between gap-2 py-2.5 text-[13.5px]">
            <span className="truncate font-medium">{p.productName}</span>
            <span className="shrink-0 text-[11.5px] text-[color:var(--text-tertiary)]">{p.totalScans} сканирований</span>
          </div>
        ))}
      </div>
    </Card>
  )
}

function ReportPriceForm({ productId, storeOptions, onSubmitted }: { productId: number; storeOptions: { storeId: number; storeName: string }[]; onSubmitted: () => void }) {
  const [storeId, setStoreId] = useState(storeOptions[0] ? String(storeOptions[0].storeId) : '')
  const [price, setPrice] = useState('')
  const [status, setStatus] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const p = Number(price)
    if (!storeId || Number.isNaN(p) || p <= 0) return
    setBusy(true)
    setStatus('')
    try {
      await pricingApi.submitPriceUpdate(productId, Number(storeId), p, 'TJS')
      setStatus('Спасибо! Цена обновлена.')
      setPrice('')
      onSubmitted()
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось отправить цену')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={submit} className="flex flex-wrap items-center gap-2 rounded-2xl bg-[color:var(--bg-section)] p-4">
      <span className="text-[13px] font-medium text-[color:var(--text-secondary)]">Знаете другую цену? Сообщите:</span>
      <select
        value={storeId}
        onChange={(e) => setStoreId(e.target.value)}
        className="rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-2.5 py-1.5 text-[13px] outline-none"
      >
        {storeOptions.map((s) => (
          <option key={s.storeId} value={s.storeId}>
            {s.storeName}
          </option>
        ))}
      </select>
      <input
        value={price}
        onChange={(e) => setPrice(e.target.value)}
        type="number"
        step="0.01"
        min={0}
        placeholder="Цена"
        className="w-24 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-2.5 py-1.5 text-[13px] outline-none"
      />
      <button
        type="submit"
        disabled={busy}
        className="rounded-lg bg-[color:var(--text-primary)] px-4 py-1.5 text-[12.5px] font-bold text-[color:var(--bg-app)] disabled:opacity-50"
      >
        Отправить
      </button>
      {status && <span className="text-[12px] text-[color:var(--text-secondary)]">{status}</span>}
    </form>
  )
}

function StarPicker({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  return (
    <div className="flex items-center gap-1">
      {[1, 2, 3, 4, 5].map((n) => (
        <button
          key={n}
          type="button"
          onClick={() => onChange(n)}
          aria-label={`Оценка ${n} из 5`}
          className={n <= value ? 'text-[color:var(--color-brand)]' : 'text-[color:var(--border-strong)]'}
        >
          <StarIcon width={20} height={20} />
        </button>
      ))}
    </div>
  )
}

function StarDisplay({ rating }: { rating: number }) {
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((n) => (
        <StarIcon
          key={n}
          width={14}
          height={14}
          className={n <= rating ? 'text-[color:var(--color-brand)]' : 'text-[color:var(--border-strong)]'}
        />
      ))}
    </div>
  )
}

function ReviewsSection({ productId, storeOptions }: { productId: number; storeOptions: { storeId: number; storeName: string }[] }) {
  const [reviews, setReviews] = useState<Review[] | null>(null)
  const [error, setError] = useState('')
  const [rating, setRating] = useState(5)
  const [comment, setComment] = useState('')
  const [storeId, setStoreId] = useState('')
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getReviews(productId)
      setReviews([...res.reviews].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить отзывы')
    }
  }, [productId])

  useEffect(() => {
    setReviews(null)
    setError('')
    setStatus('')
    load()
  }, [load])

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (!comment.trim()) return
    setBusy(true)
    setStatus('')
    try {
      await submitReview(productId, rating, comment.trim(), storeId ? Number(storeId) : undefined)
      setComment('')
      setRating(5)
      setStatus('Спасибо за отзыв!')
      await load()
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось отправить отзыв')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card>
      <div className="mb-4 flex items-center gap-2 text-[15px] font-bold">
        <StarIcon width={16} height={16} />
        Отзывы
      </div>

      {reviews === null && !error && <p className="text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p>}
      {error && <p className="text-[13px] text-[color:var(--text-secondary)]">{error}</p>}
      {reviews && reviews.length === 0 && (
        <p className="text-[13px] text-[color:var(--text-tertiary)]">Отзывов пока нет — станьте первым</p>
      )}
      {reviews && reviews.length > 0 && (
        <div className="mb-4 flex flex-col divide-y divide-[color:var(--border-subtle)]">
          {reviews.map((r) => (
            <div key={r.reviewId} className="py-3 first:pt-0 last:pb-0">
              <div className="flex items-center justify-between gap-2">
                <StarDisplay rating={r.rating} />
                <span className="text-[11.5px] text-[color:var(--text-tertiary)]">{new Date(r.createdAt).toLocaleDateString('ru-RU')}</span>
              </div>
              {r.comment && <p className="mt-1.5 text-[13.5px] text-[color:var(--text-secondary)]">{r.comment}</p>}
            </div>
          ))}
        </div>
      )}

      <form onSubmit={submit} className="flex flex-col gap-2.5 border-t border-[color:var(--border-subtle)] pt-4">
        <span className="text-[13px] font-semibold text-[color:var(--text-secondary)]">Оставить отзыв</span>
        <StarPicker value={rating} onChange={setRating} />
        {storeOptions.length > 0 && (
          <select
            value={storeId}
            onChange={(e) => setStoreId(e.target.value)}
            className="w-full rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-2.5 py-1.5 text-[13px] outline-none"
          >
            <option value="">Магазин (необязательно)</option>
            {storeOptions.map((s) => (
              <option key={s.storeId} value={s.storeId}>
                {s.storeName}
              </option>
            ))}
          </select>
        )}
        <textarea
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder="Ваш отзыв о товаре"
          rows={3}
          className="w-full resize-none rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3.5 py-2.5 text-[13.5px] outline-none focus:border-[color:var(--color-brand)]"
        />
        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={busy || !comment.trim()}
            className="rounded-xl bg-[color:var(--color-brand)] px-5 py-2.5 text-[13px] font-bold text-white disabled:opacity-50"
          >
            Отправить отзыв
          </button>
          {status && <span className="text-[12.5px] text-[color:var(--text-secondary)]">{status}</span>}
        </div>
      </form>
    </Card>
  )
}

export function ScanPage() {
  const [barcode, setBarcode] = useState('')
  const [result, setResult] = useState<ScanBarcodeResult | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [favStatus, setFavStatus] = useState('')

  async function runScan(codeOverride?: string) {
    const code = (codeOverride ?? barcode).trim()
    if (!code) return
    setLoading(true)
    setError('')
    setNotFound(false)
    setFavStatus('')
    try {
      const res = await productsApi.scanBarcode(code)
      setResult(res)
      productsApi.recordScan(res.productId, res.stores[0]?.storeId).catch(() => {})
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setResult(null)
        setNotFound(true)
      } else {
        setError(err instanceof ApiError ? err.message : 'Не удалось выполнить поиск')
      }
    } finally {
      setLoading(false)
    }
  }

  function handleScan(e: FormEvent) {
    e.preventDefault()
    runScan()
  }

  function handleDetected(code: string) {
    setBarcode(code)
    runScan(code)
  }

  async function addToFavorites() {
    if (!result) return
    try {
      await favoritesApi.addFavorite('Product', result.productId)
      setFavStatus('Добавлено в избранное')
    } catch (err) {
      setFavStatus(err instanceof ApiError ? err.message : 'Не удалось добавить')
    }
  }

  const sortedStores = result ? [...result.stores].sort((a, b) => a.price - b.price) : []
  const cheapest = sortedStores[0]?.price

  return (
    <div className="mx-auto max-w-2xl">
      <div className="mb-6 text-center">
        <h1 className="text-[26px] font-extrabold tracking-tight">Сканируй. Сравнивай. Экономь.</h1>
        <p className="mt-1 text-[14px] text-[color:var(--text-secondary)]">Введи штрихкод товара — покажем цену во всех ближайших магазинах</p>
      </div>

      <Card>
        <div className="mb-4">
          <CameraScanner onDetected={handleDetected} />
        </div>
        <div className="mb-3 flex items-center gap-3 text-[12px] font-semibold uppercase tracking-wide text-[color:var(--text-tertiary)]">
          <span className="h-px flex-1 bg-[color:var(--border-subtle)]" />
          или введите вручную
          <span className="h-px flex-1 bg-[color:var(--border-subtle)]" />
        </div>
        <form onSubmit={handleScan} className="flex gap-2">
          <div className="flex flex-1 items-center gap-2 rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3.5 py-3">
            <BarcodeIcon width={20} height={20} className="shrink-0 text-[color:var(--text-tertiary)]" />
            <input
              value={barcode}
              onChange={(e) => setBarcode(e.target.value)}
              placeholder="Штрихкод, например 4870204120983"
              className="w-full min-w-0 bg-transparent text-[15px] outline-none"
              inputMode="numeric"
            />
          </div>
          <button
            type="submit"
            disabled={loading || !barcode.trim()}
            className="shrink-0 rounded-xl bg-[color:var(--color-brand)] px-6 py-3 text-[14px] font-bold text-white shadow-[var(--shadow-brand)] transition-transform hover:scale-[1.02] disabled:opacity-50"
          >
            {loading ? 'Ищем…' : 'Найти'}
          </button>
        </form>
      </Card>

      {!result && !notFound && !error && <MostScannedSection />}

      {error && (
        <Card className="mt-4 text-center text-[14px] text-[color:var(--text-secondary)]">{error}</Card>
      )}

      {notFound && (
        <Card className="mt-4 text-center">
          <p className="text-[14px] font-semibold">Товар с таким штрихкодом не найден</p>
          <p className="mt-1 text-[13px] text-[color:var(--text-secondary)]">Проверьте штрихкод или попробуйте другой товар</p>
        </Card>
      )}

      {result && (
        <div className="mt-5 flex flex-col gap-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-[19px] font-bold">{result.productName}</h2>
              <p className="text-[12.5px] text-[color:var(--text-tertiary)]">Штрихкод {barcode.trim()}</p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <button
                onClick={addToFavorites}
                className="flex items-center gap-1.5 rounded-full border border-[color:var(--border-subtle)] px-3.5 py-2 text-[13px] font-semibold text-[color:var(--text-secondary)] hover:border-[color:var(--border-strong)] hover:text-[color:var(--text-primary)]"
              >
                <HeartIcon width={15} height={15} />В избранное
              </button>
              <PriceAlertButton productId={result.productId} cheapest={cheapest} />
              <AddToListMenu productId={result.productId} />
            </div>
          </div>
          {favStatus && <p className="text-[12.5px] text-[color:var(--text-secondary)]">{favStatus}</p>}

          {sortedStores.length === 0 ? (
            <Card className="text-center text-[14px] text-[color:var(--text-secondary)]">Пока нет цен на этот товар ни в одном магазине</Card>
          ) : (
            <div className="flex flex-col gap-2.5">
              {sortedStores.map((s, i) => (
                <Card key={s.storeId} className={i === 0 ? 'flex items-center justify-between gap-3 !p-4 ring-2 ring-[color:var(--color-brand)]' : 'flex items-center justify-between gap-3 !p-4'}>
                  <div className="flex min-w-0 items-center gap-3">
                    <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-[color:var(--bg-section)] text-[color:var(--text-tertiary)]">
                      <MapPinIcon width={18} height={18} />
                    </span>
                    <div className="min-w-0">
                      <div className="truncate text-[14.5px] font-bold">{s.storeName}</div>
                      {i === 0 && <div className="text-[12px] font-semibold text-[color:var(--color-brand)]">лучшая цена</div>}
                      {s.distanceKm != null && <div className="text-[12px] text-[color:var(--text-tertiary)]">{s.distanceKm.toFixed(1)} км</div>}
                    </div>
                  </div>
                  <div className="flex shrink-0 flex-col items-end gap-1">
                    <div className="text-[19px] font-extrabold">
                      {s.price.toFixed(2)} <span className="text-[13px] font-semibold text-[color:var(--text-tertiary)]">{s.currency}</span>
                    </div>
                    <div className="flex items-center gap-2.5">
                      <DisputePriceButton priceEntryId={s.priceEntryId} />
                      <OutOfStockButton productId={result.productId} storeId={s.storeId} />
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          )}

          {sortedStores.length > 0 && (
            <ReportPriceForm
              productId={result.productId}
              storeOptions={sortedStores.map((s) => ({ storeId: s.storeId, storeName: s.storeName }))}
              onSubmitted={runScan}
            />
          )}

          <ReviewsSection
            productId={result.productId}
            storeOptions={sortedStores.map((s) => ({ storeId: s.storeId, storeName: s.storeName }))}
          />
        </div>
      )}
    </div>
  )
}
