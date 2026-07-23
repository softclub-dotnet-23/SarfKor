import { useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Card } from '../components/Card'
import { BarcodeIcon, PlusIcon, MinusIcon, TrashIcon, CheckIcon, AlertIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { productsApi, salesApi, ApiError, type ScanBarcodeResult, type ProcessSaleResult } from '../../lib/api'

const CURRENCY = 'TJS'

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
    default:
      return 'Не удалось провести продажу'
  }
}

export function PosPage() {
  const { storeId } = useAuth()
  const inputRef = useRef<HTMLInputElement>(null)
  const idempotencyKeyRef = useRef<string | null>(null)

  const [barcode, setBarcode] = useState('')
  const [scanning, setScanning] = useState(false)
  const [scanError, setScanError] = useState('')
  const [lastScan, setLastScan] = useState<ScanBarcodeResult | null>(null)

  const [cart, setCart] = useState<CartLine[]>([])
  const [checkoutBusy, setCheckoutBusy] = useState(false)
  const [checkoutError, setCheckoutError] = useState('')
  const [successInfo, setSuccessInfo] = useState<{ totalAmount: number; currency: string } | null>(null)

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

  async function handleScan(e: React.FormEvent) {
    e.preventDefault()
    const code = barcode.trim()
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
      setBarcode('')
      inputRef.current?.focus()
    }
  }

  const total = cart.reduce((sum, l) => sum + l.unitPrice * l.quantity, 0)
  const itemCount = cart.reduce((sum, l) => sum + l.quantity, 0)

  async function completeSale() {
    if (cart.length === 0 || checkoutBusy || !storeId) return
    setCheckoutBusy(true)
    setCheckoutError('')
    const key = idempotencyKeyRef.current ?? crypto.randomUUID()
    idempotencyKeyRef.current = key
    try {
      const result = await salesApi.processSale({
        storeId,
        idempotencyKey: key,
        currency: CURRENCY,
        lines: cart.map((l) => ({ productId: l.productId, quantity: l.quantity })),
      })
      if (result.outcome === 'Completed') {
        setSuccessInfo({ totalAmount: result.totalAmount ?? total, currency: result.currency ?? CURRENCY })
        setCart([])
        setLastScan(null)
        idempotencyKeyRef.current = null
        setTimeout(() => setSuccessInfo(null), 2800)
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
    <div className="mx-auto grid max-w-[1400px] grid-cols-1 gap-6 lg:grid-cols-[1fr_380px]">
      <div className="flex min-w-0 flex-col gap-5">
        <form onSubmit={handleScan} className="relative">
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
            className="w-full rounded-[14px] border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] py-3.5 pl-11 pr-4 text-sm text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)] disabled:opacity-60"
          />
        </form>

        {scanError && (
          <div className="flex items-center gap-2.5 rounded-xl bg-[#f87171]/10 px-4 py-3 text-[13px] font-medium text-[#f87171]">
            <AlertIcon width={16} height={16} className="shrink-0" />
            {scanError}
          </div>
        )}

        {lastScan && (
          <Card className="p-5">
            <div className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
              Найдено по штрихкоду
            </div>
            <div className="mb-4 text-[16px] font-bold text-[color:var(--admin-text)]">{lastScan.productName}</div>
            <div className="flex flex-col gap-2">
              {lastScan.stores.map((s) => (
                <div
                  key={s.storeId}
                  className={`flex items-center justify-between rounded-xl px-3.5 py-2.5 text-[13px] ${
                    s.storeId === storeId
                      ? 'bg-[color:var(--admin-accent-soft)] font-semibold text-[color:var(--admin-accent)]'
                      : 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-secondary)]'
                  }`}
                >
                  <span>
                    {s.storeName}
                    {s.storeId === storeId ? ' (ваш магазин)' : ''}
                  </span>
                  <span className="font-bold">
                    {fmt(s.price)} {s.currency}
                  </span>
                </div>
              ))}
            </div>
          </Card>
        )}

        {!lastScan && !scanError && (
          <div className="flex flex-1 flex-col items-center justify-center gap-3 rounded-[16px] border border-dashed border-[color:var(--admin-border)] py-20 text-center text-[color:var(--admin-text-tertiary)]">
            <BarcodeIcon width={36} height={36} />
            <p className="max-w-xs text-[13px]">
              Поиск товаров работает только по точному штрихкоду — отсканируйте товар, чтобы добавить его в чек
            </p>
          </div>
        )}
      </div>

      {/* Cart / checkout */}
      <Card className="flex h-fit flex-col gap-4 p-5 lg:sticky lg:top-6">
        <div className="flex items-center justify-between">
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Текущий чек</span>
          {cart.length > 0 && (
            <button
              onClick={() => {
                setCart([])
                idempotencyKeyRef.current = null
              }}
              className="text-xs font-medium text-[color:var(--admin-text-tertiary)] hover:text-[#f87171]"
            >
              Очистить
            </button>
          )}
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
                  <div className="truncate text-[12.5px] font-semibold text-[color:var(--admin-text)]">{line.productName}</div>
                  <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                    {fmt(line.unitPrice)} смн × {line.quantity}
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-1">
                  <button
                    onClick={() => changeQty(line.productId, -1)}
                    className="grid h-6 w-6 place-items-center rounded-md bg-[color:var(--admin-card)] text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]"
                    aria-label="Уменьшить"
                  >
                    <MinusIcon width={12} height={12} />
                  </button>
                  <span className="w-5 text-center text-[12px] font-bold text-[color:var(--admin-text)]">{line.quantity}</span>
                  <button
                    onClick={() => changeQty(line.productId, 1)}
                    className="grid h-6 w-6 place-items-center rounded-md bg-[color:var(--admin-card)] text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]"
                    aria-label="Увеличить"
                  >
                    <PlusIcon width={12} height={12} />
                  </button>
                </div>
                <button
                  onClick={() => removeLine(line.productId)}
                  className="grid h-6 w-6 shrink-0 place-items-center rounded-md text-[color:var(--admin-text-tertiary)] hover:text-[#f87171]"
                  aria-label="Удалить"
                >
                  <TrashIcon width={13} height={13} />
                </button>
              </motion.div>
            ))}
          </AnimatePresence>
          {cart.length === 0 && (
            <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
              Чек пуст — отсканируйте товар слева
            </div>
          )}
        </div>

        <div className="flex flex-col gap-2 border-t border-[color:var(--admin-border)] pt-4">
          <div className="flex items-center justify-between text-[13px] text-[color:var(--admin-text-secondary)]">
            <span>Товаров</span>
            <span className="font-semibold text-[color:var(--admin-text)]">{itemCount}</span>
          </div>
          <div className="flex items-center justify-between text-[15px]">
            <span className="font-semibold text-[color:var(--admin-text)]">Итого</span>
            <span className="text-[22px] font-extrabold text-[color:var(--admin-text)]">{fmt(total)} смн</span>
          </div>
        </div>

        {checkoutError && (
          <div className="flex items-center gap-2 rounded-xl bg-[#f87171]/10 px-3.5 py-2.5 text-[12.5px] font-medium text-[#f87171]">
            <AlertIcon width={14} height={14} className="shrink-0" />
            {checkoutError}
          </div>
        )}

        <button
          onClick={completeSale}
          disabled={cart.length === 0 || checkoutBusy}
          className="rounded-xl bg-[color:var(--admin-accent)] py-3.5 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:scale-100"
        >
          {checkoutBusy ? 'Проводим продажу…' : 'Оформить продажу'}
        </button>
      </Card>

      <AnimatePresence>
        {successInfo && (
          <motion.div
            initial={{ opacity: 0, y: 20, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.95 }}
            className="fixed bottom-8 right-8 z-50 flex items-center gap-3 rounded-2xl bg-[#0f172a] px-5 py-4 text-white shadow-2xl"
          >
            <span className="grid h-9 w-9 place-items-center rounded-full bg-[#34d399]/20 text-[#34d399]">
              <CheckIcon width={18} height={18} />
            </span>
            <div>
              <div className="text-sm font-bold">Продажа оформлена</div>
              <div className="text-xs text-white/60">
                {fmt(successInfo.totalAmount)} {successInfo.currency}
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
