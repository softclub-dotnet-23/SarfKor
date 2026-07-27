import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { LogoMark } from '../../components/Logo'
import { useAuth } from '../../auth/AuthContext'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../../components/icons'
import {
  LogOutIcon,
  CheckIcon,
  XIcon,
  ReportIcon,
  PackageIcon,
  AlertIcon,
  TagIcon,
  PercentIcon,
  GridIcon,
  ClockIcon,
} from '../components/icons'
import {
  adminApi,
  ApiError,
  type PriceEntryDispute,
  type ReportDispute,
  type ProductSubmission,
  type Report,
  type Brand,
  type Category,
  type TaxRate,
  type AuditLogEntry,
} from '../../lib/api'

const REPORT_TYPE_LABEL: Record<Report['type'], string> = {
  WrongPrice: 'Неверная цена',
  OutOfStock: 'Нет в наличии',
  ReceiptMismatch: 'Расхождение с чеком',
  Other: 'Другое',
}

const AUDIT_ACTION_LABEL: Record<string, string> = {
  'ProductSubmission.Approved': 'Товар одобрен',
  'ProductSubmission.Rejected': 'Товар отклонён',
  'Report.Resolved': 'Жалоба разрешена',
  'Report.Rejected': 'Жалоба отклонена',
  'PriceEntryDispute.Upheld': 'Спор по цене поддержан',
  'PriceEntryDispute.Dismissed': 'Спор по цене отклонён',
  'ReportDispute.Upheld': 'Спор по жалобе поддержан',
  'ReportDispute.Dismissed': 'Спор по жалобе отклонён',
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

function fmtTime(iso: string) {
  return new Date(iso).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function shortUserId(id: string) {
  return id.length > 10 ? `${id.slice(0, 8)}…` : id
}

/* ---------- shared data hooks (lifted so sidebar badges + section content never drift apart) ---------- */

function usePendingPriceDisputes() {
  const [items, setItems] = useState<PriceEntryDispute[] | null>(null)
  const [error, setError] = useState('')
  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await adminApi.getPendingPriceEntryDisputes()).disputes)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить споры по ценам')
    }
  }, [])
  useEffect(() => {
    load()
  }, [load])
  return { items, error, load, setItems }
}

function usePendingReportDisputes() {
  const [items, setItems] = useState<ReportDispute[] | null>(null)
  const [error, setError] = useState('')
  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await adminApi.getPendingReportDisputes()).disputes)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить споры по жалобам')
    }
  }, [])
  useEffect(() => {
    load()
  }, [load])
  return { items, error, load, setItems }
}

function usePendingProducts() {
  const [items, setItems] = useState<ProductSubmission[] | null>(null)
  const [error, setError] = useState('')
  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await adminApi.getPendingProductSubmissions()).submissions)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить новые товары')
    }
  }, [])
  useEffect(() => {
    load()
  }, [load])
  return { items, error, load, setItems }
}

function usePendingReports() {
  const [items, setItems] = useState<Report[] | null>(null)
  const [error, setError] = useState('')
  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await adminApi.getPendingReports()).reports)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить жалобы')
    }
  }, [])
  useEffect(() => {
    load()
  }, [load])
  return { items, error, load, setItems }
}

/* ---------- shared bits ---------- */

function Panel({ children, className = '', style }: { children: React.ReactNode; className?: string; style?: React.CSSProperties }) {
  return (
    <div
      className={`rounded-2xl bg-[color:var(--mod-panel)] ring-1 ring-[color:var(--mod-border)] ${className}`}
      style={style}
    >
      {children}
    </div>
  )
}

function EmptyState({ text }: { text: string }) {
  return (
    <Panel className="flex flex-col items-center justify-center gap-4 px-6 py-16 text-center">
      <span className="grid h-14 w-14 place-items-center rounded-full bg-[color:var(--mod-ok-dim)] text-[color:var(--mod-ok)]">
        <CheckIcon width={26} height={26} strokeWidth={2.5} />
      </span>
      <div>
        <div className="text-[15px] font-bold text-[color:var(--mod-text)]">Очередь пуста</div>
        <div className="mt-1 max-w-xs text-[13px] text-[color:var(--mod-muted)]">{text}</div>
      </div>
    </Panel>
  )
}

