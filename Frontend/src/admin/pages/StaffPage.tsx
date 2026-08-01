import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Card } from '../components/Card'
import { Select } from '../components/Select'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { ClockIcon, ShieldIcon, AlertIcon, PlusIcon, TrashIcon } from '../components/icons'
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
    <Card className="p-5">
      <div className="mb-4 flex items-center gap-2">
        <ShieldIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
        <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Сотрудники магазина</span>
      </div>

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
          className="flex items-center justify-center gap-1.5 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[13px] font-semibold text-white hover:opacity-90 disabled:opacity-50"
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

      <div className="flex flex-col gap-2.5">
        {employees === null && !error && (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">Загрузка…</div>
        )}
        {employees?.map((emp) => (
          <div
            key={emp.storeEmployeeId}
            className="flex items-center justify-between gap-3 rounded-[14px] bg-[color:var(--admin-hover)] p-3.5"
          >
            <div className="min-w-0">
              <div className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">
                {shortId(emp.userId, user?.userId)}
              </div>
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                {emp.role === 'Owner' ? 'Владелец' : 'Кассир'} · с {new Date(emp.addedAt).toLocaleDateString('ru-RU')}
              </div>
            </div>
            <button
              onClick={() => handleRemove(emp.storeEmployeeId)}
              disabled={removingId === emp.storeEmployeeId}
              aria-label="Удалить сотрудника"
              className="grid h-8 w-8 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-danger-dim)] hover:text-[color:var(--admin-danger)] disabled:opacity-50"
            >
              <TrashIcon width={14} height={14} />
            </button>
          </div>
        ))}
        {employees?.length === 0 && (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
            В магазине пока нет добавленных сотрудников
          </div>
        )}
      </div>
    </Card>
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
    if (!storeId) return
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
      <Card>
        <ErrorState message={error || 'Нет данных'} />
      </Card>
    )
  }

  const openShifts = shifts.filter((s) => !s.endedAt)
  const sortedShifts = [...shifts].sort((a, b) => b.startedAt.localeCompare(a.startedAt))

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Смен всего</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-text)]">{shifts.length}</div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Сейчас на смене</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-success)]">{openShifts.length}</div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Кассиров активно (30 дней)</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-text)]">{anomalies.length}</div>
        </Card>
      </div>

      <EmployeesSection />

      <Card className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <ClockIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Смены</span>
        </div>
        <div className="flex flex-col gap-3">
          {sortedShifts.map((s) => (
            <div
              key={s.cashierShiftId}
              className="flex flex-col gap-2 rounded-[16px] bg-[color:var(--admin-hover)] p-4 sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="flex items-center gap-3">
                <span
                  className="grid h-10 w-10 shrink-0 place-items-center rounded-xl text-[13px] font-bold text-white"
                  style={{ background: 'linear-gradient(135deg, var(--admin-accent), color-mix(in srgb, var(--admin-accent) 65%, black))' }}
                >
                  {shortId(s.cashierUserId, user?.userId).charAt(0).toUpperCase()}
                </span>
                <div>
                  <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">
                    {shortId(s.cashierUserId, user?.userId)}
                  </div>
                  <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                    {new Date(s.startedAt).toLocaleString('ru-RU', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                    {s.endedAt ? ` — ${new Date(s.endedAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}` : ''}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-4">
                <div className="text-right text-[12px] text-[color:var(--admin-text-secondary)]">
                  <div>Открытие: {fmt(s.openingCash)} {s.currency}</div>
                  {s.closingCash !== undefined && <div>Закрытие: {fmt(s.closingCash)} {s.currency}</div>}
                </div>
                <span
                  className={`shrink-0 rounded-full px-3 py-1.5 text-[11px] font-semibold ${
                    s.endedAt ? 'bg-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)]' : 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]'
                  }`}
                >
                  {s.endedAt ? 'Закрыта' : 'Открыта'}
                </span>
              </div>
            </div>
          ))}
          {sortedShifts.length === 0 && (
            <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">Смен ещё не было</div>
          )}
        </div>
      </Card>

      <Card className="p-5">
        <div className="mb-1 flex items-center gap-2">
          <AlertIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Активность кассиров за 30 дней</span>
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Аномальным считается кассир с необычно высокой долей отмен продаж — эта метрика считается на бэкенде.
        </p>
        {anomaliesForbidden ? (
          <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
            Эта метрика видна только владельцу магазина
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[520px] border-collapse text-left text-[13px]">
              <thead>
                <tr className="text-[11px] uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                  <th className="pb-3 font-semibold">Кассир</th>
                  <th className="pb-3 font-semibold">Продаж</th>
                  <th className="pb-3 font-semibold">Отмен</th>
                  <th className="pb-3 font-semibold">% отмен</th>
                  <th className="pb-3 font-semibold" />
                </tr>
              </thead>
              <tbody>
                {anomalies.map((a) => (
                  <tr key={a.cashierUserId} className="border-t border-[color:var(--admin-border)]">
                    <td className="py-3 pr-3 font-semibold text-[color:var(--admin-text)]">{shortId(a.cashierUserId, user?.userId)}</td>
                    <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{a.totalSales}</td>
                    <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{a.voidedSales}</td>
                    <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{(a.voidRate * 100).toFixed(1)}%</td>
                    <td className="py-3">
                      {a.isAnomalous && (
                        <span className="rounded-full bg-[color:var(--admin-danger-dim)] px-2.5 py-1 text-[11px] font-semibold text-[color:var(--admin-danger)]">Аномалия</span>
                      )}
                    </td>
                  </tr>
                ))}
                {anomalies.length === 0 && (
                  <tr>
                    <td colSpan={5} className="py-10 text-center text-[color:var(--admin-text-tertiary)]">
                      Нет продаж за последние 30 дней
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <Card className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <ShieldIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Роли и доступ</span>
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Отдельной JWT-роли «кассир» пока нет — все, кто работает с кассой этого магазина, входят под ролью
          StorePartner. Доступ к себестоимости и отчётам о прибыли ограничен отдельно: только владелец магазина, не
          добавленные сотрудники.
        </p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          {ROLE_ACCESS.map((r) => (
            <div key={r.role} className="rounded-[14px] bg-[color:var(--admin-hover)] p-4">
              <div className="mb-2.5 text-[13px] font-bold text-[color:var(--admin-text)]">{r.role}</div>
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
      </Card>
    </div>
  )
}
