import { useCallback, useEffect, useState } from 'react'
import { SidePanel, PanelTabs, FieldRow } from './SidePanel'
import { ReasonModal } from './ReasonModal'
import { StoreStatusBadge, SubscriptionStatusBadge } from './StatusBadge'
import { Loading } from './Loading'
import { ErrorState, classifyError, type ErrorKind } from './ErrorState'
import { EmptyState } from './EmptyState'
import { Select } from './Select'
import { DateField } from './DateField'
import { AuditLogRow } from './AuditLogRow'
import { CheckIcon, StoreIcon, UsersIcon, ShieldIcon, ClockIcon } from './icons'
import {
  adminApi,
  subscriptionsApi,
  type AdminStoreDetail,
  type AdminStoreLocation,
  type AdminStoreEmployee,
  type AdminStoreDiagnostics,
  type AuditLogEntry,
  type StoreStatus,
  type SubscriptionPlan,
  type SubscriptionPayment,
} from '../../lib/api'

type TabId = 'profile' | 'subscription' | 'employees' | 'locations' | 'diagnostics' | 'history'

const TABS: { id: TabId; label: string }[] = [
  { id: 'profile', label: 'Профиль' },
  { id: 'subscription', label: 'Подписка и платежи' },
  { id: 'employees', label: 'Сотрудники' },
  { id: 'locations', label: 'Торговые точки' },
  { id: 'diagnostics', label: 'Диагностика' },
  { id: 'history', label: 'История' },
]

const TRANSITIONS: Record<StoreStatus, { to: StoreStatus; label: string; danger: boolean }[]> = {
  PendingApproval: [{ to: 'Rejected', label: 'Отклонить', danger: true }],
  Active: [
    { to: 'Suspended', label: 'Приостановить', danger: true },
    { to: 'Blocked', label: 'Заблокировать', danger: true },
    { to: 'Archived', label: 'Архивировать', danger: true },
  ],
  Suspended: [
    { to: 'Active', label: 'Снять приостановку', danger: false },
    { to: 'Blocked', label: 'Заблокировать', danger: true },
    { to: 'Archived', label: 'Архивировать', danger: true },
  ],
  Blocked: [
    { to: 'Active', label: 'Разблокировать', danger: false },
    { to: 'Archived', label: 'Архивировать', danger: true },
  ],
  Archived: [],
  Rejected: [],
}

function fmtDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