function ErrorState({ text, onRetry }: { text: string; onRetry: () => void }) {
  return (
    <Panel className="p-8 text-center">
      <p className="mb-4 text-[14px] text-[color:var(--mod-muted)]">{text}</p>
      <button
        onClick={onRetry}
        className="rounded-xl bg-[color:var(--mod-accent)] px-5 py-2.5 text-[13px] font-bold text-white transition-transform hover:brightness-110 active:scale-95"
      >
        Повторить
      </button>
    </Panel>
  )
}

function Loading() {
  return <div className="py-16 text-center text-[13px] text-[color:var(--mod-faint)]">Загрузка…</div>
}

function ModRow({
  icon,
  title,
  subtitle,
  meta,
  approveLabel,
  rejectLabel,
  busy,
  onApprove,
  onReject,
  leading,
}: {
  icon: React.ReactNode
  title: string
  subtitle: string
  meta: string
  approveLabel: string
  rejectLabel: string
  busy: boolean
  onApprove: () => void
  onReject: () => void
  leading?: React.ReactNode
}) {
  return (
    <Panel className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex min-w-0 items-center gap-3.5">
        {leading}
        <span className="grid h-11 w-11 shrink-0 place-items-center rounded-[12px] bg-[color:var(--mod-panel2)] ring-1 ring-[color:var(--mod-border)] text-[color:var(--mod-accent2)]">
          {icon}
        </span>
        <div className="min-w-0">
          <div className="truncate text-[14px] font-bold text-[color:var(--mod-text)]">{title}</div>
          <div className="mt-0.5 truncate text-[13px] text-[color:var(--mod-muted)]">{subtitle}</div>
          <div className="mt-1 font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--mod-faint)]">{meta}</div>
        </div>
      </div>
      <div className="flex shrink-0 gap-2 self-stretch sm:self-auto">
        <button
          onClick={onReject}
          disabled={busy}
          title={rejectLabel}
          className="flex h-11 flex-1 items-center justify-center gap-1.5 rounded-xl border border-[color:var(--mod-border)] text-[color:var(--mod-danger)] transition-colors hover:bg-[color:var(--mod-danger-dim)] disabled:opacity-40 sm:w-11 sm:flex-none"
        >
          <XIcon width={17} height={17} strokeWidth={2.5} />
        </button>
        <button
          onClick={onApprove}
          disabled={busy}
          title={approveLabel}
          className="flex h-11 flex-1 items-center justify-center gap-1.5 rounded-xl bg-[color:var(--mod-ok)] text-white shadow-[0_3px_10px_var(--mod-ok-dim)] transition-transform hover:brightness-110 active:scale-95 disabled:opacity-40 sm:w-11 sm:flex-none"
        >
          <CheckIcon width={18} height={18} strokeWidth={2.6} />
        </button>
      </div>
    </Panel>
  )
}

/* ---------- Overview ---------- */

