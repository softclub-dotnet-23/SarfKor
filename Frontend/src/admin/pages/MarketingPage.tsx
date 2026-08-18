import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Panel } from '../cabinet/components/primitives'
import { Select } from '../components/Select'
import { SectionSelect } from '../components/SectionSelect'
import { AddButton } from '../components/Button'
import { Badge } from '../components/Badge'
import { Loading } from '../components/Loading'
import { FormModal, FormField } from '../components/FormModal'
import { ProductPicker } from '../components/ProductPicker'
import { CategoryPicker } from '../components/CategoryPicker'
import { TagIcon, PercentIcon, ClockIcon, AlertIcon, CheckIcon, PlusIcon, TrashIcon } from '../components/icons'
import { StarIcon } from '../../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { productsApi, catalogApi, ApiError, type Category, type ProductSearchItem } from '../../lib/api'
import { errorMessage } from '../../lib/errorKind'
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

const TAB_OPTIONS = [
  { value: 'promotions' as const, label: 'Акции', icon: <PercentIcon width={15} height={15} /> },
  { value: 'bundles' as const, label: 'Наборы товаров', icon: <TagIcon width={15} height={15} /> },
  { value: 'offers' as const, label: 'Скоро истекает', icon: <ClockIcon width={15} height={15} /> },
  { value: 'replies' as const, label: 'Ответы на отзывы', icon: <StarIcon width={15} height={15} /> },
]