function fmtDateTime(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

/* ---------- Профиль ---------- */

function ProfileTab({ detail, onChanged }: { detail: AdminStoreDetail; onChanged: () => void }) {
  const [pendingTransition, setPendingTransition] = useState<{ to: StoreStatus; label: string; danger: boolean } | null>(null)
  const [approveBusy, setApproveBusy] = useState(false)
  const [approveError, setApproveError] = useState('')
  const [isVatPayer, setIsVatPayer] = useState(detail.isVatPayer ?? true)
  const [taxRegime, setTaxRegime] = useState(detail.taxRegime ?? 'General')
  const [taxBusy, setTaxBusy] = useState(false)
  const [taxError, setTaxError] = useState('')
  const [taxSaved, setTaxSaved] = useState(false)

  async function handleApprove() {
    setApproveBusy(true)
    setApproveError('')
    try {
      await adminApi.approveStore(detail.storeId)
      onChanged()
    } catch (err) {
      console.error('Не удалось одобрить магазин:', err)
      setApproveError('Не удалось одобрить магазин')
    } finally {
      setApproveBusy(false)
    }
  }

  async function handleSaveTax() {
    setTaxBusy(true)
    setTaxError('')
    setTaxSaved(false)
    try {
      await adminApi.updateStoreTaxSettings(detail.storeId, isVatPayer, taxRegime)
      setTaxSaved(true)
      onChanged()
    } catch (err) {
      console.error('Не удалось сохранить настройки налогов:', err)
      setTaxError('Не удалось сохранить настройки налогов')
    } finally {
      setTaxBusy(false)
    }
  }

  const transitions = detail.status ? TRANSITIONS[detail.status] : []

  return (
    <div className="flex flex-col gap-5">
      <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
        <FieldRow label="Название" value={detail.name} />
        <FieldRow label="Адрес" value={detail.address} />
        <FieldRow label="Владелец" value={detail.ownerEmail ?? detail.ownerUserId} />
        <FieldRow label="Статус" value={detail.status && <StoreStatusBadge status={detail.status} size="sm" />} />
        {detail.statusReason && <FieldRow label="Причина статуса" value={detail.statusReason} />}
        <FieldRow label="Статус изменён" value={fmtDateTime(detail.statusChangedAt)} />
      </div>

      <div>
        <div className="mb-2 text-[12px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">Действия со статусом</div>
        <div className="flex flex-wrap gap-2">
          {detail.status === 'PendingApproval' && (
            <button
              onClick={handleApprove}
              disabled={approveBusy}
              className="flex items-center gap-1.5 rounded-xl bg-[color:var(--admin-success)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--admin-success-fg)] transition-transform hover:brightness-110 active:scale-95 disabled:opacity-50"
            >
              <CheckIcon width={14} height={14} />
              {approveBusy ? 'Секунду…' : 'Одобрить магазин'}
            </button>
          )}
          {transitions.map((t) => (
            <button
              key={t.to}
              onClick={() => setPendingTransition(t)}
              className={`rounded-xl border px-4 py-2.5 text-[12.5px] font-bold transition-colors ${
                t.danger
                  ? 'border-[color:var(--admin-danger)] text-[color:var(--admin-danger)] hover:bg-[color:var(--admin-danger-dim)]'
                  : 'border-[color:var(--admin-border)] text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]'
              }`}
            >
              {t.label}
            </button>
          ))}
          {transitions.length === 0 && detail.status !== 'PendingApproval' && (
            <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">Это финальный статус — переходов нет.</p>
          )}
        </div>
        {approveError && <p className="mt-2 text-[12px] font-medium text-[color:var(--admin-danger)]">{approveError}</p>}
      </div>

      <div>
        <div className="mb-2 text-[12px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">Налоговый режим</div>
        <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
          <label className="mb-3 flex items-center gap-2.5 text-[13px] font-semibold text-[color:var(--admin-text)]">
            <input type="checkbox" checked={isVatPayer} onChange={(e) => setIsVatPayer(e.target.checked)} className="h-4 w-4 accent-[color:var(--admin-accent)]" />
            Плательщик НДС
          </label>
          <Select
            scheme="admin"
            value={taxRegime}
            onChange={(v) => setTaxRegime(v as 'General' | 'Simplified')}
            options={[
              { value: 'General', label: 'Общий режим' },
              { value: 'Simplified', label: 'Упрощённый режим' },
            ]}
          />
          <button
            onClick={handleSaveTax}
            disabled={taxBusy}
            className="mt-3 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2 text-[12.5px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:brightness-110 active:scale-95 disabled:opacity-50"
          >
            {taxBusy ? 'Секунду…' : taxSaved ? 'Сохранено ✓' : 'Сохранить'}
          </button>
          {taxError && <p className="mt-2 text-[12px] font-medium text-[color:var(--admin-danger)]">{taxError}</p>}
        </div>
      </div>

      <ReasonModal
        open={!!pendingTransition}
        onClose={() => setPendingTransition(null)}
        title={pendingTransition?.label ?? ''}
        description={`Магазин «${detail.name}» перейдёт в статус «${pendingTransition?.to}». Причина попадёт в журнал действий.`}
        confirmLabel={pendingTransition?.label ?? 'Подтвердить'}
        danger={pendingTransition?.danger}
        onConfirm={async (reason) => {
          if (!pendingTransition) return
          await adminApi.changeStoreStatus(detail.storeId, pendingTransition.to, reason)
          onChanged()
        }}
      />
    </div>
  )
}

/* ---------- Подписка и платежи ---------- */

function SubscriptionTab({ detail, onChanged }: { detail: AdminStoreDetail; onChanged: () => void }) {
  const [plans, setPlans] = useState<SubscriptionPlan[] | null>(null)
  const [payments, setPayments] = useState<SubscriptionPayment[] | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const [planBusy, setPlanBusy] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)
  const [payAmount, setPayAmount] = useState('')
  const [payMethod, setPayMethod] = useState<'Cash' | 'BankTransfer' | 'Card' | 'Other'>('Cash')
  const [payFrom, setPayFrom] = useState('')
  const [payTo, setPayTo] = useState('')
  const [payComment, setPayComment] = useState('')
  const [payBusy, setPayBusy] = useState(false)
  const [payError, setPayError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const [plansRes, paymentsRes] = await Promise.all([
        subscriptionsApi.getSubscriptionPlans(),
        subscriptionsApi.getSubscriptionPayments({ storeId: detail.storeId, take: 20 }),
      ])
      setPlans(plansRes.plans)
      setPayments(paymentsRes.payments)
    } catch (err) {
      console.error('Не удалось загрузить подписку:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить подписку')
    }
  }, [detail.storeId])

  useEffect(() => {
    load()
  }, [load])

  async function handleChangePlan() {
    if (!detail.subscription || !selectedPlanId) return
    setPlanBusy(true)
    try {
      await subscriptionsApi.changeStoreSubscriptionPlan(detail.subscription.storeSubscriptionId, Number(selectedPlanId))
      setSelectedPlanId('')
      onChanged()
    } catch (err) {
      console.error('Не удалось сменить тариф:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось сменить тариф')
    } finally {
      setPlanBusy(false)
    }
  }

  async function handleRecordPayment() {
    if (!detail.subscription || !payAmount || !payFrom || !payTo || payBusy) return
    setPayBusy(true)
    setPayError('')
    try {
      await subscriptionsApi.recordSubscriptionPayment(detail.subscription.storeSubscriptionId, {
        amount: Number(payAmount),
        currency: detail.subscription.priceAtIssueCurrency || 'TJS',
        periodStart: payFrom,
        periodEnd: payTo,
        method: payMethod,
        comment: payComment.trim() || undefined,
      })
      setPayAmount('')
      setPayComment('')
      await load()
      onChanged()
    } catch (err) {
      console.error('Не удалось записать платёж:', err)
      setPayError('Не удалось записать платёж')
    } finally {
      setPayBusy(false)
    }
  }

  if (!detail.subscription) {
    return <EmptyState scheme="admin" title="Подписки нет" body="У этого магазина ещё не выпущена подписка." />
  }

  const sub = detail.subscription

  return (
    <div className="flex flex-col gap-5">
      <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
        <FieldRow label="Статус" value={<SubscriptionStatusBadge status={sub.status} size="sm" />} />
        <FieldRow label="Тариф" value={sub.subscriptionPlanName} />
        <FieldRow label="Цена на момент выпуска" value={`${sub.priceAtIssueAmount} ${sub.priceAtIssueCurrency}`} />
        <FieldRow label="Начало периода" value={fmtDate(sub.startedAt)} />
        <FieldRow label="Конец периода" value={fmtDate(sub.currentPeriodEndsAt)} />
        {sub.note && <FieldRow label="Заметка" value={sub.note} />}
      </div>

      {plans === null && !error && <Loading scheme="admin" />}
      {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}

      {plans && sub.status !== 'Cancelled' && (
        <div>
          <div className="mb-2 text-[12px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">Сменить тариф</div>
          <p className="mb-2 text-[11.5px] text-[color:var(--admin-text-tertiary)]">Смена вступит в силу со следующего периода, без перерасчёта задним числом.</p>
          <div className="flex gap-2">
            <Select
              scheme="admin"
              className="flex-1"
              value={selectedPlanId}
              onChange={setSelectedPlanId}
              placeholder="Выберите тариф"
              options={plans.filter((p) => p.isActive).map((p) => ({ value: String(p.subscriptionPlanId), label: `${p.name} · ${p.monthlyPriceAmount} ${p.monthlyPriceCurrency}` }))}
            />
            <button
              onClick={handleChangePlan}
              disabled={!selectedPlanId || planBusy}
              className="shrink-0 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--admin-accent-fg)] disabled:opacity-50"
            >
              {planBusy ? 'Секунду…' : 'Сменить'}
            </button>
          </div>
          <button onClick={() => setCancelOpen(true)} className="mt-2 text-[12px] font-semibold text-[color:var(--admin-danger)] hover:underline">
            Отменить подписку
          </button>
        </div>
      )}

      <div>
        <div className="mb-2 text-[12px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">Записать платёж</div>
        <div className="grid grid-cols-2 gap-2">
          <input
            type="number"
            min={0}
            step="0.01"
            value={payAmount}
            onChange={(e) => setPayAmount(e.target.value)}
            placeholder={`Сумма, ${sub.priceAtIssueCurrency}`}
            className="col-span-2 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)] sm:col-span-1"
          />
          <Select
            scheme="admin"
            value={payMethod}
            onChange={(v) => setPayMethod(v as typeof payMethod)}
            options={[
              { value: 'Cash', label: 'Наличные' },
              { value: 'BankTransfer', label: 'Банковский перевод' },
              { value: 'Card', label: 'Карта' },
              { value: 'Other', label: 'Другое' },
            ]}
          />
          <DateField value={payFrom} onChange={setPayFrom} outputFormat="dateOnly" title="Начало периода" />
          <DateField value={payTo} onChange={setPayTo} outputFormat="dateOnly" title="Конец периода" />
          <input
            value={payComment}
            onChange={(e) => setPayComment(e.target.value)}
            placeholder="Комментарий (необязательно)"
            className="col-span-2 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </div>
        {payError && <p className="mt-2 text-[12px] font-medium text-[color:var(--admin-danger)]">{payError}</p>}
        <button
          onClick={handleRecordPayment}
          disabled={!payAmount || !payFrom || !payTo || payBusy}
          className="mt-2 rounded-xl bg-[color:var(--admin-success)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--admin-success-fg)] disabled:opacity-50"
        >
          {payBusy ? 'Секунду…' : 'Записать платёж'}
        </button>
      </div>

      <div>
        <div className="mb-2 text-[12px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">История платежей</div>
        {payments && payments.length === 0 && <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">Платежей ещё не было</p>}
        {payments && payments.length > 0 && (
          <div className="flex flex-col gap-1.5">
            {payments.map((p) => (
              <div key={p.subscriptionPaymentId} className="flex items-center justify-between gap-2 rounded-lg bg-[color:var(--admin-hover)] px-3 py-2 text-[12.5px]">
                <span className={`font-[JetBrains_Mono,monospace] font-bold ${p.isReversal ? 'text-[color:var(--admin-danger)]' : 'text-[color:var(--admin-text)]'}`}>
                  {p.isReversal ? '−' : '+'}
                  {p.amount} {p.currency}
                </span>
                <span className="truncate text-[color:var(--admin-text-secondary)]">
                  {fmtDate(p.periodStart)} – {fmtDate(p.periodEnd)} · {p.recordedByEmail ?? 'система'}
                </span>
                <span className="shrink-0 text-[11px] text-[color:var(--admin-text-tertiary)]">{fmtDate(p.recordedAt)}</span>
              </div>
            ))}
          </div>
        )}
      </div>

      <ReasonModal
        open={cancelOpen}
        onClose={() => setCancelOpen(false)}
        title="Отменить подписку"
        description="Магазин перейдёт в статус Cancelled и потеряет доступ к кабинету и кассе."
        confirmLabel="Отменить подписку"
        danger
        onConfirm={async (reason) => {
          await subscriptionsApi.cancelStoreSubscription(sub.storeSubscriptionId, reason)
          onChanged()
        }}
      />
    </div>
  )
}

