import { useCallback, useEffect, useState } from 'react'
import clsx from 'clsx'
import { Card } from '../components/Card'
import { AdminModal } from '../components/AdminModal'
import { PackageIcon, AlertIcon, ReportIcon, CheckIcon, XIcon, ClockIcon } from '../components/icons'
import {
  adminApi,
  ApiError,
  REPORT_TYPE_LABELS,
  type ProductSubmission,
  type Report,
  type PriceEntryDispute,
  type ReportDispute,
} from '../../lib/api'

type Tab = 'submissions' | 'reports' | 'price-disputes' | 'report-disputes'

const TABS: { id: Tab; label: string; icon: typeof PackageIcon }[] = [
  { id: 'submissions', label: 'Заявки на товары', icon: PackageIcon },
  { id: 'reports', label: 'Жалобы', icon: AlertIcon },
  { id: 'price-disputes', label: 'Споры о ценах', icon: ReportIcon },
  { id: 'report-disputes', label: 'Споры о жалобах', icon: ReportIcon },
]

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
}

function EmptyState({ label }: { label: string }) {
  return (
    <div className="flex flex-col items-center gap-2 py-16 text-center text-[color:var(--admin-text-tertiary)]">
      <ClockIcon width={28} height={28} />
      <p className="text-[13px]">{label}</p>
    </div>
  )
}

function ActionRow({
  onApprove,
  onReject,
  approveLabel,
  rejectLabel,
  busy,
}: {
  onApprove: () => void
  onReject: () => void
  approveLabel: string
  rejectLabel: string
  busy: boolean
}) {
  return (
    <div className="flex gap-2">
      <button
        onClick={onApprove}
        disabled={busy}
        className="inline-flex items-center gap-1.5 rounded-lg bg-[#34d39922] px-3 py-1.5 text-[12px] font-semibold text-[#34d399] hover:opacity-80 disabled:opacity-50"
      >
        <CheckIcon width={13} height={13} />
        {approveLabel}
      </button>
      <button
        onClick={onReject}
        disabled={busy}
        className="inline-flex items-center gap-1.5 rounded-lg bg-[#f8717122] px-3 py-1.5 text-[12px] font-semibold text-[#f87171] hover:opacity-80 disabled:opacity-50"
      >
        <XIcon width={13} height={13} />
        {rejectLabel}
      </button>
    </div>
  )
}

