import { useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../../lib/api'
import {
  uploadReceipt,
  verifyReceipt,
  type ReceiptLineInput,
  type ReceiptLineComparison,
  type VerifyReceiptOutcome,
} from '../../lib/api/receipts'
import { ReceiptIcon, CloseIcon } from '../../components/icons'

// There is no "list my receipts" backend endpoint (upload-only + verify-by-id),
// so the session's uploaded receipt ids are cached locally — same pattern as
// admin/pages/InventoryPage.tsx's NAME_CACHE_KEY — purely a client-side
// convenience list, not a source of truth for receipt content.
const RECEIPTS_CACHE_KEY = 'sarfkor-uploaded-receipts'

interface UploadedReceipt {
  receiptId: number
  uploadedAt: string
}

function loadReceiptsCache(): UploadedReceipt[] {
  try {
    const raw = JSON.parse(localStorage.getItem(RECEIPTS_CACHE_KEY) ?? '[]')
    return Array.isArray(raw) ? raw : []
  } catch {
    return []
  }
}

function saveReceiptsCache(list: UploadedReceipt[]) {
  localStorage.setItem(RECEIPTS_CACHE_KEY, JSON.stringify(list))
}

const VERIFY_OUTCOME_LABEL: Record<VerifyReceiptOutcome, string> = {
  Verified: 'Чек сверен — цены совпадают',
  Mismatched: 'Обнаружены расхождения в ценах',
  NotFound: 'Чек не найден',
  Forbidden: 'Нет доступа к этому чеку',
  MissingStore: 'Не указан магазин — сверка невозможна',
  AlreadyProcessed: 'Чек уже был обработан ранее',
}

interface LineRow {
  productId: string
  recognizedName: string
  quantity: string
  price: string
  currency: string
}

function emptyRow(): LineRow {
  return { productId: '', recognizedName: '', quantity: '1', price: '', currency: 'TJS' }
}

function Card({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`rounded-3xl bg-[color:var(--bg-card)] p-6 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)] ${className}`}>
      {children}
    </div>
  )
}

function LineItemsForm({ lines, onChange }: { lines: LineRow[]; onChange: (lines: LineRow[]) => void }) {
  function update(i: number, patch: Partial<LineRow>) {
    onChange(lines.map((l, idx) => (idx === i ? { ...l, ...patch } : l)))
  }

  return (
    <div className="flex flex-col gap-2.5">
      {lines.map((line, i) => (
        <div key={i} className="flex flex-col gap-2 rounded-xl bg-[color:var(--bg-section)] p-3 sm:flex-row sm:flex-wrap sm:items-center">
          <input
            value={line.productId}
            onChange={(e) => update(i, { productId: e.target.value })}
            placeholder="ID товара (если известен)"
            inputMode="numeric"
            className="w-full min-w-0 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-2.5 py-1.5 text-[13px] outline-none sm:w-32"
          />
          <input
            value={line.recognizedName}
            onChange={(e) => update(i, { recognizedName: e.target.value })}
            placeholder="Название на чеке"
            className="w-full min-w-0 flex-1 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-2.5 py-1.5 text-[13px] outline-none sm:min-w-[10rem]"
          />
          <input
            value={line.quantity}
            onChange={(e) => update(i, { quantity: e.target.value })}
            type="number"
            min={0}
            step="1"
            placeholder="Кол-во"
            className="w-full min-w-0 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-2.5 py-1.5 text-[13px] outline-none sm:w-20"
          />
          <input
            value={line.price}
            onChange={(e) => update(i, { price: e.target.value })}
            type="number"
            min={0}
            step="0.01"
            placeholder="Цена"
            className="w-full min-w-0 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-2.5 py-1.5 text-[13px] outline-none sm:w-24"
          />
          <select
            value={line.currency}
            onChange={(e) => update(i, { currency: e.target.value })}
            className="w-full min-w-0 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-2.5 py-1.5 text-[13px] outline-none sm:w-20"
          >
            <option value="TJS">TJS</option>
            <option value="USD">USD</option>
            <option value="RUB">RUB</option>
          </select>
          <button
            type="button"
            onClick={() => onChange(lines.filter((_, idx) => idx !== i))}
            disabled={lines.length <= 1}
            aria-label="Удалить позицию"
            className="grid h-8 w-8 shrink-0 place-items-center self-end rounded-lg text-[color:var(--text-tertiary)] hover:bg-[color:var(--bg-card)] hover:text-[color:var(--text-primary)] disabled:opacity-30 sm:self-center"
          >
            <CloseIcon width={14} height={14} />
          </button>
        </div>
      ))}
      <button
        type="button"
        onClick={() => onChange([...lines, emptyRow()])}
        className="self-start rounded-lg border border-dashed border-[color:var(--border-strong)] px-3.5 py-2 text-[12.5px] font-semibold text-[color:var(--text-secondary)] hover:text-[color:var(--text-primary)]"
      >
        + Добавить позицию
      </button>
    </div>
  )
}

