import { useEffect, useRef, useState, type FormEvent, type SVGProps } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Card } from '../components/Card'
import { Select } from '../components/Select'
import { Toast } from '../components/Toast'
import { EmptyState } from '../components/EmptyState'
import { BarcodeScannerView } from '../components/BarcodeScannerView'
import {
  BarcodeIcon,
  CameraIcon,
  PlusIcon,
  MinusIcon,
  TrashIcon,
  CheckIcon,
  AlertIcon,
  ChevronDownIcon,
  CashIcon,
  EyeIcon,
} from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { useBarcodeScanner } from '../../hooks/useBarcodeScanner'
import { publishCustomerDisplayState } from '../lib/customerDisplay'
import {
  productsApi,
  salesApi,
  bundlesApi,
  ApiError,
  type ScanBarcodeResult,
  type ProcessSaleResult,
  type ProcessSaleResultLine,
  type BundleLine,
  type ProductBundle,
  type Commission,
  type SaleReturn,
} from '../../lib/api'

const CURRENCY = 'TJS'
const RECENT_SALES_KEY = 'sarfkor-recent-sales'

interface RecentSale {
  saleTransactionId: number
  totalAmount: number
  currency: string
  completedAt: string
  lines: ProcessSaleResultLine[]
  voided: boolean
}

function loadRecentSales(): RecentSale[] {
  try {
    const raw = JSON.parse(localStorage.getItem(RECENT_SALES_KEY) ?? '[]')
    return Array.isArray(raw) ? raw : []
  } catch {
    return []
  }
}

function saveRecentSales(sales: RecentSale[]) {
  // Keep this local cache small — it exists only so a cashier can void/refund a
  // sale they just rang up without a "list my sales" endpoint, not as a ledger.
  localStorage.setItem(RECENT_SALES_KEY, JSON.stringify(sales.slice(0, 20)))
}

function ReceiptPercentIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={14} height={14} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <line x1="19" y1="5" x2="5" y2="19" />
      <circle cx="6.5" cy="6.5" r="2.5" />
      <circle cx="17.5" cy="17.5" r="2.5" />
    </svg>
  )
}

function ReturnIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={14} height={14} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <path d="M3 7v6h6" />
      <path d="M21 17a9 9 0 0 0-15-6.7L3 13" />
    </svg>
  )
}

interface CartLine {
  productId: number
  productName: string
  unitPrice: number
  quantity: number
}

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function describeOutcome(result: ProcessSaleResult): string {
  switch (result.outcome) {
    case 'StoreNotFound':
      return 'Магазин не найден'
    case 'Forbidden':
      return 'Нет доступа к этому магазину'
    case 'ProductNotFound':
      return `Товар #${result.failedProductId} не найден`
    case 'PriceNotFound':
      return `Нет цены на товар #${result.failedProductId} в вашем магазине`
    case 'InsufficientStock':
      return `Недостаточно товара #${result.failedProductId} на складе`
    case 'GiftCardNotFound':
      return 'Подарочная карта с таким кодом не найдена'
    case 'GiftCardNotUsable':
      return 'Эта подарочная карта неактивна или просрочена'
    case 'CustomerNotFound':
      return 'Клиент с таким ID не найден'
    case 'BundleNotFound':
      return 'Набор товаров не найден'
    default:
      return 'Не удалось провести продажу'
  }
}