/* ---------- Сотрудники ---------- */

function EmployeesTab({ storeId }: { storeId: number }) {
  const [employees, setEmployees] = useState<AdminStoreEmployee[] | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getStoreEmployees(storeId)
      setEmployees(res.employees ?? [])
    } catch (err) {
      console.error('Не удалось загрузить сотрудников:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить сотрудников')
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  if (employees === null && !error) return <Loading scheme="admin" />
  if (error) return <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />
  if (employees!.length === 0) return <EmptyState scheme="admin" icon={<UsersIcon width={22} height={22} />} title="Сотрудников нет" body="В этом магазине пока не добавлено ни одного сотрудника." />

  return (
    <div className="flex flex-col gap-1.5">
      {employees!.map((e) => (
        <div key={e.storeEmployeeId} className="flex items-center justify-between gap-2 rounded-lg bg-[color:var(--admin-hover)] px-3.5 py-2.5">
          <div className="min-w-0">
            <div className="truncate text-[12.5px] font-semibold text-[color:var(--admin-text)]">{e.email ?? e.userId}</div>
            <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">с {fmtDate(e.addedAt)}</div>
          </div>
          <span className="shrink-0 rounded-full bg-[color:var(--admin-accent-soft)] px-2.5 py-1 text-[11px] font-bold text-[color:var(--admin-accent)]">
            {e.role === 'Owner' ? 'Владелец' : 'Кассир'}
          </span>
        </div>
      ))}
    </div>
  )
}

