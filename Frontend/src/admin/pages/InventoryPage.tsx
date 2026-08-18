import { useCallback, useEffect, useMemo, useState } from 'react'
import { Panel, RowDivider } from '../cabinet/components/primitives'
import { AdminModal } from '../components/AdminModal'
import { Select } from '../components/Select'
import { SupplierPicker } from '../components/SupplierPicker'
import { Badge } from '../components/Badge'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { errorMessage } from '../../lib/errorKind'
import { Reveal } from '../components/Reveal'
import { BarcodeScannerView } from '../components/BarcodeScannerView'
import { ProductPicker } from '../components/ProductPicker'
import { SearchIcon, PlusIcon, TruckIcon, BarcodeIcon, CameraIcon, PackageIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { useBarcodeScanner } from '../../hooks/useBarcodeScanner'
import {
  inventoryApi,
  productsApi,
  storesApi,
  catalogApi,
  pricingApi,
  ApiError,
  type StockLevel,
  type ReorderAlert,
  type Category,
  type Brand,
  type Supplier,
  type ProductSearchItem,
} from '../../lib/api'
import { createReorderRule } from '../../lib/api/reorderRules'

// Bumped key (was 'sarfkor-product-names', name-only) -- now caches barcode alongside the name so
// rows/modals can show a real, scan-verified identifier instead of the raw database id ("ID 7"),
// which a customer/cashier has no way to act on and shouldn't see (numeric ids are internal).
const NAME_CACHE_KEY = 'sarfkor-product-info'

interface ProductInfo {
  name: string
  barcode: string
}

function loadNameCache(): Record<number, ProductInfo> {
  try {
    return JSON.parse(localStorage.getItem(NAME_CACHE_KEY) ?? '{}')
  } catch {
    return {}
  }
}

function saveNameCache(cache: Record<number, ProductInfo>) {
  localStorage.setItem(NAME_CACHE_KEY, JSON.stringify(cache))
}

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface ReceiptTarget {
  productId: number
  productName?: string
  productBarcode?: string
}

export function InventoryPage() {
  const { storeId } = useAuth()
  const [nameCache, setNameCache] = useState<Record<number, ProductInfo>>(loadNameCache)
  const [stock, setStock] = useState<StockLevel[] | null>(null)
  const [alerts, setAlerts] = useState<ReorderAlert[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [query, setQuery] = useState('')

  const [scanOpen, setScanOpen] = useState(false)
  const [scanBarcode, setScanBarcode] = useState('')
  const [scanBusy, setScanBusy] = useState(false)
  const [scanError, setScanError] = useState('')
  const [notFoundBarcode, setNotFoundBarcode] = useState('')
  const [cameraOpen, setCameraOpen] = useState(false)

  const [categories, setCategories] = useState<Category[]>([])
  const [brands, setBrands] = useState<Brand[]>([])
  const [submitOpen, setSubmitOpen] = useState(false)
  const [submitName, setSubmitName] = useState('')
  const [submitCategoryId, setSubmitCategoryId] = useState('')
  const [submitBrandId, setSubmitBrandId] = useState('')
  const [submitCountry, setSubmitCountry] = useState('')
  const [submitBusy, setSubmitBusy] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [submitDone, setSubmitDone] = useState(false)

  const [newCategoryOpen, setNewCategoryOpen] = useState(false)
  const [newCategoryName, setNewCategoryName] = useState('')
  const [newCategoryBusy, setNewCategoryBusy] = useState(false)
  const [newCategoryError, setNewCategoryError] = useState('')

  const [newBrandOpen, setNewBrandOpen] = useState(false)
  const [newBrandName, setNewBrandName] = useState('')
  const [newBrandBusy, setNewBrandBusy] = useState(false)
  const [newBrandError, setNewBrandError] = useState('')

  const [receiptFor, setReceiptFor] = useState<ReceiptTarget | null>(null)
  const [receiptQty, setReceiptQty] = useState(10)
  // Optional: blank means "keep the current price" (the common restock case, where a price
  // already exists) — filled in, it's set in the same step as the quantity so a brand-new
  // product doesn't need a separate visit to become sellable.
  const [receiptPrice, setReceiptPrice] = useState('')
  const [receiptBusy, setReceiptBusy] = useState(false)
  const [receiptError, setReceiptError] = useState('')

  const [costFor, setCostFor] = useState<ReceiptTarget | null>(null)
  const [costAmount, setCostAmount] = useState('')
  const [costBusy, setCostBusy] = useState(false)
  const [costError, setCostError] = useState('')
  const [costDone, setCostDone] = useState(false)

  const [priceFor, setPriceFor] = useState<ReceiptTarget | null>(null)
  const [priceAmount, setPriceAmount] = useState('')
  const [priceBusy, setPriceBusy] = useState(false)
  const [priceError, setPriceError] = useState('')
  const [priceDone, setPriceDone] = useState(false)

  const [ruleOpen, setRuleOpen] = useState(false)
  const [ruleProduct, setRuleProduct] = useState<ProductSearchItem | null>(null)
  const [ruleThreshold, setRuleThreshold] = useState('5')
  const [ruleReorderQty, setRuleReorderQty] = useState('20')
  const [ruleSupplier, setRuleSupplier] = useState<Supplier | null>(null)
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

      // Batch-resolve any product names not yet in the local cache.
      const cached = loadNameCache()
      const missing = stockRes.filter((s) => !cached[s.productId]).map((s) => s.productId)
      if (missing.length > 0) {
        const results = await Promise.allSettled(missing.map((id) => productsApi.getProductById(id)))
        const updates: Record<number, ProductInfo> = {}
        results.forEach((r, i) => {
          if (r.status === 'fulfilled') updates[missing[i]] = { name: r.value.productName, barcode: r.value.barcode }
        })
        if (Object.keys(updates).length > 0) {
          setNameCache((c) => {
            const next = { ...c, ...updates }
            saveNameCache(next)
            return next
          })
        }
      }
    } catch (err) {
      console.error('Failed to load inventory:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить склад')
    } finally {
      setLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    catalogApi.getCategories().then((res) => setCategories(res.categories)).catch(() => {})
    catalogApi.getBrands().then((res) => setBrands(res.brands)).catch(() => {})
  }, [])

  const alertMap = useMemo(() => new Map(alerts.map((a) => [a.productId, a])), [alerts])

  const filtered = useMemo(() => {
    if (!stock) return []
    const q = query.trim().toLowerCase()
    if (!q) return stock
    return stock.filter((s) => {
      const info = nameCache[s.productId]
      return (info?.barcode ?? '').includes(q) || (info?.name.toLowerCase() ?? '').includes(q)
    })
  }, [stock, query, nameCache])

  const totalUnits = stock?.reduce((sum, s) => sum + s.quantity, 0) ?? 0

  function rememberName(productId: number, name: string, barcode: string) {
    setNameCache((c) => {
      const next = { ...c, [productId]: { name, barcode } }
      saveNameCache(next)
      return next
    })
  }

  function closeScanModal() {
    setScanOpen(false)
    setCameraOpen(false)
    scanner.stop()
  }

  // Shared by the manual-entry form and the camera scanner below -- both just need
  // to resolve a raw barcode string to "found, go record a receipt" or "not found,
  // offer to submit it as a new product."
  async function lookupBarcode(code: string) {
    if (!code || scanBusy) return
    setScanBusy(true)
    setScanError('')
    setNotFoundBarcode('')
    try {
      const result = await productsApi.scanBarcode(code)
      rememberName(result.productId, result.productName, code)
      closeScanModal()
      setScanBarcode('')
      setReceiptFor({ productId: result.productId, productName: result.productName, productBarcode: code })
      setReceiptQty(10)
      setReceiptPrice('')
      setReceiptError('')
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setScanError('Товар с таким штрихкодом не найден')
        setNotFoundBarcode(code)
      } else {
        console.error('Failed to look up barcode:', err)
        setScanError(errorMessage(err, 'Не удалось выполнить поиск'))
      }
    } finally {
      setScanBusy(false)
    }
  }

  async function handleScanSubmit(e: React.FormEvent) {
    e.preventDefault()
    await lookupBarcode(scanBarcode.trim())
  }

  // Single-shot: a warehouse receipt is one product at a time, so the camera stops
  // itself after the first hit (continuous defaults to false) -- matches ScanPage.
  const scanner = useBarcodeScanner({
    onDetect: (code) => {
      lookupBarcode(code)
    },
    beepOnDetect: true,
  })

  useEffect(() => {
    if (cameraOpen) {
      scanner.start()
    } else {
      scanner.stop()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cameraOpen])

  function openSubmitForm() {
    setScanOpen(false)
    setSubmitOpen(true)
    setSubmitName('')
    setSubmitCategoryId('')
    setSubmitBrandId('')
    setSubmitCountry('')
    setSubmitError('')
    setSubmitDone(false)
  }

  async function confirmSubmitNewProduct() {
    if (!notFoundBarcode || !submitName.trim() || !submitCategoryId || !submitBrandId || !submitCountry.trim() || submitBusy) return
    setSubmitBusy(true)
    setSubmitError('')
    try {
      const submitResult = await productsApi.submitNewProduct({
        barcode: notFoundBarcode,
        name: submitName.trim(),
        categoryId: Number(submitCategoryId),
        brandId: Number(submitBrandId),
        countryOfOrigin: submitCountry.trim(),
      })
      // Moderation is gone — every submission publishes a Product immediately, so productId is
      // always present on success.
      const newProductId: number | null = submitResult.productId
      setSubmitDone(true)
      setTimeout(() => {
        setSubmitOpen(false)
        setNotFoundBarcode('')
        setScanBarcode('')
        // Creating the catalog entry doesn't put any units on the shelf — a new product added
        // through "Приход по штрихкоду" needs its quantity recorded right away, the same as an
        // existing product found by scanBarcode does above, or it silently stays at zero stock
        // with no obvious next step (this was previously a dead end).
        if (newProductId) {
          rememberName(newProductId, submitName.trim(), notFoundBarcode)
          setReceiptFor({ productId: newProductId, productName: submitName.trim(), productBarcode: notFoundBarcode })
          setReceiptQty(10)
          setReceiptPrice('')
          setReceiptError('')
        }
      }, 1500)
    } catch (err) {
      console.error('Failed to submit new product:', err)
      setSubmitError(errorMessage(err, 'Не удалось отправить заявку'))
    } finally {
      setSubmitBusy(false)
    }
  }

  async function handleCreateCategory() {
    if (!newCategoryName.trim() || newCategoryBusy) return
    setNewCategoryBusy(true)
    setNewCategoryError('')
    try {
      const res = await catalogApi.createCategory(newCategoryName.trim())
      if (res.outcome === 'Created' && res.categoryId) {
        const created = { categoryId: res.categoryId, name: newCategoryName.trim(), parentCategoryId: null, displayOrder: 0, isHidden: false }
        setCategories((c) => [...c, created])
        setSubmitCategoryId(String(res.categoryId))
        setNewCategoryOpen(false)
        setNewCategoryName('')
      } else {
        setNewCategoryError('Категория не создана — попробуйте ещё раз')
      }
    } catch (err) {
      console.error('Failed to create category:', err)
      setNewCategoryError('Не удалось создать категорию')
    } finally {
      setNewCategoryBusy(false)
    }
  }

  async function handleCreateBrand() {
    if (!newBrandName.trim() || newBrandBusy) return
    setNewBrandBusy(true)
    setNewBrandError('')
    try {
      const res = await catalogApi.createBrand(newBrandName.trim())
      const created = { brandId: res.brandId, name: newBrandName.trim(), productCount: 0 }
      setBrands((b) => [...b, created])
      setSubmitBrandId(String(res.brandId))
      setNewBrandOpen(false)
      setNewBrandName('')
    } catch (err) {
      console.error('Failed to create brand:', err)
      setNewBrandError(errorMessage(err, 'Не удалось создать бренд'))
    } finally {
      setNewBrandBusy(false)
    }
  }

  async function confirmReceipt() {
    if (!receiptFor || !storeId || receiptBusy) return
    setReceiptBusy(true)
    setReceiptError('')
    try {
      await inventoryApi.recordStockReceipt(storeId, receiptFor.productId, receiptQty)
      // The receipt already succeeded at this point -- close the modal unconditionally so a
      // retry can never re-submit the same stock. Price is optional and secondary; if it fails,
      // warn separately instead of blocking on it.
      const priceAmount = Number(receiptPrice)
      if (receiptPrice.trim() && priceAmount > 0) {
        try {
          await pricingApi.submitPriceUpdate(receiptFor.productId, storeId, priceAmount, 'TJS')
        } catch (err) {
          console.error('Failed to save price after stock receipt:', err)
          window.alert('Партия оприходована, но цену сохранить не удалось: ошибка сервера')
        }
      }
      setReceiptFor(null)
      setReceiptQty(10)
      setReceiptPrice('')
      await load()
    } catch (err) {
      console.error('Failed to record stock receipt:', err)
      setReceiptError(errorMessage(err, 'Не удалось оприходовать поставку'))
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
      console.error('Failed to save cost price:', err)
      setCostError(errorMessage(err, 'Не удалось сохранить себестоимость'))
    } finally {
      setCostBusy(false)
    }
  }

  async function confirmSetPrice() {
    if (!priceFor || !storeId || priceBusy) return
    const amount = Number(priceAmount)
    if (!amount || amount <= 0) return
    setPriceBusy(true)
    setPriceError('')
    try {
      await pricingApi.submitPriceUpdate(priceFor.productId, storeId, amount, 'TJS')
      setPriceDone(true)
      setTimeout(() => {
        setPriceFor(null)
        setPriceAmount('')
        setPriceDone(false)
      }, 1200)
    } catch (err) {
      console.error('Failed to save price:', err)
      setPriceError(errorMessage(err, 'Не удалось сохранить цену'))
    } finally {
      setPriceBusy(false)
    }
  }

  async function confirmCreateRule() {
    if (!storeId || ruleBusy) return
    const productId = ruleProduct?.productId
    const thresholdQuantity = Number(ruleThreshold)
    const reorderQuantity = Number(ruleReorderQty)
    if (!productId || !thresholdQuantity || thresholdQuantity < 0 || !reorderQuantity || reorderQuantity <= 0) {
      setRuleError('Проверьте товар, порог и количество пополнения')
      return
    }
    const preferredSupplierId = ruleSupplier?.supplierId
    setRuleBusy(true)
    setRuleError('')
    try {
      const result = await createReorderRule(storeId, productId, thresholdQuantity, reorderQuantity, preferredSupplierId)
      if (result.outcome !== 'Created') {
        setRuleError(result.outcome === 'Forbidden' ? 'Нет доступа к этому магазину' : 'Магазин не найден')
        return
      }
      setRuleOpen(false)
      setRuleProduct(null)
      setRuleThreshold('5')
      setRuleReorderQty('20')
      setRuleSupplier(null)
      await load()
    } catch (err) {
      console.error('Failed to create reorder rule:', err)
      setRuleError(errorMessage(err, 'Не удалось создать правило пополнения'))
    } finally {
      setRuleBusy(false)
    }
  }

  if (loading) {
    return <Loading label="Загружаем склад…" />
  }

  if (error) {
    return (
      <Panel>
        <ErrorState message={error} kind={errorKind} onRetry={load} />
      </Panel>
    )
  }

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <Reveal className="grid grid-cols-1 gap-5 sm:grid-cols-3">
        <Panel className="p-6">
          <div className="text-[32px] font-[500] tabular-nums text-[color:var(--admin-text)]">{stock?.length ?? 0}</div>
          <div className="mt-1.5 text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">Позиций на складе</div>
        </Panel>
        <Panel className="p-6">
          <div className="flex items-start justify-between gap-2">
            <div>
              <div className="text-[32px] font-[500] tabular-nums text-[color:var(--admin-text)]">{alerts.length}</div>
              <div className="mt-1.5 text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">Требуют пополнения</div>
            </div>
            <button
              onClick={() => { setRuleOpen(true); setRuleError('') }}
              className="mt-1 shrink-0 rounded-[6px] bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[12px] font-[500] text-[color:var(--admin-accent)] transition-opacity hover:opacity-80"
            >
              + Правило
            </button>
          </div>
        </Panel>
        <Panel className="p-6">
          <div className="text-[32px] font-[500] tabular-nums text-[color:var(--admin-text)]">{fmt(totalUnits)}</div>
          <div className="mt-1.5 text-[12px] font-[400] text-[color:var(--admin-text-tertiary)]">Всего единиц</div>
        </Panel>
      </Reveal>

      <Reveal i={1}>
      <Panel className="p-5">
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <SearchIcon width={16} height={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)]" />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Поиск по штрихкоду или названию (если уже распознано)"
              className="w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] py-2.5 pl-9 pr-3 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none transition-colors placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]"
            />
          </div>
          <button
            onClick={() => {
              setScanOpen(true)
              setScanError('')
            }}
            className="flex shrink-0 items-center justify-center gap-2 rounded-[8px] bg-[color:var(--admin-accent)] px-5 py-[10px] text-[14px] font-[500] text-[color:var(--admin-accent-fg)] transition-all duration-150 ease-out hover:scale-[1.01] hover:shadow-[0_4px_16px_rgba(0,0,0,0.18)]"
          >
            <BarcodeIcon width={15} height={15} />
            Приход по штрихкоду
          </button>
        </div>

        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Отсканируйте штрихкод товара через кассу, чтобы увидеть его название здесь.
        </p>

        <div className="flex flex-col">
          {filtered.map((s, i) => {
            const alert = alertMap.get(s.productId)
            const info = nameCache[s.productId]
            return (
              <div key={s.productId}>
                {i > 0 && <RowDivider />}
                <div className="flex flex-col gap-3 py-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="min-w-0">
                    <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">{info?.name ?? 'Товар без названия'}</div>
                    {/* Barcode, never the internal numeric id -- a cashier/owner can act on a
                        barcode (scan it, quote it to a supplier); the database id means nothing
                        to them. Omitted entirely, not "ID —", when even that isn't known yet. */}
                    {info?.barcode && (
                      <div className="mt-0.5 font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--admin-text-tertiary)]">{info.barcode}</div>
                    )}
                  </div>
                  <div className="flex shrink-0 flex-wrap items-center gap-2">
                    {alert ? (
                      <Badge variant="warning">Мало · {s.quantity} (порог {alert.thresholdQuantity})</Badge>
                    ) : s.quantity === 0 ? (
                      <Badge variant="danger">Нет в наличии</Badge>
                    ) : (
                      <Badge variant="success">{s.quantity}</Badge>
                    )}
                    <button
                      onClick={() => { setReceiptFor({ productId: s.productId, productName: info?.name, productBarcode: info?.barcode }); setReceiptQty(10); setReceiptPrice(''); setReceiptError('') }}
                      className="inline-flex items-center gap-1.5 rounded-[6px] bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[12px] font-[500] text-[color:var(--admin-accent)] transition-opacity hover:opacity-80"
                    >
                      <TruckIcon width={12} height={12} />
                      Приход
                    </button>
                    <button
                      onClick={() => { setPriceFor({ productId: s.productId, productName: info?.name, productBarcode: info?.barcode }); setPriceAmount(''); setPriceDone(false); setPriceError('') }}
                      className="inline-flex items-center gap-1.5 rounded-[6px] border border-[color:var(--admin-border)] px-3 py-1.5 text-[12px] font-[400] text-[color:var(--admin-text-secondary)] transition-colors hover:text-[color:var(--admin-text)]"
                    >
                      Цена
                    </button>
                    <button
                      onClick={() => { setCostFor({ productId: s.productId, productName: info?.name, productBarcode: info?.barcode }); setCostAmount(''); setCostDone(false); setCostError('') }}
                      className="inline-flex items-center gap-1.5 rounded-[6px] border border-[color:var(--admin-border)] px-3 py-1.5 text-[12px] font-[400] text-[color:var(--admin-text-secondary)] transition-colors hover:text-[color:var(--admin-text)]"
                    >
                      Себест.
                    </button>
                  </div>
                </div>
              </div>
            )
          })}
          {filtered.length === 0 && (
            <div className="py-12 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
              {stock?.length === 0 ? 'На складе пока нет ни одной позиции' : 'Ничего не найдено'}
            </div>
          )}
        </div>
      </Panel>
      </Reveal>

      {/* Scan-to-identify, for a first-ever receipt of a product not yet in stock */}
      <AdminModal open={scanOpen} onClose={closeScanModal} title="Приход по штрихкоду">
        <form onSubmit={handleScanSubmit} className="flex flex-col gap-4">
          <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">
            Отсканируйте или введите штрихкод товара, чтобы найти его и оприходовать поставку.
          </p>

          <button
            type="button"
            onClick={() => setCameraOpen((v) => !v)}
            className="flex items-center justify-center gap-2 rounded-[8px] bg-[color:var(--admin-hover)] py-2.5 text-[13px] font-[500] text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]"
          >
            <CameraIcon width={15} height={15} />
            {cameraOpen ? 'Скрыть камеру' : 'Сканировать камерой'}
          </button>

          {cameraOpen && (
            <BarcodeScannerView
              videoRef={scanner.videoRef}
              phase={scanner.phase}
              onStart={scanner.start}
              justDetected={scanner.justDetected}
              devices={scanner.devices}
              selectedDeviceId={scanner.selectedDeviceId}
              onSelectDevice={scanner.selectDevice}
            />
          )}

          <input
            autoFocus
            value={scanBarcode}
            onChange={(e) => setScanBarcode(e.target.value)}
            placeholder="Штрихкод"
            className="w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
          {scanError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{scanError}</div>}
          {notFoundBarcode && (
            <button
              type="button"
              onClick={openSubmitForm}
              className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-hover)] py-2.5 text-[13px] font-semibold text-[color:var(--admin-text)] hover:bg-[color:var(--admin-border)]"
            >
              <PlusIcon width={14} height={14} />
              Подать заявку на новый товар
            </button>
          )}
          <button
            type="submit"
            disabled={scanBusy}
            className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
          >
            {scanBusy ? 'Ищем…' : 'Найти'}
          </button>
        </form>
      </AdminModal>

      <AdminModal open={submitOpen} onClose={() => setSubmitOpen(false)} title="Новый товар">
        <div className="flex flex-col gap-4">
          <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">
            Штрихкод {notFoundBarcode} появится в каталоге сразу — модерация Admin не требуется.
          </p>
          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Название товара</span>
            <input
              value={submitName}
              onChange={(e) => setSubmitName(e.target.value)}
              placeholder="Например, Coca-Cola 0.5л"
              className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
            />
          </label>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <div className="flex items-center justify-between">
                <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Категория</span>
                <button
                  type="button"
                  onClick={() => setNewCategoryOpen((v) => !v)}
                  className="text-[11px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80"
                >
                  {newCategoryOpen ? 'Отмена' : '+ Создать'}
                </button>
              </div>
              {newCategoryOpen ? (
                <div className="flex gap-1.5">
                  <input
                    value={newCategoryName}
                    onChange={(e) => setNewCategoryName(e.target.value)}
                    placeholder="Название категории"
                    className="w-full min-w-0 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
                  />
                  <button
                    type="button"
                    onClick={handleCreateCategory}
                    disabled={newCategoryBusy || !newCategoryName.trim()}
                    className="shrink-0 rounded-xl bg-[color:var(--admin-accent)] px-3 py-2.5 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] disabled:opacity-50"
                  >
                    {newCategoryBusy ? '…' : 'OK'}
                  </button>
                </div>
              ) : (
                <Select
                  value={submitCategoryId}
                  onChange={setSubmitCategoryId}
                  options={categories.map((c) => ({ value: String(c.categoryId), label: c.name }))}
                />
              )}
              {newCategoryError && <span className="text-[11px] text-[color:var(--admin-danger)]">{newCategoryError}</span>}
            </div>
            <div className="flex flex-col gap-1.5">
              <div className="flex items-center justify-between">
                <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Бренд</span>
                <button
                  type="button"
                  onClick={() => setNewBrandOpen((v) => !v)}
                  className="text-[11px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80"
                >
                  {newBrandOpen ? 'Отмена' : '+ Создать'}
                </button>
              </div>
              {newBrandOpen ? (
                <div className="flex gap-1.5">
                  <input
                    value={newBrandName}
                    onChange={(e) => setNewBrandName(e.target.value)}
                    placeholder="Название бренда"
                    className="w-full min-w-0 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
                  />
                  <button
                    type="button"
                    onClick={handleCreateBrand}
                    disabled={newBrandBusy || !newBrandName.trim()}
                    className="shrink-0 rounded-xl bg-[color:var(--admin-accent)] px-3 py-2.5 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] disabled:opacity-50"
                  >
                    {newBrandBusy ? '…' : 'OK'}
                  </button>
                </div>
              ) : (
                <Select
                  value={submitBrandId}
                  onChange={setSubmitBrandId}
                  options={brands.map((b) => ({ value: String(b.brandId), label: b.name }))}
                />
              )}
              {newBrandError && <span className="text-[11px] text-[color:var(--admin-danger)]">{newBrandError}</span>}
            </div>
          </div>
          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Страна производства</span>
            <input
              value={submitCountry}
              onChange={(e) => setSubmitCountry(e.target.value)}
              placeholder="Например, TJ"
              className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
            />
          </label>
          {submitError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{submitError}</div>}
          <button
            onClick={confirmSubmitNewProduct}
            disabled={submitBusy || !submitName.trim() || !submitCategoryId || !submitBrandId || !submitCountry.trim()}
            className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
          >
            {submitDone ? 'Добавлено ✓' : submitBusy ? 'Добавляем…' : 'Добавить товар'}
          </button>
        </div>
      </AdminModal>

      <AdminModal open={!!receiptFor} onClose={() => setReceiptFor(null)} title="Оприходовать поставку">
        {receiptFor && (
          <div className="flex flex-col gap-4">
            <div className="flex items-center gap-3 rounded-xl bg-[color:var(--admin-hover)] p-3">
              {/* PackageIcon (no photo yet), not AlertIcon -- a triangle-and-exclamation mark next
                  to a product name reads as "something's wrong with this item", not "no photo". */}
              <span className="grid h-10 w-10 place-items-center rounded-[10px] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]">
                <PackageIcon width={17} height={17} />
              </span>
              <div>
                <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">
                  {receiptFor.productName ?? 'Товар без названия'}
                </div>
                {receiptFor.productBarcode && (
                  <div className="font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--admin-text-tertiary)]">{receiptFor.productBarcode}</div>
                )}
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

            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">
                Цена продажи, TJS <span className="font-normal text-[color:var(--admin-text-tertiary)]">(необязательно)</span>
              </span>
              <input
                type="number"
                value={receiptPrice}
                onChange={(e) => setReceiptPrice(e.target.value)}
                min={0}
                step="0.01"
                placeholder="Оставьте пустым, чтобы не менять"
                className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[15px] font-bold text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>

            {receiptError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{receiptError}</div>}

            <button
              onClick={confirmReceipt}
              disabled={receiptBusy}
              className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
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
            <div>
              <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">
                {costFor.productName ?? 'Товар без названия'}
              </div>
              {costFor.productBarcode && (
                <div className="mt-0.5 font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--admin-text-tertiary)]">{costFor.productBarcode}</div>
              )}
            </div>
            <p className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
              Текущая себестоимость нигде не показывается — можно только задать новое значение.
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
              className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
            >
              {costDone ? 'Сохранено ✓' : costBusy ? 'Сохраняем…' : 'Сохранить'}
            </button>
            {costError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{costError}</div>}
          </div>
        )}
      </AdminModal>

      <AdminModal open={!!priceFor} onClose={() => setPriceFor(null)} title="Установить цену продажи">
        {priceFor && (
          <div className="flex flex-col gap-4">
            <div>
              <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">
                {priceFor.productName ?? 'Товар без названия'}
              </div>
              {priceFor.productBarcode && (
                <div className="mt-0.5 font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--admin-text-tertiary)]">{priceFor.productBarcode}</div>
              )}
            </div>
            <p className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
              Эта цена — то, что видит касса при сканировании и что сравнивается с другими магазинами. Отличается от
              себестоимости (внутренней закупочной цены).
            </p>
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Цена продажи, TJS</span>
              <input
                type="number"
                value={priceAmount}
                onChange={(e) => setPriceAmount(e.target.value)}
                min={0}
                step="0.01"
                placeholder="0.00"
                className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[15px] font-bold text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
            <button
              onClick={confirmSetPrice}
              disabled={priceBusy || !priceAmount}
              className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
            >
              {priceDone ? 'Сохранено ✓' : priceBusy ? 'Сохраняем…' : 'Сохранить'}
            </button>
            {priceError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{priceError}</div>}
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
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Товар</span>
            <ProductPicker value={ruleProduct} onChange={setRuleProduct} storeId={storeId ?? undefined} scheme="admin" scanEnabled />
          </label>

          <div className="grid grid-cols-2 gap-3">
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Порог (мин. остаток)</span>
              <input
                type="number"
                min={0}
                value={ruleThreshold}
                onChange={(e) => setRuleThreshold(e.target.value)}
                className="w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Кол-во пополнения</span>
              <input
                type="number"
                min={1}
                value={ruleReorderQty}
                onChange={(e) => setRuleReorderQty(e.target.value)}
                className="w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
          </div>

          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Предпочитаемый поставщик (необязательно)</span>
            <SupplierPicker
              storeId={storeId!}
              value={ruleSupplier}
              onChange={setRuleSupplier}
              placeholder="Без предпочтения"
            />
          </label>

          {ruleError && <div className="text-[12px] font-medium text-[color:var(--admin-danger)]">{ruleError}</div>}

          <button
            onClick={confirmCreateRule}
            disabled={ruleBusy || !ruleProduct}
            className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
          >
            <PlusIcon width={16} height={16} />
            {ruleBusy ? 'Создаём…' : 'Создать правило'}
          </button>
        </div>
      </AdminModal>
    </div>
  )
}
