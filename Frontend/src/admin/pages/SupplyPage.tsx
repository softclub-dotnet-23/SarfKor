import { useCallback, useEffect, useState, type SVGProps } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Panel } from '../cabinet/components/primitives'
import { SectionSelect } from '../components/SectionSelect'
import { Loading } from '../components/Loading'
import { AddButton } from '../components/Button'
import { FormModal, FormField } from '../components/FormModal'
import { ProductPicker } from '../components/ProductPicker'
import { SupplierPicker } from '../components/SupplierPicker'
import { StorePicker } from '../components/StorePicker'
import { TruckIcon, PlusIcon, TrashIcon, RefreshIcon, PhoneIcon, MailIcon, AlertIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { productsApi, type ProductSearchItem, type MyStoreSearchItem } from '../../lib/api'
import { describeError } from '../../lib/errorKind'
import { useT } from '../../i18n/translations'
import { useSubscriptionGate, gateButtonProps } from '../../lib/subscriptionGate'
import { createSupplier, getSuppliers, type Supplier } from '../../lib/api/suppliers'
import {
  createPurchaseOrder,
  submitPurchaseOrder,
  receivePurchaseOrder,
  getPurchaseOrders,
  type PurchaseOrder,
  type PurchaseOrderLine,
} from '../../lib/api/purchaseOrders'
import {
  initiateStockTransfer,
  completeStockTransfer,
  getStockTransfers,
  type StockTransfer,
} from '../../lib/api/stockTransfers'

/* ---------- small icons not present in the shared admin icon set ---------- */

function DocumentIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={18} height={18} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z" />
      <path d="M14 2v6h6" />
      <path d="M9 13h6M9 17h6" />
    </svg>
  )
}

function SwapIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={18} height={18} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <path d="m17 2 4 4-4 4" />
      <path d="M3 11V9a4 4 0 0 1 4-4h14" />
      <path d="m7 22-4-4 4-4" />
      <path d="M21 13v2a4 4 0 0 1-4 4H3" />
    </svg>
  )
}

/* ---------- shared helpers ---------- */

function fmtDateTime(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
}

function describeSubmitOutcome(outcome: 'NotFound' | 'Forbidden' | 'NotDraft' | 'Submitted'): string {
  switch (outcome) {
    case 'NotFound':
      return 'Заказ не найден'
    case 'Forbidden':
      return 'Нет доступа к этому заказу'
    case 'NotDraft':
      return 'Заказ уже отправлен или обработан'
    default:
      return 'Не удалось отправить заказ поставщику'
  }
}

function describeReceiveOutcome(outcome: 'NotFound' | 'Forbidden' | 'NotSubmitted' | 'Received'): string {
  switch (outcome) {
    case 'NotFound':
      return 'Заказ не найден'
    case 'Forbidden':
      return 'Нет доступа к этому заказу'
    case 'NotSubmitted':
      return 'Заказ ещё не отправлен поставщику'
    default:
      return 'Не удалось оприходовать заказ'
  }
}

function describeInitiateTransferOutcome(
  outcome: 'FromStoreNotFound' | 'ToStoreNotFound' | 'Forbidden' | 'InsufficientStock' | 'Initiated',
): string {
  switch (outcome) {
    case 'FromStoreNotFound':
      return 'Магазин-источник не найден'
    case 'ToStoreNotFound':
      return 'Магазин назначения не найден'
    case 'Forbidden':
      return 'Нет доступа к этому магазину'
    case 'InsufficientStock':
      return 'Недостаточно товара на складе-источнике'
    default:
      return 'Не удалось инициировать перемещение'
  }
}

function describeCompleteTransferOutcome(outcome: 'NotFound' | 'Forbidden' | 'NotInTransit' | 'Completed'): string {
  switch (outcome) {
    case 'NotFound':
      return 'Перемещение не найдено'
    case 'Forbidden':
      return 'Нет доступа к этому перемещению'
    case 'NotInTransit':
      return 'Перемещение не находится в пути'
    default:
      return 'Не удалось завершить перемещение'
  }
}

const PO_STATUS_LABEL: Record<PurchaseOrder['status'], string> = {
  Draft: 'Черновик',
  Submitted: 'Отправлен',
  Received: 'Оприходован',
  Cancelled: 'Отменён',
}

