import { useCallback, useEffect, useState } from 'react'
import { SidePanel, FieldRow } from './SidePanel'
import { ReasonModal } from './ReasonModal'
import { Loading } from './Loading'
import { ErrorState } from './ErrorState'
import { EmptyState } from './EmptyState'
import { Badge } from './Badge'
import { ShieldIcon, StoreIcon, PlusIcon, MinusIcon } from './icons'
import { adminUsersApi, ApiError, type AdminUserDetail, type TrustScoreAdjustment } from '../../lib/api'

function fmtDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

function fmtDateTime(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

function AdjustScoreForm({ userId, onAdjusted }: { userId: string; onAdjusted: () => void }) {
  const [open, setOpen] = useState(false)
  const [delta, setDelta] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function submit() {
    const n = Number(delta)
    if (!delta || Number.isNaN(n) || !reason.trim() || busy) return
    setBusy(true)
    setError('')
    try {
      await adminUsersApi.adjustTrustScore(userId, n, reason.trim())
      setDelta('')
      setReason('')
      setOpen(false)
      onAdjusted()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось изменить рейтинг')
    } finally {
      setBusy(false)
    }
  }

  if (!open) {
    return (
      <button onClick={() => setOpen(true)} className="flex items-center gap-1.5 rounded-xl border border-[color:var(--mod-border)] px-3.5 py-2 text-[12px] font-bold text-[color:var(--mod-text)] hover:bg-[color:var(--mod-panel2)]">
        <PlusIcon width={13} height={13} />
        Скорректировать вручную
      </button>
    )
  }

  return (
    <div className="rounded-xl bg-[color:var(--mod-panel2)] p-3.5">
      <div className="mb-2 flex gap-2">
        <button onClick={() => setDelta((d) => (d.startsWith('-') ? d.slice(1) : `${Math.abs(Number(d) || 0) || ''}`))} className={`rounded-lg px-3 py-1.5 text-[12px] font-bold ${!delta.startsWith('-') ? 'bg-[color:var(--mod-ok)] text-white' : 'text-[color:var(--mod-muted)]'}`}>
          <PlusIcon width={12} height={12} />
        </button>
        <input
          value={delta.replace('-', '')}
          onChange={(e) => setDelta((delta.startsWith('-') ? '-' : '') + e.target.value.replace(/[^0-9.]/g, ''))}
          placeholder="Δ баллов"
          className="w-24 rounded-lg border border-[color:var(--mod-border)] bg-[color:var(--mod-panel)] px-2.5 py-1.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        <button onClick={() => setDelta((d) => (d.startsWith('-') ? d : `-${d}`))} className={`rounded-lg px-3 py-1.5 text-[12px] font-bold ${delta.startsWith('-') ? 'bg-[color:var(--mod-danger)] text-white' : 'text-[color:var(--mod-muted)]'}`}>
          <MinusIcon width={12} height={12} />
        </button>
      </div>
      <textarea
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        rows={2}
        placeholder="Причина корректировки (обязательно)…"
        className="mb-2 w-full resize-none rounded-lg border border-[color:var(--mod-border)] bg-[color:var(--mod-panel)] px-2.5 py-1.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
      />
      {error && <p className="mb-2 text-[12px] font-medium text-[color:var(--mod-danger)]">{error}</p>}
      <div className="flex gap-2">
        <button onClick={submit} disabled={busy || !delta || !reason.trim()} className="rounded-lg bg-[color:var(--mod-accent)] px-3.5 py-1.5 text-[12px] font-bold text-white disabled:opacity-50">
          {busy ? 'Секунду…' : 'Применить'}
        </button>
        <button onClick={() => setOpen(false)} className="rounded-lg border border-[color:var(--mod-border)] px-3.5 py-1.5 text-[12px] font-semibold text-[color:var(--mod-text)]">
          Отмена
        </button>
      </div>
    </div>
  )
}

function TrustScoreSection({ userId, currentScore }: { userId: string; currentScore?: number }) {
  const [history, setHistory] = useState<TrustScoreAdjustment[] | null>(null)
  const [score, setScore] = useState<number | undefined>(currentScore)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminUsersApi.getTrustScoreHistory(userId)
      setHistory(res.history)
      setScore(res.currentScore)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить историю рейтинга')
    }
  }, [userId])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <div>
          <div className="text-[11.5px] font-semibold uppercase tracking-wide text-[color:var(--mod-faint)]">Рейтинг доверия</div>
          <div className="font-[JetBrains_Mono,monospace] text-[26px] font-bold text-[color:var(--mod-text)]">{score ?? '—'}</div>
        </div>
        <AdjustScoreForm userId={userId} onAdjusted={load} />
      </div>
      {history === null && !error && <Loading scheme="mod" />}
      {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
      {history && history.length === 0 && <p className="text-[12.5px] text-[color:var(--mod-faint)]">Изменений рейтинга ещё не было</p>}
      {history && history.length > 0 && (
        <div className="flex flex-col gap-1.5">
          {history.map((h, i) => (
            <div key={i} className="flex items-center justify-between gap-2 rounded-lg bg-[color:var(--mod-panel2)] px-3 py-2">
              <div className="min-w-0">
                <div className="truncate text-[12px] text-[color:var(--mod-text)]">{h.reason}</div>
                <div className="text-[10.5px] text-[color:var(--mod-faint)]">
                  {h.isManual ? 'вручную' : 'автоматически'} · {fmtDateTime(h.occurredAt)}
                </div>
              </div>
              <span className={`shrink-0 font-[JetBrains_Mono,monospace] text-[13px] font-bold ${h.delta >= 0 ? 'text-[color:var(--mod-ok)]' : 'text-[color:var(--mod-danger)]'}`}>
                {h.delta >= 0 ? '+' : ''}
                {h.delta}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export function UserDetailPanel({ userId, onClose }: { userId: string; onClose: () => void }) {
  const [detail, setDetail] = useState<AdminUserDetail | null>(null)
  const [error, setError] = useState('')
  const [blockOpen, setBlockOpen] = useState(false)
  const [unblockOpen, setUnblockOpen] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      setDetail(await adminUsersApi.getUserDetail(userId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить пользователя')
    }
  }, [userId])

  useEffect(() => {
    setDetail(null)
    load()
  }, [load])

  return (
    <SidePanel
      open
      onClose={onClose}
      title={
        <span className="flex items-center gap-2">
          <ShieldIcon width={16} height={16} className="shrink-0 text-[color:var(--mod-faint)]" />
          {detail?.email ?? userId}
        </span>
      }
      subtitle={detail?.roles.join(', ')}
    >
      {detail === null && !error && <Loading scheme="mod" />}
      {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
      {detail && detail.outcome === 'NotFound' && <EmptyState scheme="mod" title="Пользователь не найден" />}
      {detail && detail.outcome === 'Found' && (
        <div className="flex flex-col gap-5">
          <div className="rounded-xl bg-[color:var(--mod-panel2)] p-4">
            <FieldRow label="Email" value={detail.email ?? '—'} />
            <FieldRow label="Регистрация" value={fmtDate(detail.createdAt)} />
            <FieldRow label="Роли" value={detail.roles.join(', ') || '—'} />
            <FieldRow
              label="Статус"
              value={
                detail.isBlocked ? (
                  <Badge scheme="mod" variant="danger" size="sm">Заблокирован</Badge>
                ) : (
                  <Badge scheme="mod" variant="success" size="sm">Активен</Badge>
                )
              }
            />
            {detail.isBlocked && detail.blockedReason && <FieldRow label="Причина блокировки" value={detail.blockedReason} />}
            <FieldRow label="Отправлено цен" value={detail.priceSubmissionsTotal} />
            <FieldRow label="Из них подтверждено" value={detail.priceSubmissionsVerified} />
            <FieldRow label="Жалоб за 90 дней" value={detail.reportsAgainstLast90Days} />
          </div>

          <div>
            {detail.isBlocked ? (
              <button
                onClick={() => setUnblockOpen(true)}
                className="rounded-xl border border-[color:var(--mod-border)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--mod-text)] hover:bg-[color:var(--mod-panel2)]"
              >
                Разблокировать
              </button>
            ) : (
              <button
                onClick={() => setBlockOpen(true)}
                className="rounded-xl border border-[color:var(--mod-danger)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--mod-danger)] hover:bg-[color:var(--mod-danger-dim)]"
              >
                Заблокировать
              </button>
            )}
          </div>

          <div className="border-t border-[color:var(--mod-border)] pt-4">
            <TrustScoreSection userId={userId} currentScore={detail.trustScore} />
          </div>

          <div className="border-t border-[color:var(--mod-border)] pt-4">
            <div className="mb-2 flex items-center gap-2 text-[11.5px] font-semibold uppercase tracking-wide text-[color:var(--mod-faint)]">
              <StoreIcon width={13} height={13} />
              Магазины
            </div>
            {detail.stores.length === 0 && <p className="text-[12.5px] text-[color:var(--mod-faint)]">Не связан ни с одним магазином</p>}
            {detail.stores.length > 0 && (
              <div className="flex flex-col gap-1.5">
                {detail.stores.map((s) => (
                  <div key={s.storeId} className="flex items-center justify-between gap-2 rounded-lg bg-[color:var(--mod-panel2)] px-3 py-2 text-[12.5px]">
                    <span className="truncate font-semibold text-[color:var(--mod-text)]">{s.storeName}</span>
                    <span className="shrink-0 text-[11px] text-[color:var(--mod-faint)]">{s.relationship}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      <ReasonModal
        open={blockOpen}
        onClose={() => setBlockOpen(false)}
        title="Заблокировать пользователя"
        description="Пользователь не сможет войти, а все активные сессии завершатся немедленно."
        confirmLabel="Заблокировать"
        danger
        onConfirm={async (reason) => {
          await adminUsersApi.blockUser(userId, reason)
          load()
        }}
      />
      <ReasonModal
        open={unblockOpen}
        onClose={() => setUnblockOpen(false)}
        title="Разблокировать пользователя"
        confirmLabel="Разблокировать"
        onConfirm={async (reason) => {
          await adminUsersApi.unblockUser(userId, reason)
          load()
        }}
      />
    </SidePanel>
  )
}
