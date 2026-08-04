import { useCallback, useEffect, useState, type ReactElement } from 'react'
import { Panel } from '../cabinet/components/primitives'
import { Select } from '../components/Select'
import { Badge } from '../components/Badge'
import { Loading } from '../components/Loading'
import { TagIcon, PercentIcon, ClockIcon, AlertIcon, CheckIcon, PlusIcon, TrashIcon } from '../components/icons'
import { StarIcon } from '../../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { ApiError } from '../../lib/api'
import {
  createPromotion,
  getActivePromotions,
  type Promotion,
  type PromotionDiscountType,
} from '../../lib/api/promotions'
import { createProductBundle, getProductBundles, type ProductBundle, type BundleItem } from '../../lib/api/bundles'
import {
  publishExpiringOffer,
  getExpiringOffersForStore,
  type ExpiringOffer,
} from '../../lib/api/expiringOffers'
import { getReviews, replyToReview, type Review } from '../../lib/api/reviews'

type Tab = 'promotions' | 'bundles' | 'offers' | 'replies'

const TABS: { key: Tab; label: string; icon: (props: { width: number; height: number }) => ReactElement }[] = [
  { key: 'promotions', label: 'Акции', icon: (p) => <PercentIcon {...p} /> },
  { key: 'bundles', label: 'Наборы товаров', icon: (p) => <TagIcon {...p} /> },
  { key: 'offers', label: 'Скоро истекает', icon: (p) => <ClockIcon {...p} /> },
  { key: 'replies', label: 'Ответы на отзывы', icon: (p) => <StarIcon {...p} /> },
]

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function inputCls() {
  return 'w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none transition-colors placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]'
}

function labelCls() {
  return 'text-[12px] font-medium text-[color:var(--admin-text-secondary)]'
}

function ReplyIcon(props: { width?: number; height?: number; className?: string }) {
  return (
    <svg
      width={props.width ?? 16}
      height={props.height ?? 16}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={props.className}
    >
      <path d="M9 17 4 12l5-5" />
      <path d="M4 12h10a6 6 0 0 1 6 6v1" />
    </svg>
  )
}

function outcomeMessage(outcome: string): string | null {
  switch (outcome) {
    case 'Created':
    case 'Published':
      return null
    case 'StoreNotFound':
      return 'Магазин не найден'
    case 'ProductNotFound':
      return 'Товар не найден'
    case 'Forbidden':
      return 'Нет доступа к этому магазину'
    default:
      return `Не удалось выполнить операцию (${outcome})`
  }
}