export function ConsolePage() {
  const [tab, setTab] = useState<Tab>('submissions')

  const [submissions, setSubmissions] = useState<ProductSubmission[] | null>(null)
  const [reports, setReports] = useState<Report[] | null>(null)
  const [priceDisputes, setPriceDisputes] = useState<PriceEntryDispute[] | null>(null)
  const [reportDisputes, setReportDisputes] = useState<ReportDispute[] | null>(null)

  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)
  const [rejectTarget, setRejectTarget] = useState<{ kind: 'submission' | 'report'; id: number } | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const loadAll = useCallback(async () => {
    setError('')
    try {
      const [s, r, pd, rd] = await Promise.all([
        adminApi.getPendingProductSubmissions(),
        adminApi.getPendingReports(),
        adminApi.getPendingPriceEntryDisputes(),
        adminApi.getPendingReportDisputes(),
      ])
      setSubmissions(s.submissions)
      setReports(r.reports)
      setPriceDisputes(pd.disputes)
      setReportDisputes(rd.disputes)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить очередь модерации')
    }
  }, [])

  useEffect(() => {
    loadAll()
  }, [loadAll])

  async function approveSubmission(id: number) {
    setBusyId(id)
    try {
      await adminApi.moderateProductSubmission(id, true)
      setSubmissions((prev) => prev?.filter((s) => s.productSubmissionId !== id) ?? null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось одобрить заявку')
    } finally {
      setBusyId(null)
    }
  }

  async function rejectSubmission(id: number, reason: string) {
    setBusyId(id)
    try {
      await adminApi.moderateProductSubmission(id, false, reason || undefined)
      setSubmissions((prev) => prev?.filter((s) => s.productSubmissionId !== id) ?? null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось отклонить заявку')
    } finally {
      setBusyId(null)
    }
  }

  async function resolveReport(id: number, resolve: boolean, reason?: string) {
    setBusyId(id)
    try {
      await adminApi.moderateReport(id, resolve, reason || undefined)
      setReports((prev) => prev?.filter((r) => r.reportId !== id) ?? null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось обработать жалобу')
    } finally {
      setBusyId(null)
    }
  }

  async function resolvePriceDispute(id: number, uphold: boolean) {
    setBusyId(id)
    try {
      await adminApi.resolvePriceEntryDispute(id, uphold)
      setPriceDisputes((prev) => prev?.filter((d) => d.disputeId !== id) ?? null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось обработать спор')
    } finally {
      setBusyId(null)
    }
  }

  async function resolveReportDispute(id: number, uphold: boolean) {
    setBusyId(id)
    try {
      await adminApi.resolveReportDispute(id, uphold)
      setReportDisputes((prev) => prev?.filter((d) => d.disputeId !== id) ?? null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось обработать спор')
    } finally {
      setBusyId(null)
    }
  }

  function openReject(kind: 'submission' | 'report', id: number) {
    setRejectTarget({ kind, id })
    setRejectReason('')
  }

  async function confirmReject() {
    if (!rejectTarget) return
    const { kind, id } = rejectTarget
    setRejectTarget(null)
    if (kind === 'submission') await rejectSubmission(id, rejectReason)
    else await resolveReport(id, false, rejectReason)
  }

  const counts: Record<Tab, number> = {
    submissions: submissions?.length ?? 0,
    reports: reports?.length ?? 0,
    'price-disputes': priceDisputes?.length ?? 0,
    'report-disputes': reportDisputes?.length ?? 0,
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-[22px] font-extrabold tracking-tight">Модерация</h1>
        <p className="text-[13px] text-[color:var(--admin-text-tertiary)]">
          Заявки на новые товары, жалобы пользователей и споры, требующие решения платформы
        </p>
      </div>

      {error && (
        <div className="rounded-lg bg-[#f8717118] px-3.5 py-2.5 text-[12.5px] font-medium text-[#f87171]">{error}</div>
      )}

      <div className="flex flex-wrap gap-2">
        {TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={clsx(
              'flex items-center gap-2 rounded-xl px-4 py-2.5 text-[13px] font-semibold transition-colors',
              tab === t.id
                ? 'bg-[color:var(--admin-accent)] text-white'
                : 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]',
            )}
          >
            <t.icon width={15} height={15} />
            {t.label}
            {counts[t.id] > 0 && (
              <span
                className={clsx(
                  'grid h-5 min-w-5 place-items-center rounded-full px-1 text-[11px] font-bold',
                  tab === t.id ? 'bg-white/25' : 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
                )}
              >
                {counts[t.id]}
              </span>
            )}
          </button>
        ))}
      </div>

      {tab === 'submissions' && (
        <Card className="p-5">
          {submissions === null ? null : submissions.length === 0 ? (
            <EmptyState label="Нет заявок на новые товары" />
          ) : (
            <div className="flex flex-col gap-3">
              {submissions.map((s) => (
                <div
                  key={s.productSubmissionId}
                  className="flex flex-col gap-3 rounded-xl bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="min-w-0">
                    <div className="font-semibold text-[color:var(--admin-text)]">{s.name}</div>
                    <div className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                      Штрихкод {s.barcode} · Категория #{s.categoryId} · Бренд #{s.brandId} · {s.countryOfOrigin} ·{' '}
                      {fmtDate(s.createdAt)}
                    </div>
                  </div>
                  <ActionRow
                    approveLabel="Одобрить"
                    rejectLabel="Отклонить"
                    busy={busyId === s.productSubmissionId}
                    onApprove={() => approveSubmission(s.productSubmissionId)}
                    onReject={() => openReject('submission', s.productSubmissionId)}
                  />
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {tab === 'reports' && (
        <Card className="p-5">
          {reports === null ? null : reports.length === 0 ? (
            <EmptyState label="Нет жалоб, ожидающих рассмотрения" />
          ) : (
            <div className="flex flex-col gap-3">
              {reports.map((r) => (
                <div
                  key={r.reportId}
                  className="flex flex-col gap-3 rounded-xl bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="min-w-0">
                    <div className="font-semibold text-[color:var(--admin-text)]">
                      {REPORT_TYPE_LABELS[r.type] ?? `Тип ${r.type}`} · Товар #{r.productId}
                      {r.storeId ? ` · Магазин #${r.storeId}` : ''}
                    </div>
                    <div className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                      {r.description} · {fmtDate(r.createdAt)}
                    </div>
                  </div>
                  <ActionRow
                    approveLabel="Разрешить"
                    rejectLabel="Отклонить"
                    busy={busyId === r.reportId}
                    onApprove={() => resolveReport(r.reportId, true)}
                    onReject={() => openReject('report', r.reportId)}
                  />
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {tab === 'price-disputes' && (
        <Card className="p-5">
          {priceDisputes === null ? null : priceDisputes.length === 0 ? (
            <EmptyState label="Нет споров о ценах" />
          ) : (
            <div className="flex flex-col gap-3">
              {priceDisputes.map((d) => (
                <div
                  key={d.disputeId}
                  className="flex flex-col gap-3 rounded-xl bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="min-w-0">
                    <div className="font-semibold text-[color:var(--admin-text)]">Цена #{d.priceEntryId}</div>
                    <div className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                      «{d.reason}» · {fmtDate(d.createdAt)}
                    </div>
                  </div>
                  <ActionRow
                    approveLabel="Оставить в силе"
                    rejectLabel="Отклонить"
                    busy={busyId === d.disputeId}
                    onApprove={() => resolvePriceDispute(d.disputeId, true)}
                    onReject={() => resolvePriceDispute(d.disputeId, false)}
                  />
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {tab === 'report-disputes' && (
        <Card className="p-5">
          {reportDisputes === null ? null : reportDisputes.length === 0 ? (
            <EmptyState label="Нет споров по жалобам" />
          ) : (
            <div className="flex flex-col gap-3">
              {reportDisputes.map((d) => (
                <div
                  key={d.disputeId}
                  className="flex flex-col gap-3 rounded-xl bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="min-w-0">
                    <div className="font-semibold text-[color:var(--admin-text)]">Жалоба #{d.reportId}</div>
                    <div className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                      «{d.reason}» · {fmtDate(d.createdAt)}
                    </div>
                  </div>
                  <ActionRow
                    approveLabel="Оставить в силе"
                    rejectLabel="Отклонить"
                    busy={busyId === d.disputeId}
                    onApprove={() => resolveReportDispute(d.disputeId, true)}
                    onReject={() => resolveReportDispute(d.disputeId, false)}
                  />
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      <AdminModal open={!!rejectTarget} onClose={() => setRejectTarget(null)} title="Причина отклонения (необязательно)">
        <div className="flex flex-col gap-4">
          <textarea
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            rows={3}
            placeholder="Например: дубликат, некорректные данные..."
            className="w-full resize-none rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
          <button
            onClick={confirmReject}
            className="rounded-xl bg-[#f87171] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98]"
          >
            Отклонить
          </button>
        </div>
      </AdminModal>
    </div>
  )
}