function OverviewSection({
  priceDisputes,
  reportDisputes,
  products,
  reports,
}: {
  priceDisputes: PriceEntryDispute[] | null
  reportDisputes: ReportDispute[] | null
  products: ProductSubmission[] | null
  reports: Report[] | null
}) {
  const [feed, setFeed] = useState<AuditLogEntry[] | null>(null)
  const [feedError, setFeedError] = useState('')

  const loadFeed = useCallback(async () => {
    try {
      setFeed((await adminApi.getRecentAuditLogs(20)).logs)
      setFeedError('')
    } catch (err) {
      setFeedError(err instanceof ApiError ? err.message : 'Не удалось загрузить ленту событий')
    }
  }, [])

  useEffect(() => {
    loadFeed()
    // Real actions land here (moderation, disputes) rather than synthetic
    // events — a modest interval keeps the feed "live" without hammering
    // the API for something that changes at human pace, not per-second.
    const id = setInterval(loadFeed, 15000)
    return () => clearInterval(id)
  }, [loadFeed])

  const kpis = [
    { label: 'Товаров на модерации', value: products?.length, icon: <PackageIcon width={18} height={18} /> },
    { label: 'Открытых споров по ценам', value: priceDisputes?.length, icon: <AlertIcon width={18} height={18} /> },
    { label: 'Открытых споров по жалобам', value: reportDisputes?.length, icon: <AlertIcon width={18} height={18} /> },
    { label: 'Необработанных жалоб', value: reports?.length, icon: <ReportIcon width={18} height={18} /> },
  ]

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 grid grid-cols-1 gap-3.5 sm:grid-cols-2 xl:grid-cols-4">
        {kpis.map((k) => (
          <Panel key={k.label} className="p-5 transition-transform hover:-translate-y-0.5">
            <div className="flex items-start justify-between gap-2">
              <span className="text-[11.5px] font-semibold leading-tight text-[color:var(--mod-muted)]">{k.label}</span>
              <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[color:var(--mod-accent-dim)] text-[color:var(--mod-accent2)]">
                {k.icon}
              </span>
            </div>
            <div className="mt-3 font-[JetBrains_Mono,monospace] text-[27px] font-bold tracking-tight text-[color:var(--mod-text)]">
              {k.value ?? '—'}
            </div>
          </Panel>
        ))}
      </div>

      <Panel className="p-5">
        <div className="mb-3 flex items-center justify-between">
          <div className="flex items-center gap-2 text-[14px] font-bold text-[color:var(--mod-text)]">
            <span
              className="h-[7px] w-[7px] rounded-full bg-[color:var(--mod-accent)]"
              style={{ animation: 'mod-live-ping 1.5s ease-in-out infinite' }}
            />
            Лента событий
          </div>
          <span className="font-[JetBrains_Mono,monospace] text-[10.5px] text-[color:var(--mod-faint)]">
            обновляется каждые 15с
          </span>
        </div>

        {feed === null && !feedError && <Loading />}
        {feedError && <ErrorState text={feedError} onRetry={loadFeed} />}
        {feed && feed.length === 0 && <EmptyState text="Пока нет действий модерации в журнале" />}
        {feed && feed.length > 0 && (
          <div className="flex flex-col gap-1">
            {feed.map((e) => (
              <div key={e.auditLogId} className="flex items-center gap-3 rounded-xl px-2.5 py-2.5 transition-colors hover:bg-[color:var(--mod-panel2)]">
                <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[color:var(--mod-accent-dim)] text-[color:var(--mod-accent2)]">
                  <ClockIcon width={15} height={15} />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[12.5px] font-semibold text-[color:var(--mod-text)]">
                    {AUDIT_ACTION_LABEL[e.action] ?? e.action}
                  </div>
                  <div className="truncate text-[11px] text-[color:var(--mod-muted)]">
                    {e.entityType} #{e.entityId}
                    {e.details ? ` · ${e.details}` : ''} · {shortUserId(e.performedByUserId)}
                  </div>
                </div>
                <span className="shrink-0 font-[JetBrains_Mono,monospace] text-[10px] text-[color:var(--mod-faint)]">
                  {fmtTime(e.occurredAt)}
                </span>
              </div>
            ))}
          </div>
        )}
      </Panel>
    </div>
  )
}

/* ---------- Moderation (products) — checkbox multi-select + bulk approve ---------- */

function ModerationSection({
  items,
  error,
  load,
  setItems,
}: ReturnType<typeof usePendingProducts>) {
  const [busyIds, setBusyIds] = useState<Set<number>>(new Set())
  const [selected, setSelected] = useState<Set<number>>(new Set())

  async function moderate(submissionId: number, approve: boolean) {
    setBusyIds((s) => new Set(s).add(submissionId))
    try {
      await adminApi.moderateProductSubmission(submissionId, approve)
      setItems((cur) => cur?.filter((s) => s.submissionId !== submissionId) ?? null)
      setSelected((s) => {
        const next = new Set(s)
        next.delete(submissionId)
        return next
      })
    } catch {
      await load()
    } finally {
      setBusyIds((s) => {
        const next = new Set(s)
        next.delete(submissionId)
        return next
      })
    }
  }

  async function approveSelected() {
    const ids = [...selected]
    for (const id of ids) {
      await moderate(id, true)
    }
  }

  if (items === null && !error) return <Loading />
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Все новые товары обработаны. Новые карточки появятся здесь автоматически." />

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      {selected.size > 0 && (
        <div className="mb-4 flex items-center justify-between rounded-xl bg-[color:var(--mod-accent-dim)] px-4 py-3">
          <span className="text-[13px] font-semibold text-[color:var(--mod-text)]">Выбрано: {selected.size}</span>
          <button
            onClick={approveSelected}
            className="rounded-lg bg-[color:var(--mod-ok)] px-4 py-2 text-[12.5px] font-bold text-white transition-transform hover:brightness-110 active:scale-95"
          >
            Одобрить выбранные ({selected.size})
          </button>
        </div>
      )}
      <div className="flex flex-col gap-3">
        {items!.map((s) => (
          <ModRow
            key={s.submissionId}
            leading={
              <input
                type="checkbox"
                checked={selected.has(s.submissionId)}
                onChange={() =>
                  setSelected((cur) => {
                    const next = new Set(cur)
                    if (next.has(s.submissionId)) next.delete(s.submissionId)
                    else next.add(s.submissionId)
                    return next
                  })
                }
                className="h-[18px] w-[18px] shrink-0 accent-[#8b5cf6]"
              />
            }
            icon={<PackageIcon width={18} height={18} />}
            title={s.name}
            subtitle={`Штрихкод ${s.barcode} · Страна: ${s.countryOfOrigin}`}
            meta={`от ${shortUserId(s.submittedByUserId)} · ${fmtDate(s.createdAt)}`}
            approveLabel="Одобрить"
            rejectLabel="Отклонить"
            busy={busyIds.has(s.submissionId)}
            onApprove={() => moderate(s.submissionId, true)}
            onReject={() => moderate(s.submissionId, false)}
          />
        ))}
      </div>
    </div>
  )
}