export function MarketingPage() {
  const { storeId } = useAuth()
  const [tab, setTab] = useState<Tab>('promotions')

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="flex flex-wrap gap-x-6 border-b border-[color:var(--admin-border)]">
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`-mb-px flex items-center gap-2 border-b-2 pb-3 text-[13px] font-semibold transition-colors ${
              tab === t.key
                ? 'border-[color:var(--admin-text)] text-[color:var(--admin-text)]'
                : 'border-transparent text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text-secondary)]'
            }`}
          >
            {t.icon({ width: 15, height: 15 })}
            {t.label}
          </button>
        ))}
      </div>

      {!storeId ? (
        <Panel className="p-8 text-center">
          <p className="text-[14px] text-[color:var(--admin-text-secondary)]">Магазин не выбран</p>
        </Panel>
      ) : (
        <>
          {tab === 'promotions' && <PromotionsSection storeId={storeId} />}
          {tab === 'bundles' && <BundlesSection storeId={storeId} />}
          {tab === 'offers' && <OffersSection storeId={storeId} />}
          {tab === 'replies' && <RepliesSection />}
        </>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Акции (Promotions)
// ---------------------------------------------------------------------------

const DISCOUNT_TYPE_LABELS: Record<string, string> = {
  PercentageOff: 'Скидка, %',
  FixedAmountOff: 'Скидка на сумму',
  BuyOneGetOne: '1+1',
}

function discountValueLabel(p: Promotion) {
  if (p.discountType === 'PercentageOff') return `${fmt(p.discountValue)}%`
  if (p.discountType === 'BuyOneGetOne') return '1+1'
  return fmt(p.discountValue)
}

function promoStatus(p: Promotion): { label: string; variant: 'accent' | 'neutral' | 'success' } {
  const now = Date.now()
  const starts = new Date(p.startsAt).getTime()
  const ends = new Date(p.endsAt).getTime()
  if (now < starts) return { label: 'Скоро начнётся', variant: 'accent' }
  if (now > ends) return { label: 'Истекла', variant: 'neutral' }
  return { label: 'Активна', variant: 'success' }
}

function PromotionsSection({ storeId }: { storeId: number }) {
  const [promotions, setPromotions] = useState<Promotion[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [targetMode, setTargetMode] = useState<'product' | 'category'>('product')
  const [productId, setProductId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [discountType, setDiscountType] = useState<PromotionDiscountType>('PercentageOff')
  const [discountValue, setDiscountValue] = useState('')
  const [startsAt, setStartsAt] = useState('')
  const [endsAt, setEndsAt] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')
  const [createSuccess, setCreateSuccess] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getActivePromotions(storeId)
      setPromotions(res.promotions ?? [])
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить акции')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (creating) return
    const value = Number(discountValue)
    if (!value || value <= 0) {
      setCreateError('Укажите значение скидки')
      return
    }
    if (!startsAt || !endsAt) {
      setCreateError('Укажите период действия акции')
      return
    }
    const targetId = targetMode === 'product' ? Number(productId) : Number(categoryId)
    if (!targetId) {
      setCreateError(targetMode === 'product' ? 'Укажите ID товара' : 'Укажите ID категории')
      return
    }
    setCreating(true)
    setCreateError('')
    setCreateSuccess(false)
    try {
      const res = await createPromotion(storeId, {
        productId: targetMode === 'product' ? targetId : undefined,
        categoryId: targetMode === 'category' ? targetId : undefined,
        discountType,
        discountValue: value,
        startsAt: new Date(startsAt).toISOString(),
        endsAt: new Date(endsAt).toISOString(),
      })
      const msg = outcomeMessage(res.outcome)
      if (msg) {
        setCreateError(msg)
        return
      }
      setCreateSuccess(true)
      setProductId('')
      setCategoryId('')
      setDiscountValue('')
      setStartsAt('')
      setEndsAt('')
      await load()
      setTimeout(() => setCreateSuccess(false), 2000)
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : 'Не удалось создать акцию')
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <PercentIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Новая акция</span>
        </div>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setTargetMode('product')}
              className={`flex-1 rounded-[8px] px-4 py-2.5 text-[13px] font-[500] transition-colors ${
                targetMode === 'product'
                  ? 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]'
                  : 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-secondary)]'
              }`}
            >
              По товару
            </button>
            <button
              type="button"
              onClick={() => setTargetMode('category')}
              className={`flex-1 rounded-[8px] px-4 py-2.5 text-[13px] font-[500] transition-colors ${
                targetMode === 'category'
                  ? 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]'
                  : 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-secondary)]'
              }`}
            >
              По категории
            </button>
          </div>

          {targetMode === 'product' ? (
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>ID товара</span>
              <input
                type="number"
                min={1}
                value={productId}
                onChange={(e) => setProductId(e.target.value)}
                placeholder="Например, 1024"
                className={inputCls()}
              />
            </label>
          ) : (
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>ID категории</span>
              <input
                type="number"
                min={1}
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                placeholder="Например, 5"
                className={inputCls()}
              />
            </label>
          )}

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Тип скидки</span>
              <Select
                value={discountType}
                onChange={(v) => setDiscountType(v as PromotionDiscountType)}
                options={[
                  { value: 'PercentageOff', label: 'Процент от цены' },
                  { value: 'FixedAmountOff', label: 'Фиксированная сумма' },
                  { value: 'BuyOneGetOne', label: '1+1' },
                ]}
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Значение скидки</span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={discountValue}
                onChange={(e) => setDiscountValue(e.target.value)}
                placeholder={discountType === 'PercentageOff' ? '%' : 'Сумма'}
                className={inputCls()}
              />
            </label>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Начало</span>
              <input
                type="datetime-local"
                value={startsAt}
                onChange={(e) => setStartsAt(e.target.value)}
                className={inputCls()}
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Окончание</span>
              <input
                type="datetime-local"
                value={endsAt}
                onChange={(e) => setEndsAt(e.target.value)}
                className={inputCls()}
              />
            </label>
          </div>

          {createError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{createError}</div>}

          <button
            type="submit"
            disabled={creating}
            className="flex items-center justify-center gap-2 rounded-[8px] bg-[color:var(--admin-accent)] px-5 py-[10px] text-[14px] font-[500] text-[color:var(--admin-accent-fg)] transition-all duration-150 ease-out hover:scale-[1.01] hover:shadow-[0_4px_16px_rgba(0,0,0,0.18)] active:scale-[0.99] disabled:opacity-40"
          >
            {createSuccess ? (
              <>
                <CheckIcon width={16} height={16} /> Акция создана
              </>
            ) : creating ? (
              'Создаём…'
            ) : (
              <>
                <PlusIcon width={16} height={16} /> Создать акцию
              </>
            )}
          </button>
        </form>
      </Panel>

      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <TagIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Активные акции</span>
        </div>
        {loading ? (
          <Loading label="Загружаем акции…" />
        ) : error ? (
          <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-secondary)]">{error}</div>
        ) : (
          <div className="flex flex-col gap-3">
            {(promotions ?? []).map((p) => {
              const status = promoStatus(p)
              return (
                <div
                  key={p.promotionId}
                  className="flex flex-col gap-2 rounded-[16px] bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div>
                    <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">
                      {DISCOUNT_TYPE_LABELS[p.discountType] ?? p.discountType}: {discountValueLabel(p)}
                    </div>
                    <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                      {p.productId ? `Товар #${p.productId}` : p.categoryId ? `Категория #${p.categoryId}` : '—'} ·{' '}
                      {fmtDate(p.startsAt)} — {fmtDate(p.endsAt)}
                    </div>
                  </div>
                  <Badge variant={status.variant} className="shrink-0 self-start sm:self-center">
                    {status.label}
                  </Badge>
                </div>
              )
            })}
            {(promotions ?? []).length === 0 && (
              <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
                Активных акций пока нет
              </div>
            )}
          </div>
        )}
      </Panel>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Наборы товаров (Product bundles)
// ---------------------------------------------------------------------------

interface ItemRow {
  productId: string
  quantity: string
}

function BundlesSection({ storeId }: { storeId: number }) {
  const [bundles, setBundles] = useState<ProductBundle[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [name, setName] = useState('')
  const [bundlePrice, setBundlePrice] = useState('')
  const [currency, setCurrency] = useState('TJS')
  const [items, setItems] = useState<ItemRow[]>([{ productId: '', quantity: '1' }])
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')
  const [createSuccess, setCreateSuccess] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getProductBundles(storeId)
      setBundles(res.bundles ?? [])
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить наборы товаров')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  function updateItem(index: number, patch: Partial<ItemRow>) {
    setItems((rows) => rows.map((r, i) => (i === index ? { ...r, ...patch } : r)))
  }

  function addItemRow() {
    setItems((rows) => [...rows, { productId: '', quantity: '1' }])
  }

  function removeItemRow(index: number) {
    setItems((rows) => (rows.length <= 1 ? rows : rows.filter((_, i) => i !== index)))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (creating) return
    const trimmedName = name.trim()
    const price = Number(bundlePrice)
    if (!trimmedName) {
      setCreateError('Укажите название набора')
      return
    }
    if (!price || price <= 0) {
      setCreateError('Укажите цену набора')
      return
    }
    const parsedItems: BundleItem[] = items
      .filter((r) => Number(r.productId) > 0)
      .map((r) => ({ productId: Number(r.productId), quantity: Math.max(1, Number(r.quantity) || 1) }))
    if (parsedItems.length === 0) {
      setCreateError('Добавьте хотя бы один товар в набор')
      return
    }
    setCreating(true)
    setCreateError('')
    setCreateSuccess(false)
    try {
      const res = await createProductBundle(storeId, trimmedName, price, currency.trim() || 'TJS', parsedItems)
      const msg = outcomeMessage(res.outcome)
      if (msg) {
        setCreateError(msg)
        return
      }
      setCreateSuccess(true)
      setName('')
      setBundlePrice('')
      setItems([{ productId: '', quantity: '1' }])
      await load()
      setTimeout(() => setCreateSuccess(false), 2000)
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : 'Не удалось создать набор')
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <TagIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Новый набор товаров</span>
        </div>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5">
            <span className={labelCls()}>Название набора</span>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Например, «Завтрак набор»"
              className={inputCls()}
            />
          </label>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Цена набора</span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={bundlePrice}
                onChange={(e) => setBundlePrice(e.target.value)}
                placeholder="0.00"
                className={inputCls()}
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Валюта</span>
              <input
                value={currency}
                onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                maxLength={3}
                placeholder="TJS"
                className={inputCls()}
              />
            </label>
          </div>

          <div className="flex flex-col gap-2">
            <span className={labelCls()}>Состав набора</span>
            {items.map((row, i) => (
              <div key={i} className="flex items-center gap-2">
                <input
                  type="number"
                  min={1}
                  value={row.productId}
                  onChange={(e) => updateItem(i, { productId: e.target.value })}
                  placeholder="ID товара"
                  className={inputCls()}
                />
                <input
                  type="number"
                  min={1}
                  value={row.quantity}
                  onChange={(e) => updateItem(i, { quantity: e.target.value })}
                  placeholder="Кол-во"
                  className={`${inputCls()} max-w-[110px]`}
                />
                <button
                  type="button"
                  onClick={() => removeItemRow(i)}
                  disabled={items.length <= 1}
                  className="grid h-10 w-10 shrink-0 place-items-center rounded-[8px] bg-[color:var(--admin-hover)] text-[color:var(--admin-danger)] transition-colors hover:bg-[color:var(--admin-danger-dim)] disabled:opacity-30"
                >
                  <TrashIcon width={15} height={15} />
                </button>
              </div>
            ))}
            <button
              type="button"
              onClick={addItemRow}
              className="mt-1 flex items-center justify-center gap-1.5 self-start rounded-lg bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[11px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80"
            >
              <PlusIcon width={13} height={13} />
              Добавить товар
            </button>
          </div>

          {createError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{createError}</div>}

          <button
            type="submit"
            disabled={creating}
            className="flex items-center justify-center gap-2 rounded-[8px] bg-[color:var(--admin-accent)] px-5 py-[10px] text-[14px] font-[500] text-[color:var(--admin-accent-fg)] transition-all duration-150 ease-out hover:scale-[1.01] hover:shadow-[0_4px_16px_rgba(0,0,0,0.18)] active:scale-[0.99] disabled:opacity-40"
          >
            {createSuccess ? (
              <>
                <CheckIcon width={16} height={16} /> Набор создан
              </>
            ) : creating ? (
              'Создаём…'
            ) : (
              <>
                <PlusIcon width={16} height={16} /> Создать набор
              </>
            )}
          </button>
        </form>
      </Panel>

      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <TagIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Наборы товаров магазина</span>
        </div>
        {loading ? (
          <Loading label="Загружаем наборы…" />
        ) : error ? (
          <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-secondary)]">{error}</div>
        ) : (
          <div className="flex flex-col gap-3">
            {(bundles ?? []).map((b) => (
              <div key={b.productBundleId} className="rounded-[16px] bg-[color:var(--admin-hover)] p-4">
                <div className="flex items-center justify-between">
                  <span className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">{b.name}</span>
                  <span className="text-[14px] font-bold text-[color:var(--admin-accent)]">
                    {fmt(b.bundlePrice)} {b.currency}
                  </span>
                </div>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {b.items.map((it, idx) => (
                    <span
                      key={idx}
                      className="rounded-[4px] bg-[color:var(--admin-border)] px-2 py-0.5 text-[11px] font-[400] text-[color:var(--admin-text-secondary)]"
                    >
                      Товар #{it.productId} × {it.quantity}
                    </span>
                  ))}
                </div>
              </div>
            ))}
            {(bundles ?? []).length === 0 && (
              <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
                Наборов товаров пока нет
              </div>
            )}
          </div>
        )}
      </Panel>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Скоро истекает (Expiring offers)
// ---------------------------------------------------------------------------

function timeRemaining(expiresAt: string): { label: string; expired: boolean } {
  const diffMs = new Date(expiresAt).getTime() - Date.now()
  if (diffMs <= 0) return { label: 'Истёк', expired: true }
  const totalMinutes = Math.floor(diffMs / 60000)
  const days = Math.floor(totalMinutes / (60 * 24))
  const hours = Math.floor((totalMinutes % (60 * 24)) / 60)
  const minutes = totalMinutes % 60
  if (days > 0) return { label: `Осталось ${days} дн. ${hours} ч.`, expired: false }
  if (hours > 0) return { label: `Осталось ${hours} ч. ${minutes} мин.`, expired: false }
  return { label: `Осталось ${minutes} мин.`, expired: false }
}

function OffersSection({ storeId }: { storeId: number }) {
  const [offers, setOffers] = useState<ExpiringOffer[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [productId, setProductId] = useState('')
  const [originalPrice, setOriginalPrice] = useState('')
  const [discountedPrice, setDiscountedPrice] = useState('')
  const [currency, setCurrency] = useState('TJS')
  const [expiresAt, setExpiresAt] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')
  const [createSuccess, setCreateSuccess] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getExpiringOffersForStore(storeId)
      setOffers(res.offers ?? [])
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить предложения')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (creating) return
    const pid = Number(productId)
    const original = Number(originalPrice)
    const discounted = Number(discountedPrice)
    if (!pid) {
      setCreateError('Укажите ID товара')
      return
    }
    if (!original || original <= 0 || !discounted || discounted <= 0) {
      setCreateError('Укажите корректные цены')
      return
    }
    if (discounted >= original) {
      setCreateError('Цена со скидкой должна быть меньше исходной')
      return
    }
    if (!expiresAt) {
      setCreateError('Укажите срок действия предложения')
      return
    }
    setCreating(true)
    setCreateError('')
    setCreateSuccess(false)
    try {
      const res = await publishExpiringOffer(
        storeId,
        pid,
        original,
        discounted,
        currency.trim() || 'TJS',
        new Date(expiresAt).toISOString(),
      )
      const msg = outcomeMessage(res.outcome)
      if (msg) {
        setCreateError(msg)
        return
      }
      setCreateSuccess(true)
      setProductId('')
      setOriginalPrice('')
      setDiscountedPrice('')
      setExpiresAt('')
      await load()
      setTimeout(() => setCreateSuccess(false), 2000)
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : 'Не удалось опубликовать предложение')
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <ClockIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Опубликовать «скоро истекает»</span>
        </div>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5">
            <span className={labelCls()}>ID товара</span>
            <input
              type="number"
              min={1}
              value={productId}
              onChange={(e) => setProductId(e.target.value)}
              placeholder="Например, 1024"
              className={inputCls()}
            />
          </label>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Исходная цена</span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={originalPrice}
                onChange={(e) => setOriginalPrice(e.target.value)}
                placeholder="0.00"
                className={inputCls()}
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Цена со скидкой</span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={discountedPrice}
                onChange={(e) => setDiscountedPrice(e.target.value)}
                placeholder="0.00"
                className={inputCls()}
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelCls()}>Валюта</span>
              <input
                value={currency}
                onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                maxLength={3}
                placeholder="TJS"
                className={inputCls()}
              />
            </label>
          </div>

          <label className="flex flex-col gap-1.5">
            <span className={labelCls()}>Истекает</span>
            <input
              type="datetime-local"
              value={expiresAt}
              onChange={(e) => setExpiresAt(e.target.value)}
              className={inputCls()}
            />
          </label>

          {createError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{createError}</div>}

          <button
            type="submit"
            disabled={creating}
            className="flex items-center justify-center gap-2 rounded-[8px] bg-[color:var(--admin-accent)] px-5 py-[10px] text-[14px] font-[500] text-[color:var(--admin-accent-fg)] transition-all duration-150 ease-out hover:scale-[1.01] hover:shadow-[0_4px_16px_rgba(0,0,0,0.18)] active:scale-[0.99] disabled:opacity-40"
          >
            {createSuccess ? (
              <>
                <CheckIcon width={16} height={16} /> Опубликовано
              </>
            ) : creating ? (
              'Публикуем…'
            ) : (
              <>
                <PlusIcon width={16} height={16} /> Опубликовать
              </>
            )}
          </button>
        </form>
      </Panel>

      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <AlertIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Предложения этого магазина</span>
        </div>
        {loading ? (
          <Loading label="Загружаем предложения…" />
        ) : error ? (
          <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-secondary)]">{error}</div>
        ) : (
          <div className="flex flex-col gap-3">
            {(offers ?? []).map((o) => {
              const remaining = timeRemaining(o.expiresAt)
              return (
                <div
                  key={o.offerId}
                  className="flex flex-col gap-2 rounded-[16px] bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div>
                    <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">
                      {o.productName || `Товар #${o.productId}`}
                    </div>
                    <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                      <span className="line-through">{fmt(o.originalPrice)}</span>{' '}
                      <span className="font-semibold text-[color:var(--admin-text-secondary)]">
                        {fmt(o.discountedPrice)} {o.currency}
                      </span>
                    </div>
                  </div>
                  <Badge variant={remaining.expired ? 'neutral' : 'warning'} className="shrink-0 self-start sm:self-center">
                    {remaining.label}
                  </Badge>
                </div>
              )
            })}
            {(offers ?? []).length === 0 && (
              <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
                Предложений пока нет
              </div>
            )}
          </div>
        )}
      </Panel>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Ответы на отзывы (Review replies)
// ---------------------------------------------------------------------------

function RepliesSection() {
  const [productIdInput, setProductIdInput] = useState('')
  const [reviews, setReviews] = useState<Review[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [searched, setSearched] = useState(false)

  const [openReplyId, setOpenReplyId] = useState<number | null>(null)
  const [replyText, setReplyText] = useState('')
  const [replyBusy, setReplyBusy] = useState(false)
  const [replyError, setReplyError] = useState('')
  const [repliedIds, setRepliedIds] = useState<Set<number>>(new Set())

  async function handleLookup(e: React.FormEvent) {
    e.preventDefault()
    const pid = Number(productIdInput)
    if (!pid || loading) return
    setLoading(true)
    setError('')
    setSearched(true)
    setOpenReplyId(null)
    try {
      const res = await getReviews(pid)
      setReviews(res.reviews ?? [])
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить отзывы')
      setReviews(null)
    } finally {
      setLoading(false)
    }
  }

  function openReply(reviewId: number) {
    setOpenReplyId(reviewId)
    setReplyText('')
    setReplyError('')
  }

  async function submitReply(reviewId: number) {
    const message = replyText.trim()
    if (!message || replyBusy) return
    setReplyBusy(true)
    setReplyError('')
    try {
      await replyToReview(reviewId, message)
      setRepliedIds((s) => new Set(s).add(reviewId))
      setOpenReplyId(null)
      setReplyText('')
    } catch (err) {
      setReplyError(
        err instanceof ApiError
          ? err.status === 404
            ? 'Отзыв не найден'
            : err.status === 403
              ? 'Нет доступа, чтобы отвечать на этот отзыв'
              : err.message
          : 'Не удалось отправить ответ',
      )
    } finally {
      setReplyBusy(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <StarIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Найти отзывы по товару</span>
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          В бэкенде нет эндпоинта «все отзывы моего магазина» — отзывы можно посмотреть только по ID конкретного
          товара.
        </p>
        <form onSubmit={handleLookup} className="flex flex-col gap-3 sm:flex-row">
          <input
            type="number"
            min={1}
            value={productIdInput}
            onChange={(e) => setProductIdInput(e.target.value)}
            placeholder="ID товара"
            className={`${inputCls()} sm:max-w-[220px]`}
          />
          <button
            type="submit"
            disabled={loading}
            className="shrink-0 rounded-[8px] bg-[color:var(--admin-accent)] px-5 py-[10px] text-[14px] font-[500] text-[color:var(--admin-accent-fg)] transition-all duration-150 ease-out hover:scale-[1.01] hover:shadow-[0_4px_16px_rgba(0,0,0,0.18)] disabled:opacity-40"
          >
            {loading ? 'Ищем…' : 'Показать отзывы'}
          </button>
        </form>
      </Panel>

      {searched && (
        <Panel className="p-5">
          <div className="mb-4 flex items-center gap-2">
            <span className="text-[18px] font-[500] text-[color:var(--admin-text)]">Отзывы</span>
          </div>
          {loading ? (
            <Loading label="Загружаем отзывы…" />
          ) : error ? (
            <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-secondary)]">{error}</div>
          ) : (
            <div className="flex flex-col gap-3">
              {(reviews ?? []).map((r) => (
                <div key={r.reviewId} className="rounded-[16px] bg-[color:var(--admin-hover)] p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-1">
                      {Array.from({ length: 5 }).map((_, i) => (
                        <StarIcon
                          key={i}
                          width={13}
                          height={13}
                          className={i < r.rating ? 'text-[color:var(--admin-warning)]' : 'text-[color:var(--admin-border)]'}
                        />
                      ))}
                    </div>
                    <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">{fmtDate(r.createdAt)}</span>
                  </div>
                  <p className="mt-2 text-[13px] text-[color:var(--admin-text)]">{r.comment}</p>

                  {repliedIds.has(r.reviewId) ? (
                    <div className="mt-2.5 flex items-center gap-1.5 text-[11.5px] font-semibold text-[color:var(--admin-success)]">
                      <CheckIcon width={13} height={13} />
                      Ответ отправлен
                    </div>
                  ) : openReplyId === r.reviewId ? (
                    <div className="mt-3 flex flex-col gap-2">
                      <textarea
                        autoFocus
                        value={replyText}
                        onChange={(e) => setReplyText(e.target.value)}
                        placeholder="Ваш ответ на отзыв"
                        rows={3}
                        className="w-full resize-none rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none transition-colors placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]"
                      />
                      {replyError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{replyError}</div>}
                      <div className="flex gap-2">
                        <button
                          onClick={() => submitReply(r.reviewId)}
                          disabled={replyBusy || !replyText.trim()}
                          className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent)] px-3.5 py-2 text-[12px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90 disabled:opacity-50"
                        >
                          {replyBusy ? 'Отправляем…' : 'Отправить'}
                        </button>
                        <button
                          onClick={() => setOpenReplyId(null)}
                          className="rounded-lg bg-[color:var(--admin-border)] px-3.5 py-2 text-[12px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]"
                        >
                          Отмена
                        </button>
                      </div>
                    </div>
                  ) : (
                    <button
                      onClick={() => openReply(r.reviewId)}
                      className="mt-2.5 inline-flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[11px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80"
                    >
                      <ReplyIcon width={13} height={13} />
                      Ответить
                    </button>
                  )}
                </div>
              ))}
              {(reviews ?? []).length === 0 && (
                <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
                  У этого товара пока нет отзывов
                </div>
              )}
            </div>
          )}
        </Panel>
      )}
    </div>
  )
}