const PO_STATUS_STYLE: Record<PurchaseOrder['status'], string> = {
  Draft: 'bg-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)]',
  Submitted: 'bg-[color:var(--admin-warning-dim)] text-[color:var(--admin-warning)]',
  Received: 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]',
  Cancelled: 'bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)]',
}

const TRANSFER_STATUS_LABEL: Record<StockTransfer['status'], string> = {
  Pending: 'Ожидает',
  InTransit: 'В пути',
  Completed: 'Завершено',
  Cancelled: 'Отменено',
}

const TRANSFER_STATUS_STYLE: Record<StockTransfer['status'], string> = {
  Pending: 'bg-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)]',
  InTransit: 'bg-[color:var(--admin-warning-dim)] text-[color:var(--admin-warning)]',
  Completed: 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]',
  Cancelled: 'bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)]',
}

function StatusBadge({ label, className }: { label: string; className: string }) {
  return <span className={`shrink-0 rounded-full px-2.5 py-1 text-[11px] font-semibold ${className}`}>{label}</span>
}

function FieldError({ message }: { message: string }) {
  if (!message) return null
  return (
    <div className="flex items-center gap-1.5 text-[12px] font-medium text-[color:var(--admin-danger)]">
      <AlertIcon width={13} height={13} className="shrink-0" />
      {message}
    </div>
  )
}

const inputClass =
  'w-full rounded-[8px] border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[14px] font-[400] text-[color:var(--admin-text)] outline-none transition-colors placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)]'

/* ---------- Suppliers ---------- */

function CreateSupplierModal({ open, onClose, storeId, onCreated }: { open: boolean; onClose: () => void; storeId: number; onCreated: () => Promise<void> }) {
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [nameError, setNameError] = useState('')

  function handleClose() {
    setName('')
    setPhone('')
    setEmail('')
    setNameError('')
    onClose()
  }

  async function submit() {
    if (!name.trim()) {
      setNameError('Укажите название')
      throw new Error('Укажите название')
    }
    await createSupplier(storeId, name.trim(), phone.trim() || undefined, email.trim() || undefined)
    await onCreated()
    handleClose()
  }

  return (
    <FormModal open={open} onClose={handleClose} title="Новый поставщик" isDirty={!!(name || phone || email)} onSubmit={submit} submitLabel="Добавить поставщика" scheme="admin">
      <FormField label="Название" required error={nameError} scheme="admin">
        <input value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
      </FormField>
      <FormField label="Телефон" scheme="admin">
        <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="Необязательно" className={inputClass} />
      </FormField>
      <FormField label="Email" scheme="admin">
        <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Необязательно" type="email" className={inputClass} />
      </FormField>
    </FormModal>
  )
}

function SuppliersSection({
  storeId,
  suppliers,
  loading,
  error,
  load,
  createOpen,
  onCloseCreate,
}: {
  storeId: number
  suppliers: Supplier[]
  loading: boolean
  error: string
  load: () => Promise<void>
  createOpen: boolean
  onCloseCreate: () => void
}) {
  return (
    <div className="flex flex-col gap-5">
      <Panel className="p-5">
        <div className="mb-4 flex items-center justify-between">
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Поставщики</span>
          <button
            onClick={load}
            aria-label="Обновить"
            className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]"
          >
            <RefreshIcon width={15} height={15} />
          </button>
        </div>

        {loading && <Loading />}

        {!loading && error && (
          <div className="py-6 text-center">
            <p className="mb-3 text-[13px] text-[color:var(--admin-text-secondary)]">{error}</p>
            <button onClick={load} className="rounded-xl bg-[color:var(--admin-accent)] px-4 py-2 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90">
              Повторить
            </button>
          </div>
        )}

        {!loading && !error && (
          <div className="flex flex-col gap-2">
            {suppliers.map((s) => (
              <div key={s.supplierId} className="flex flex-col gap-2 rounded-[14px] bg-[color:var(--admin-hover)] p-3.5 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">{s.name}</div>
                  {(s.contactPhone || s.contactEmail) && (
                    <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">{s.contactPhone || s.contactEmail}</div>
                  )}
                </div>
                <div className="flex flex-col gap-1 text-[12px] text-[color:var(--admin-text-secondary)] sm:items-end">
                  {s.contactPhone && (
                    <span className="flex items-center gap-1.5">
                      <PhoneIcon width={12} height={12} />
                      {s.contactPhone}
                    </span>
                  )}
                  {s.contactEmail && (
                    <span className="flex items-center gap-1.5">
                      <MailIcon width={12} height={12} />
                      {s.contactEmail}
                    </span>
                  )}
                </div>
              </div>
            ))}
            {suppliers.length === 0 && (
              <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">Поставщиков пока нет</div>
            )}
          </div>
        )}
      </Panel>

      <CreateSupplierModal open={createOpen} onClose={onCloseCreate} storeId={storeId} onCreated={load} />
    </div>
  )
}