/* ---------- Disputes ---------- */

function PriceDisputesSection({ items, error, load, setItems }: ReturnType<typeof usePendingPriceDisputes>) {
  const [busyId, setBusyId] = useState<number | null>(null)

  async function resolve(disputeId: number, uphold: boolean) {
    setBusyId(disputeId)
    try {
      await adminApi.resolvePriceEntryDispute(disputeId, uphold)
      setItems((cur) => cur?.filter((d) => d.disputeId !== disputeId) ?? null)
    } catch {
      await load()
    } finally {
      setBusyId(null)
    }
  }

  if (items === null && !error) return <Loading />
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Нет споров по ценам, ожидающих решения" />

  return (
    <div className="flex flex-col gap-3" style={{ animation: 'mod-fade-in .3s ease' }}>
      {items!.map((d) => (
        <ModRow
          key={d.disputeId}
          icon={<AlertIcon width={18} height={18} />}
          title={`Спор по цене #${d.priceEntryId}`}
          subtitle={d.reason}
          meta={`от ${shortUserId(d.disputedByUserId)} · ${fmtDate(d.createdAt)}`}
          approveLabel="Поддержать"
          rejectLabel="Отклонить"
          busy={busyId === d.disputeId}
          onApprove={() => resolve(d.disputeId, true)}
          onReject={() => resolve(d.disputeId, false)}
        />
      ))}
    </div>
  )
}

function ReportDisputesSection({ items, error, load, setItems }: ReturnType<typeof usePendingReportDisputes>) {
  const [busyId, setBusyId] = useState<number | null>(null)

  async function resolve(disputeId: number, uphold: boolean) {
    setBusyId(disputeId)
    try {
      await adminApi.resolveReportDispute(disputeId, uphold)
      setItems((cur) => cur?.filter((d) => d.disputeId !== disputeId) ?? null)
    } catch {
      await load()
    } finally {
      setBusyId(null)
    }
  }

  if (items === null && !error) return <Loading />
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Нет споров по жалобам, ожидающих решения" />

  return (
    <div className="flex flex-col gap-3" style={{ animation: 'mod-fade-in .3s ease' }}>
      {items!.map((d) => (
        <ModRow
          key={d.disputeId}
          icon={<AlertIcon width={18} height={18} />}
          title={`Спор по жалобе #${d.reportId}`}
          subtitle={d.reason}
          meta={`от ${shortUserId(d.disputedByUserId)} · ${fmtDate(d.createdAt)}`}
          approveLabel="Поддержать"
          rejectLabel="Отклонить"
          busy={busyId === d.disputeId}
          onApprove={() => resolve(d.disputeId, true)}
          onReject={() => resolve(d.disputeId, false)}
        />
      ))}
    </div>
  )
}

