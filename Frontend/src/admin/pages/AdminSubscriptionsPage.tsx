import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Select } from '../components/Select'
import { SectionSelect } from '../components/SectionSelect'
import { Pagination } from '../components/Pagination'
import { ReasonModal } from '../components/ReasonModal'
import { FormModal, FormField } from '../components/FormModal'
import { SubscriptionStatusBadge } from '../components/StatusBadge'
import { Badge } from '../components/Badge'
import { CardIcon, ClockIcon, EditIcon, TagIcon, CashIcon } from '../components/icons'
import { AddButton } from '../components/Button'
import {
  subscriptionsApi,
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
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

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
      console.error('Failed to load subscriptions:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить подписки')
    }
  }, [subFilter, skip, status, search])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center gap-2">
        <div className="flex gap-1 rounded-lg bg-[color:var(--admin-hover)] p-1">
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
                subFilter === id ? 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]' : 'text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)]'
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
              className="min-w-[200px] flex-1 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
            />
            <Select scheme="admin" value={status} onChange={(v) => setStatus(v as SubscriptionStatus)} placeholder="Все статусы" options={STATUS_OPTIONS} className="min-w-[180px]" />
          </>
        )}
      </div>

      {subFilter !== 'all' && (
        <Card scheme="admin" className="overflow-hidden">
          {(subFilter === 'expiring' ? expiring : pastDue) === null && !error && <Loading scheme="admin" />}
          {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
          {(subFilter === 'expiring' ? expiring : pastDue)?.length === 0 && (
            <EmptyState scheme="admin" icon={<ClockIcon width={22} height={22} />} title="Пусто" body="Сейчас таких подписок нет." />
          )}
          {(subFilter === 'expiring' ? expiring : pastDue) && (subFilter === 'expiring' ? expiring! : pastDue!).length > 0 && (
            <div className="flex flex-col">
              {(subFilter === 'expiring' ? expiring! : pastDue!).map((s) => (
                <div key={s.storeSubscriptionId} className="flex items-center justify-between gap-2 border-b border-[color:var(--admin-border)] px-4 py-3 last:border-0">
                  <div>
                    <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">{s.storeName}</div>
                    <div className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">{s.subscriptionPlanName}</div>
                  </div>
                  <span className="font-[JetBrains_Mono,monospace] text-[12px] text-[color:var(--admin-warning)]">{fmtDate(s.currentPeriodEndsAt)}</span>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {subFilter === 'all' && (
        <Card scheme="admin" className="overflow-hidden">
          {rows === null && !error && <Loading scheme="admin" />}
          {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
          {rows && rows.length === 0 && <EmptyState scheme="admin" icon={<CardIcon width={22} height={22} />} title="Подписок не найдено" body="Измените фильтры." />}
          {rows && rows.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full border-collapse text-[13px]">
                <thead>
                  <tr className="border-b border-[color:var(--admin-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                    <th className="px-4 py-3">Магазин</th>
                    <th className="px-4 py-3">Тариф</th>
                    <th className="px-4 py-3">Статус</th>
                    <th className="px-4 py-3">Цена</th>
                    <th className="px-4 py-3">Конец периода</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((s) => (
                    <tr key={s.storeSubscriptionId} className="border-b border-[color:var(--admin-border)] transition-colors last:border-0 hover:bg-[color:var(--admin-hover)]">
                      <td className="px-4 py-3 font-semibold text-[color:var(--admin-text)]">{s.storeName}</td>
                      <td className="px-4 py-3 text-[color:var(--admin-text-secondary)]">{s.subscriptionPlanName}</td>
                      <td className="px-4 py-3">
                        <SubscriptionStatusBadge status={s.status} size="sm" />
                      </td>
                      <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--admin-text)]">
                        {s.priceAtIssueAmount} {s.priceAtIssueCurrency}
                      </td>
                      <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--admin-text-tertiary)]">{fmtDate(s.currentPeriodEndsAt)}</td>
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

function PlanFormFields({
  isEdit, name, setName, code, setCode, price, setPrice, currency, setCurrency,
  maxStores, setMaxStores, maxEmployees, setMaxEmployees, features, setFeatures, isActive, setIsActive,
  nameError, priceError, codeError,
}: {
  isEdit: boolean
  name: string; setName: (v: string) => void
  code: string; setCode: (v: string) => void
  price: string; setPrice: (v: string) => void
  currency: string; setCurrency: (v: string) => void
  maxStores: string; setMaxStores: (v: string) => void
  maxEmployees: string; setMaxEmployees: (v: string) => void
  features: string; setFeatures: (v: string) => void
  isActive: boolean; setIsActive: (v: boolean) => void
  nameError?: string; priceError?: string; codeError?: string
}) {
  const fieldClass = 'w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]'
  return (
    <>
      <FormField label="Название" required error={nameError} scheme="admin">
        <input value={name} onChange={(e) => setName(e.target.value)} className={fieldClass} />
      </FormField>
      {!isEdit && (
        <FormField label="Код" required error={codeError} scheme="admin">
          <input value={code} onChange={(e) => setCode(e.target.value)} placeholder="standard, pro…" className={fieldClass} />
        </FormField>
      )}
      <div className="grid grid-cols-2 gap-2.5">
        <FormField label="Цена / мес" required error={priceError} scheme="admin">
          <input value={price} onChange={(e) => setPrice(e.target.value)} type="number" min={0} step="0.01" className={fieldClass} />
        </FormField>
        <FormField label="Валюта" scheme="admin">
          <input value={currency} onChange={(e) => setCurrency(e.target.value)} className={fieldClass} />
        </FormField>
      </div>
      <div className="grid grid-cols-2 gap-2.5">
        <FormField label="Лимит точек" scheme="admin">
          <input value={maxStores} onChange={(e) => setMaxStores(e.target.value)} type="number" min={0} placeholder="Без лимита" className={fieldClass} />
        </FormField>
        <FormField label="Лимит сотрудников" scheme="admin">
          <input value={maxEmployees} onChange={(e) => setMaxEmployees(e.target.value)} type="number" min={0} placeholder="Без лимита" className={fieldClass} />
        </FormField>
      </div>
      <FormField label="Возможности" scheme="admin">
        <input value={features} onChange={(e) => setFeatures(e.target.value)} placeholder="Через запятую" className={fieldClass} />
      </FormField>
      {isEdit && (
        <label className="flex items-center gap-2 text-[12.5px] font-semibold text-[color:var(--admin-text)]">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} className="h-4 w-4 accent-[color:var(--admin-accent)]" />
          Тариф активен (доступен для назначения)
        </label>
      )}
    </>
  )
}

function PlanFormModal({ plan, onClose, onSaved }: { plan: SubscriptionPlan | 'create' | null; onClose: () => void; onSaved: () => Promise<void> }) {
  const isEdit = plan !== null && plan !== 'create'
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [price, setPrice] = useState('')
  const [currency, setCurrency] = useState('TJS')
  const [maxStores, setMaxStores] = useState('')
  const [maxEmployees, setMaxEmployees] = useState('')
  const [features, setFeatures] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [nameError, setNameError] = useState('')
  const [priceError, setPriceError] = useState('')
  const [codeError, setCodeError] = useState('')

  useEffect(() => {
    if (isEdit) {
      const p = plan as SubscriptionPlan
      setName(p.name)
      setCode(p.code)
      setPrice(String(p.monthlyPriceAmount))
      setCurrency(p.monthlyPriceCurrency)
      setMaxStores(p.maxStores ? String(p.maxStores) : '')
      setMaxEmployees(p.maxEmployees ? String(p.maxEmployees) : '')
      setFeatures(p.features.join(', '))
      setIsActive(p.isActive)
    } else {
      setName('')
      setCode('')
      setPrice('')
      setCurrency('TJS')
      setMaxStores('')
      setMaxEmployees('')
      setFeatures('')
      setIsActive(true)
    }
    setNameError('')
    setPriceError('')
    setCodeError('')
  }, [plan, isEdit])

  async function submit() {
    let hasError = false
    if (!name.trim()) {
      setNameError('Укажите название')
      hasError = true
    }
    const priceNum = Number(price)
    if (!price || Number.isNaN(priceNum) || priceNum < 0) {
      setPriceError('Укажите цену')
      hasError = true
    }
    if (!isEdit && !code.trim()) {
      setCodeError('Укажите код')
      hasError = true
    }
    if (hasError) throw new Error('Проверьте поля формы')

    const featureList = features.split(',').map((f) => f.trim()).filter(Boolean)
    if (isEdit) {
      await subscriptionsApi.updateSubscriptionPlan((plan as SubscriptionPlan).subscriptionPlanId, {
        name: name.trim(),
        monthlyPriceAmount: priceNum,
        monthlyPriceCurrency: currency,
        maxStores: maxStores ? Number(maxStores) : undefined,
        maxEmployees: maxEmployees ? Number(maxEmployees) : undefined,
        features: featureList,
        isActive,
      })
    } else {
      await subscriptionsApi.createSubscriptionPlan({
        name: name.trim(),
        code: code.trim(),
        monthlyPriceAmount: priceNum,
        monthlyPriceCurrency: currency,
        maxStores: maxStores ? Number(maxStores) : undefined,
        maxEmployees: maxEmployees ? Number(maxEmployees) : undefined,
        features: featureList,
      })
    }
    await onSaved()
  }

  return (
    <FormModal
      open={plan !== null}
      onClose={onClose}
      title={isEdit ? 'Изменить тариф' : 'Новый тариф'}
      isDirty={!!(name || code || price || maxStores || maxEmployees || features)}
      onSubmit={submit}
      submitLabel={isEdit ? 'Сохранить' : 'Создать тариф'}
      scheme="admin"
    >
      <PlanFormFields
        isEdit={isEdit} name={name} setName={setName} code={code} setCode={setCode} price={price} setPrice={setPrice}
        currency={currency} setCurrency={setCurrency} maxStores={maxStores} setMaxStores={setMaxStores}
        maxEmployees={maxEmployees} setMaxEmployees={setMaxEmployees} features={features} setFeatures={setFeatures}
        isActive={isActive} setIsActive={setIsActive} nameError={nameError} priceError={priceError} codeError={codeError}
      />
    </FormModal>
  )
}

function PlansSection({ createOpen, onCloseCreate }: { createOpen: boolean; onCloseCreate: () => void }) {
  const [plans, setPlans] = useState<SubscriptionPlan[] | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [editingPlan, setEditingPlan] = useState<SubscriptionPlan | 'create' | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      setPlans((await subscriptionsApi.getSubscriptionPlans(true)).plans)
    } catch (err) {
      console.error('Failed to load subscription plans:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить тарифы')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  // The page-level "Новый тариф" button (next to the section selector) and this section's own
  // per-row "Редактировать" both open the same FormModal -- create is just edit with no plan yet.
  useEffect(() => {
    if (createOpen) setEditingPlan('create')
  }, [createOpen])

  function closeModal() {
    setEditingPlan(null)
    onCloseCreate()
  }

  return (
    <div className="flex flex-col gap-3">
      {plans === null && !error && <Loading scheme="admin" />}
      {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
      {plans &&
        plans.map((p) => (
          <Card key={p.subscriptionPlanId} scheme="admin" className="flex items-center justify-between gap-3 p-4">
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <span className="font-bold text-[color:var(--admin-text)]">{p.name}</span>
                <Badge scheme="admin" variant={p.isActive ? 'success' : 'neutral'} size="sm">
                  {p.isActive ? 'Активен' : 'Отключён'}
                </Badge>
              </div>
              <div className="mt-0.5 font-[JetBrains_Mono,monospace] text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                {p.code} · {p.monthlyPriceAmount} {p.monthlyPriceCurrency}/мес
                {p.maxStores ? ` · до ${p.maxStores} точек` : ''}
                {p.maxEmployees ? ` · до ${p.maxEmployees} сотрудников` : ''}
              </div>
              {p.features.length > 0 && <div className="mt-1 text-[12px] text-[color:var(--admin-text-secondary)]">{p.features.join(' · ')}</div>}
            </div>
            <button onClick={() => setEditingPlan(p)} className="shrink-0 grid h-9 w-9 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]">
              <EditIcon width={16} height={16} />
            </button>
          </Card>
        ))}

      <PlanFormModal plan={editingPlan} onClose={closeModal} onSaved={async () => { await load(); closeModal() }} />
    </div>
  )
}

/* ---------- Платежи ---------- */

function PaymentsSection() {
  const [skip, setSkip] = useState(0)
  const [payments, setPayments] = useState<SubscriptionPayment[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [reversing, setReversing] = useState<SubscriptionPayment | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const res = await subscriptionsApi.getSubscriptionPayments({ skip, take: TAKE })
      setPayments(res.payments)
      setTotalCount(res.totalCount)
    } catch (err) {
      console.error('Failed to load subscription payments:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить платежи')
    }
  }, [skip])

  useEffect(() => {
    load()
  }, [load])

  return (
    <Card scheme="admin" className="overflow-hidden">
      {payments === null && !error && <Loading scheme="admin" />}
      {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
      {payments && payments.length === 0 && <EmptyState scheme="admin" icon={<CardIcon width={22} height={22} />} title="Платежей ещё не было" />}
      {payments && payments.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-[13px]">
            <thead>
              <tr className="border-b border-[color:var(--admin-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
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
                <tr key={p.subscriptionPaymentId} className="border-b border-[color:var(--admin-border)] last:border-0">
                  <td className="px-4 py-3 font-semibold text-[color:var(--admin-text)]">{p.storeName}</td>
                  <td className={`px-4 py-3 font-[JetBrains_Mono,monospace] font-bold ${p.isReversal ? 'text-[color:var(--admin-danger)]' : 'text-[color:var(--admin-text)]'}`}>
                    {p.isReversal ? '−' : '+'}
                    {p.amount} {p.currency}
                  </td>
                  <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[color:var(--admin-text-tertiary)]">
                    {fmtDate(p.periodStart)} – {fmtDate(p.periodEnd)}
                  </td>
                  <td className="px-4 py-3 text-[color:var(--admin-text-secondary)]">{p.method}</td>
                  <td className="px-4 py-3 text-[color:var(--admin-text-secondary)]">{p.recordedByEmail ?? '—'}</td>
                  <td className="px-4 py-3 text-right">
                    {!p.isReversal && (
                      <button onClick={() => setReversing(p)} className="text-[11.5px] font-semibold text-[color:var(--admin-danger)] hover:underline">
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

const MAIN_TAB_OPTIONS = [
  { value: 'subscriptions' as const, label: 'Подписки', icon: <CardIcon width={15} height={15} /> },
  { value: 'plans' as const, label: 'Тарифы', icon: <TagIcon width={15} height={15} /> },
  { value: 'payments' as const, label: 'Платежи', icon: <CashIcon width={15} height={15} /> },
]

export function AdminSubscriptionsPage() {
  const [params, setParams] = useSearchParams()
  const tabParam = params.get('tab')
  const mainTab: MainTab = tabParam === 'plans' || tabParam === 'payments' ? tabParam : 'subscriptions'
  const subFilter: SubFilter = tabParam === 'expiring' || tabParam === 'pastdue' ? tabParam : 'all'
  const [createOpen, setCreateOpen] = useState(false)

  function setMainTab(t: MainTab) {
    setParams(t === 'subscriptions' ? {} : { tab: t })
  }
  function setSubFilter(f: SubFilter) {
    setParams(f === 'all' ? {} : { tab: f })
  }

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 flex items-center justify-between gap-3">
        <SectionSelect value={mainTab} onChange={setMainTab} options={MAIN_TAB_OPTIONS} ariaLabel="Раздел подписок" />
        {mainTab === 'plans' && <AddButton onClick={() => setCreateOpen(true)}>Новый тариф</AddButton>}
      </div>

      {mainTab === 'subscriptions' && <SubscriptionsSection subFilter={subFilter} onSubFilterChange={setSubFilter} />}
      {mainTab === 'plans' && <PlansSection createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />}
      {mainTab === 'payments' && <PaymentsSection />}
    </div>
  )
}