/* ---------- Purchase orders ---------- */

interface DraftLine {
  product: ProductSearchItem | null
  quantity: string
  unitCost: string
  currency: string
}

function emptyLine(): DraftLine {
  return { product: null, quantity: '1', unitCost: '', currency: 'TJS' }
}

function CreateOrderModal({ open, onClose, storeId, suppliers, onCreated }: { open: boolean; onClose: () => void; storeId: number; suppliers: Supplier[]; onCreated: () => Promise<void> }) {
  const [supplier, setSupplier] = useState<Supplier | null>(null)
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()])
  const [linesError, setLinesError] = useState('')

  function updateLine(i: number, patch: Partial<DraftLine>) {
    setLines((ls) => ls.map((l, idx) => (idx === i ? { ...l, ...patch } : l)))
  }
  function addLine() {
    setLines((ls) => [...ls, emptyLine()])
  }
  function removeLine(i: number) {
    setLines((ls) => (ls.length > 1 ? ls.filter((_, idx) => idx !== i) : ls))
  }

  function handleClose() {
    setSupplier(null)
    setLines([emptyLine()])
    setLinesError('')
    onClose()
  }

  async function submit() {
    if (!supplier) throw new Error('Выберите поставщика')

    const parsedLines: PurchaseOrderLine[] = []
    for (const l of lines) {
      const productId = l.product?.productId
      const quantity = Number(l.quantity)
      const unitCost = Number(l.unitCost)
      if (!productId || !quantity || quantity <= 0 || Number.isNaN(unitCost) || unitCost < 0 || !l.currency.trim()) {
        setLinesError('Проверьте строки заказа — товар, количество и цена обязательны')
        throw new Error('Проверьте строки заказа')
      }
      parsedLines.push({ productId, quantity, unitCost, currency: l.currency.trim() })
    }
    setLinesError('')

    const result = await createPurchaseOrder(storeId, supplier.supplierId, parsedLines)
    if (result.outcome !== 'Created') {
      throw new Error(result.outcome === 'Forbidden' ? 'Нет доступа к этому магазину' : 'Магазин не найден')
    }
    await onCreated()
    handleClose()
  }

  return (
    <FormModal open={open} onClose={handleClose} title="Новый заказ поставщику" isDirty={!!supplier} onSubmit={submit} submitLabel="Создать заказ" scheme="admin" size="lg">
      {suppliers.length === 0 ? (
        <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">
          Сначала добавьте хотя бы одного поставщика на вкладке «Поставщики».
        </p>
      ) : (
        <>
          <FormField label="Поставщик" required scheme="admin">
            <SupplierPicker storeId={storeId} value={supplier} onChange={setSupplier} scheme="admin" />
          </FormField>

          <FormField label="Позиции" error={linesError} scheme="admin">
            <div className="flex flex-col gap-2">
              {lines.map((line, i) => (
                <div key={i} className="grid grid-cols-2 gap-2 sm:grid-cols-[1fr_1fr_1fr_90px_auto]">
                  <ProductPicker
                    className="col-span-2 sm:col-span-1"
                    value={line.product}
                    onChange={(p) => updateLine(i, { product: p })}
                    storeId={storeId}
                    scheme="admin"
                  />
                  <input
                    value={line.quantity}
                    onChange={(e) => updateLine(i, { quantity: e.target.value })}
                    placeholder="Кол-во"
                    type="number"
                    min={1}
                    className={inputClass}
                  />
                  <input
                    value={line.unitCost}
                    onChange={(e) => updateLine(i, { unitCost: e.target.value })}
                    placeholder="Цена за ед."
                    type="number"
                    min={0}
                    step="0.01"
                    className={inputClass}
                  />
                  <input
                    value={line.currency}
                    onChange={(e) => updateLine(i, { currency: e.target.value })}
                    placeholder="Валюта"
                    className={inputClass}
                  />
                  <button
                    type="button"
                    onClick={() => removeLine(i)}
                    disabled={lines.length === 1}
                    aria-label="Удалить строку"
                    className="grid h-full min-h-[42px] w-full place-items-center rounded-xl bg-[color:var(--admin-hover)] text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-danger)] disabled:opacity-40 sm:w-10"
                  >
                    <TrashIcon width={14} height={14} />
                  </button>
                </div>
              ))}
            </div>
            <button
              type="button"
              onClick={addLine}
              className="mt-2 flex w-fit items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[12px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80"
            >
              <PlusIcon width={13} height={13} />
              Добавить позицию
            </button>
          </FormField>
        </>
      )}
    </FormModal>
  )
}

