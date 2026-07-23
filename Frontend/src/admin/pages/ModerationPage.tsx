import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { LogoMark } from '../../components/Logo'
import { useAuth } from '../../auth/AuthContext'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../../components/icons'
import { LogOutIcon, CheckIcon, XIcon, ShieldIcon, ReportIcon, PackageIcon, AlertIcon } from '../components/icons'
import {
  adminApi,
  ApiError,
  type PriceEntryDispute,
  type ReportDispute,
  type ProductSubmission,
  type Report,
} from '../../lib/api'

const REPORT_TYPE_LABEL: Record<Report['type'], string> = {
  WrongPrice: 'Неверная цена',
  OutOfStock: 'Нет в наличии',
  ReceiptMismatch: 'Расхождение с чеком',
  Other: 'Другое',
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

function shortUserId(id: string) {
  return id.length > 10 ? `${id.slice(0, 8)}…` : id
}

function EmptyState({ text }: { text: string }) {
  return (
    <Card className="p-10 text-center">
      <p className="text-[14px] text-[color:var(--admin-text-tertiary)]">{text}</p>
    </Card>
  )
}

function ErrorState({ text, onRetry }: { text: string; onRetry: () => void }) {
  return (
    <Card className="p-8 text-center">
      <p className="mb-4 text-[14px] text-[color:var(--admin-text-secondary)]">{text}</p>
      <button
        onClick={onRetry}
        className="rounded-xl bg-[color:var(--admin-accent)] px-5 py-2.5 text-[13px] font-semibold text-white hover:opacity-90"
      >
        Повторить
      </button>
    </Card>
  )
}

function ModerationItemRow({
  icon,
  title,
  subtitle,
  meta,
  approveLabel,
  rejectLabel,
  busy,
  onApprove,
  onReject,
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
}) {
  return (
    <Card className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex min-w-0 items-start gap-3.5">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-[12px] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]">
          {icon}
        </span>
        <div className="min-w-0">
          <div className="truncate text-[14px] font-semibold text-[color:var(--admin-text)]">{title}</div>
          <div className="mt-0.5 text-[13px] text-[color:var(--admin-text-secondary)]">{subtitle}</div>
          <div className="mt-1 text-[11.5px] text-[color:var(--admin-text-tertiary)]">{meta}</div>
        </div>
      </div>
      <div className="flex shrink-0 gap-2 self-stretch sm:self-auto">
        <button
          onClick={onReject}
          disabled={busy}
          className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-[#f8717122] px-3.5 py-2 text-[13px] font-semibold text-[#f87171] transition-opacity hover:opacity-80 disabled:opacity-40 sm:flex-none"
        >
          <XIcon width={14} height={14} />
          {rejectLabel}
        </button>
        <button
          onClick={onApprove}
          disabled={busy}
          className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-[#34d39922] px-3.5 py-2 text-[13px] font-semibold text-[#34d399] transition-opacity hover:opacity-80 disabled:opacity-40 sm:flex-none"
        >
          <CheckIcon width={14} height={14} />
          {approveLabel}
        </button>
      </div>
    </Card>
  )
}

function PriceDisputesSection() {
  const [items, setItems] = useState<PriceEntryDispute[] | null>(null)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getPendingPriceEntryDisputes()
      setItems(res.disputes)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить споры по ценам')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function resolve(disputeId: number, uphold: boolean) {
    setBusyId(disputeId)
    try {
      await adminApi.resolvePriceEntryDispute(disputeId, uphold)
      setItems((cur) => cur?.filter((d) => d.disputeId !== disputeId) ?? null)
    } catch {
      // load() below surfaces a fresh error/state if the action failed server-side
      await load()
    } finally {
      setBusyId(null)
    }
  }

  if (items === null && !error) return <div className="py-16 text-center text-[color:var(--admin-text-tertiary)]">Загрузка…</div>
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Нет споров по ценам, ожидающих решения" />

  return (
    <div className="flex flex-col gap-3">
      {items!.map((d) => (
        <ModerationItemRow
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

function ReportDisputesSection() {
  const [items, setItems] = useState<ReportDispute[] | null>(null)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getPendingReportDisputes()
      setItems(res.disputes)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить споры по жалобам')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

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

  if (items === null && !error) return <div className="py-16 text-center text-[color:var(--admin-text-tertiary)]">Загрузка…</div>
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Нет споров по жалобам, ожидающих решения" />

  return (
    <div className="flex flex-col gap-3">
      {items!.map((d) => (
        <ModerationItemRow
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

function ProductSubmissionsSection() {
  const [items, setItems] = useState<ProductSubmission[] | null>(null)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getPendingProductSubmissions()
      setItems(res.submissions)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить новые товары')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function moderate(submissionId: number, approve: boolean) {
    setBusyId(submissionId)
    try {
      await adminApi.moderateProductSubmission(submissionId, approve)
      setItems((cur) => cur?.filter((s) => s.submissionId !== submissionId) ?? null)
    } catch {
      await load()
    } finally {
      setBusyId(null)
    }
  }

  if (items === null && !error) return <div className="py-16 text-center text-[color:var(--admin-text-tertiary)]">Загрузка…</div>
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Нет новых товаров на модерации" />

  return (
    <div className="flex flex-col gap-3">
      {items!.map((s) => (
        <ModerationItemRow
          key={s.submissionId}
          icon={<PackageIcon width={18} height={18} />}
          title={s.name}
          subtitle={`Штрихкод ${s.barcode} · Страна: ${s.countryOfOrigin}`}
          meta={`от ${shortUserId(s.submittedByUserId)} · ${fmtDate(s.createdAt)}`}
          approveLabel="Одобрить"
          rejectLabel="Отклонить"
          busy={busyId === s.submissionId}
          onApprove={() => moderate(s.submissionId, true)}
          onReject={() => moderate(s.submissionId, false)}
        />
      ))}
    </div>
  )
}

function ReportsSection() {
  const [items, setItems] = useState<Report[] | null>(null)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getPendingReports()
      setItems(res.reports)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить жалобы')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

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

  if (items === null && !error) return <div className="py-16 text-center text-[color:var(--admin-text-tertiary)]">Загрузка…</div>
  if (error) return <ErrorState text={error} onRetry={load} />
  if (items!.length === 0) return <EmptyState text="Нет жалоб, ожидающих рассмотрения" />

  return (
    <div className="flex flex-col gap-3">
      {items!.map((r) => (
        <ModerationItemRow
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

type TabId = 'price-disputes' | 'report-disputes' | 'products' | 'reports'

const TABS: { id: TabId; label: string; icon: (p: { width: number; height: number }) => React.ReactNode }[] = [
  { id: 'price-disputes', label: 'Споры по ценам', icon: (p) => <AlertIcon {...p} /> },
  { id: 'report-disputes', label: 'Споры по жалобам', icon: (p) => <AlertIcon {...p} /> },
  { id: 'products', label: 'Новые товары', icon: (p) => <PackageIcon {...p} /> },
  { id: 'reports', label: 'Жалобы', icon: (p) => <ReportIcon {...p} /> },
]

export function ModerationPage() {
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const [tab, setTab] = useState<TabId>('price-disputes')
  const isDark = theme === 'dark'

  return (
    <div className="admin-shell min-h-screen bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <header className="flex items-center gap-4 border-b border-[color:var(--admin-border)] px-6 py-4">
        <div className="flex items-center gap-2.5">
          <LogoMark size={26} />
          <span className="text-[17px] font-extrabold tracking-tight">Sarfkor</span>
          <span className="ml-1 flex items-center gap-1.5 rounded-full bg-[color:var(--admin-accent-soft)] px-2.5 py-1 text-[11px] font-bold uppercase tracking-wide text-[color:var(--admin-accent)]">
            <ShieldIcon width={12} height={12} />
            Модерация
          </span>
        </div>

        <div className="flex-1" />

        <button
          onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
          aria-label="Переключить тему"
          className="grid h-9 w-9 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
        >
          {isDark ? <SunIcon width={17} height={17} /> : <MoonIcon width={17} height={17} />}
        </button>

        <div className="hidden shrink-0 items-center gap-2.5 border-l border-[color:var(--admin-border)] pl-4 sm:flex">
          <div
            className="grid h-9 w-9 place-items-center rounded-xl text-[15px] font-bold text-white"
            style={{ background: 'linear-gradient(135deg,#818cf8,#6366f1)' }}
          >
            {user?.email?.charAt(0).toUpperCase() ?? '?'}
          </div>
          <div className="hidden lg:block">
            <div className="max-w-[180px] truncate text-[13px] font-semibold leading-tight">{user?.email}</div>
            <div className="text-[11px] leading-tight text-[color:var(--admin-text-tertiary)]">Администратор</div>
          </div>
        </div>

        <button
          onClick={logout}
          aria-label="Выйти"
          className="grid h-9 w-9 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]"
        >
          <LogOutIcon width={17} height={17} />
        </button>
      </header>

      <main className="mx-auto max-w-[900px] px-6 py-8">
        <div className="mb-1 text-[22px] font-extrabold tracking-tight">Модерация платформы</div>
        <p className="mb-6 text-[13.5px] text-[color:var(--admin-text-tertiary)]">
          Споры, новые товары и жалобы, ожидающие решения
        </p>

        <div className="mb-6 flex flex-wrap gap-2">
          {TABS.map((t) => (
            <button
              key={t.id}
              onClick={() => setTab(t.id)}
              className={
                tab === t.id
                  ? 'flex items-center gap-2 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[13px] font-semibold text-white'
                  : 'flex items-center gap-2 rounded-xl bg-[color:var(--admin-card)] px-4 py-2.5 text-[13px] font-medium text-[color:var(--admin-text-secondary)] ring-1 ring-[color:var(--admin-border)] hover:bg-[color:var(--admin-hover)]'
              }
            >
              {t.icon({ width: 15, height: 15 })}
              {t.label}
            </button>
          ))}
        </div>

        {tab === 'price-disputes' && <PriceDisputesSection />}
        {tab === 'report-disputes' && <ReportDisputesSection />}
        {tab === 'products' && <ProductSubmissionsSection />}
        {tab === 'reports' && <ReportsSection />}
      </main>
    </div>
  )
}