function ReportsSection({ items, error, load, setItems }: ReturnType<typeof usePendingReports>) {
  const [busyId, setBusyId] = useState<number | null>(null)

  async function moderate(reportId: number, resolveFlag: boolean) {
    setBusyId(reportId)
    try {
      await adminApi.moderateReport(reportId, resolveFlag)
      setItems((cur) => cur?.filter((r) => r.reportId !== reportId) ?? null)
    } catch {
      await load()
    } finally {
      setBusyId(null)
    }
  }

  if (items === null && !error) return <Loading />
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Нет жалоб, ожидающих рассмотрения" />

  return (
    <div className="flex flex-col gap-3" style={{ animation: 'mod-fade-in .3s ease' }}>
      {items!.map((r) => (
        <ModRow
          key={r.reportId}
          icon={<ReportIcon width={18} height={18} />}
          title={`${REPORT_TYPE_LABEL[r.type]} · товар #${r.productId}`}
          subtitle={r.description}
          meta={`от ${shortUserId(r.userId)}${r.storeId ? ` · магазин #${r.storeId}` : ''} · ${fmtDate(r.createdAt)}`}
          approveLabel="Разрешить"
          rejectLabel="Отклонить"
          busy={busyId === r.reportId}
          onApprove={() => moderate(r.reportId, true)}
          onReject={() => moderate(r.reportId, false)}
        />
      ))}
    </div>
  )
}

/* ---------- Catalog ---------- */

function BrandsSubsection() {
  const [items, setItems] = useState<Brand[] | null>(null)
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await adminApi.getBrands()).brands)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить бренды')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) return
    setSubmitting(true)
    setFormError('')
    try {
      await adminApi.createBrand(name.trim())
      setName('')
      await load()
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Не удалось создать бренд')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Panel className="p-5">
      <div className="mb-1 flex items-center gap-2 text-[14px] font-bold text-[color:var(--mod-text)]">
        <TagIcon width={16} height={16} />
        Бренды
      </div>
      <p className="mb-4 text-[12px] text-[color:var(--mod-muted)]">Используются в карточках товаров</p>

      <form onSubmit={handleCreate} className="mb-4 flex gap-2">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Название бренда"
          className="flex-1 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        <button
          type="submit"
          disabled={submitting || !name.trim()}
          className="shrink-0 rounded-xl bg-[color:var(--mod-accent)] px-4 py-2.5 text-[13px] font-bold text-white disabled:opacity-50"
        >
          Добавить
        </button>
      </form>
      {formError && <p className="mb-3 text-[12px] font-medium text-[color:var(--mod-danger)]">{formError}</p>}

      {items === null && !error && <Loading />}
      {error && <ErrorState text={error} onRetry={load} />}
      {items && items.length === 0 && <p className="text-[13px] text-[color:var(--mod-faint)]">Брендов пока нет</p>}
      {items && items.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {items.map((b) => (
            <span key={b.brandId} className="rounded-full bg-[color:var(--mod-panel2)] px-3 py-1.5 text-[12.5px] font-medium text-[color:var(--mod-text)]">
              {b.name}
            </span>
          ))}
        </div>
      )}
    </Panel>
  )
}

function CategoriesSubsection() {
  const [items, setItems] = useState<Category[] | null>(null)
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [parentId, setParentId] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await adminApi.getCategories()).categories)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить категории')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) return
    setSubmitting(true)
    setFormError('')
    try {
      const res = await adminApi.createCategory(name.trim(), parentId ? Number(parentId) : undefined)
      if (res.outcome === 'ParentCategoryNotFound') {
        setFormError('Родительская категория не найдена')
        return
      }
      setName('')
      setParentId('')
      await load()
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Не удалось создать категорию')
    } finally {
      setSubmitting(false)
    }
  }

  const nameById = new Map((items ?? []).map((c) => [c.categoryId, c.name]))

  return (
    <Panel className="p-5">
      <div className="mb-1 flex items-center gap-2 text-[14px] font-bold text-[color:var(--mod-text)]">
        <TagIcon width={16} height={16} />
        Категории
      </div>
      <p className="mb-4 text-[12px] text-[color:var(--mod-muted)]">Дерево категорий товаров</p>

      <form onSubmit={handleCreate} className="mb-4 flex flex-wrap gap-2">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Название категории"
          className="min-w-[160px] flex-1 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        <select
          value={parentId}
          onChange={(e) => setParentId(e.target.value)}
          className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        >
          <option value="">Без родителя</option>
          {(items ?? []).map((c) => (
            <option key={c.categoryId} value={c.categoryId}>
              {c.name}
            </option>
          ))}
        </select>
        <button
          type="submit"
          disabled={submitting || !name.trim()}
          className="shrink-0 rounded-xl bg-[color:var(--mod-accent)] px-4 py-2.5 text-[13px] font-bold text-white disabled:opacity-50"
        >
          Добавить
        </button>
      </form>
      {formError && <p className="mb-3 text-[12px] font-medium text-[color:var(--mod-danger)]">{formError}</p>}

      {items === null && !error && <Loading />}
      {error && <ErrorState text={error} onRetry={load} />}
      {items && items.length === 0 && <p className="text-[13px] text-[color:var(--mod-faint)]">Категорий пока нет</p>}
      {items && items.length > 0 && (
        <div className="flex flex-col gap-1.5">
          {items.map((c) => (
            <div key={c.categoryId} className="flex items-center gap-2 text-[13px] text-[color:var(--mod-text)]">
              <span className="font-medium">{c.name}</span>
              {c.parentCategoryId && (
                <span className="text-[11.5px] text-[color:var(--mod-faint)]">в «{nameById.get(c.parentCategoryId) ?? c.parentCategoryId}»</span>
              )}
            </div>
          ))}
        </div>
      )}
    </Panel>
  )
}