function OrdersSection({
  storeId,
  suppliers,
  createOpen,
  onCloseCreate,
}: {
  storeId: number
  suppliers: Supplier[]
  createOpen: boolean
  onCloseCreate: () => void
}) {
  const t = useT()
  const gate = useSubscriptionGate()
  const blockedTitle = gate.isOwner ? t('common.errorSubscriptionInactiveOwner') : t('common.errorSubscriptionInactiveCashier')
  const [orders, setOrders] = useState<PurchaseOrder[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [busyId, setBusyId] = useState<number | null>(null)
  const [rowError, setRowError] = useState<{ id: number; message: string } | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getPurchaseOrders(storeId)
      if (res.outcome !== 'Found') {
        setError(res.outcome === 'Forbidden' ? 'Нет доступа к этому магазину' : 'Магазин не найден')
        setOrders([])
        return
      }
      setOrders(res.orders ?? [])
    } catch (err) {
      console.error('Failed to load purchase orders:', err)
      setError(describeError(err, t, { isOwner: gate.isOwner }))
    } finally {
      setLoading(false)
    }
  }, [storeId, t, gate.isOwner])

  useEffect(() => {
    load()
  }, [load])

  async function handleSubmitOrder(id: number) {
    setBusyId(id)
    setRowError(null)
    try {
      const result = await submitPurchaseOrder(id)
      if (result.outcome !== 'Submitted') {
        setRowError({ id, message: describeSubmitOutcome(result.outcome) })
        return
      }
      await load()
    } catch (err) {
      console.error('Failed to submit purchase order:', err)
      setRowError({ id, message: describeError(err, t, { isOwner: gate.isOwner }) })
    } finally {
      setBusyId(null)
    }
  }

  async function handleReceiveOrder(id: number) {
    setBusyId(id)
    setRowError(null)
    try {
      const result = await receivePurchaseOrder(id)
      if (result.outcome !== 'Received') {
        setRowError({ id, message: describeReceiveOutcome(result.outcome) })
        return
      }
      await load()
    } catch (err) {
      console.error('Failed to receive purchase order:', err)
      setRowError({ id, message: describeError(err, t, { isOwner: gate.isOwner }) })
    } finally {
      setBusyId(null)
    }
  }

  const supplierNameById = new Map(suppliers.map((s) => [s.supplierId, s.name]))

  return (
    <div className="flex flex-col gap-5">
      <Panel className="p-5">
        <div className="mb-4 flex items-center justify-between">
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Заказы поставщикам</span>
          <button
            onClick={load}
            aria-label="Обновить"
            className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]"
          >
            <RefreshIcon width={15} height={15} />
          </button>
        </div>

        {loading && <Loading />}

        {!loading && error && (
          <div className="py-6 text-center">
            <p className="mb-3 text-[13px] text-[color:var(--admin-text-secondary)]">{error}</p>
            <button onClick={load} className="rounded-xl bg-[color:var(--admin-accent)] px-4 py-2 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90">
              Повторить
            </button>
          </div>
        )}

        {!loading && !error && (
          <div className="flex flex-col gap-3">
            {(orders ?? []).map((o) => (
              <div key={o.purchaseOrderId} className="flex flex-col gap-2.5 rounded-[16px] bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">
                    {supplierNameById.get(o.supplierId) ?? 'Поставщик удалён'}
                  </div>
                  <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                    Создан {fmtDateTime(o.createdAt)}
                    {o.receivedAt ? ` · оприходован ${fmtDateTime(o.receivedAt)}` : ''}
                  </div>
                  {rowError?.id === o.purchaseOrderId && (
                    <div className="mt-1.5">
                      <FieldError message={rowError.message} />
                    </div>
                  )}
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <StatusBadge label={PO_STATUS_LABEL[o.status]} className={PO_STATUS_STYLE[o.status]} />
                  {o.status === 'Draft' && (
                    <button
                      onClick={() => handleSubmitOrder(o.purchaseOrderId)}
                      disabled={busyId === o.purchaseOrderId || (!gate.loading && !gate.isOperational)}
                      title={!gate.loading && !gate.isOperational ? blockedTitle : undefined}
                      className="rounded-lg bg-[color:var(--admin-accent-soft)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80 disabled:opacity-50"
                    >
                      {busyId === o.purchaseOrderId ? 'Отправляем…' : 'Отправить поставщику'}
                    </button>
                  )}
                  {o.status === 'Submitted' && (
                    <button
                      onClick={() => handleReceiveOrder(o.purchaseOrderId)}
                      disabled={busyId === o.purchaseOrderId || (!gate.loading && !gate.isOperational)}
                      title={!gate.loading && !gate.isOperational ? blockedTitle : undefined}
                      className="rounded-lg bg-[color:var(--admin-accent)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90 disabled:opacity-50"
                    >
                      {busyId === o.purchaseOrderId ? 'Оприходуем…' : 'Оприходовать'}
                    </button>
                  )}
                </div>
              </div>
            ))}
            {(orders ?? []).length === 0 && (
              <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">Заказов поставщикам пока нет</div>
            )}
          </div>
        )}
      </Panel>

      <CreateOrderModal open={createOpen} onClose={onCloseCreate} storeId={storeId} suppliers={suppliers} onCreated={load} />
    </div>
  )
}