/* ---------- Торговые точки ---------- */

function LocationsTab({ storeId, currentStoreId, onNavigateToStore }: { storeId: number; currentStoreId: number; onNavigateToStore: (id: number) => void }) {
  const [locations, setLocations] = useState<AdminStoreLocation[] | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getStoreLocations(storeId)
      setLocations(res.locations ?? [])
    } catch (err) {
      console.error('Не удалось загрузить точки продаж:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить точки продаж')
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  if (locations === null && !error) return <Loading scheme="admin" />
  if (error) return <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />

  return (
    <div className="flex flex-col gap-1.5">
      {locations!.map((loc) => (
        <button
          key={loc.storeId}
          onClick={() => loc.storeId !== currentStoreId && onNavigateToStore(loc.storeId)}
          disabled={loc.storeId === currentStoreId}
          className={`flex items-center justify-between gap-2 rounded-lg px-3.5 py-2.5 text-left transition-colors ${
            loc.storeId === currentStoreId ? 'bg-[color:var(--admin-accent-soft)]' : 'bg-[color:var(--admin-hover)] hover:bg-[color:var(--admin-border)]'
          }`}
        >
          <div className="flex min-w-0 items-center gap-2.5">
            <StoreIcon width={15} height={15} className="shrink-0 text-[color:var(--admin-text-tertiary)]" />
            <div className="min-w-0">
              <div className="truncate text-[12.5px] font-semibold text-[color:var(--admin-text)]">
                {loc.name}
                {loc.storeId === currentStoreId && <span className="ml-1.5 text-[11px] font-normal text-[color:var(--admin-text-tertiary)]">(этот)</span>}
              </div>
              <div className="truncate text-[11px] text-[color:var(--admin-text-tertiary)]">{loc.address}</div>
            </div>
          </div>
          <StoreStatusBadge status={loc.status} size="sm" />
        </button>
      ))}
    </div>
  )
}

