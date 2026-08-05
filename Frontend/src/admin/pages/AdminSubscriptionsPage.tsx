import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Select } from '../components/Select'
import { Pagination } from '../components/Pagination'
import { ReasonModal } from '../components/ReasonModal'
import { SubscriptionStatusBadge } from '../components/StatusBadge'
import { Badge } from '../components/Badge'
import { CardIcon, ClockIcon, PlusIcon, EditIcon } from '../components/icons'
import {
  subscriptionsApi,
  ApiError,
  type SubscriptionStatus,
  type SubscriptionPlan,
  type StoreSubscriptionListItem,
  type ExpiringSubscription,
  type SubscriptionPayment,
} from '../../lib/api'

type MainTab = 'subscriptions' | 'plans' | 'payments'
type SubFilter = 'all' | 'expiring' | 'pastdue'

const TAKE = 25

function fmtDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

const STATUS_OPTIONS: { value: SubscriptionStatus; label: string }[] = [
  { value: 'Trial', label: 'Пробный период' },
  { value: 'Active', label: 'Активна' },
  { value: 'PastDue', label: 'Просрочена' },
  { value: 'Suspended', label: 'Приостановлена' },
  { value: 'Cancelled', label: 'Отменена' },
]

/* ---------- Подписки ---------- */

function SubscriptionsSection({ subFilter, onSubFilterChange }: { subFilter: SubFilter; onSubFilterChange: (f: SubFilter) => void }) {
  const [skip, setSkip] = useState(0)
  const [status, setStatus] = useState<SubscriptionStatus | ''>('')
  const [search, setSearch] = useState('')
  const [rows, setRows] = useState<StoreSubscriptionListItem[] | null>(null)
  const [expiring, setExpiring] = useState<ExpiringSubscription[] | null>(null)
  const [pastDue, setPastDue] = useState<ExpiringSubscription[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      if (subFilter === 'expiring') {
        setExpiring((await subscriptionsApi.getExpiringSoonSubscriptions(7)).subscriptions)
      } else if (subFilter === 'pastdue') {
        setPastDue((await subscriptionsApi.getPastDueSubscriptions()).subscriptions)
      } else {
        const res = await subscriptionsApi.getStoreSubscriptions({ skip, take: TAKE, status: status || undefined, storeSearch: search || undefined })
        setRows(res.subscriptions)
        setTotalCount(res.totalCount)
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить подписки')
    }
  }, [subFilter, skip, status, search])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center gap-2">
        <div className="flex gap-1 rounded-lg bg-[color:var(--mod-panel2)] p-1">
          {(
            [
              ['all', 'Все'],
              ['expiring', 'Истекают за 7 дней'],
              ['pastdue', 'Просрочены'],
            ] as [SubFilter, string][]
          ).map(([id, label]) => (
            <button
              key={id}
              onClick={() => onSubFilterChange(id)}
              className={`rounded-md px-3 py-1.5 text-[12px] font-bold transition-colors ${
                subFilter === id ? 'bg-[color:var(--mod-accent)] text-white' : 'text-[color:var(--mod-muted)] hover:text-[color:var(--mod-text)]'
              }`}
            >
              {label}
            </button>
          ))}
        </div>
        {subFilter === 'all' && (
          <>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Поиск по магазину…"
              className="min-w-[200px] flex-1 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
            />
            <Select scheme="mod" value={status} onChange={(v) => setStatus(v as SubscriptionStatus)} placeholder="Все статусы" options={STATUS_OPTIONS} className="min-w-[180px]" />
          </>
        )}
      </div>

      {subFilter !== 'all' && (
        <Card scheme="mod" className="overflow-hidden">
          {(subFilter === 'expiring' ? expiring : pastDue) === null && !error && <Loading scheme="mod" />}
          {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
          {(subFilter === 'expiring' ? expiring : pastDue)?.length === 0 && (
            <EmptyState scheme="mod" icon={<ClockIcon width={22} height={22} />} title="Пусто" body="Сейчас таких подписок нет." />
          )}
          {(subFilter === 'expiring' ? expiring : pastDue) && (subFilter === 'expiring' ? expiring! : pastDue!).length > 0 && (
            <div className="flex flex-col">
              {(subFilter === 'expiring' ? expiring! : pastDue!).map((s) => (
                <div key={s.storeSubscriptionId} className="flex items-center justify-between gap-2 border-b border-[color:var(--mod-border)] px-4 py-3 last:border-0">
                  <div>
                    <div className="text-[13px] font-semibold text-[color:var(--mod-text)]">{s.storeName}</div>
                    <div className="text-[11.5px] text-[color:var(--mod-faint)]">{s.subscriptionPlanName}</div>
                  </div>
                  <span className="font-[JetBrains_Mono,monospace] text-[12px] text-[color:var(--mod-warn)]">{fmtDate(s.currentPeriodEndsAt)}</span>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {subFilter === 'all' && (
        <Card scheme="mod" className="overflow-hidden">
          {rows === null && !error && <Loading scheme="mod" />}
          {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
          {rows && rows.length === 0 && <EmptyState scheme="mod" icon={<CardIcon width={22} height={22} />} title="Подписок не найдено" body="Измените фильтры." />}
          {rows && rows.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full border-collapse text-[13px]">
                <thead>
                  <tr className="border-b border-[color:var(--mod-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--mod-faint)]">
                    <th className="px-4 py-3">Магазин</th>
                    <th className="px-4 py-3">Тариф</th>
                    <th className="px-4 py-3">Статус</th>
                    <th className="px-4 py-3">Цена</th>
                    <th className="px-4 py-3">Конец периода</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((s) => (
                    <tr key={s.storeSubscriptionId} className="border-b border-[color:var(--mod-border)] transition-colors last:border-0 hover:bg-[color:var(--mod-panel2)]">
                      <td className="px-4 py-3 font-semibold text-[color:var(--mod-text)]">{s.storeName}</td>
                      <td className="px-4 py-3 text-[color:var(--mod-muted)]">{s.subscriptionPlanName}</td>
                      <td className="px-4 py-3">
                        <SubscriptionStatusBadge status={s.status} size="sm" />
                      </td>
                      <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--mod-text)]">
                        {s.priceAtIssueAmount} {s.priceAtIssueCurrency}
                      </td>
                      <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--mod-faint)]">{fmtDate(s.currentPeriodEndsAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {rows && rows.length > 0 && (
            <div className="px-4 pb-4">
              <Pagination skip={skip} take={TAKE} totalCount={totalCount} onChange={setSkip} />
            </div>
          )}
        </Card>
      )}
    </div>
  )
}

/* ---------- Тарифы ---------- */

function PlanForm({ plan, onSaved, onCancel }: { plan?: SubscriptionPlan; onSaved: () => void; onCancel?: () => void }) {
  const [name, setName] = useState(plan?.name ?? '')
  const [code, setCode] = useState(plan?.code ?? '')
  const [price, setPrice] = useState(String(plan?.monthlyPriceAmount ?? ''))
  const [currency, setCurrency] = useState(plan?.monthlyPriceCurrency ?? 'TJS')
  const [maxStores, setMaxStores] = useState(plan?.maxStores ? String(plan.maxStores) : '')
  const [maxEmployees, setMaxEmployees] = useState(plan?.maxEmployees ? String(plan.maxEmployees) : '')
  const [features, setFeatures] = useState(plan?.features.join(', ') ?? '')
  const [isActive, setIsActive] = useState(plan?.isActive ?? true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!name.trim() || !price || busy) return
    setBusy(true)
    setError('')
    try {
      const featureList = features.split(',').map((f) => f.trim()).filter(Boolean)
      if (plan) {
        await subscriptionsApi.updateSubscriptionPlan(plan.subscriptionPlanId, {
          name: name.trim(),
          monthlyPriceAmount: Number(price),
          monthlyPriceCurrency: currency,
          maxStores: maxStores ? Number(maxStores) : undefined,
          maxEmployees: maxEmployees ? Number(maxEmployees) : undefined,
          features: featureList,
          isActive,
        })
      } else {
        if (!code.trim()) return
        await subscriptionsApi.createSubscriptionPlan({
          name: name.trim(),
          code: code.trim(),
          monthlyPriceAmount: Number(price),
          monthlyPriceCurrency: currency,
          maxStores: maxStores ? Number(maxStores) : undefined,
          maxEmployees: maxEmployees ? Number(maxEmployees) : undefined,
          features: featureList,
        })
        setName('')
        setCode('')
        setPrice('')
        setMaxStores('')
        setMaxEmployees('')
        setFeatures('')
      }
      onSaved()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить тариф')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-2.5">
      <div className="grid grid-cols-2 gap-2.5">
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Название" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        {!plan && (
          <input value={code} onChange={(e) => setCode(e.target.value)} placeholder="Код (standard, pro…)" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        )}
        <input value={price} onChange={(e) => setPrice(e.target.value)} type="number" min={0} step="0.01" placeholder="Цена / мес" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <input value={currency} onChange={(e) => setCurrency(e.target.value)} placeholder="Валюта" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <input value={maxStores} onChange={(e) => setMaxStores(e.target.value)} type="number" min={0} placeholder="Лимит точек (необязательно)" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <input value={maxEmployees} onChange={(e) => setMaxEmployees(e.target.value)} type="number" min={0} placeholder="Лимит сотрудников (необязательно)" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
      </div>
      <input value={features} onChange={(e) => setFeatures(e.target.value)} placeholder="Возможности через запятую" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
      {plan && (
        <label className="flex items-center gap-2 text-[12.5px] font-semibold text-[color:var(--mod-text)]">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} className="h-4 w-4 accent-[color:var(--mod-accent)]" />
          Тариф активен (доступен для назначения)
        </label>
      )}
      {error && <p className="text-[12px] font-medium text-[color:var(--mod-danger)]">{error}</p>}
      <div className="flex gap-2">
        <button type="submit" disabled={busy} className="rounded-xl bg-[color:var(--mod-accent)] px-4 py-2.5 text-[12.5px] font-bold text-white disabled:opacity-50">
          {busy ? 'Секунду…' : plan ? 'Сохранить' : 'Создать тариф'}
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} className="rounded-xl border border-[color:var(--mod-border)] px-4 py-2.5 text-[12.5px] font-semibold text-[color:var(--mod-text)]">
            Отмена
          </button>
        )}
      </div>
    </form>
  )
}