function SaleCard({ sale, onVoided }: { sale: RecentSale; onVoided: () => void }) {
  const [expanded, setExpanded] = useState(false)
  const [voidBusy, setVoidBusy] = useState(false)
  const [voidReason, setVoidReason] = useState('')
  const [voidOpen, setVoidOpen] = useState(false)
  const [voidError, setVoidError] = useState('')

  const [commissionOpen, setCommissionOpen] = useState(false)
  const [commissionAmount, setCommissionAmount] = useState('')
  const [commissionBusy, setCommissionBusy] = useState(false)
  const [commissionStatus, setCommissionStatus] = useState('')
  const [commissions, setCommissions] = useState<Commission[] | null>(null)

  const [returnOpen, setReturnOpen] = useState(false)
  const [returnLineId, setReturnLineId] = useState('')
  const [returnQty, setReturnQty] = useState('1')
  const [returnReason, setReturnReason] = useState('')
  const [returnBusy, setReturnBusy] = useState(false)
  const [returnStatus, setReturnStatus] = useState('')
  const [returns, setReturns] = useState<SaleReturn[] | null>(null)

  async function handleVoid(e: FormEvent) {
    e.preventDefault()
    if (!voidReason.trim() || voidBusy) return
    setVoidBusy(true)
    setVoidError('')
    try {
      const res = await salesApi.voidSale(sale.saleTransactionId, voidReason.trim())
      if (res.outcome === 'Voided') {
        onVoided()
      } else if (res.outcome === 'AlreadyVoided') {
        setVoidError('Эта продажа уже отменена')
      } else {
        setVoidError('Не удалось отменить продажу')
      }
    } catch (err) {
      setVoidError(err instanceof ApiError ? err.message : 'Не удалось отменить продажу')
    } finally {
      setVoidBusy(false)
    }
  }

  async function handleCommission(e: FormEvent) {
    e.preventDefault()
    const amount = Number(commissionAmount)
    if (!amount || amount <= 0 || commissionBusy) return
    setCommissionBusy(true)
    setCommissionStatus('')
    try {
      const res = await salesApi.recordCommission(sale.saleTransactionId, amount, sale.currency)
      if (res.outcome === 'Recorded') {
        setCommissionStatus('Комиссия записана')
        setCommissionAmount('')
        await loadCommissions()
      } else {
        setCommissionStatus(res.outcome === 'Forbidden' ? 'Нет доступа' : 'Продажа не найдена')
      }
    } catch (err) {
      setCommissionStatus(err instanceof ApiError ? err.message : 'Не удалось записать комиссию')
    } finally {
      setCommissionBusy(false)
    }
  }

  async function loadCommissions() {
    try {
      const res = await salesApi.getCommissionsForSale(sale.saleTransactionId)
      if (res.outcome === 'Found') setCommissions(res.commissions ?? [])
    } catch {
      // Leave whatever was loaded before — not worth surfacing an error for a side list.
    }
  }

  async function loadReturns() {
    try {
      const res = await salesApi.getReturnsForSale(sale.saleTransactionId)
      if (res.outcome === 'Found') setReturns(res.returns ?? [])
    } catch {
      // Same as loadCommissions — best-effort refresh.
    }
  }

  async function handleReturn(e: FormEvent) {
    e.preventDefault()
    const lineId = Number(returnLineId)
    const qty = Number(returnQty)
    if (!lineId || !qty || qty <= 0 || !returnReason.trim() || returnBusy) return
    setReturnBusy(true)
    setReturnStatus('')
    try {
      const res = await salesApi.processReturn(sale.saleTransactionId, [{ saleLineItemId: lineId, quantity: qty }], returnReason.trim())
      if (res.outcome === 'Processed') {
        setReturnStatus(`Возврат оформлен — возмещено ${fmt(res.totalRefund ?? 0)} ${sale.currency}`)
        setReturnReason('')
        setReturnQty('1')
        await loadReturns()
      } else if (res.outcome === 'ExceedsAvailableQuantity') {
        setReturnStatus('Количество превышает то, что было продано (с учётом прошлых возвратов)')
      } else if (res.outcome === 'LineNotFound') {
        setReturnStatus('Такой позиции нет в этой продаже')
      } else if (res.outcome === 'SaleNotCompleted') {
        setReturnStatus('Продажа отменена — возврат невозможен')
      } else {
        setReturnStatus('Нет доступа')
      }
    } catch (err) {
      setReturnStatus(err instanceof ApiError ? err.message : 'Не удалось оформить возврат')
    } finally {
      setReturnBusy(false)
    }
  }

  return (
    <div className="rounded-[16px] bg-[color:var(--admin-hover)] p-4">
      <button onClick={() => setExpanded((v) => !v)} className="flex min-h-11 w-full items-center justify-between gap-3 text-left">
        <div>
          <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">
            Продажа #{sale.saleTransactionId} {sale.voided && <span className="text-[color:var(--admin-danger)]">· отменена</span>}
          </div>
          <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
            {fmt(sale.totalAmount)} {sale.currency} · {new Date(sale.completedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}
          </div>
        </div>
        <ChevronDownIcon width={16} height={16} className={`shrink-0 text-[color:var(--admin-text-tertiary)] transition-transform ${expanded ? 'rotate-180' : ''}`} />
      </button>

      {expanded && (
        <div className="mt-3 flex flex-col gap-3 border-t border-[color:var(--admin-border)] pt-3">
          <div className="flex flex-wrap gap-2 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
            {sale.lines.map((l) => (
              <span key={l.saleLineItemId} className="rounded-full bg-[color:var(--admin-card)] px-2.5 py-1">
                #{l.saleLineItemId} · товар {l.productId} × {l.quantity}
              </span>
            ))}
          </div>

          {!sale.voided && (
            <div className="flex flex-wrap gap-2">
              <button
                onClick={() => setVoidOpen((v) => !v)}
                className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-danger-dim)] px-3 py-2.5 text-[11.5px] font-semibold text-[color:var(--admin-danger)] hover:opacity-80 lg:py-1.5"
              >
                <AlertIcon width={12} height={12} />
                Отменить продажу
              </button>
              <button
                onClick={() => {
                  setCommissionOpen((v) => !v)
                  if (!commissions) loadCommissions()
                }}
                className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-card)] px-3 py-2.5 text-[11.5px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] lg:py-1.5"
              >
                <ReceiptPercentIcon />
                Комиссия
              </button>
              <button
                onClick={() => {
                  setReturnOpen((v) => !v)
                  if (!returns) loadReturns()
                }}
                className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-card)] px-3 py-2.5 text-[11.5px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] lg:py-1.5"
              >
                <ReturnIcon />
                Возврат
              </button>
            </div>
          )}

          {voidOpen && !sale.voided && (
            <form onSubmit={handleVoid} className="flex flex-col gap-2 rounded-xl bg-[color:var(--admin-card)] p-3">
              <input
                value={voidReason}
                onChange={(e) => setVoidReason(e.target.value)}
                placeholder="Причина отмены (обязательно)"
                className="rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-2.5 py-2.5 text-[12.5px] text-[color:var(--admin-text)] outline-none lg:py-1.5"
              />
              {voidError && <div className="text-[11.5px] font-medium text-[color:var(--admin-danger)]">{voidError}</div>}
              <button
                type="submit"
                disabled={voidBusy || !voidReason.trim()}
                className="self-start rounded-lg bg-[color:var(--admin-danger)] px-3.5 py-2.5 text-[12px] font-semibold text-white disabled:opacity-50 lg:py-1.5"
              >
                {voidBusy ? 'Отменяем…' : 'Подтвердить отмену'}
              </button>
            </form>
          )}

          {commissionOpen && (
            <div className="rounded-xl bg-[color:var(--admin-card)] p-3">
              <form onSubmit={handleCommission} className="flex items-center gap-2">
                <input
                  value={commissionAmount}
                  onChange={(e) => setCommissionAmount(e.target.value)}
                  type="number"
                  min={0}
                  step="0.01"
                  placeholder="Сумма комиссии"
                  className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-2.5 py-2.5 text-[12.5px] text-[color:var(--admin-text)] outline-none lg:py-1.5"
                />
                <button
                  type="submit"
                  disabled={commissionBusy || !commissionAmount}
                  className="shrink-0 rounded-lg bg-[color:var(--admin-accent)] px-3.5 py-2.5 text-[12px] font-semibold text-white disabled:opacity-50 lg:py-1.5"
                >
                  {commissionBusy ? 'Сохраняем…' : 'Записать'}
                </button>
              </form>
              {commissionStatus && <div className="mt-1.5 text-[11.5px] text-[color:var(--admin-text-secondary)]">{commissionStatus}</div>}
              {commissions && commissions.length > 0 && (
                <div className="mt-2 flex flex-col gap-1">
                  {commissions.map((c) => (
                    <div key={c.commissionId} className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                      {fmt(c.amount)} {c.currency} · {new Date(c.createdAt).toLocaleString('ru-RU')}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {returnOpen && (
            <div className="rounded-xl bg-[color:var(--admin-card)] p-3">
              <form onSubmit={handleReturn} className="flex flex-col gap-2">
                <div className="flex gap-2">
                  <Select
                    value={returnLineId}
                    onChange={setReturnLineId}
                    className="min-w-0 flex-1"
                    size="sm"
                    placeholder="Позиция"
                    options={sale.lines.map((l) => ({
                      value: String(l.saleLineItemId),
                      label: `Товар ${l.productId} (продано ${l.quantity})`,
                    }))}
                  />
                  <input
                    value={returnQty}
                    onChange={(e) => setReturnQty(e.target.value)}
                    type="number"
                    min={1}
                    className="w-20 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-2.5 py-2.5 text-[12.5px] text-[color:var(--admin-text)] outline-none lg:py-1.5"
                  />
                </div>
                <input
                  value={returnReason}
                  onChange={(e) => setReturnReason(e.target.value)}
                  placeholder="Причина возврата (обязательно)"
                  className="rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-2.5 py-2.5 text-[12.5px] text-[color:var(--admin-text)] outline-none lg:py-1.5"
                />
                <button
                  type="submit"
                  disabled={returnBusy || !returnLineId || !returnReason.trim()}
                  className="self-start rounded-lg bg-[color:var(--admin-accent)] px-3.5 py-2.5 text-[12px] font-semibold text-white disabled:opacity-50 lg:py-1.5"
                >
                  {returnBusy ? 'Оформляем…' : 'Оформить возврат'}
                </button>
              </form>
              {returnStatus && <div className="mt-1.5 text-[11.5px] text-[color:var(--admin-text-secondary)]">{returnStatus}</div>}
              {returns && returns.length > 0 && (
                <div className="mt-2 flex flex-col gap-1">
                  {returns.map((r) => (
                    <div key={r.saleReturnId} className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                      Возврат #{r.saleReturnId} · {r.reason} · {new Date(r.createdAt).toLocaleString('ru-RU')}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function BundlePicker({ storeId, onAdd }: { storeId: number; onAdd: (bundle: ProductBundle) => void }) {
  const [open, setOpen] = useState(false)
  const [bundles, setBundles] = useState<ProductBundle[] | null>(null)

  async function toggle() {
    setOpen((v) => !v)
    if (bundles === null) {
      try {
        setBundles((await bundlesApi.getProductBundles(storeId)).bundles)
      } catch {
        setBundles([])
      }
    }
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={toggle}
        className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-hover)] px-3 py-1.5 text-[12px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]"
      >
        <PlusIcon width={12} height={12} />
        Набор
      </button>
      {open && (
        <div className="absolute left-0 top-full z-10 mt-1.5 w-64 rounded-xl bg-[color:var(--admin-card)] p-2 shadow-lg ring-1 ring-[color:var(--admin-border)]">
          {bundles === null && <div className="p-2 text-[12px] text-[color:var(--admin-text-tertiary)]">Загрузка…</div>}
          {bundles && bundles.length === 0 && <div className="p-2 text-[12px] text-[color:var(--admin-text-tertiary)]">В магазине нет наборов</div>}
          {bundles?.map((b) => (
            <button
              key={b.productBundleId}
              onClick={() => {
                onAdd(b)
                setOpen(false)
              }}
              className="flex w-full items-center justify-between gap-2 rounded-lg px-2.5 py-2 text-left text-[12.5px] hover:bg-[color:var(--admin-hover)]"
            >
              <span className="truncate font-medium text-[color:var(--admin-text)]">{b.name}</span>
              <span className="shrink-0 text-[color:var(--admin-text-tertiary)]">{fmt(b.bundlePrice)} {b.currency}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

export function PosPage() {
  const { storeId } = useAuth()
  const inputRef = useRef<HTMLInputElement>(null)
  const idempotencyKeyRef = useRef<string | null>(null)

  const [barcode, setBarcode] = useState('')
  const [scanning, setScanning] = useState(false)
  const [scanError, setScanError] = useState('')
  const [lastScan, setLastScan] = useState<ScanBarcodeResult | null>(null)
  const [cameraOpen, setCameraOpen] = useState(false)

  const [cart, setCart] = useState<CartLine[]>([])
  const [cartBundles, setCartBundles] = useState<{ productBundleId: number; name: string; bundlePrice: number; currency: string; quantity: number }[]>([])
  const [checkoutBusy, setCheckoutBusy] = useState(false)
  const [checkoutError, setCheckoutError] = useState('')
  const [successInfo, setSuccessInfo] = useState<{ totalAmount: number; currency: string } | null>(null)

  const [recentSales, setRecentSales] = useState<RecentSale[]>(() => loadRecentSales())

  function addBundleToCart(bundle: ProductBundle) {
    setCartBundles((bs) => {
      const existing = bs.find((b) => b.productBundleId === bundle.productBundleId)
      if (existing) {
        return bs.map((b) => (b.productBundleId === bundle.productBundleId ? { ...b, quantity: b.quantity + 1 } : b))
      }
      return [...bs, { productBundleId: bundle.productBundleId, name: bundle.name, bundlePrice: bundle.bundlePrice, currency: bundle.currency, quantity: 1 }]
    })
  }

  function removeBundleLine(productBundleId: number) {
    setCartBundles((bs) => bs.filter((b) => b.productBundleId !== productBundleId))
  }

  function addToCart(productId: number, productName: string, unitPrice: number) {
    setCart((c) => {
      const existing = c.find((l) => l.productId === productId)
      if (existing) {
        return c.map((l) => (l.productId === productId ? { ...l, quantity: l.quantity + 1 } : l))
      }
      return [...c, { productId, productName, unitPrice, quantity: 1 }]
    })
  }

  function changeQty(productId: number, delta: number) {
    setCart((c) =>
      c.map((l) => (l.productId === productId ? { ...l, quantity: Math.max(0, l.quantity + delta) } : l)).filter((l) => l.quantity > 0),
    )
  }

  function removeLine(productId: number) {
    setCart((c) => c.filter((l) => l.productId !== productId))
  }

  // Shared by the manual-entry form and the camera scanner below -- both just need
  // to resolve a raw barcode string to a cart line (or an explanatory error).
  async function lookupAndAddToCart(code: string) {
    if (!code || scanning) return
    setScanning(true)
    setScanError('')
    try {
      const result = await productsApi.scanBarcode(code)
      setLastScan(result)
      const here = result.stores.find((s) => s.storeId === storeId)
      if (!here) {
        setScanError(`«${result.productName}» не продаётся в вашем магазине — нет цены`)
      } else {
        addToCart(result.productId, result.productName, here.price)
      }
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setScanError('Товар с таким штрихкодом не найден')
      } else if (err instanceof ApiError && err.status === 429) {
        setScanError('Слишком много запросов — подождите немного')
      } else {
        setScanError(err instanceof ApiError ? err.message : 'Не удалось выполнить поиск')
      }
      setLastScan(null)
    } finally {
      setScanning(false)
    }
  }

  async function handleScan(e: React.FormEvent) {
    e.preventDefault()
    const code = barcode.trim()
    if (!code || scanning) return
    await lookupAndAddToCart(code)
    setBarcode('')
    inputRef.current?.focus()
  }

  // continuous: true -- a cashier rings up many items in a row, so the camera keeps
  // reading after each hit instead of closing (the hook's own value-based dedupe
  // stops the same barcode from being added twice while it's still in frame).
  const scanner = useBarcodeScanner({
    onDetect: (code) => {
      lookupAndAddToCart(code)
    },
    continuous: true,
  })

  useEffect(() => {
    if (cameraOpen) {
      scanner.start()
    } else {
      scanner.stop()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cameraOpen])

  const total =
    cart.reduce((sum, l) => sum + l.unitPrice * l.quantity, 0) +
    cartBundles.reduce((sum, b) => sum + b.bundlePrice * b.quantity, 0)
  const itemCount = cart.reduce((sum, l) => sum + l.quantity, 0) + cartBundles.reduce((sum, b) => sum + b.quantity, 0)

  // Mirrors the cart to a customer-facing display window in real time — see
  // admin/lib/customerDisplay.ts for why this is a same-tab broadcast, not an API call.
  useEffect(() => {
    publishCustomerDisplayState({
      storeId: storeId ?? null,
      lines: [
        ...cart.map((l) => ({ key: `p${l.productId}`, name: l.productName, unitPrice: l.unitPrice, quantity: l.quantity })),
        ...cartBundles.map((b) => ({ key: `b${b.productBundleId}`, name: `Набор: ${b.name}`, unitPrice: b.bundlePrice, quantity: b.quantity })),
      ],
      total,
      currency: CURRENCY,
    })
  }, [cart, cartBundles, total, storeId])

  async function completeSale() {
    if ((cart.length === 0 && cartBundles.length === 0) || checkoutBusy || !storeId) return
    setCheckoutBusy(true)
    setCheckoutError('')
    const key = idempotencyKeyRef.current ?? crypto.randomUUID()
    idempotencyKeyRef.current = key
    try {
      const bundleLines: BundleLine[] = cartBundles.map((b) => ({ productBundleId: b.productBundleId, quantity: b.quantity }))
      const result = await salesApi.processSale({
        storeId,
        idempotencyKey: key,
        currency: CURRENCY,
        lines: cart.map((l) => ({ productId: l.productId, quantity: l.quantity })),
        bundleLines: bundleLines.length > 0 ? bundleLines : undefined,
      })
      if (result.outcome === 'Completed') {
        setSuccessInfo({
          totalAmount: result.totalAmount ?? total,
          currency: result.currency ?? CURRENCY,
        })
        publishCustomerDisplayState({
          storeId: storeId ?? null,
          lines: [],
          total: 0,
          currency: CURRENCY,
          completedTotal: { amount: result.totalAmount ?? total, currency: result.currency ?? CURRENCY },
        })
        if (result.saleTransactionId != null) {
          const next = [
            {
              saleTransactionId: result.saleTransactionId,
              totalAmount: result.totalAmount ?? total,
              currency: result.currency ?? CURRENCY,
              completedAt: new Date().toISOString(),
              lines: result.lines ?? [],
              voided: false,
            },
            ...recentSales,
          ]
          setRecentSales(next)
          saveRecentSales(next)
        }
        setCart([])
        setCartBundles([])
        setLastScan(null)
        idempotencyKeyRef.current = null
        setTimeout(() => setSuccessInfo(null), 3600)
      } else {
        // A definitive rejection (bad line, no access, etc.) means the next
        // attempt is a different sale once the cashier fixes the cart — reuse
        // of this key would then incorrectly dedupe against the fix.
        idempotencyKeyRef.current = null
        setCheckoutError(describeOutcome(result))
      }
    } catch (err) {
      // Network/5xx failure: the request may or may not have landed, so keep
      // the same idempotency key — retrying with it is safe by construction.
      setCheckoutError(err instanceof ApiError ? err.message : 'Не удалось провести продажу — проверьте соединение')
    } finally {
      setCheckoutBusy(false)
    }
  }

  return (
    <div className="mx-auto grid max-w-[1400px] grid-cols-1 gap-4 lg:gap-6 lg:grid-cols-[1fr_380px]">
      <div className="flex min-w-0 flex-col gap-3 lg:gap-5">
        <div className="flex gap-2">
          <form onSubmit={handleScan} className="relative flex-1">
            <BarcodeIcon
              width={17}
              height={17}
              className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)]"
            />
            <input
              ref={inputRef}
              autoFocus
              value={barcode}
              onChange={(e) => setBarcode(e.target.value)}
              type="text"
              placeholder="Сканируйте штрихкод (или введите вручную) и нажмите Enter"
              disabled={scanning}
              className="w-full rounded-[14px] border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] py-4 pl-11 pr-4 text-sm text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)] disabled:opacity-60 lg:py-3.5"
            />
          </form>
          <button
            type="button"
            onClick={() => setCameraOpen((v) => !v)}
            title={cameraOpen ? 'Скрыть камеру' : 'Сканировать камерой'}
            aria-pressed={cameraOpen}
            className={`grid shrink-0 place-items-center rounded-[14px] border px-5 transition-colors lg:px-4 ${
              cameraOpen
                ? 'border-[color:var(--admin-accent)] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]'
                : 'border-[color:var(--admin-border)] bg-[color:var(--admin-card)] text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]'
            }`}
          >
            <CameraIcon width={19} height={19} />
          </button>
        </div>

        {cameraOpen && (
          <BarcodeScannerView
            videoRef={scanner.videoRef}
            phase={scanner.phase}
            onStart={scanner.start}
            className="aspect-video max-h-[280px] w-full"
          />
        )}

        {scanError && (
          <div className="flex items-center gap-2.5 rounded-xl bg-[color:var(--admin-danger-dim)] px-4 py-3 text-[13px] font-medium text-[color:var(--admin-danger)]">
            <AlertIcon width={16} height={16} className="shrink-0" />
            {scanError}
          </div>
        )}

        {lastScan &&
          (() => {
            const here = lastScan.stores.find((s) => s.storeId === storeId)
            if (!here) return null
            return (
              <Card className="p-5">
                <div className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                  Найдено по штрихкоду
                </div>
                <div className="flex items-center justify-between gap-3">
                  <div className="text-[16px] font-bold text-[color:var(--admin-text)]">{lastScan.productName}</div>
                  <div className="text-[16px] font-bold text-[color:var(--admin-accent)]">
                    {fmt(here.price)} {here.currency}
                  </div>
                </div>
              </Card>
            )
          })()}

        {!lastScan && !scanError && (
          <div className="flex flex-1 flex-col items-center justify-center gap-3 rounded-[16px] border border-dashed border-[color:var(--admin-border)] py-8 text-center text-[color:var(--admin-text-tertiary)] lg:py-20">
            <BarcodeIcon width={32} height={32} />
            <p className="max-w-xs text-[13px]">
              Поиск товаров работает только по точному штрихкоду — отсканируйте товар, чтобы добавить его в чек
            </p>
          </div>
        )}
      </div>

      {/* Cart / checkout */}
      <Card className="flex h-fit flex-col gap-3 p-5 lg:sticky lg:top-6 lg:gap-4">
        <div className="flex items-center justify-between gap-2">
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Текущий чек</span>
          <div className="flex items-center gap-3">
            <button
              onClick={() => window.open('/admin/pos/display', 'sarfkor-customer-display', 'width=900,height=700')}
              title="Открыть на втором мониторе для покупателя"
              className="flex items-center gap-1 text-xs font-medium text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-accent)]"
            >
              <EyeIcon width={13} height={13} />
              Экран покупателя
            </button>
            {(cart.length > 0 || cartBundles.length > 0) && (
              <button
                onClick={() => {
                  setCart([])
                  setCartBundles([])
                  idempotencyKeyRef.current = null
                }}
                className="text-xs font-medium text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-danger)]"
              >
                Очистить
              </button>
            )}
          </div>
        </div>

        <div className="flex max-h-[360px] flex-col gap-2 overflow-y-auto">
          <AnimatePresence initial={false}>
            {cart.map((line) => (
              <motion.div
                key={line.productId}
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                transition={{ duration: 0.2 }}
                className="flex items-center gap-2.5 overflow-hidden rounded-xl bg-[color:var(--admin-hover)] p-2.5"
              >
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[14px] font-semibold text-[color:var(--admin-text)] lg:text-[12.5px]">{line.productName}</div>
                  <div className="font-[JetBrains_Mono,monospace] text-[12.5px] tabular-nums text-[color:var(--admin-text-tertiary)] lg:text-[11px]">
                    {fmt(line.unitPrice)} смн × {line.quantity}
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-1">
                  <button
                    onClick={() => changeQty(line.productId, -1)}
                    className="grid h-11 w-11 place-items-center rounded-md bg-[color:var(--admin-card)] text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] lg:h-6 lg:w-6"
                    aria-label="Уменьшить"
                  >
                    <MinusIcon width={14} height={14} />
                  </button>
                  <span className="w-6 text-center font-[JetBrains_Mono,monospace] text-[14px] font-bold tabular-nums text-[color:var(--admin-text)] lg:w-5 lg:text-[12px]">{line.quantity}</span>
                  <button
                    onClick={() => changeQty(line.productId, 1)}
                    className="grid h-11 w-11 place-items-center rounded-md bg-[color:var(--admin-card)] text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] lg:h-6 lg:w-6"
                    aria-label="Увеличить"
                  >
                    <PlusIcon width={14} height={14} />
                  </button>
                </div>
                <button
                  onClick={() => removeLine(line.productId)}
                  className="grid h-11 w-11 shrink-0 place-items-center rounded-md text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-danger)] lg:h-6 lg:w-6"
                  aria-label="Удалить"
                >
                  <TrashIcon width={15} height={15} />
                </button>
              </motion.div>
            ))}
            {cartBundles.map((b) => (
              <motion.div
                key={`bundle-${b.productBundleId}`}
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                transition={{ duration: 0.2 }}
                className="flex items-center gap-2.5 overflow-hidden rounded-xl bg-[color:var(--admin-accent-soft)] p-2.5"
              >
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[12.5px] font-semibold text-[color:var(--admin-accent)]">Набор: {b.name}</div>
                  <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                    {fmt(b.bundlePrice)} {b.currency} × {b.quantity}
                  </div>
                </div>
                <button
                  onClick={() => removeBundleLine(b.productBundleId)}
                  className="grid h-11 w-11 shrink-0 place-items-center rounded-md text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-danger)] lg:h-6 lg:w-6"
                  aria-label="Удалить набор"
                >
                  <TrashIcon width={15} height={15} />
                </button>
              </motion.div>
            ))}
          </AnimatePresence>
          {cart.length === 0 && cartBundles.length === 0 && (
            <EmptyState title="Чек пуст" body="Отсканируйте товар слева, чтобы добавить его в продажу" />
          )}
        </div>

        {storeId != null && <BundlePicker storeId={storeId} onAdd={addBundleToCart} />}

        {/* Total + checkout — the one thing that must always be reachable
            without hunting for it. Sticks to the bottom of the scroll area
            on mobile/Cashier (a phone in one hand, in a hurry); on desktop
            (lg+) it's just the card's normal in-flow footer, as before. */}
        <div className="sticky bottom-0 -mx-5 -mb-5 flex flex-col gap-3 rounded-b-2xl border-t border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-5 py-4 lg:static lg:mx-0 lg:mb-0 lg:gap-2 lg:rounded-none lg:px-0 lg:py-0 lg:pt-4">
          <div className="flex items-center justify-between text-[13px] text-[color:var(--admin-text-secondary)]">
            <span>Товаров</span>
            <span className="font-[JetBrains_Mono,monospace] font-semibold tabular-nums text-[color:var(--admin-text)]">{itemCount}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-[15px] font-semibold text-[color:var(--admin-text)]">Итого</span>
            <span className="font-[JetBrains_Mono,monospace] text-[36px] font-extrabold leading-none tabular-nums text-[color:var(--admin-text)] lg:text-[22px]">
              {fmt(total)} смн
            </span>
          </div>

          {checkoutError && (
            <div className="flex items-center gap-2 rounded-xl bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">
              <AlertIcon width={14} height={14} className="shrink-0" />
              {checkoutError}
            </div>
          )}

          <button
            onClick={completeSale}
            disabled={(cart.length === 0 && cartBundles.length === 0) || checkoutBusy}
            className="rounded-xl bg-[color:var(--admin-accent)] py-5 text-[17px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:scale-100 lg:py-3.5 lg:text-[14px]"
          >
            {checkoutBusy ? 'Проводим продажу…' : 'Оформить продажу'}
          </button>
        </div>
      </Card>

      {recentSales.length > 0 && (
        <Card className="p-5 lg:col-span-2">
          <div className="mb-3 flex items-center gap-2">
            <CashIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
            <span className="text-[15px] font-bold text-[color:var(--admin-text)]">Недавние продажи</span>
          </div>
          <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
            Список продаж, оформленных в этом браузере — бэкенд не отдаёт историю продаж списком, поэтому отменить,
            добавить комиссию или оформить возврат можно только по продажам из этого списка.
          </p>
          <div className="flex flex-col gap-2.5">
            {recentSales.map((sale) => (
              <SaleCard
                key={sale.saleTransactionId}
                sale={sale}
                onVoided={() => {
                  const next = recentSales.map((s) => (s.saleTransactionId === sale.saleTransactionId ? { ...s, voided: true } : s))
                  setRecentSales(next)
                  saveRecentSales(next)
                }}
              />
            ))}
          </div>
        </Card>
      )}

      <Toast open={!!successInfo} variant="success">
        <span className="grid h-6 w-6 shrink-0 place-items-center rounded-full bg-white/15">
          <CheckIcon width={13} height={13} />
        </span>
        <span>
          Продажа оформлена ·{' '}
          <span className="opacity-70">
            {successInfo ? `${fmt(successInfo.totalAmount)} ${successInfo.currency}` : ''}
          </span>
        </span>
      </Toast>
    </div>
  )
}