/* ---------- Stock transfers ---------- */

function CreateTransferModal({ open, onClose, storeId, onCreated }: { open: boolean; onClose: () => void; storeId: number; onCreated: () => Promise<void> }) {
  const { myStores } = useAuth()
  const currentStore = myStores?.find((s) => s.storeId === storeId)
  const [product, setProduct] = useState<ProductSearchItem | null>(null)
  // "From" defaults to the store this page is already open on -- a transfer almost always starts
  // there, and the owner shouldn't have to re-pick the store they're already looking at.
  const [fromStore, setFromStore] = useState<MyStoreSearchItem | null>(
    currentStore ? { storeId: currentStore.storeId, name: currentStore.name, address: '' } : null,
  )
  const [toStore, setToStore] = useState<MyStoreSearchItem | null>(null)
  const [quantity, setQuantity] = useState('1')
  const [fieldError, setFieldError] = useState('')

  function handleClose() {
    setProduct(null)
    setFromStore(currentStore ? { storeId: currentStore.storeId, name: currentStore.name, address: '' } : null)
    setToStore(null)
    setQuantity('1')
    setFieldError('')
    onClose()
  }

  async function submit() {
    const pid = product?.productId
    const qty = Number(quantity)
    if (!pid || !fromStore || !toStore || !qty || qty <= 0) {
      setFieldError('Заполните товар, оба магазина и количество (числом больше нуля)')
      throw new Error('Проверьте поля формы')
    }
    setFieldError('')
    const result = await initiateStockTransfer(pid, fromStore.storeId, toStore.storeId, qty)
    if (result.outcome !== 'Initiated') throw new Error(describeInitiateTransferOutcome(result.outcome))
    await onCreated()
    handleClose()
  }

  return (
    <FormModal open={open} onClose={handleClose} title="Новое перемещение" isDirty={!!(product || toStore)} onSubmit={submit} submitLabel="Инициировать перемещение" scheme="admin">
      <FormField label="Товар" required scheme="admin">
        <ProductPicker value={product} onChange={setProduct} storeId={storeId} scheme="admin" scanEnabled />
      </FormField>
      <div className="grid grid-cols-2 gap-2.5">
        <FormField label="Из магазина" required scheme="admin">
          <StorePicker value={fromStore} onChange={setFromStore} excludeStoreId={toStore?.storeId} scheme="admin" />
        </FormField>
        <FormField label="В магазин" required scheme="admin">
          <StorePicker value={toStore} onChange={setToStore} excludeStoreId={fromStore?.storeId} scheme="admin" />
        </FormField>
      </div>
      <FormField label="Количество" required error={fieldError} scheme="admin">
        <input value={quantity} onChange={(e) => setQuantity(e.target.value)} type="number" min={1} className={inputClass} />
      </FormField>
    </FormModal>
  )
}