interface VerifyState {
  busy: boolean
  outcome?: VerifyReceiptOutcome
  lines?: ReceiptLineComparison[]
  error?: string
}

function UploadedReceiptCard({ receipt }: { receipt: UploadedReceipt }) {
  const [state, setState] = useState<VerifyState>({ busy: false })

  async function verify() {
    setState({ busy: true })
    try {
      const res = await verifyReceipt(receipt.receiptId)
      setState({ busy: false, outcome: res.outcome, lines: res.lines })
    } catch (err) {
      setState({ busy: false, error: err instanceof ApiError ? err.message : 'Не удалось сверить чек' })
    }
  }

  return (
    <div className="rounded-2xl bg-[color:var(--bg-section)] p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <div className="text-[13.5px] font-bold">Чек №{receipt.receiptId}</div>
          <div className="text-[12px] text-[color:var(--text-tertiary)]">Загружен {new Date(receipt.uploadedAt).toLocaleString('ru-RU')}</div>
        </div>
        <button
          onClick={verify}
          disabled={state.busy}
          className="rounded-lg border border-[color:var(--border-subtle)] px-3.5 py-2 text-[12.5px] font-semibold text-[color:var(--text-secondary)] hover:border-[color:var(--border-strong)] hover:text-[color:var(--text-primary)] disabled:opacity-50"
        >
          {state.busy ? 'Сверяем…' : 'Сверить с ценником'}
        </button>
      </div>

      {state.error && <p className="mt-2.5 text-[12.5px] text-[color:var(--text-secondary)]">{state.error}</p>}

      {state.outcome && (
        <div className="mt-3 border-t border-[color:var(--border-subtle)] pt-3">
          <p
            className={`text-[13px] font-semibold ${
              state.outcome === 'Verified' ? 'text-[color:var(--color-brand)]' : 'text-[color:var(--text-primary)]'
            }`}
          >
            {VERIFY_OUTCOME_LABEL[state.outcome] ?? state.outcome}
          </p>
          {state.lines && state.lines.length > 0 && (
            <div className="mt-2 flex flex-col divide-y divide-[color:var(--border-subtle)]">
              {state.lines.map((l, i) => (
                <div key={i} className="flex flex-wrap items-center justify-between gap-2 py-2 text-[12.5px]">
                  <span className="text-[color:var(--text-secondary)]">{l.productId != null ? `Товар #${l.productId}` : 'Товар'}</span>
                  <span className="text-[color:var(--text-tertiary)]">
                    Чек: {l.receiptPrice.toFixed(2)}
                    {l.currentPrice != null && <> · Ценник: {l.currentPrice.toFixed(2)}</>}
                  </span>
                  <span
                    className={`rounded-full px-2.5 py-0.5 text-[11px] font-bold ${
                      l.matches
                        ? 'bg-[color:var(--color-brand-light)] text-[color:var(--color-brand)]'
                        : 'bg-[color:var(--text-primary)]/10 text-[color:var(--text-primary)]'
                    }`}
                  >
                    {l.matches ? 'Совпадает' : 'Расхождение'}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export function ReceiptsPage() {
  const [file, setFile] = useState<File | null>(null)
  const [storeId, setStoreId] = useState('')
  const [lines, setLines] = useState<LineRow[]>([emptyRow()])
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState('')
  const [receipts, setReceipts] = useState<UploadedReceipt[]>([])

  useEffect(() => {
    setReceipts(loadReceiptsCache())
  }, [])

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (!file) {
      setStatus('Выберите файл чека (JPEG или PNG)')
      return
    }
    const parsedLines: ReceiptLineInput[] = []
    for (const l of lines) {
      const quantity = Number(l.quantity)
      const price = Number(l.price)
      if (!l.quantity || !l.price || Number.isNaN(quantity) || Number.isNaN(price) || quantity <= 0 || price < 0) continue
      parsedLines.push({
        productId: l.productId ? Number(l.productId) : undefined,
        recognizedName: l.recognizedName.trim() || undefined,
        quantity,
        price,
        currency: l.currency || 'TJS',
      })
    }
    if (parsedLines.length === 0) {
      setStatus('Укажите хотя бы одну позицию с количеством и ценой')
      return
    }
    setBusy(true)
    setStatus('')
    try {
      const res = await uploadReceipt(file, parsedLines, storeId ? Number(storeId) : undefined)
      const next = [{ receiptId: res.receiptId, uploadedAt: new Date().toISOString() }, ...receipts]
      setReceipts(next)
      saveReceiptsCache(next)
      setStatus('Чек загружен')
      setFile(null)
      setLines([emptyRow()])
      const input = document.getElementById('receipt-file-input') as HTMLInputElement | null
      if (input) input.value = ''
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось загрузить чек')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-1 text-[22px] font-extrabold tracking-tight">Чеки</h1>
      <p className="mb-6 text-[14px] text-[color:var(--text-secondary)]">Загрузите чек и сверьте цены с ценником в магазине</p>

      <div className="flex flex-col gap-4">
        <Card>
          <div className="mb-4 flex items-center gap-2 text-[15px] font-bold">
            <ReceiptIcon width={17} height={17} />
            Загрузить чек
          </div>
          <form onSubmit={submit} className="flex flex-col gap-3.5">
            <label className="block">
              <span className="mb-1.5 block text-[12.5px] font-semibold text-[color:var(--text-secondary)]">Фото чека (JPEG или PNG, до 5 МБ)</span>
              <input
                id="receipt-file-input"
                type="file"
                accept="image/jpeg,image/png"
                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                className="w-full rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3.5 py-2.5 text-[13px] outline-none file:mr-3 file:rounded-lg file:border-0 file:bg-[color:var(--color-brand)] file:px-3 file:py-1.5 file:text-[12.5px] file:font-bold file:text-white"
              />
            </label>
            <label className="block">
              <span className="mb-1.5 block text-[12.5px] font-semibold text-[color:var(--text-secondary)]">ID магазина (необязательно)</span>
              <input
                value={storeId}
                onChange={(e) => setStoreId(e.target.value)}
                type="number"
                min={0}
                inputMode="numeric"
                placeholder="Например, 12"
                className="w-full rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3.5 py-2.5 text-[14px] outline-none focus:border-[color:var(--color-brand)]"
              />
            </label>
            <div>
              <span className="mb-1.5 block text-[12.5px] font-semibold text-[color:var(--text-secondary)]">Что вы купили</span>
              <LineItemsForm lines={lines} onChange={setLines} />
            </div>
            <div className="flex items-center gap-3">
              <button
                type="submit"
                disabled={busy}
                className="rounded-xl bg-[color:var(--color-brand)] px-5 py-2.5 text-[13.5px] font-bold text-white disabled:opacity-50"
              >
                {busy ? 'Загружаем…' : 'Загрузить чек'}
              </button>
              {status && <span className="text-[12.5px] text-[color:var(--text-secondary)]">{status}</span>}
            </div>
          </form>
        </Card>

        <Card>
          <div className="mb-4 flex items-center gap-2 text-[15px] font-bold">
            <ReceiptIcon width={17} height={17} />
            Загруженные чеки
          </div>
          {receipts.length === 0 ? (
            <p className="text-[13px] text-[color:var(--text-tertiary)]">Вы ещё не загружали чеки в этом браузере</p>
          ) : (
            <div className="flex flex-col gap-2.5">
              {receipts.map((r) => (
                <UploadedReceiptCard key={r.receiptId} receipt={r} />
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  )
}