/* ---------- Диагностика ---------- */

function DiagnosticsTab({ storeId }: { storeId: number }) {
  const [diag, setDiag] = useState<AdminStoreDiagnostics | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      setDiag(await adminApi.getStoreDiagnostics(storeId))
    } catch (err) {
      console.error('Не удалось загрузить диагностику:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить диагностику')
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  if (diag === null && !error) return <Loading scheme="admin" />
  if (error) return <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />
  if (!diag) return null

  return (
    <div className="flex flex-col gap-4">
      <p className="rounded-lg bg-[color:var(--admin-accent-soft)] px-3 py-2 text-[11.5px] text-[color:var(--admin-accent)]">
        Только техническая диагностика для поддержки — без выручки, себестоимости и данных поставщиков.
      </p>
      <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
        <FieldRow label="Последний вход владельца" value={fmtDateTime(diag.ownerLastLoginAt)} />
        <FieldRow label="Последняя продажа" value={fmtDateTime(diag.lastSaleAt)} />
        <FieldRow label="Точек у владельца" value={diag.storeLocationsOwnedByThisOwner ?? '—'} />
        <FieldRow label="Сотрудников" value={diag.employeeCount ?? '—'} />
        <FieldRow label="Товаров на остатке" value={diag.distinctProductsInStock ?? '—'} />
        <FieldRow label="Единиц на остатке" value={diag.totalStockUnits ?? '—'} />
        <FieldRow label="Подписка" value={diag.subscriptionStatus ? <SubscriptionStatusBadge status={diag.subscriptionStatus} size="sm" /> : '—'} />
        <FieldRow label="Тариф" value={diag.subscriptionPlanName ?? '—'} />
        <FieldRow label="Конец периода" value={fmtDate(diag.subscriptionCurrentPeriodEndsAt)} />
      </div>
    </div>
  )
}