function TransfersSection({ storeId, createOpen, onCloseCreate }: { storeId: number; createOpen: boolean; onCloseCreate: () => void }) {
  const { myStores } = useAuth()
  const t = useT()
  const gate = useSubscriptionGate()
  const blockedTitle = gate.isOwner ? t('common.errorSubscriptionInactiveOwner') : t('common.errorSubscriptionInactiveCashier')
  const storeNameById = new Map((myStores ?? []).map((s) => [s.storeId, s.name]))
  const [transfers, setTransfers] = useState<StockTransfer[] | null>(null)
  const [productNames, setProductNames] = useState<Record<number, string>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [busyId, setBusyId] = useState<number | null>(null)
  const [rowError, setRowError] = useState<{ id: number; message: string } | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await getStockTransfers(storeId)
      if (res.outcome !== 'Found') {
        setError(res.outcome === 'Forbidden' ? 'Нет доступа к этому магазину' : 'Магазин не найден')
        setTransfers([])
        return
      }
      const list = res.transfers ?? []
      setTransfers(list)
      // Batch-resolve product names for display -- StockTransfer only carries a bare productId,
      // and a raw "товар #7" is exactly the kind of database id this screen must never show.
      const uniqueIds = [...new Set(list.map((t) => t.productId))]
      const results = await Promise.allSettled(uniqueIds.map((id) => productsApi.getProductById(id)))
      const names: Record<number, string> = {}
      results.forEach((r, i) => { if (r.status === 'fulfilled') names[uniqueIds[i]] = r.value.productName })
      setProductNames(names)
    } catch (err) {
      console.error('Failed to load stock transfers:', err)
      setError(describeError(err, t, { isOwner: gate.isOwner }))
    } finally {
      setLoading(false)
    }
  }, [storeId, t, gate.isOwner])

  useEffect(() => {
    load()
  }, [load])

  async function handleComplete(id: number) {
    setBusyId(id)
    setRowError(null)
    try {
      const result = await completeStockTransfer(id)
      if (result.outcome !== 'Completed') {
        setRowError({ id, message: describeCompleteTransferOutcome(result.outcome) })
        return
      }
      await load()
    } catch (err) {
      console.error('Failed to complete stock transfer:', err)
      setRowError({ id, message: describeError(err, t, { isOwner: gate.isOwner }) })
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="flex flex-col gap-5">
      <Panel className="p-5">
        <div className="mb-4 flex items-center justify-between">
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Перемещения этого магазина</span>
          <button
            onClick={load}
            aria-label="Обновить"
            className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]"
          >
            <RefreshIcon width={15} height={15} />
          </button>
        </div>

        {loading && <Loading />}

        {!loading && error && (
          <div className="py-6 text-center">
            <p className="mb-3 text-[13px] text-[color:var(--admin-text-secondary)]">{error}</p>
            <button onClick={load} className="rounded-xl bg-[color:var(--admin-accent)] px-4 py-2 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90">
              Повторить
            </button>
          </div>
        )}

        {!loading && !error && (
          <div className="flex flex-col gap-3">
            {(transfers ?? []).map((tr) => (
              <div key={tr.stockTransferId} className="flex flex-col gap-2.5 rounded-[16px] bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">
                    {productNames[tr.productId] ?? 'Товар без названия'}
                  </div>
                  <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                    {storeNameById.get(tr.fromStoreId) ?? 'Магазин'} → {storeNameById.get(tr.toStoreId) ?? 'Магазин'} · {tr.quantity} ед. · создано {fmtDateTime(tr.createdAt)}
                    {tr.completedAt ? ` · завершено ${fmtDateTime(tr.completedAt)}` : ''}
                  </div>
                  {rowError?.id === tr.stockTransferId && (
                    <div className="mt-1.5">
                      <FieldError message={rowError.message} />
                    </div>
                  )}
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <StatusBadge label={TRANSFER_STATUS_LABEL[tr.status]} className={TRANSFER_STATUS_STYLE[tr.status]} />
                  {tr.status === 'InTransit' && (
                    <button
                      onClick={() => handleComplete(tr.stockTransferId)}
                      disabled={busyId === tr.stockTransferId || (!gate.loading && !gate.isOperational)}
                      title={!gate.loading && !gate.isOperational ? blockedTitle : undefined}
                      className="rounded-lg bg-[color:var(--admin-accent)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90 disabled:opacity-50"
                    >
                      {busyId === tr.stockTransferId ? 'Завершаем…' : 'Завершить перемещение'}
                    </button>
                  )}
                </div>
              </div>
            ))}
            {(transfers ?? []).length === 0 && (
              <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">Перемещений пока не было</div>
            )}
          </div>
        )}
      </Panel>

      <CreateTransferModal open={createOpen} onClose={onCloseCreate} storeId={storeId} onCreated={load} />
    </div>
  )
}