function TaxRatesSubsection() {
  const [items, setItems] = useState<TaxRate[] | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [percentage, setPercentage] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const [taxRes, catRes] = await Promise.all([adminApi.getTaxRates(), adminApi.getCategories()])
      setItems(taxRes.taxRates)
      setCategories(catRes.categories)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить налоговые ставки')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    const pct = Number(percentage)
    if (!name.trim() || Number.isNaN(pct)) return
    setSubmitting(true)
    setFormError('')
    try {
      await adminApi.createTaxRate(name.trim(), pct, categoryId ? Number(categoryId) : undefined)
      setName('')
      setPercentage('')
      setCategoryId('')
      await load()
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Не удалось создать налоговую ставку')
    } finally {
      setSubmitting(false)
    }
  }

  const categoryNameById = new Map(categories.map((c) => [c.categoryId, c.name]))

  return (
    <Panel className="p-5">
      <div className="mb-1 flex items-center gap-2 text-[14px] font-bold text-[color:var(--mod-text)]">
        <PercentIcon width={16} height={16} />
        Налоговые ставки
      </div>
      <p className="mb-4 text-[12px] text-[color:var(--mod-muted)]">Применяются при расчёте продаж</p>

      <form onSubmit={handleCreate} className="mb-4 flex flex-wrap gap-2">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Название"
          className="min-w-[140px] flex-1 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        <input
          value={percentage}
          onChange={(e) => setPercentage(e.target.value)}
          placeholder="%"
          type="number"
          min={0}
          max={100}
          step="0.01"
          className="w-20 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        <select
          value={categoryId}
          onChange={(e) => setCategoryId(e.target.value)}
          className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        >
          <option value="">Все категории</option>
          {categories.map((c) => (
            <option key={c.categoryId} value={c.categoryId}>
              {c.name}
            </option>
          ))}
        </select>
        <button
          type="submit"
          disabled={submitting || !name.trim() || percentage === ''}
          className="shrink-0 rounded-xl bg-[color:var(--mod-accent)] px-4 py-2.5 text-[13px] font-bold text-white disabled:opacity-50"
        >
          Добавить
        </button>
      </form>
      {formError && <p className="mb-3 text-[12px] font-medium text-[color:var(--mod-danger)]">{formError}</p>}

      {items === null && !error && <Loading />}
      {error && <ErrorState text={error} onRetry={load} />}
      {items && items.length === 0 && <p className="text-[13px] text-[color:var(--mod-faint)]">Ставок пока нет</p>}
      {items && items.length > 0 && (
        <div className="flex flex-col gap-1.5">
          {items.map((t) => (
            <div key={t.taxRateId} className="flex items-center gap-2 text-[13px] text-[color:var(--mod-text)]">
              <span className="font-medium">{t.name}</span>
              <span className="text-[color:var(--mod-accent2)]">{t.percentage}%</span>
              {t.categoryId && (
                <span className="text-[11.5px] text-[color:var(--mod-faint)]">· {categoryNameById.get(t.categoryId) ?? `категория #${t.categoryId}`}</span>
              )}
            </div>
          ))}
        </div>
      )}
    </Panel>
  )
}

function CatalogSection() {
  return (
    <div className="flex flex-col gap-4" style={{ animation: 'mod-fade-in .3s ease' }}>
      <BrandsSubsection />
      <CategoriesSubsection />
      <TaxRatesSubsection />
    </div>
  )
}

/* ---------- shell ---------- */