/* ---------- История действий ---------- */

function HistoryTab({ storeId }: { storeId: number }) {
  const [entries, setEntries] = useState<AuditLogEntry[] | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await adminApi.getAuditLog({ entityType: 'Store', entityId: storeId, take: 50 })
      setEntries(res.entries)
    } catch (err) {
      console.error('Не удалось загрузить историю действий:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить историю действий')
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  if (entries === null && !error) return <Loading scheme="admin" />
  if (error) return <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />
  if (entries!.length === 0) return <EmptyState scheme="admin" icon={<ClockIcon width={22} height={22} />} title="Действий не было" body="По этому магазину ещё нет записей в журнале." />

  return (
    <div className="flex flex-col gap-1.5">
      {entries!.map((e) => (
        <AuditLogRow key={e.auditLogId} entry={e} />
      ))}
    </div>
  )
}

/* ---------- panel ---------- */

export function StoreDetailPanel({ storeId, onClose, onNavigateToStore }: { storeId: number; onClose: () => void; onNavigateToStore: (id: number) => void }) {
  const [tab, setTab] = useState<TabId>('profile')
  const [detail, setDetail] = useState<AdminStoreDetail | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const load = useCallback(async () => {
    setError('')
    try {
      setDetail(await adminApi.getStoreDetail(storeId))
    } catch (err) {
      console.error('Не удалось загрузить магазин:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить магазин')
    }
  }, [storeId])

  useEffect(() => {
    setDetail(null)
    setTab('profile')
    load()
  }, [load])

  return (
    <SidePanel
      open
      onClose={onClose}
      title={
        <span className="flex items-center gap-2">
          <ShieldIcon width={16} height={16} className="shrink-0 text-[color:var(--admin-text-tertiary)]" />
          {detail?.name ?? `Магазин #${storeId}`}
        </span>
      }
      subtitle={detail?.address}
    >
      {detail === null && !error && <Loading scheme="admin" />}
      {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
      {detail && (
        <>
          <PanelTabs tabs={TABS} active={tab} onChange={setTab} />
          {tab === 'profile' && <ProfileTab detail={detail} onChanged={load} />}
          {tab === 'subscription' && <SubscriptionTab detail={detail} onChanged={load} />}
          {tab === 'employees' && <EmployeesTab storeId={storeId} />}
          {tab === 'locations' && <LocationsTab storeId={storeId} currentStoreId={storeId} onNavigateToStore={onNavigateToStore} />}
          {tab === 'diagnostics' && <DiagnosticsTab storeId={storeId} />}
          {tab === 'history' && <HistoryTab storeId={storeId} />}
        </>
      )}
    </SidePanel>
  )
}