/* ---------- shell ---------- */

type SupplyTab = 'suppliers' | 'orders' | 'transfers'

const SUPPLY_TAB_OPTIONS = [
  { value: 'suppliers' as const, label: 'Поставщики', icon: <TruckIcon width={15} height={15} /> },
  { value: 'orders' as const, label: 'Заказы поставщикам', icon: <DocumentIcon width={15} height={15} /> },
  { value: 'transfers' as const, label: 'Перемещения между магазинами', icon: <SwapIcon width={15} height={15} /> },
]

const SUPPLY_ADD_LABEL: Record<SupplyTab, string> = {
  suppliers: 'Добавить поставщика',
  orders: 'Новый заказ',
  transfers: 'Новое перемещение',
}

export function SupplyPage() {
  const { storeId } = useAuth()
  const t = useT()
  const gate = useSubscriptionGate()
  const blockedTitle = gate.isOwner ? t('common.errorSubscriptionInactiveOwner') : t('common.errorSubscriptionInactiveCashier')
  const [params, setParams] = useSearchParams()
  const tabParam = params.get('tab')
  const tab: SupplyTab = tabParam === 'orders' || tabParam === 'transfers' ? tabParam : 'suppliers'
  const [createOpen, setCreateOpen] = useState(false)

  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [suppliersLoading, setSuppliersLoading] = useState(true)
  const [suppliersError, setSuppliersError] = useState('')

  const loadSuppliers = useCallback(async () => {
    if (!storeId) return
    setSuppliersError('')
    try {
      const res = await getSuppliers(storeId)
      setSuppliers(res.suppliers ?? [])
    } catch (err) {
      console.error('Failed to load suppliers:', err)
      setSuppliersError(describeError(err, t, { isOwner: gate.isOwner }))
    } finally {
      setSuppliersLoading(false)
    }
  }, [storeId, t, gate.isOwner])

  useEffect(() => {
    loadSuppliers()
  }, [loadSuppliers])

  if (!storeId) {
    return (
      <Panel className="p-8 text-center">
        <p className="text-[14px] text-[color:var(--admin-text-secondary)]">
          Сначала выберите магазин, чтобы управлять поставками
        </p>
      </Panel>
    )
  }

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <SectionSelect
          value={tab}
          onChange={(v) => setParams(v === 'suppliers' ? {} : { tab: v })}
          options={SUPPLY_TAB_OPTIONS}
          ariaLabel="Раздел поставок"
        />
        <AddButton onClick={() => setCreateOpen(true)} {...gateButtonProps(gate, blockedTitle)}>{SUPPLY_ADD_LABEL[tab]}</AddButton>
      </div>

      {tab === 'suppliers' && (
        <SuppliersSection
          storeId={storeId}
          suppliers={suppliers}
          loading={suppliersLoading}
          error={suppliersError}
          load={loadSuppliers}
          createOpen={createOpen}
          onCloseCreate={() => setCreateOpen(false)}
        />
      )}
      {tab === 'orders' && (
        <OrdersSection storeId={storeId} suppliers={suppliers} createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />
      )}
      {tab === 'transfers' && <TransfersSection storeId={storeId} createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />}
    </div>
  )
}