const MARKETING_ADD_LABEL: Partial<Record<Tab, string>> = {
  promotions: 'Создать акцию',
  bundles: 'Новый набор',
  offers: 'Опубликовать',
}

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function inputCls() {
  return 'w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none transition-colors placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]'
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
  const [params, setParams] = useSearchParams()
  const tabParam = params.get('tab')
  const tab: Tab = tabParam === 'bundles' || tabParam === 'offers' || tabParam === 'replies' ? tabParam : 'promotions'
  const [createOpen, setCreateOpen] = useState(false)
  const addLabel = MARKETING_ADD_LABEL[tab]

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <SectionSelect value={tab} onChange={(v) => setParams(v === 'promotions' ? {} : { tab: v })} options={TAB_OPTIONS} ariaLabel="Раздел маркетинга" />
        {addLabel && <AddButton onClick={() => setCreateOpen(true)}>{addLabel}</AddButton>}
      </div>

      {!storeId ? (
        <Panel className="p-8 text-center">
          <p className="text-[14px] text-[color:var(--admin-text-secondary)]">Магазин не выбран</p>
        </Panel>
      ) : (
        <>
          {tab === 'promotions' && <PromotionsSection storeId={storeId} createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />}
          {tab === 'bundles' && <BundlesSection storeId={storeId} createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />}
          {tab === 'offers' && <OffersSection storeId={storeId} createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />}
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

function PromotionFormFields({
  targetMode, setTargetMode, product, setProduct, category, setCategory,
  discountType, setDiscountType, discountValue, setDiscountValue, startsAt, setStartsAt, endsAt, setEndsAt,
  targetError, valueError, periodError,
}: {
  targetMode: 'product' | 'category'; setTargetMode: (v: 'product' | 'category') => void
  product: ProductSearchItem | null; setProduct: (v: ProductSearchItem | null) => void
  category: Category | null; setCategory: (v: Category | null) => void
  discountType: PromotionDiscountType; setDiscountType: (v: PromotionDiscountType) => void
  discountValue: string; setDiscountValue: (v: string) => void
  startsAt: string; setStartsAt: (v: string) => void
  endsAt: string; setEndsAt: (v: string) => void
  targetError?: string; valueError?: string; periodError?: string
}) {
  return (
    <>
      <div className="mb-4 flex gap-2">
        <button
          type="button"
          onClick={() => setTargetMode('product')}
          className={`flex-1 rounded-xl px-4 py-2.5 text-[13px] font-semibold transition-colors ${
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
          className={`flex-1 rounded-xl px-4 py-2.5 text-[13px] font-semibold transition-colors ${
            targetMode === 'category'
              ? 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]'
              : 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-secondary)]'
          }`}
        >
          По категории
        </button>
      </div>

      {targetMode === 'product' ? (
        <FormField label="Товар" required error={targetError} scheme="admin">
          <ProductPicker value={product} onChange={setProduct} scheme="admin" scanEnabled />
        </FormField>
      ) : (
        <FormField label="Категория" required error={targetError} scheme="admin">
          <CategoryPicker value={category} onChange={setCategory} scheme="admin" placeholder="Выберите категорию" />
        </FormField>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField label="Тип скидки" scheme="admin">
          <Select
            value={discountType}
            onChange={(v) => setDiscountType(v as PromotionDiscountType)}
            options={[
              { value: 'PercentageOff', label: 'Процент от цены' },
              { value: 'FixedAmountOff', label: 'Фиксированная сумма' },
              { value: 'BuyOneGetOne', label: '1+1' },
            ]}
          />
        </FormField>
        <FormField label="Значение скидки" required error={valueError} scheme="admin">
          <input
            type="number"
            min={0}
            step="0.01"
            value={discountValue}
            onChange={(e) => setDiscountValue(e.target.value)}
            placeholder={discountType === 'PercentageOff' ? '%' : 'Сумма'}
            className={inputCls()}
          />
        </FormField>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField label="Начало" required error={periodError} scheme="admin">
          <input type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} className={inputCls()} />
        </FormField>
        <FormField label="Окончание" scheme="admin">
          <input type="datetime-local" value={endsAt} onChange={(e) => setEndsAt(e.target.value)} className={inputCls()} />
        </FormField>
      </div>
    </>
  )
}

function CreatePromotionModal({ open, onClose, storeId, onCreated }: { open: boolean; onClose: () => void; storeId: number; onCreated: () => Promise<void> }) {
  const [targetMode, setTargetMode] = useState<'product' | 'category'>('product')
  const [product, setProduct] = useState<ProductSearchItem | null>(null)
  const [category, setCategory] = useState<Category | null>(null)
  const [discountType, setDiscountType] = useState<PromotionDiscountType>('PercentageOff')
  const [discountValue, setDiscountValue] = useState('')
  const [startsAt, setStartsAt] = useState('')
  const [endsAt, setEndsAt] = useState('')
  const [targetError, setTargetError] = useState('')
  const [valueError, setValueError] = useState('')
  const [periodError, setPeriodError] = useState('')

  useEffect(() => {
    if (!open) return
    setTargetMode('product')
    setProduct(null)
    setCategory(null)
    setDiscountType('PercentageOff')
    setDiscountValue('')
    setStartsAt('')
    setEndsAt('')
    setTargetError('')
    setValueError('')
    setPeriodError('')
  }, [open])

  async function submit() {
    setTargetError('')
    setValueError('')
    setPeriodError('')
    const value = Number(discountValue)
    let hasError = false
    if (!value || value <= 0) {
      setValueError('Укажите значение скидки')
      hasError = true
    }
    if (!startsAt || !endsAt) {
      setPeriodError('Укажите период действия акции')
      hasError = true
    }
    const targetId = targetMode === 'product' ? product?.productId : category?.categoryId
    if (!targetId) {
      setTargetError(targetMode === 'product' ? 'Выберите товар' : 'Выберите категорию')
      hasError = true
    }
    if (hasError) throw new Error('Проверьте поля формы')

    const res = await createPromotion(storeId, {
      productId: targetMode === 'product' ? targetId : undefined,
      categoryId: targetMode === 'category' ? targetId : undefined,
      discountType,
      discountValue: value,
      startsAt: new Date(startsAt).toISOString(),
      endsAt: new Date(endsAt).toISOString(),
    })
    const msg = outcomeMessage(res.outcome)
    if (msg) throw new Error(msg)
    await onCreated()
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title="Новая акция"
      isDirty={!!(product || category || discountValue || startsAt || endsAt)}
      onSubmit={submit}
      submitLabel="Создать акцию"
      submitBusyLabel="Создаём…"
      scheme="admin"
    >
      <PromotionFormFields
        targetMode={targetMode} setTargetMode={setTargetMode}
        product={product} setProduct={setProduct}
        category={category} setCategory={setCategory}
        discountType={discountType} setDiscountType={setDiscountType}
        discountValue={discountValue} setDiscountValue={setDiscountValue}
        startsAt={startsAt} setStartsAt={setStartsAt}
        endsAt={endsAt} setEndsAt={setEndsAt}
        targetError={targetError} valueError={valueError} periodError={periodError}
      />
    </FormModal>
  )
}

function PromotionsSection({ storeId, createOpen, onCloseCreate }: { storeId: number; createOpen: boolean; onCloseCreate: () => void }) {
  const [promotions, setPromotions] = useState<Promotion[] | null>(null)
  const [productNames, setProductNames] = useState<Record<number, string>>({})
  const [categoryNames, setCategoryNames] = useState<Record<number, string>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getActivePromotions(storeId)
      const list = res.promotions ?? []
      setPromotions(list)
      // Promotion only carries bare productId/categoryId -- batch-resolve names once so a
      // promotion row never renders as "Товар #7"/"Категория #3".
      const productIds = [...new Set(list.flatMap((p) => (p.productId ? [p.productId] : [])))]
      const categoryIds = [...new Set(list.flatMap((p) => (p.categoryId ? [p.categoryId] : [])))]
      const [productResults, categoriesRes] = await Promise.all([
        Promise.allSettled(productIds.map((id) => productsApi.getProductById(id))),
        categoryIds.length > 0 ? catalogApi.getCategories() : Promise.resolve(null),
      ])
      const pNames: Record<number, string> = {}
      productResults.forEach((r, i) => { if (r.status === 'fulfilled') pNames[productIds[i]] = r.value.productName })
      setProductNames(pNames)
      if (categoriesRes) {
        const byId = new Map(categoriesRes.categories.map((c) => [c.categoryId, c.name]))
        const cNames: Record<number, string> = {}
        for (const id of categoryIds) { const name = byId.get(id); if (name) cNames[id] = name }
        setCategoryNames(cNames)
      }
    } catch (err) {
      console.error('Failed to load promotions:', err)
      setError(errorMessage(err, 'Не удалось загрузить акции'))
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div className="flex flex-col gap-6">
      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <TagIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Активные акции</span>
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
                      {p.productId ? (productNames[p.productId] ?? 'Товар') : p.categoryId ? (categoryNames[p.categoryId] ?? 'Категория') : 'Все товары'} ·{' '}
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

      <CreatePromotionModal open={createOpen} onClose={onCloseCreate} storeId={storeId} onCreated={async () => { await load(); onCloseCreate() }} />
    </div>
  )
}

// ---------------------------------------------------------------------------
// Наборы товаров (Product bundles)
// ---------------------------------------------------------------------------

interface ItemRow {
  product: ProductSearchItem | null
  quantity: string
}

function BundleFormFields({
  name, setName, bundlePrice, setBundlePrice, currency, setCurrency, items, updateItem, addItemRow, removeItemRow,
  nameError, priceError, itemsError,
}: {
  name: string; setName: (v: string) => void
  bundlePrice: string; setBundlePrice: (v: string) => void
  currency: string; setCurrency: (v: string) => void
  items: ItemRow[]
  updateItem: (index: number, patch: Partial<ItemRow>) => void
  addItemRow: () => void
  removeItemRow: (index: number) => void
  nameError?: string; priceError?: string; itemsError?: string
}) {
  return (
    <>
      <FormField label="Название набора" required error={nameError} scheme="admin">
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Например, «Завтрак набор»" className={inputCls()} />
      </FormField>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField label="Цена набора" required error={priceError} scheme="admin">
          <input type="number" min={0} step="0.01" value={bundlePrice} onChange={(e) => setBundlePrice(e.target.value)} placeholder="0.00" className={inputCls()} />
        </FormField>
        <FormField label="Валюта" scheme="admin">
          <input value={currency} onChange={(e) => setCurrency(e.target.value.toUpperCase())} maxLength={3} placeholder="TJS" className={inputCls()} />
        </FormField>
      </div>

      <FormField label="Состав набора" error={itemsError} scheme="admin">
        <div className="flex flex-col gap-2">
          {items.map((row, i) => (
            <div key={i} className="flex items-center gap-2">
              <ProductPicker className="flex-1" value={row.product} onChange={(p) => updateItem(i, { product: p })} scheme="admin" />
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
                className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-[color:var(--admin-hover)] text-[color:var(--admin-danger)] disabled:opacity-30"
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
      </FormField>
    </>
  )
}

function CreateBundleModal({ open, onClose, storeId, onCreated }: { open: boolean; onClose: () => void; storeId: number; onCreated: () => Promise<void> }) {
  const [name, setName] = useState('')
  const [bundlePrice, setBundlePrice] = useState('')
  const [currency, setCurrency] = useState('TJS')
  const [items, setItems] = useState<ItemRow[]>([{ product: null, quantity: '1' }])
  const [nameError, setNameError] = useState('')
  const [priceError, setPriceError] = useState('')
  const [itemsError, setItemsError] = useState('')

  useEffect(() => {
    if (!open) return
    setName('')
    setBundlePrice('')
    setCurrency('TJS')
    setItems([{ product: null, quantity: '1' }])
    setNameError('')
    setPriceError('')
    setItemsError('')
  }, [open])

  function updateItem(index: number, patch: Partial<ItemRow>) {
    setItems((rows) => rows.map((r, i) => (i === index ? { ...r, ...patch } : r)))
  }

  function addItemRow() {
    setItems((rows) => [...rows, { product: null, quantity: '1' }])
  }

  function removeItemRow(index: number) {
    setItems((rows) => (rows.length <= 1 ? rows : rows.filter((_, i) => i !== index)))
  }

  async function submit() {
    setNameError('')
    setPriceError('')
    setItemsError('')
    const trimmedName = name.trim()
    const price = Number(bundlePrice)
    let hasError = false
    if (!trimmedName) {
      setNameError('Укажите название набора')
      hasError = true
    }
    if (!price || price <= 0) {
      setPriceError('Укажите цену набора')
      hasError = true
    }
    const parsedItems: BundleItem[] = items
      .filter((r) => r.product)
      .map((r) => ({ productId: r.product!.productId, quantity: Math.max(1, Number(r.quantity) || 1) }))
    if (parsedItems.length === 0) {
      setItemsError('Добавьте хотя бы один товар в набор')
      hasError = true
    }
    if (hasError) throw new Error('Проверьте поля формы')

    const res = await createProductBundle(storeId, trimmedName, price, currency.trim() || 'TJS', parsedItems)
    const msg = outcomeMessage(res.outcome)
    if (msg) throw new Error(msg)
    await onCreated()
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title="Новый набор товаров"
      isDirty={!!(name || bundlePrice || items.some((r) => r.product))}
      onSubmit={submit}
      submitLabel="Создать набор"
      submitBusyLabel="Создаём…"
      scheme="admin"
      size="lg"
    >
      <BundleFormFields
        name={name} setName={setName}
        bundlePrice={bundlePrice} setBundlePrice={setBundlePrice}
        currency={currency} setCurrency={setCurrency}
        items={items} updateItem={updateItem} addItemRow={addItemRow} removeItemRow={removeItemRow}
        nameError={nameError} priceError={priceError} itemsError={itemsError}
      />
    </FormModal>
  )
}

function BundlesSection({ storeId, createOpen, onCloseCreate }: { storeId: number; createOpen: boolean; onCloseCreate: () => void }) {
  const [bundles, setBundles] = useState<ProductBundle[] | null>(null)
  const [productNames, setProductNames] = useState<Record<number, string>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getProductBundles(storeId)
      const list = res.bundles ?? []
      setBundles(list)
      // BundleItem only carries a bare productId -- batch-resolve names once so bundle contents
      // never render as "Товар #7".
      const uniqueIds = [...new Set(list.flatMap((b) => b.items.map((it) => it.productId)))]
      const results = await Promise.allSettled(uniqueIds.map((id) => productsApi.getProductById(id)))
      const names: Record<number, string> = {}
      results.forEach((r, i) => { if (r.status === 'fulfilled') names[uniqueIds[i]] = r.value.productName })
      setProductNames(names)
    } catch (err) {
      console.error('Failed to load product bundles:', err)
      setError(errorMessage(err, 'Не удалось загрузить наборы товаров'))
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div className="flex flex-col gap-6">
      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <TagIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Наборы товаров магазина</span>
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
                      {productNames[it.productId] ?? 'Товар'} × {it.quantity}
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

      <CreateBundleModal open={createOpen} onClose={onCloseCreate} storeId={storeId} onCreated={async () => { await load(); onCloseCreate() }} />
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

function OfferFormFields({
  product, setProduct, originalPrice, setOriginalPrice, discountedPrice, setDiscountedPrice,
  currency, setCurrency, expiresAt, setExpiresAt, productError, priceError, expiryError,
}: {
  product: ProductSearchItem | null; setProduct: (v: ProductSearchItem | null) => void
  originalPrice: string; setOriginalPrice: (v: string) => void
  discountedPrice: string; setDiscountedPrice: (v: string) => void
  currency: string; setCurrency: (v: string) => void
  expiresAt: string; setExpiresAt: (v: string) => void
  productError?: string; priceError?: string; expiryError?: string
}) {
  return (
    <>
      <FormField label="Товар" required error={productError} scheme="admin">
        <ProductPicker value={product} onChange={setProduct} scheme="admin" scanEnabled />
      </FormField>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <FormField label="Исходная цена" required error={priceError} scheme="admin">
          <input type="number" min={0} step="0.01" value={originalPrice} onChange={(e) => setOriginalPrice(e.target.value)} placeholder="0.00" className={inputCls()} />
        </FormField>
        <FormField label="Цена со скидкой" scheme="admin">
          <input type="number" min={0} step="0.01" value={discountedPrice} onChange={(e) => setDiscountedPrice(e.target.value)} placeholder="0.00" className={inputCls()} />
        </FormField>
        <FormField label="Валюта" scheme="admin">
          <input value={currency} onChange={(e) => setCurrency(e.target.value.toUpperCase())} maxLength={3} placeholder="TJS" className={inputCls()} />
        </FormField>
      </div>

      <FormField label="Истекает" required error={expiryError} scheme="admin">
        <input type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} className={inputCls()} />
      </FormField>
    </>
  )
}

function PublishOfferModal({ open, onClose, storeId, onCreated }: { open: boolean; onClose: () => void; storeId: number; onCreated: () => Promise<void> }) {
  const [product, setProduct] = useState<ProductSearchItem | null>(null)
  const [originalPrice, setOriginalPrice] = useState('')
  const [discountedPrice, setDiscountedPrice] = useState('')
  const [currency, setCurrency] = useState('TJS')
  const [expiresAt, setExpiresAt] = useState('')
  const [productError, setProductError] = useState('')
  const [priceError, setPriceError] = useState('')
  const [expiryError, setExpiryError] = useState('')

  useEffect(() => {
    if (!open) return
    setProduct(null)
    setOriginalPrice('')
    setDiscountedPrice('')
    setCurrency('TJS')
    setExpiresAt('')
    setProductError('')
    setPriceError('')
    setExpiryError('')
  }, [open])

  async function submit() {
    setProductError('')
    setPriceError('')
    setExpiryError('')
    const pid = product?.productId
    const original = Number(originalPrice)
    const discounted = Number(discountedPrice)
    let hasError = false
    if (!pid) {
      setProductError('Выберите товар')
      hasError = true
    }
    if (!original || original <= 0 || !discounted || discounted <= 0) {
      setPriceError('Укажите корректные цены')
      hasError = true
    } else if (discounted >= original) {
      setPriceError('Цена со скидкой должна быть меньше исходной')
      hasError = true
    }
    if (!expiresAt) {
      setExpiryError('Укажите срок действия предложения')
      hasError = true
    }
    if (hasError || !pid) throw new Error('Проверьте поля формы')

    const res = await publishExpiringOffer(storeId, pid, original, discounted, currency.trim() || 'TJS', new Date(expiresAt).toISOString())
    const msg = outcomeMessage(res.outcome)
    if (msg) throw new Error(msg)
    await onCreated()
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title="Опубликовать «скоро истекает»"
      isDirty={!!(product || originalPrice || discountedPrice || expiresAt)}
      onSubmit={submit}
      submitLabel="Опубликовать"
      submitBusyLabel="Публикуем…"
      scheme="admin"
    >
      <OfferFormFields
        product={product} setProduct={setProduct}
        originalPrice={originalPrice} setOriginalPrice={setOriginalPrice}
        discountedPrice={discountedPrice} setDiscountedPrice={setDiscountedPrice}
        currency={currency} setCurrency={setCurrency}
        expiresAt={expiresAt} setExpiresAt={setExpiresAt}
        productError={productError} priceError={priceError} expiryError={expiryError}
      />
    </FormModal>
  )
}

function OffersSection({ storeId, createOpen, onCloseCreate }: { storeId: number; createOpen: boolean; onCloseCreate: () => void }) {
  const [offers, setOffers] = useState<ExpiringOffer[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getExpiringOffersForStore(storeId)
      setOffers(res.offers ?? [])
    } catch (err) {
      console.error('Failed to load expiring offers:', err)
      setError(errorMessage(err, 'Не удалось загрузить предложения'))
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div className="flex flex-col gap-6">
      <Panel className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <AlertIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Предложения этого магазина</span>
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
                      {o.productName || 'Товар'}
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

      <PublishOfferModal open={createOpen} onClose={onCloseCreate} storeId={storeId} onCreated={async () => { await load(); onCloseCreate() }} />
    </div>
  )
}

// ---------------------------------------------------------------------------
// Ответы на отзывы (Review replies)
// ---------------------------------------------------------------------------

function RepliesSection() {
  const [product, setProduct] = useState<ProductSearchItem | null>(null)
  const [reviews, setReviews] = useState<Review[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [searched, setSearched] = useState(false)

  const [openReplyId, setOpenReplyId] = useState<number | null>(null)
  const [replyText, setReplyText] = useState('')
  const [replyBusy, setReplyBusy] = useState(false)
  const [replyError, setReplyError] = useState('')
  const [repliedIds, setRepliedIds] = useState<Set<number>>(new Set())

  async function handleLookup(picked: ProductSearchItem | null) {
    setProduct(picked)
    const pid = picked?.productId
    if (!pid || loading) return
    setLoading(true)
    setError('')
    setSearched(true)
    setOpenReplyId(null)
    try {
      const res = await getReviews(pid)
      setReviews(res.reviews ?? [])
    } catch (err) {
      console.error('Failed to load reviews:', err)
      setError(errorMessage(err, 'Не удалось загрузить отзывы'))
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
      console.error('Failed to submit review reply:', err)
      setReplyError(
        err instanceof ApiError
          ? err.status === 404
            ? 'Отзыв не найден'
            : err.status === 403
              ? 'Нет доступа, чтобы отвечать на этот отзыв'
              : 'Не удалось отправить ответ'
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
          Отзывы можно посмотреть только по конкретному товару — выберите его ниже.
        </p>
        <ProductPicker value={product} onChange={handleLookup} scheme="admin" className="sm:max-w-[360px]" />
        {loading && <p className="mt-2 text-[12px] text-[color:var(--admin-text-tertiary)]">Ищем отзывы…</p>}
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