function PlansSection() {
  const [plans, setPlans] = useState<SubscriptionPlan[] | null>(null)
  const [error, setError] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      setPlans((await subscriptionsApi.getSubscriptionPlans(true)).plans)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить тарифы')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div className="flex flex-col gap-3">
      {plans === null && !error && <Loading scheme="mod" />}
      {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
      {plans &&
        plans.map((p) =>
          editingId === p.subscriptionPlanId ? (
            <Card key={p.subscriptionPlanId} scheme="mod" className="p-4">
              <PlanForm plan={p} onSaved={() => { setEditingId(null); load() }} onCancel={() => setEditingId(null)} />
            </Card>
          ) : (
            <Card key={p.subscriptionPlanId} scheme="mod" className="flex items-center justify-between gap-3 p-4">
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <span className="font-bold text-[color:var(--mod-text)]">{p.name}</span>
                  <Badge scheme="mod" variant={p.isActive ? 'success' : 'neutral'} size="sm">
                    {p.isActive ? 'Активен' : 'Отключён'}
                  </Badge>
                </div>
                <div className="mt-0.5 font-[JetBrains_Mono,monospace] text-[11.5px] text-[color:var(--mod-faint)]">
                  {p.code} · {p.monthlyPriceAmount} {p.monthlyPriceCurrency}/мес
                  {p.maxStores ? ` · до ${p.maxStores} точек` : ''}
                  {p.maxEmployees ? ` · до ${p.maxEmployees} сотрудников` : ''}
                </div>
                {p.features.length > 0 && <div className="mt-1 text-[12px] text-[color:var(--mod-muted)]">{p.features.join(' · ')}</div>}
              </div>
              <button onClick={() => setEditingId(p.subscriptionPlanId)} className="shrink-0 grid h-9 w-9 place-items-center rounded-lg text-[color:var(--mod-muted)] hover:bg-[color:var(--mod-panel2)]">
                <EditIcon width={16} height={16} />
              </button>
            </Card>
          ),
        )}

      {showCreate ? (
        <Card scheme="mod" className="p-4">
          <PlanForm onSaved={() => { setShowCreate(false); load() }} onCancel={() => setShowCreate(false)} />
        </Card>
      ) : (
        <button
          onClick={() => setShowCreate(true)}
          className="flex items-center justify-center gap-2 rounded-2xl border border-dashed border-[color:var(--mod-border2)] py-4 text-[13px] font-bold text-[color:var(--mod-muted)] transition-colors hover:border-[color:var(--mod-accent)] hover:text-[color:var(--mod-accent2)]"
        >
          <PlusIcon width={16} height={16} />
          Новый тариф
        </button>
      )}
    </div>
  )
}

