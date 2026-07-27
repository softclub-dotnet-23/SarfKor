import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { AdminModal } from '../components/AdminModal'
import { ClockIcon, ShieldIcon, AlertIcon, PlusIcon, TrashIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { storesApi, salesApi, ApiError, type CashierShift, type CashierAnomaly, type StoreEmployee } from '../../lib/api'
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

const ADD_EMPLOYEE_ERRORS: Record<string, string> = {
  EmployeeNotFound: 'Нет пользователя с таким email — попросите кассира сначала зарегистрироваться в приложении',
  AlreadyEmployed: 'Этот пользователь уже добавлен в этот магазин',
  Forbidden: 'Добавлять сотрудников может только владелец магазина',
  StoreNotFound: 'Магазин не найден',
}

export function StaffPage() {
  const { storeId, user } = useAuth()
  const [shifts, setShifts] = useState<CashierShift[] | null>(null)
  const [anomalies, setAnomalies] = useState<CashierAnomaly[]>([])
  const [anomaliesForbidden, setAnomaliesForbidden] = useState(false)
  const [employees, setEmployees] = useState<StoreEmployee[]>([])
  const [employeesForbidden, setEmployeesForbidden] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [addOpen, setAddOpen] = useState(false)
  const [addEmail, setAddEmail] = useState('')
  const [addBusy, setAddBusy] = useState(false)
  const [addError, setAddError] = useState('')
  const [removingId, setRemovingId] = useState<number | null>(null)

  const loadEmployees = useCallback(async () => {
    if (!storeId) return
    try {
      const res = await storesApi.getStoreEmployees(storeId)
      setEmployees(res.employees ?? [])
      setEmployeesForbidden(false)
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setEmployeesForbidden(true)
      }
    }
  }, [storeId])

  useEffect(() => {
    if (!storeId) return
    let cancelled = false
    async function load() {
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

      await loadEmployees()
      if (!cancelled) setLoading(false)
    }
    load()
    return () => {
      cancelled = true
    }
  }, [storeId, loadEmployees])

  function openAddForm() {
    setAddEmail('')
    setAddError('')
    setAddOpen(true)
  }

  async function confirmAddEmployee() {
    if (!storeId || !addEmail.trim() || addBusy) return
    setAddBusy(true)
    setAddError('')
    try {
      const res = await storesApi.addStoreEmployee(storeId, addEmail.trim())
      if (res.outcome !== 'Added') {
        setAddError(ADD_EMPLOYEE_ERRORS[res.outcome] ?? 'Не удалось добавить сотрудника')
        return
      }
      setAddOpen(false)
      await loadEmployees()
    } catch (err) {
      setAddError(err instanceof ApiError ? err.message : 'Не удалось добавить сотрудника')
    } finally {
      setAddBusy(false)
    }
  }

  async function removeEmployee(storeEmployeeId: number) {
    if (removingId) return
    setRemovingId(storeEmployeeId)
    try {
      await storesApi.removeStoreEmployee(storeEmployeeId)
      await loadEmployees()
    } catch {
      // Best-effort — a stale list refreshes on the next load anyway.
    } finally {
      setRemovingId(null)
    }
  }

  if (loading) {
    return <div className="py-24 text-center text-[color:var(--admin-text-tertiary)]">Загружаем данные…</div>
  }

  if (error || !shifts) {
    return (
      <Card className="p-8 text-center">
        <p className="text-[14px] text-[color:var(--admin-text-secondary)]">{error || 'Нет данных'}</p>
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
          <div className="mt-2 text-[26px] font-extrabold text-[#34d399]">{openShifts.length}</div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">Кассиров активно (30 дней)</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-text)]">{anomalies.length}</div>
        </Card>
      </div>

      <Card className="p-5">
        <div className="mb-4 flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <ShieldIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
            <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Сотрудники магазина</span>
          </div>
          <button
            onClick={openAddForm}
            className="flex items-center gap-1.5 rounded-xl bg-[color:var(--admin-accent)] px-3.5 py-2 text-[12.5px] font-semibold text-white hover:opacity-90"
          >
            <PlusIcon width={14} height={14} />
            Добавить кассира
          </button>
        </div>
        {employeesForbidden ? (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
            Список сотрудников виден только владельцу магазина
          </div>
        ) : (
          <div className="flex flex-col gap-2.5">
            {employees.map((emp) => (
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
                  onClick={() => removeEmployee(emp.storeEmployeeId)}
                  disabled={removingId === emp.storeEmployeeId}
                  aria-label="Удалить сотрудника"
                  className="grid h-8 w-8 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[#f8717122] hover:text-[#f87171] disabled:opacity-50"
                >
                  <TrashIcon width={14} height={14} />
                </button>
              </div>
            ))}
            {employees.length === 0 && (
              <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">
                В магазине пока нет добавленных сотрудников
              </div>
            )}
          </div>
        )}
      </Card>

      <AdminModal open={addOpen} onClose={() => setAddOpen(false)} title="Добавить кассира">
        <div className="flex flex-col gap-4">
          <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">
            Кассир должен сначала зарегистрироваться в приложении — добавьте его по email аккаунта.
          </p>
          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Email кассира</span>
            <input
              type="email"
              value={addEmail}
              onChange={(e) => setAddEmail(e.target.value)}
              placeholder="cashier@sarfkor.tj"
              className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
            />
          </label>
          {addError && <div className="text-[12px] font-medium text-[#f87171]">{addError}</div>}
          <button
            onClick={confirmAddEmployee}
            disabled={addBusy || !addEmail.trim()}
            className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50"
          >
            {addBusy ? 'Добавляем…' : 'Добавить'}
          </button>
        </div>
      </AdminModal>

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
                  style={{ background: 'linear-gradient(135deg,#38bdf8,#0ea5e9)' }}
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
                    s.endedAt ? 'bg-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)]' : 'bg-[#34d39922] text-[#34d399]'
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
                        <span className="rounded-full bg-[#f8717122] px-2.5 py-1 text-[11px] font-semibold text-[#f87171]">Аномалия</span>
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
          StorePartner. Доступ к себестоимости и отчётам о прибыли ограничен отдельно: только владелец магазина
          (Store.OwnerUserId), не добавленные сотрудники.
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