type ViewId = 'overview' | 'moderation' | 'price-disputes' | 'report-disputes' | 'reports' | 'catalog'

export function ModerationPage() {
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const [view, setView] = useState<ViewId>('overview')
  const isDark = theme === 'dark'

  const priceDisputes = usePendingPriceDisputes()
  const reportDisputes = usePendingReportDisputes()
  const products = usePendingProducts()
  const reports = usePendingReports()

  const navItems: { id: ViewId; label: string; icon: React.ReactNode; badge: number }[] = [
    { id: 'overview', label: 'Обзор', icon: <GridIcon width={18} height={18} />, badge: 0 },
    { id: 'moderation', label: 'Модерация товаров', icon: <PackageIcon width={18} height={18} />, badge: products.items?.length ?? 0 },
    { id: 'price-disputes', label: 'Споры по ценам', icon: <AlertIcon width={18} height={18} />, badge: priceDisputes.items?.length ?? 0 },
    { id: 'report-disputes', label: 'Споры по жалобам', icon: <AlertIcon width={18} height={18} />, badge: reportDisputes.items?.length ?? 0 },
    { id: 'reports', label: 'Жалобы', icon: <ReportIcon width={18} height={18} />, badge: reports.items?.length ?? 0 },
    { id: 'catalog', label: 'Каталог', icon: <TagIcon width={18} height={18} />, badge: 0 },
  ]

  const titles: Record<ViewId, [string, string]> = {
    overview: ['Обзор', 'живой дашборд платформы'],
    moderation: ['Модерация товаров', 'очередь новых товаров'],
    'price-disputes': ['Споры по ценам', 'конфликты цен от пользователей'],
    'report-disputes': ['Споры по жалобам', 'конфликты по жалобам от пользователей'],
    reports: ['Жалобы', 'необработанные жалобы пользователей'],
    catalog: ['Каталог', 'бренды, категории и налоговые ставки'],
  }
  const [title, subtitle] = titles[view]

  return (
    <div
      className="mod-shell"
      style={{
        display: 'flex',
        height: '100vh',
        width: '100%',
        overflow: 'hidden',
        background: 'var(--mod-bg)',
        color: 'var(--mod-text)',
        fontFamily: "'Manrope',system-ui,sans-serif",
      }}
    >
      {/* Sidebar */}
      <aside
        style={{
          width: 246,
          flex: '0 0 246px',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          background: 'var(--mod-panel)',
          borderRight: '1px solid var(--mod-border)',
        }}
      >
        <div style={{ padding: '20px 20px 16px', display: 'flex', alignItems: 'center', gap: 11 }}>
          <div style={{ width: 34, height: 34, borderRadius: 9, background: 'var(--mod-accent)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <LogoMark size={20} />
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1 }}>
            <span style={{ fontWeight: 800, fontSize: 16, letterSpacing: '-.02em' }}>Sarfkor</span>
            <span
              style={{
                marginTop: 4,
                fontFamily: "'JetBrains Mono',monospace",
                fontSize: 9.5,
                fontWeight: 600,
                letterSpacing: '.12em',
                color: 'var(--mod-accent2)',
                textTransform: 'uppercase',
                background: 'var(--mod-accent-dim)',
                padding: '2px 6px',
                borderRadius: 5,
                alignSelf: 'flex-start',
              }}
            >
              Admin
            </span>
          </div>
        </div>

        <nav style={{ flex: 1, overflowY: 'auto', padding: '8px 12px', display: 'flex', flexDirection: 'column', gap: 2 }}>
          {navItems.map((item) => {
            const on = view === item.id
            return (
              <button
                key={item.id}
                onClick={() => setView(item.id)}
                style={{
                  width: '100%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  gap: 8,
                  padding: '10px 12px',
                  borderRadius: 11,
                  border: 'none',
                  cursor: 'pointer',
                  textAlign: 'left',
                  transition: 'all .18s ease',
                  background: on ? 'var(--mod-accent-dim)' : 'transparent',
                  color: on ? 'var(--mod-text)' : 'var(--mod-muted)',
                  fontFamily: 'inherit',
                  boxShadow: on ? 'inset 3px 0 0 var(--mod-accent)' : 'none',
                }}
              >
                <span style={{ display: 'flex', alignItems: 'center', gap: 11, minWidth: 0 }}>
                  <span style={{ width: 18, height: 18, flex: '0 0 18px', opacity: 0.9, display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}>
                    {item.icon}
                  </span>
                  <span style={{ fontSize: 13.5, fontWeight: 600, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{item.label}</span>
                </span>
                {item.badge > 0 && (
                  <span
                    style={{
                      fontFamily: "'JetBrains Mono',monospace",
                      fontSize: 11,
                      fontWeight: 700,
                      minWidth: 20,
                      textAlign: 'center',
                      padding: '1px 6px',
                      borderRadius: 7,
                      color: '#fff',
                      background: 'var(--mod-accent)',
                    }}
                  >
                    {item.badge}
                  </span>
                )}
              </button>
            )
          })}
        </nav>

        <div style={{ padding: 12, borderTop: '1px solid var(--mod-border)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 11, padding: '9px 10px', borderRadius: 11, background: 'var(--mod-panel2)' }}>
            <div style={{ position: 'relative', width: 36, height: 36, flex: '0 0 36px' }}>
              <div
                style={{
                  width: 36,
                  height: 36,
                  borderRadius: '50%',
                  background: 'var(--mod-accent-dim)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 700,
                  fontSize: 13,
                  color: 'var(--mod-accent2)',
                }}
              >
                {user?.email?.charAt(0).toUpperCase() ?? '?'}
              </div>
              <span
                style={{
                  position: 'absolute',
                  right: -1,
                  bottom: -1,
                  width: 11,
                  height: 11,
                  borderRadius: '50%',
                  background: 'var(--mod-accent)',
                  border: '2px solid var(--mod-panel)',
                  animation: 'mod-live-ping 2s ease-in-out infinite',
                }}
              />
            </div>
            <div style={{ minWidth: 0, lineHeight: 1.25, flex: 1 }}>
              <div style={{ fontSize: 13, fontWeight: 700, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{user?.email}</div>
              <div style={{ fontSize: 11, color: 'var(--mod-accent2)', fontWeight: 600 }}>Администратор</div>
            </div>
            <button
              onClick={logout}
              aria-label="Выйти"
              style={{ flex: '0 0 auto', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--mod-muted)', background: 'none', border: 'none', cursor: 'pointer' }}
            >
              <LogOutIcon width={16} height={16} />
            </button>
          </div>
        </div>
      </aside>

      {/* Main */}
      <div style={{ flex: 1, minWidth: 0, height: '100%', display: 'flex', flexDirection: 'column' }}>
        <header
          style={{
            flex: '0 0 auto',
            height: 62,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 26px',
            borderBottom: '1px solid var(--mod-border)',
            background: 'var(--mod-panel)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 13 }}>
            <h1 style={{ margin: 0, fontSize: 18, fontWeight: 800, letterSpacing: '-.02em' }}>{title}</h1>
            <span style={{ fontSize: 12, color: 'var(--mod-muted)', fontWeight: 500 }}>{subtitle}</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 7,
                fontFamily: "'JetBrains Mono',monospace",
                fontSize: 11.5,
                fontWeight: 600,
                color: 'var(--mod-muted)',
                padding: '6px 11px',
                border: '1px solid var(--mod-border)',
                borderRadius: 9,
              }}
            >
              <span style={{ width: 7, height: 7, borderRadius: '50%', background: 'var(--mod-accent)', animation: 'mod-live-ping 1.6s ease-in-out infinite' }} />
              LIVE
            </div>
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              aria-label="Переключить тему"
              style={{
                width: 38,
                height: 38,
                borderRadius: 10,
                border: '1px solid var(--mod-border)',
                background: 'var(--mod-panel2)',
                color: 'var(--mod-text)',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              {isDark ? <SunIcon width={16} height={16} /> : <MoonIcon width={16} height={16} />}
            </button>
          </div>
        </header>

        <main style={{ flex: 1, minHeight: 0, overflowY: 'auto', padding: '24px 26px 40px' }}>
          {view === 'overview' && (
            <OverviewSection
              priceDisputes={priceDisputes.items}
              reportDisputes={reportDisputes.items}
              products={products.items}
              reports={reports.items}
            />
          )}
          {view === 'moderation' && <ModerationSection {...products} />}
          {view === 'price-disputes' && <PriceDisputesSection {...priceDisputes} />}
          {view === 'report-disputes' && <ReportDisputesSection {...reportDisputes} />}
          {view === 'reports' && <ReportsSection {...reports} />}
          {view === 'catalog' && <CatalogSection />}
        </main>
      </div>
    </div>
  )
}