/* ---------- Платежи ---------- */

function PaymentsSection() {
  const [skip, setSkip] = useState(0)
  const [payments, setPayments] = useState<SubscriptionPayment[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [error, setError] = useState('')
  const [reversing, setReversing] = useState<SubscriptionPayment | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await subscriptionsApi.getSubscriptionPayments({ skip, take: TAKE })
      setPayments(res.payments)
      setTotalCount(res.totalCount)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить платежи')
    }
  }, [skip])

  useEffect(() => {
    load()
  }, [load])

  return (
    <Card scheme="mod" className="overflow-hidden">
      {payments === null && !error && <Loading scheme="mod" />}
      {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
      {payments && payments.length === 0 && <EmptyState scheme="mod" icon={<CardIcon width={22} height={22} />} title="Платежей ещё не было" />}
      {payments && payments.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-[13px]">
            <thead>
              <tr className="border-b border-[color:var(--mod-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--mod-faint)]">
                <th className="px-4 py-3">Магазин</th>
                <th className="px-4 py-3">Сумма</th>
                <th className="px-4 py-3">Период</th>
                <th className="px-4 py-3">Способ</th>
                <th className="px-4 py-3">Записал</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {payments.map((p) => (
                <tr key={p.subscriptionPaymentId} className="border-b border-[color:var(--mod-border)] last:border-0">
                  <td className="px-4 py-3 font-semibold text-[color:var(--mod-text)]">{p.storeName}</td>
                  <td className={`px-4 py-3 font-[JetBrains_Mono,monospace] font-bold ${p.isReversal ? 'text-[color:var(--mod-danger)]' : 'text-[color:var(--mod-text)]'}`}>
                    {p.isReversal ? '−' : '+'}
                    {p.amount} {p.currency}
                  </td>
                  <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--mod-faint)]">
                    {fmtDate(p.periodStart)} – {fmtDate(p.periodEnd)}
                  </td>
                  <td className="px-4 py-3 text-[color:var(--mod-muted)]">{p.method}</td>
                  <td className="px-4 py-3 text-[color:var(--mod-muted)]">{p.recordedByEmail ?? '—'}</td>
                  <td className="px-4 py-3 text-right">
                    {!p.isReversal && (
                      <button onClick={() => setReversing(p)} className="text-[11.5px] font-semibold text-[color:var(--mod-danger)] hover:underline">
                        Сторнировать
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {payments && payments.length > 0 && (
        <div className="px-4 pb-4">
          <Pagination skip={skip} take={TAKE} totalCount={totalCount} onChange={setSkip} />
        </div>
      )}

      <ReasonModal
        open={!!reversing}
        onClose={() => setReversing(null)}
        title="Сторнировать платёж"
        description={reversing ? `Платёж ${reversing.amount} ${reversing.currency} по «${reversing.storeName}» будет вычтен обратной записью.` : ''}
        confirmLabel="Сторнировать"
        danger
        onConfirm={async (reason) => {
          if (!reversing) return
          await subscriptionsApi.reverseSubscriptionPayment(reversing.subscriptionPaymentId, reason)
          await load()
        }}
      />
    </Card>
  )
}

/* ---------- page ---------- */

export function AdminSubscriptionsPage() {
  const [params, setParams] = useSearchParams()
  const tabParam = params.get('tab')
  const mainTab: MainTab = tabParam === 'plans' || tabParam === 'payments' ? tabParam : 'subscriptions'
  const subFilter: SubFilter = tabParam === 'expiring' || tabParam === 'pastdue' ? tabParam : 'all'

  function setMainTab(t: MainTab) {
    setParams(t === 'subscriptions' ? {} : { tab: t })
  }
  function setSubFilter(f: SubFilter) {
    setParams(f === 'all' ? {} : { tab: f })
  }

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 flex gap-1 rounded-lg bg-[color:var(--mod-panel2)] p-1" style={{ width: 'fit-content' }}>
        {(
          [
            ['subscriptions', 'Подписки'],
            ['plans', 'Тарифы'],
            ['payments', 'Платежи'],
          ] as [MainTab, string][]
        ).map(([id, label]) => (
          <button
            key={id}
            onClick={() => setMainTab(id)}
            className={`rounded-md px-4 py-2 text-[12.5px] font-bold transition-colors ${
              mainTab === id ? 'bg-[color:var(--mod-accent)] text-white' : 'text-[color:var(--mod-muted)] hover:text-[color:var(--mod-text)]'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {mainTab === 'subscriptions' && <SubscriptionsSection subFilter={subFilter} onSubFilterChange={setSubFilter} />}
      {mainTab === 'plans' && <PlansSection />}
      {mainTab === 'payments' && <PaymentsSection />}
    </div>
  )
}
