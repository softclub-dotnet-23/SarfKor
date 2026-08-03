import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Select } from '../components/Select'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { Panel, SectionHeader, Stat, Row, RowDivider, EmptyRow } from '../cabinet/components/primitives'
import { ClockIcon, ShieldIcon, PlusIcon, TrashIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import {
  storesApi,
  salesApi,
  ApiError,
  type CashierShift,
  type CashierAnomaly,
  type StoreEmployee,
  type StoreEmployeeRole,
} from '../../lib/api'
import { daysAgo, today } from '../lib/dates'

const ROLE_ACCESS: { role: string; access: string[] }[] = [
  { role: 'User', access: ['Сравнение цен', 'Личный список покупок', 'Отзывы и репорты'] },
  { role: 'StorePartner', access: ['Касса и склад своего магазина', 'Себестоимость и отчёты о прибыли', 'Управление ценами и сменами'] },
  { role: 'Admin', access: ['Модерация товаров и репортов', 'Полный доступ к аналитике платформы'] },
]

function shortId(id: string, myId?: string) {
  if (id === myId) return 'Вы'
  return id.slice(0, 8) + '…'
}

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function EmployeesSection() {
  const { storeId, user } = useAuth()
  const [employees, setEmployees] = useState<StoreEmployee[] | null>(null)
  const [error, setError] = useState('')
  const [employeeEmail, setEmployeeEmail] = useState('')
  const [role, setRole] = useState<StoreEmployeeRole>('Cashier')
  const [busy, setBusy] = useState(false)
  const [formError, setFormError] = useState('')
  const [formSuccess, setFormSuccess] = useState('')
  const [removingId, setRemovingId] = useState<number | null>(null)

  const load = useCallback(async () => {
    if (!storeId) return
    setError('')
    try {
      const res = await storesApi.getStoreEmployees(storeId)
      if (res.outcome === 'Found') {
        setEmployees(res.employees ?? [])
      } else {
        setError(res.outcome === 'Forbidden' ? 'Нет доступа к сотрудникам этого магазина' : 'Магазин не найден')
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить список сотрудников')
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  async function handleAdd(e: FormEvent) {
    e.preventDefault()
    if (!storeId || !employeeEmail.trim() || busy) return
    setBusy(true)
    setFormError('')
    setFormSuccess('')
    try {
      const res = await storesApi.addStoreEmployee(storeId, employeeEmail.trim(), role)
      if (res.outcome === 'Added') {
        setEmployeeEmail('')
        setRole('Cashier')
        await load()
      } else if (res.outcome === 'Invited') {
        setFormSuccess(`Приглашение отправлено на ${employeeEmail.trim()} — как только он(а) его примет, станет сотрудником этого магазина`)
        setEmployeeEmail('')
        setRole('Cashier')
      } else if (res.outcome === 'AlreadyEmployed') {
        setFormError('Этот пользователь уже числится сотрудником магазина')
      } else if (res.outcome === 'Forbidden') {
        setFormError('Нет доступа к этому магазину')
      } else {
        setFormError('Магазин не найден')
      }
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Не удалось добавить сотрудника')
    } finally {
      setBusy(false)
    }
  }

  async function handleRemove(storeEmployeeId: number) {
    setRemovingId(storeEmployeeId)
    setError('')
    try {
      const res = await storesApi.removeStoreEmployee(storeEmployeeId)
      if (res.outcome === 'Removed') {
        await load()
      } else if (res.outcome === 'Forbidden') {
        setError('Нет доступа для удаления этого сотрудника')
      } else {
        setError('Сотрудник не найден')
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить сотрудника')
    } finally {
      setRemovingId(null)
    }
  }

  return (
    <Panel>
      <SectionHeader title="Сотрудники магазина" />

      <form onSubmit={handleAdd} className="mb-4 flex flex-col gap-2.5 sm:flex-row sm:items-end">
        <label className="flex flex-1 flex-col gap-1.5">
          <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Email сотрудника</span>
          <input
            type="email"
            value={employeeEmail}
            onChange={(e) => setEmployeeEmail(e.target.value)}
            placeholder="cashier@sarfkor.tj"
            className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </label>
        <label className="flex flex-col gap-1.5">
          <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Роль</span>
          <Select
            value={role}
            onChange={(v) => setRole(v as StoreEmployeeRole)}
            options={[
              { value: 'Cashier', label: 'Кассир' },
              { value: 'Owner', label: 'Владелец' },
            ]}
          />
        </label>
        <button
          type="submit"
          disabled={busy || !employeeEmail.trim()}
          className="flex items-center justify-center gap-1.5 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[13px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90 disabled:opacity-50"
        >
          <PlusIcon width={14} height={14} />
          {busy ? 'Добавляем…' : 'Добавить'}
        </button>
      </form>
      {formError && <div className="mb-3 text-[12px] font-medium text-[color:var(--admin-danger)]">{formError}</div>}
      {formSuccess && <div className="mb-3 text-[12px] font-medium text-[color:var(--admin-success)]">{formSuccess}</div>}
      <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
        Если сотрудник уже зарегистрирован в Sarfkor под этим email, доступ к панели выдаётся сразу. Если нет — ему
        придёт письмо со ссылкой, чтобы задать пароль и присоединиться.
      </p>

      {error && <div className="mb-3 text-[12px] font-medium text-[color:var(--admin-danger)]">{error}</div>}

      <div>
        {employees === null && !error && <EmptyRow>Загрузка…</EmptyRow>}
        {employees?.map((emp, i) => (
          <div key={emp.storeEmployeeId}>
            {i > 0 && <RowDivider />}
            <Row
              title={shortId(emp.userId, user?.userId)}
              subtitle={`${emp.role === 'Owner' ? 'Владелец' : 'Кассир'} · с ${new Date(emp.addedAt).toLocaleDateString('ru-RU')}`}
              trailing={
                <button
                  onClick={() => handleRemove(emp.storeEmployeeId)}
                  disabled={removingId === emp.storeEmployeeId}
                  aria-label="Удалить сотрудника"
                  className="grid h-8 w-8 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-danger-dim)] hover:text-[color:var(--admin-danger)] disabled:opacity-50"
                >
                  <TrashIcon width={14} height={14} />
                </button>
              }
            />
          </div>
        ))}
        {employees?.length === 0 && <EmptyRow>В магазине пока нет добавленных сотрудников</EmptyRow>}
      </div>
    </Panel>
  )
}

export function StaffPage() {
  const { storeId, user } = useAuth()
  const [shifts, setShifts] = useState<CashierShift[] | null>(null)
  const [anomalies, setAnomalies] = useState<CashierAnomaly[]>([])
  const [anomaliesForbidden, setAnomaliesForbidden] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!storeId) { setLoading(false); return }
    let cancelled = false
    async function load() {
      // Independent try/catch per endpoint — cost/profit-adjacent metrics like cashier
      // anomalies are owner-only (see the "Роли и доступ" note below), so a 403 there
      // shouldn't blank out the shifts/KPI sections a non-owner employee can still see.
      try {
        const shiftsRes = await salesApi.getCashierShifts(storeId!)
        if (cancelled) return
        setShifts(shiftsRes.shifts ?? [])
      } catch (err) {
        if (cancelled) return
        setError(err instanceof ApiError ? err.message : 'Не удалось загрузить данные о сотрудниках')
      }

      try {
        const anomaliesRes = await storesApi.getCashierAnomalies(storeId!, daysAgo(29), today())
        if (cancelled) return
        setAnomalies(anomaliesRes.cashiers ?? [])
      } catch (err) {
        if (cancelled) return
        if (err instanceof ApiError && err.status === 403) setAnomaliesForbidden(true)
      }

      if (!cancelled) setLoading(false)
    }
    load()
    return () => {
      cancelled = true
    }
  }, [storeId])

  if (loading) {
    return <Loading label="Загружаем данные…" />
  }

  if (error || !shifts) {
    return (
      <Panel>
        <ErrorState message={error || 'Нет данных'} />
      </Panel>
    )
  }

  const openShifts = shifts.filter((s) => !s.endedAt)
  const sortedShifts = [...shifts].sort((a, b) => b.startedAt.localeCompare(a.startedAt))

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-5">
      <Panel className="grid grid-cols-1 gap-6 sm:grid-cols-3">
        <Stat label="Смен всего" value={shifts.length} accent="#38bdf8" />
        <Stat label="Сейчас на смене" value={openShifts.length} accent="var(--admin-success)" />
        <Stat label="Кассиров активно (30 дней)" value={anomalies.length} accent="#818cf8" />
      </Panel>

      <EmployeesSection />

      <Panel>
        <SectionHeader title="Смены" />
        {sortedShifts.length === 0 && <EmptyRow>Смен ещё не было</EmptyRow>}
        {sortedShifts.map((s, i) => (
          <div key={s.cashierShiftId}>
            {i > 0 && <RowDivider />}
            <Row
              icon={<ClockIcon width={15} height={15} />}
              iconTone={s.endedAt ? 'neutral' : 'accent'}
              title={shortId(s.cashierUserId, user?.userId)}
              subtitle={
                <>
                  {new Date(s.startedAt).toLocaleString('ru-RU', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                  {s.endedAt ? ` — ${new Date(s.endedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}` : ''}
                  {' · '}
                  {fmt(s.openingCash)} {s.currency}
                  {s.closingCash !== undefined ? ` → ${fmt(s.closingCash)} ${s.currency}` : ''}
                </>
              }
              trailing={
                <span
                  className={`rounded-full px-3 py-1.5 text-[11px] font-semibold ${
                    s.endedAt ? 'bg-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)]' : 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]'
                  }`}
                >
                  {s.endedAt ? 'Закрыта' : 'Открыта'}
                </span>
              }
            />
          </div>
        ))}
      </Panel>

      <Panel>
        <SectionHeader
          eyebrow="Аномалии по отменам"
          title="Активность кассиров за 30 дней"
        />
        {anomaliesForbidden ? (
          <EmptyRow>Эта метрика видна только владельцу магазина</EmptyRow>
        ) : anomalies.length === 0 ? (
          <EmptyRow>Нет продаж за последние 30 дней</EmptyRow>
        ) : (
          anomalies.map((a, i) => (
            <div key={a.cashierUserId}>
              {i > 0 && <RowDivider />}
              <Row
                title={shortId(a.cashierUserId, user?.userId)}
                subtitle={`${a.totalSales} продаж · ${a.voidedSales} отмен · ${(a.voidRate * 100).toFixed(1)}% отмен`}
                trailing={
                  a.isAnomalous ? (
                    <span className="rounded-full bg-[color:var(--admin-danger-dim)] px-2.5 py-1 text-[11px] font-semibold text-[color:var(--admin-danger)]">
                      Аномалия
                    </span>
                  ) : undefined
                }
              />
            </div>
          ))
        )}
      </Panel>

      <Panel>
        <SectionHeader title="Роли и доступ" />
        <p className="-mt-2 mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Отдельной JWT-роли «кассир» пока нет — все, кто работает с кассой этого магазина, входят под ролью
          StorePartner. Доступ к себестоимости и отчётам о прибыли ограничен отдельно: только владелец магазина, не
          добавленные сотрудники.
        </p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          {ROLE_ACCESS.map((r) => (
            <div key={r.role} className="rounded-[18px] bg-[color:var(--admin-hover)] p-4">
              <div className="mb-2.5 flex items-center gap-1.5 text-[13px] font-bold text-[color:var(--admin-text)]">
                <ShieldIcon width={14} height={14} className="text-[color:var(--admin-accent)]" />
                {r.role}
              </div>
              <ul className="flex flex-col gap-1.5">
                {r.access.map((item) => (
                  <li key={item} className="flex items-center gap-1.5 text-[12px] text-[color:var(--admin-text-secondary)]">
                    <span className="h-1 w-1 shrink-0 rounded-full bg-[color:var(--admin-accent)]" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </Panel>
    </div>
  )
}
