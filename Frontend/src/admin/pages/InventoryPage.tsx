import { useCallback, useEffect, useMemo, useState } from 'react'
import { Card } from '../components/Card'
import { AdminModal } from '../components/AdminModal'
import { SearchIcon, PlusIcon, AlertIcon, TruckIcon, BarcodeIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { inventoryApi, productsApi, storesApi, ApiError, type StockLevel, type ReorderAlert } from '../../lib/api'
import { createReorderRule } from '../../lib/api/reorderRules'

const NAME_CACHE_KEY = 'sarfkor-product-names'

function loadNameCache(): Record<number, string> {
  try {
    return JSON.parse(localStorage.getItem(NAME_CACHE_KEY) ?? '{}')
  } catch {
    return {}
  }
}

function saveNameCache(cache: Record<number, string>) {
  localStorage.setItem(NAME_CACHE_KEY, JSON.stringify(cache))
}

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface ReceiptTarget {
  productId: number
  productName?: string
}

export function InventoryPage() {
  const { storeId } = useAuth()
  const [nameCache, setNameCache] = useState<Record<number, string>>(loadNameCache)
  const [stock, setStock] = useState<StockLevel[] | null>(null)
  const [alerts, setAlerts] = useState<ReorderAlert[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [query, setQuery] = useState('')

  const [scanOpen, setScanOpen] = useState(false)
  const [scanBarcode, setScanBarcode] = useState('')
  const [scanBusy, setScanBusy] = useState(false)
  const [scanError, setScanError] = useState('')

  const [receiptFor, setReceiptFor] = useState<ReceiptTarget | null>(null)
  const [receiptQty, setReceiptQty] = useState(10)
  const [receiptBusy, setReceiptBusy] = useState(false)
  const [receiptError, setReceiptError] = useState('')

  const [costFor, setCostFor] = useState<ReceiptTarget | null>(null)
  const [costAmount, setCostAmount] = useState('')
  const [costBusy, setCostBusy] = useState(false)
  const [costError, setCostError] = useState('')
  const [costDone, setCostDone] = useState(false)

  const [ruleOpen, setRuleOpen] = useState(false)
  const [ruleProductId, setRuleProductId] = useState('')
  const [ruleThreshold, setRuleThreshold] = useState('5')
  const [ruleReorderQty, setRuleReorderQty] = useState('20')
  const [ruleSupplierId, setRuleSupplierId] = useState('')
  const [ruleBusy, setRuleBusy] = useState(false)
  const [ruleError, setRuleError] = useState('')

  const load = useCallback(async () => {
    if (!storeId) return
    setError('')
    try {
      const [stockRes, alertsRes] = await Promise.all([
        inventoryApi.getStockLevels(storeId),
        storesApi.getReorderAlerts(storeId),
      ])
      setStock(stockRes)
      setAlerts(alertsRes.alerts ?? [])
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить склад')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  const alertMap = useMemo(() => new Map(alerts.map((a) => [a.productId, a])), [alerts])

  const filtered = useMemo(() => {
    if (!stock) return []
    const q = query.trim().toLowerCase()
    if (!q) return stock
    return stock.filter((s) => {
      const name = nameCache[s.productId]?.toLowerCase() ?? ''
      return String(s.productId).includes(q) || name.includes(q)
    })
  }, [stock, query, nameCache])

  const totalUnits = stock?.reduce((sum, s) => sum + s.quantity, 0) ?? 0

  function rememberName(productId: number, name: string) {
    setNameCache((c) => {
      const next = { ...c, [productId]: name }
      saveNameCache(next)
      return next
    })
  }

  async function handleScanSubmit(e: React.FormEvent) {
    e.preventDefault()
    const code = scanBarcode.trim()
    if (!code || scanBusy) return
    setScanBusy(true)
    setScanError('')
    try {
      const result = await productsApi.scanBarcode(code)
      rememberName(result.productId, result.productName)
      setScanOpen(false)
      setScanBarcode('')
      setReceiptFor({ productId: result.productId, productName: result.productName })
      setReceiptQty(10)
      setReceiptError('')
    } catch (err) {
      setScanError(
        err instanceof ApiError && err.status === 404 ? 'Товар с таким штрихкодом не найден' : 'Не удалось выполнить поиск',
      )
    } finally {
      setScanBusy(false)
    }
  }

  async function confirmReceipt() {
    if (!receiptFor || !storeId || receiptBusy) return
    setReceiptBusy(true)
    setReceiptError('')
    try {
      await inventoryApi.recordStockReceipt(storeId, receiptFor.productId, receiptQty)
      setReceiptFor(null)
      setReceiptQty(10)
      await load()
    } catch (err) {
      setReceiptError(err instanceof ApiError ? err.message : 'Не удалось оприходовать поставку')
    } finally {
      setReceiptBusy(false)
    }
  }

  async function confirmCostPrice() {
    if (!costFor || !storeId || costBusy) return
    const amount = Number(costAmount)
    if (!amount || amount <= 0) return
    setCostBusy(true)
    setCostError('')
    try {
      await inventoryApi.setCostPrice(storeId, costFor.productId, amount, 'TJS')
      setCostDone(true)
      setTimeout(() => {
        setCostFor(null)
        setCostAmount('')
        setCostDone(false)
      }, 1200)
    } catch (err) {
      setCostError(err instanceof ApiError ? err.message : 'Не удалось сохранить себестоимость')
    } finally {
      setCostBusy(false)
    }
  }

  async function confirmCreateRule() {
    if (!storeId || ruleBusy) return
    const productId = Number(ruleProductId)
    const thresholdQuantity = Number(ruleThreshold)
    const reorderQuantity = Number(ruleReorderQty)
    if (!productId || productId <= 0 || !thresholdQuantity || thresholdQuantity < 0 || !reorderQuantity || reorderQuantity <= 0) {
      setRuleError('Проверьте товар, порог и количество пополнения')
      return
    }
    const preferredSupplierId = ruleSupplierId.trim() ? Number(ruleSupplierId) : undefined
    setRuleBusy(true)
    setRuleError('')
    try {
      const result = await createReorderRule(storeId, productId, thresholdQuantity, reorderQuantity, preferredSupplierId)
      if (result.outcome !== 'Created') {
        setRuleError(result.outcome === 'Forbidden' ? 'Нет доступа к этому магазину' : 'Магазин не найден')
        return
      }
      setRuleOpen(false)
      setRuleProductId('')
      setRuleThreshold('5')
      setRuleReorderQty('20')
      setRuleSupplierId('')
      await load()
    } catch (err) {
      setRuleError(err instanceof ApiError ? err.message : 'Не удалось создать правило пополнения')
    } finally {
      setRuleBusy(false)
    }
  }

  if (loading) {
    return <div className="py-24 text-center text-[color:var(--admin-text-tertiary)]">Загружаем склад…</div>
  }

  if (error) {
    return (
      <Card className="p-8 text-center">
        <p className="mb-4 text-[14px] text-[color:var(--admin-text-secondary)]">{error}</p>
        <button onClick={load} className="rounded-xl bg-[color:var(--admin-accent)] px-5 py-2.5 text-[13px] font-semibold text-white hover:opacity-90">
          Повторить
        </button>
      </Card>
    )
  }

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Позиций на складе</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-text)]">{stock?.length ?? 0}</div>
        </Card>
        <Card className="p-5">
          <div className="flex items-start justify-between gap-2">
            <div>
              <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Требуют пополнения</div>
              <div className="mt-2 text-[26px] font-extrabold text-[#fbbf24]">{alerts.length}</div>
            </div>
            <button
              onClick={() => {
                setRuleOpen(true)
                setRuleError('')
              }}
              className="mt-1 shrink-0 rounded-lg bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[11px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80"
            >
              + Правило пополнения
            </button>
          </div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Всего единиц</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-text)]">{fmt(totalUnits)}</div>
        </Card>
      </div>

      <Card className="p-5">
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <SearchIcon width={16} height={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)]" />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Поиск по ID товара или названию (если уже распознано)"
              className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] py-2.5 pl-9 pr-3 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
            />
          </div>
          <button
            onClick={() => {
              setScanOpen(true)
              setScanError('')
            }}
            className="flex shrink-0 items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[13px] font-semibold text-white hover:opacity-90"
          >
            <BarcodeIcon width={15} height={15} />
            Приход по штрихкоду
          </button>
        </div>

        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          В бэкенде нет каталога товаров с названиями — названия появляются только после сканирования штрихкода и
          запоминаются в этом браузере.
        </p>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[560px] border-collapse text-left text-[13px]">
            <thead>
              <tr className="text-[11px] uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                <th className="pb-3 font-semibold">Товар</th>
                <th className="pb-3 font-semibold">Остаток</th>
                <th className="pb-3 font-semibold" />
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => {
                const alert = alertMap.get(s.productId)
                const name = nameCache[s.productId]
                return (
                  <tr key={s.productId} className="border-t border-[color:var(--admin-border)]">
                    <td className="py-3 pr-3">
                      <div className="font-semibold text-[color:var(--admin-text)]">{name ?? `Товар #${s.productId}`}</div>
                      <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">ID {s.productId}</div>
                    </td>
                    <td className="py-3 pr-3">
                      {alert ? (
                        <span className="rounded-full bg-[#fbbf2422] px-2.5 py-1 text-[11px] font-semibold text-[#fbbf24]">
                          Мало · {s.quantity} (порог {alert.thresholdQuantity})
                        </span>
                      ) : s.quantity === 0 ? (
                        <span className="rounded-full bg-[#f8717122] px-2.5 py-1 text-[11px] font-semibold text-[#f87171]">
                          Нет в наличии
                        </span>
                      ) : (
                        <span className="rounded-full bg-[#34d39922] px-2.5 py-1 text-[11px] font-semibold text-[#34d399]">
                          {s.quantity}
                        </span>
                      )}
                    </td>
                    <td className="py-3 text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => {
                            setReceiptFor({ productId: s.productId, productName: name })
                            setReceiptQty(10)
                            setReceiptError('')
                          }}
                          className="inline-flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[11px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80"
                        >
                          <TruckIcon width={13} height={13} />
                          Приход
                        </button>
                        <button
                          onClick={() => {
                            setCostFor({ productId: s.productId, productName: name })
                            setCostAmount('')
                            setCostDone(false)
                            setCostError('')
                          }}
                          className="inline-flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-hover)] px-3 py-1.5 text-[11px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]"
                        >
                          Себестоимость
                        </button>
                      </div>
                    </td>
                  </tr>
                )
              })}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={3} className="py-12 text-center text-[color:var(--admin-text-tertiary)]">
                    {stock?.length === 0 ? 'На складе пока нет ни одной позиции' : 'Ничего не найдено'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Scan-to-identify, for a first-ever receipt of a product not yet in stock */}
      <AdminModal open={scanOpen} onClose={() => setScanOpen(false)} title="Приход по штрихкоду">
        <form onSubmit={handleScanSubmit} className="flex flex-col gap-4">
          <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">
            Отсканируйте или введите штрихкод товара, чтобы найти его и оприходовать поставку.
          </p>
          <input
            autoFocus
            value={scanBarcode}
            onChange={(e) => setScanBarcode(e.target.value)}
            placeholder="Штрихкод"
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
          {scanError && <div className="text-[12px] font-medium text-[#f87171]">{scanError}</div>}
          <button
            type="submit"
            disabled={scanBusy}
            className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
          >
            {scanBusy ? 'Ищем…' : 'Найти'}
          </button>
        </form>
      </AdminModal>

      <AdminModal open={!!receiptFor} onClose={() => setReceiptFor(null)} title="Оприходовать поставку">
        {receiptFor && (
          <div className="flex flex-col gap-4">
            <div className="flex items-center gap-3 rounded-xl bg-[color:var(--admin-hover)] p-3">
              <span className="grid h-10 w-10 place-items-center rounded-[10px] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]">
                <AlertIcon width={17} height={17} />
              </span>
              <div>
                <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">
                  {receiptFor.productName ?? `Товар #${receiptFor.productId}`}
                </div>
                <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">ID {receiptFor.productId}</div>
              </div>
            </div>

            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Количество к оприходованию</span>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setReceiptQty((q) => Math.max(1, q - 10))}
                  className="grid h-10 w-10 place-items-center rounded-xl bg-[color:var(--admin-hover)] text-[color:var(--admin-text)]"
                >
                  −10
                </button>
                <input
                  type="number"
                  value={receiptQty}
                  min={1}
                  onChange={(e) => setReceiptQty(Math.max(1, Number(e.target.value) || 1))}
                  className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-center text-[15px] font-bold text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <button
                  onClick={() => setReceiptQty((q) => q + 10)}
                  className="grid h-10 w-10 place-items-center rounded-xl bg-[color:var(--admin-hover)] text-[color:var(--admin-text)]"
                >
                  +10
                </button>
              </div>
            </label>

            {receiptError && <div className="text-[12px] font-medium text-[#f87171]">{receiptError}</div>}

            <button
              onClick={confirmReceipt}
              disabled={receiptBusy}
              className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
            >
              <PlusIcon width={16} height={16} />
              {receiptBusy ? 'Оприходуем…' : 'Оприходовать'}
            </button>
          </div>
        )}
      </AdminModal>

      <AdminModal open={!!costFor} onClose={() => setCostFor(null)} title="Установить себестоимость">
        {costFor && (
          <div className="flex flex-col gap-4">
            <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">
              {costFor.productName ?? `Товар #${costFor.productId}`}
            </div>
            <p className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
              Эндпоинт для чтения текущей себестоимости отсутствует в бэкенде — можно только задать новое значение.
            </p>
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Себестоимость, TJS</span>
              <input
                type="number"
                value={costAmount}
                onChange={(e) => setCostAmount(e.target.value)}
                min={0}
                step="0.01"
                placeholder="0.00"
                className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[15px] font-bold text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
            <button
              onClick={confirmCostPrice}
              disabled={costBusy || !costAmount}
              className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
            >
              {costDone ? 'Сохранено ✓' : costBusy ? 'Сохраняем…' : 'Сохранить'}
            </button>
            {costError && <div className="text-[12px] font-medium text-[#f87171]">{costError}</div>}
          </div>
        )}
      </AdminModal>

      <AdminModal open={ruleOpen} onClose={() => setRuleOpen(false)} title="Создать правило пополнения">
        <div className="flex flex-col gap-4">
          <p className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
            Когда остаток товара опустится ниже порога, магазин попадёт в список «Требуют пополнения» с
            рекомендованным количеством для заказа.
          </p>

          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">ID товара</span>
            <input
              type="number"
              min={1}
              value={ruleProductId}
              onChange={(e) => setRuleProductId(e.target.value)}
              className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
            />
          </label>

          <div className="grid grid-cols-2 gap-3">
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Порог (мин. остаток)</span>
              <input
                type="number"
                min={0}
                value={ruleThreshold}
                onChange={(e) => setRuleThreshold(e.target.value)}
                className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Кол-во пополнения</span>
              <input
                type="number"
                min={1}
                value={ruleReorderQty}
                onChange={(e) => setRuleReorderQty(e.target.value)}
                className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
          </div>

          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">ID предпочитаемого поставщика (необязательно)</span>
            <input
              type="number"
              min={1}
              value={ruleSupplierId}
              onChange={(e) => setRuleSupplierId(e.target.value)}
              className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
            />
          </label>

          {ruleError && <div className="text-[12px] font-medium text-[#f87171]">{ruleError}</div>}

          <button
            onClick={confirmCreateRule}
            disabled={ruleBusy || !ruleProductId}
            className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
          >
            <PlusIcon width={16} height={16} />
            {ruleBusy ? 'Создаём…' : 'Создать правило'}
          </button>
        </div>
      </AdminModal>
    </div>
  )
}
