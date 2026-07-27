import { useCallback, useEffect, useState, type FormEvent, type SVGProps } from 'react'
import clsx from 'clsx'
import { Card } from '../components/Card'
import { PhoneIcon, CardIcon, CashIcon, PlusIcon, CheckIcon, AlertIcon, SearchIcon, UsersIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { createCustomer, getCustomerByPhone, type Customer, type CustomerLookupResult } from '../../lib/api/customers'
import {
  createLoyaltyProgram,
  getLoyaltyProgram,
  enrollCustomerInLoyalty,
  earnLoyaltyPoints,
  redeemLoyaltyPoints,
  getLoyaltyAccount,
  type LoyaltyProgram,
  type LoyaltyAccount,
} from '../../lib/api/loyalty'
import { issueGiftCard, redeemGiftCard, getGiftCardBalance, type GiftCardBalance } from '../../lib/api/giftCards'
import { issueStoreCredit, redeemStoreCredit, getStoreCreditBalance, type StoreCreditBalance } from '../../lib/api/storeCredit'
import { ApiError } from '../../lib/api/client'

// Local icons — not in the shared admin icon set, kept inline per the
// integration note to avoid touching admin/components/icons.tsx.
function GiftIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={18} height={18} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <polyline points="20 12 20 22 4 22 4 12" />
      <rect x="2" y="7" width="20" height="5" />
      <line x1="12" y1="22" x2="12" y2="7" />
      <path d="M12 7H7.5a2.5 2.5 0 0 1 0-5C11 2 12 7 12 7Z" />
      <path d="M12 7h4.5a2.5 2.5 0 0 0 0-5C13 2 12 7 12 7Z" />
    </svg>
  )
}

function StarIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width={18} height={18} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" {...props}>
      <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
    </svg>
  )
}

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const inputClass =
  'w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]'

const primaryBtnClass =
  'flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-2.5 text-[13.5px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-50'

const secondaryBtnClass =
  'flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-hover)] py-2.5 text-[13.5px] font-bold text-[color:var(--admin-text)] transition-colors hover:bg-[color:var(--admin-border)] disabled:opacity-50'

type Feedback = { ok: boolean; text: string } | null

function FeedbackLine({ msg }: { msg: Feedback }) {
  if (!msg) return null
  return <div className={`text-[12px] font-medium ${msg.ok ? 'text-[#34d399]' : 'text-[#f87171]'}`}>{msg.text}</div>
}

/* ---------- outcome → Russian message maps ---------- */

const CREATE_PROGRAM_OUTCOME: Record<string, string> = {
  StoreNotFound: 'Магазин не найден',
  Forbidden: 'Нет доступа',
  AlreadyExists: 'Программа лояльности уже создана для этого магазина',
}

const ENROLL_OUTCOME: Record<string, string> = {
  AlreadyEnrolled: 'Клиент уже участвует в программе',
  CustomerNotFound: 'Клиент с таким ID не найден',
  ProgramNotFound: 'Программа лояльности не найдена',
  Forbidden: 'Нет доступа',
}

const EARN_OUTCOME: Record<string, string> = {
  AccountNotFound: 'Счёт лояльности не найден',
  Forbidden: 'Нет доступа',
}

const REDEEM_POINTS_OUTCOME: Record<string, string> = {
  AccountNotFound: 'Счёт лояльности не найден',
  Forbidden: 'Нет доступа',
  InsufficientPoints: 'Недостаточно баллов на счету',
}

const REDEEM_GIFT_CARD_OUTCOME: Record<string, string> = {
  NotFound: 'Карта с таким кодом не найдена',
  Inactive: 'Карта неактивна',
  Expired: 'Срок действия карты истёк',
  InsufficientBalance: 'Недостаточно средств на карте',
}

const ISSUE_CREDIT_OUTCOME: Record<string, string> = {
  StoreNotFound: 'Магазин не найден',
  CustomerNotFound: 'Клиент с таким ID не найден',
  Forbidden: 'Нет доступа',
}

const REDEEM_CREDIT_OUTCOME: Record<string, string> = {
  StoreNotFound: 'Магазин не найден',
  Forbidden: 'Нет доступа',
  NoCreditOnFile: 'На этом клиенте нет магазинного кредита',
  InsufficientBalance: 'Недостаточно средств на балансе кредита',
}

const CREDIT_BALANCE_OUTCOME: Record<string, string> = {
  StoreNotFound: 'Магазин не найден',
  Forbidden: 'Нет доступа',
}

/* ---------- Клиенты ---------- */

function CustomersSection({ customers, onRemember }: { customers: Customer[]; onRemember: (c: Customer) => void }) {
  const [phone, setPhone] = useState('')
  const [fullName, setFullName] = useState('')
  const [registering, setRegistering] = useState(false)
  const [registerMsg, setRegisterMsg] = useState<Feedback>(null)

  async function handleRegister(e: FormEvent) {
    e.preventDefault()
    const p = phone.trim()
    if (!p || registering) return
    setRegistering(true)
    setRegisterMsg(null)
    try {
      const res = await createCustomer(p, fullName.trim() || undefined)
      onRemember({ customerId: res.customerId, phoneNumber: p, fullName: fullName.trim() || undefined })
      setRegisterMsg({ ok: true, text: `Клиент создан — ID ${res.customerId}` })
      setPhone('')
      setFullName('')
    } catch (err) {
      setRegisterMsg({ ok: false, text: err instanceof ApiError ? err.message : 'Не удалось создать клиента' })
    } finally {
      setRegistering(false)
    }
  }

  const [searchPhone, setSearchPhone] = useState('')
  const [searching, setSearching] = useState(false)
  const [searchError, setSearchError] = useState('')
  const [searchResult, setSearchResult] = useState<CustomerLookupResult | null>(null)

  async function handleSearch(e: FormEvent) {
    e.preventDefault()
    const p = searchPhone.trim()
    if (!p || searching) return
    setSearching(true)
    setSearchError('')
    setSearchResult(null)
    try {
      const res = await getCustomerByPhone(p)
      setSearchResult(res)
      if (res.customerId) onRemember({ customerId: res.customerId, phoneNumber: p, fullName: res.fullName ?? undefined })
    } catch (err) {
      setSearchError(err instanceof ApiError ? err.message : 'Не удалось выполнить поиск')
    } finally {
      setSearching(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
            <PhoneIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
            Зарегистрировать клиента
          </div>
          <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
            Клиент — это просто номер телефона и, опционально, имя. К нему привязываются баллы лояльности, подарочные
            карты и магазинный кредит.
          </p>
          <form onSubmit={handleRegister} className="flex flex-col gap-3">
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Телефон</span>
              <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+992 90 000 00 00" className={inputClass} />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Имя (необязательно)</span>
              <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Имя клиента" className={inputClass} />
            </label>
            <FeedbackLine msg={registerMsg} />
            <button type="submit" disabled={registering || !phone.trim()} className={primaryBtnClass}>
              <PlusIcon width={15} height={15} />
              {registering ? 'Создаём…' : 'Создать клиента'}
            </button>
          </form>
        </Card>

        <Card className="p-5">
          <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
            <SearchIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
            Найти по телефону
          </div>
          <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
            Проверьте, зарегистрирован ли уже клиент с таким номером.
          </p>
          <form onSubmit={handleSearch} className="flex flex-col gap-3">
            <input value={searchPhone} onChange={(e) => setSearchPhone(e.target.value)} placeholder="+992 90 000 00 00" className={inputClass} />
            <button type="submit" disabled={searching || !searchPhone.trim()} className={secondaryBtnClass}>
              <SearchIcon width={14} height={14} />
              {searching ? 'Ищем…' : 'Найти'}
            </button>
            {searchError && <div className="text-[12px] font-medium text-[#f87171]">{searchError}</div>}
            {searchResult &&
              (searchResult.customerId ? (
                <div className="flex items-center gap-2 rounded-xl bg-[#34d39922] px-3.5 py-2.5 text-[12.5px] font-semibold text-[#34d399]">
                  <CheckIcon width={14} height={14} />
                  Найден — ID {searchResult.customerId}
                  {searchResult.fullName ? ` · ${searchResult.fullName}` : ''}
                </div>
              ) : (
                <div className="flex items-center gap-2 rounded-xl bg-[#fbbf2422] px-3.5 py-2.5 text-[12.5px] font-semibold text-[#fbbf24]">
                  <AlertIcon width={14} height={14} />
                  Клиент с таким номером не найден
                </div>
              ))}
          </form>
        </Card>
      </div>

      <Card className="p-5">
        <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
          <UsersIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
          Клиенты за эту сессию
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          В бэкенде нет эндпоинта для получения списка всех клиентов — здесь собраны только те, кого вы создали или
          нашли за эту сессию. ID клиента понадобится на вкладках «Лояльность» и «Магазинный кредит».
        </p>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[420px] border-collapse text-left text-[13px]">
            <thead>
              <tr className="text-[11px] uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                <th className="pb-3 font-semibold">ID клиента</th>
                <th className="pb-3 font-semibold">Телефон</th>
                <th className="pb-3 font-semibold">Имя</th>
              </tr>
            </thead>
            <tbody>
              {customers.map((c) => (
                <tr key={c.customerId} className="border-t border-[color:var(--admin-border)]">
                  <td className="py-3 pr-3 font-mono font-semibold text-[color:var(--admin-text)]">{c.customerId}</td>
                  <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{c.phoneNumber}</td>
                  <td className="py-3 text-[color:var(--admin-text-secondary)]">{c.fullName || '—'}</td>
                </tr>
              ))}
              {customers.length === 0 && (
                <tr>
                  <td colSpan={3} className="py-10 text-center text-[color:var(--admin-text-tertiary)]">
                    Пока никого — создайте или найдите клиента выше
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  )
}

/* ---------- Лояльность ---------- */

function LoyaltySection({ storeId }: { storeId: number | null }) {
  const [program, setProgram] = useState<LoyaltyProgram | null>(null)
  const [programLoading, setProgramLoading] = useState(true)
  const [programError, setProgramError] = useState('')

  const loadProgram = useCallback(async () => {
    if (!storeId) {
      setProgramLoading(false)
      return
    }
    setProgramError('')
    try {
      setProgram(await getLoyaltyProgram(storeId))
    } catch (err) {
      setProgramError(err instanceof ApiError ? err.message : 'Не удалось загрузить программу лояльности')
    } finally {
      setProgramLoading(false)
    }
  }, [storeId])

  useEffect(() => {
    loadProgram()
  }, [loadProgram])

  const hasProgram = !!(program && program.loyaltyProgramId != null)

  const [pointsPerUnit, setPointsPerUnit] = useState('1')
  const [redemptionRate, setRedemptionRate] = useState('0.01')
  const [creating, setCreating] = useState(false)
  const [createMsg, setCreateMsg] = useState<Feedback>(null)

  async function handleCreateProgram(e: FormEvent) {
    e.preventDefault()
    if (!storeId) return
    const ppu = Number(pointsPerUnit)
    const rr = Number(redemptionRate)
    if (!ppu || !rr) return
    setCreating(true)
    setCreateMsg(null)
    try {
      const res = await createLoyaltyProgram(storeId, ppu, rr)
      if (res.outcome === 'Created') {
        setCreateMsg({ ok: true, text: `Программа создана — ID ${res.loyaltyProgramId}` })
        await loadProgram()
      } else {
        setCreateMsg({ ok: false, text: CREATE_PROGRAM_OUTCOME[res.outcome] ?? res.outcome })
      }
    } catch (err) {
      setCreateMsg({ ok: false, text: err instanceof ApiError ? err.message : 'Не удалось создать программу' })
    } finally {
      setCreating(false)
    }
  }

  const [enrollCustomerId, setEnrollCustomerId] = useState('')
  const [enrolling, setEnrolling] = useState(false)
  const [enrollMsg, setEnrollMsg] = useState<Feedback>(null)

  async function handleEnroll(e: FormEvent) {
    e.preventDefault()
    const cid = Number(enrollCustomerId)
    if (!cid || !program?.loyaltyProgramId) return
    setEnrolling(true)
    setEnrollMsg(null)
    try {
      const res = await enrollCustomerInLoyalty(cid, program.loyaltyProgramId)
      if (res.outcome === 'Enrolled') {
        setEnrollMsg({ ok: true, text: `Клиент подключён — ID счёта ${res.loyaltyAccountId}` })
        setEnrollCustomerId('')
      } else {
        setEnrollMsg({ ok: false, text: ENROLL_OUTCOME[res.outcome] ?? res.outcome })
      }
    } catch (err) {
      setEnrollMsg({ ok: false, text: err instanceof ApiError ? err.message : 'Не удалось подключить клиента' })
    } finally {
      setEnrolling(false)
    }
  }

  const [accountId, setAccountId] = useState('')
  const [points, setPoints] = useState('')
  const [pointsBusy, setPointsBusy] = useState<'earn' | 'redeem' | null>(null)
  const [pointsMsg, setPointsMsg] = useState<Feedback>(null)

  async function handlePoints(kind: 'earn' | 'redeem') {
    const id = Number(accountId)
    const p = Number(points)
    if (!id || !p || p <= 0 || pointsBusy) return
    setPointsBusy(kind)
    setPointsMsg(null)
    try {
      if (kind === 'earn') {
        const res = await earnLoyaltyPoints(id, p)
        if (res.outcome === 'Earned') setPointsMsg({ ok: true, text: `Начислено. Новый баланс: ${fmt(res.newBalance ?? 0)}` })
        else setPointsMsg({ ok: false, text: EARN_OUTCOME[res.outcome] ?? res.outcome })
      } else {
        const res = await redeemLoyaltyPoints(id, p)
        if (res.outcome === 'Redeemed') setPointsMsg({ ok: true, text: `Списано. Новый баланс: ${fmt(res.newBalance ?? 0)}` })
        else setPointsMsg({ ok: false, text: REDEEM_POINTS_OUTCOME[res.outcome] ?? res.outcome })
      }
    } catch (err) {
      setPointsMsg({ ok: false, text: err instanceof ApiError ? err.message : 'Операция не выполнена' })
    } finally {
      setPointsBusy(null)
    }
  }

  const [balanceCustomerId, setBalanceCustomerId] = useState('')
  const [balanceChecking, setBalanceChecking] = useState(false)
  const [balanceError, setBalanceError] = useState('')
  const [balanceResult, setBalanceResult] = useState<LoyaltyAccount | null>(null)

  async function handleCheckBalance(e: FormEvent) {
    e.preventDefault()
    const cid = Number(balanceCustomerId)
    if (!cid || !program?.loyaltyProgramId) return
    setBalanceChecking(true)
    setBalanceError('')
    setBalanceResult(null)
    try {
      setBalanceResult(await getLoyaltyAccount(cid, program.loyaltyProgramId))
    } catch (err) {
      setBalanceError(err instanceof ApiError ? err.message : 'Не удалось получить баланс')
    } finally {
      setBalanceChecking(false)
    }
  }

  if (!storeId) {
    return <Card className="p-8 text-center text-[14px] text-[color:var(--admin-text-secondary)]">Магазин не выбран</Card>
  }

  return (
    <div className="flex flex-col gap-6">
      <Card className="p-5">
        <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
          <StarIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
          Программа лояльности магазина
        </div>

        {programLoading ? (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">Загружаем…</div>
        ) : programError ? (
          <div className="flex items-center justify-between gap-3 rounded-xl bg-[#f8717122] px-3.5 py-2.5 text-[12.5px] font-medium text-[#f87171]">
            {programError}
            <button onClick={loadProgram} className="shrink-0 font-bold underline">
              Повторить
            </button>
          </div>
        ) : hasProgram && program ? (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">ID программы</div>
              <div className="mt-1 text-[16px] font-bold text-[color:var(--admin-text)]">{program.loyaltyProgramId}</div>
            </div>
            <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">Баллов за 1 единицу валюты</div>
              <div className="mt-1 text-[16px] font-bold text-[color:var(--admin-text)]">{program.pointsPerCurrencyUnit}</div>
            </div>
            <div className="rounded-xl bg-[color:var(--admin-hover)] p-4">
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">Курс погашения</div>
              <div className="mt-1 text-[16px] font-bold text-[color:var(--admin-text)]">{program.redemptionRate}</div>
            </div>
          </div>
        ) : (
          <>
            <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
              У этого магазина ещё нет программы лояльности. Задайте, сколько баллов начисляется за единицу валюты и
              по какому курсу баллы обмениваются обратно.
            </p>
            <form onSubmit={handleCreateProgram} className="grid grid-cols-1 gap-3 sm:grid-cols-3">
              <label className="flex flex-col gap-1.5">
                <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Баллов за 1 ед. валюты</span>
                <input type="number" min={0} step="0.01" value={pointsPerUnit} onChange={(e) => setPointsPerUnit(e.target.value)} className={inputClass} />
              </label>
              <label className="flex flex-col gap-1.5">
                <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Курс погашения</span>
                <input type="number" min={0} step="0.0001" value={redemptionRate} onChange={(e) => setRedemptionRate(e.target.value)} className={inputClass} />
              </label>
              <div className="flex items-end">
                <button type="submit" disabled={creating || !pointsPerUnit || !redemptionRate} className={clsx(primaryBtnClass, 'w-full')}>
                  <PlusIcon width={15} height={15} />
                  {creating ? 'Создаём…' : 'Создать программу'}
                </button>
              </div>
            </form>
          </>
        )}
        <div className="mt-3">
          <FeedbackLine msg={createMsg} />
        </div>
      </Card>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <div className="mb-1 text-[15px] font-bold text-[color:var(--admin-text)]">Подключить клиента</div>
          <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
            Введите ID клиента (см. вкладку «Клиенты»), чтобы завести ему счёт в программе лояльности.
          </p>
          <form onSubmit={handleEnroll} className="flex flex-col gap-3">
            <input type="number" value={enrollCustomerId} onChange={(e) => setEnrollCustomerId(e.target.value)} placeholder="ID клиента" className={inputClass} />
            <button type="submit" disabled={enrolling || !enrollCustomerId || !hasProgram} className={primaryBtnClass}>
              {enrolling ? 'Подключаем…' : 'Подключить'}
            </button>
            {!hasProgram && (
              <p className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">Сначала создайте программу лояльности выше</p>
            )}
            <FeedbackLine msg={enrollMsg} />
          </form>
        </Card>

        <Card className="p-5">
          <div className="mb-1 text-[15px] font-bold text-[color:var(--admin-text)]">Баланс баллов клиента</div>
          <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">Проверить баланс баллов по ID клиента.</p>
          <form onSubmit={handleCheckBalance} className="flex flex-col gap-3">
            <input type="number" value={balanceCustomerId} onChange={(e) => setBalanceCustomerId(e.target.value)} placeholder="ID клиента" className={inputClass} />
            <button type="submit" disabled={balanceChecking || !balanceCustomerId || !hasProgram} className={secondaryBtnClass}>
              <SearchIcon width={14} height={14} />
              {balanceChecking ? 'Проверяем…' : 'Проверить баланс'}
            </button>
            {balanceError && <div className="text-[12px] font-medium text-[#f87171]">{balanceError}</div>}
            {balanceResult &&
              (balanceResult.loyaltyAccountId ? (
                <div className="rounded-xl bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[12.5px] text-[color:var(--admin-text)]">
                  Счёт #{balanceResult.loyaltyAccountId} · баланс: <span className="font-bold">{fmt(balanceResult.pointsBalance ?? 0)}</span> баллов
                </div>
              ) : (
                <div className="rounded-xl bg-[#fbbf2422] px-3.5 py-2.5 text-[12.5px] font-semibold text-[#fbbf24]">
                  У этого клиента нет счёта в этой программе
                </div>
              ))}
          </form>
        </Card>
      </div>

      <Card className="p-5">
        <div className="mb-1 text-[15px] font-bold text-[color:var(--admin-text)]">Начислить / списать баллы</div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Введите ID счёта лояльности (возвращается при подключении клиента) и количество баллов.
        </p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_1fr_auto_auto]">
          <input type="number" value={accountId} onChange={(e) => setAccountId(e.target.value)} placeholder="ID счёта лояльности" className={inputClass} />
          <input type="number" min={0} value={points} onChange={(e) => setPoints(e.target.value)} placeholder="Баллы" className={inputClass} />
          <button
            onClick={() => handlePoints('earn')}
            disabled={pointsBusy !== null || !accountId || !points}
            className="shrink-0 rounded-xl bg-[color:var(--admin-accent)] px-4 py-2.5 text-[13px] font-bold text-white disabled:opacity-50"
          >
            {pointsBusy === 'earn' ? 'Начисляем…' : 'Начислить'}
          </button>
          <button
            onClick={() => handlePoints('redeem')}
            disabled={pointsBusy !== null || !accountId || !points}
            className="shrink-0 rounded-xl bg-[color:var(--admin-hover)] px-4 py-2.5 text-[13px] font-bold text-[color:var(--admin-text)] disabled:opacity-50"
          >
            {pointsBusy === 'redeem' ? 'Списываем…' : 'Списать'}
          </button>
        </div>
        <div className="mt-3">
          <FeedbackLine msg={pointsMsg} />
        </div>
      </Card>
    </div>
  )
}

/* ---------- Подарочные карты ---------- */

function GiftCardsSection() {
  const [amount, setAmount] = useState('')
  const [currency, setCurrency] = useState('TJS')
  const [expiresAt, setExpiresAt] = useState('')
  const [issuing, setIssuing] = useState(false)
  const [issueError, setIssueError] = useState('')
  const [issuedCode, setIssuedCode] = useState<{ giftCardId: number; code: string } | null>(null)
  const [copied, setCopied] = useState(false)

  async function handleIssue(e: FormEvent) {
    e.preventDefault()
    const amt = Number(amount)
    if (!amt || amt <= 0 || issuing) return
    setIssuing(true)
    setIssueError('')
    try {
      const iso = expiresAt ? new Date(expiresAt).toISOString() : undefined
      const res = await issueGiftCard(amt, currency.trim() || 'TJS', iso)
      setIssuedCode(res)
      setAmount('')
      setExpiresAt('')
    } catch (err) {
      setIssueError(err instanceof ApiError ? err.message : 'Не удалось выпустить карту')
    } finally {
      setIssuing(false)
    }
  }

  async function handleCopy() {
    if (!issuedCode) return
    try {
      await navigator.clipboard.writeText(issuedCode.code)
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    } catch {
      // Clipboard API unavailable — the code is still visible to select and copy manually.
    }
  }

  const [checkCode, setCheckCode] = useState('')
  const [checking, setChecking] = useState(false)
  const [checkError, setCheckError] = useState('')
  const [checkResult, setCheckResult] = useState<GiftCardBalance | null>(null)

  async function handleCheck(e: FormEvent) {
    e.preventDefault()
    const code = checkCode.trim()
    if (!code || checking) return
    setChecking(true)
    setCheckError('')
    setCheckResult(null)
    try {
      setCheckResult(await getGiftCardBalance(code))
    } catch (err) {
      setCheckError(err instanceof ApiError ? err.message : 'Не удалось проверить карту')
    } finally {
      setChecking(false)
    }
  }

  const [redeemCode, setRedeemCode] = useState('')
  const [redeemAmount, setRedeemAmount] = useState('')
  const [redeeming, setRedeeming] = useState(false)
  const [redeemMsg, setRedeemMsg] = useState<Feedback>(null)

  async function handleRedeem(e: FormEvent) {
    e.preventDefault()
    const code = redeemCode.trim()
    const amt = Number(redeemAmount)
    if (!code || !amt || amt <= 0 || redeeming) return
    setRedeeming(true)
    setRedeemMsg(null)
    try {
      const res = await redeemGiftCard(code, amt)
      if (res.outcome === 'Redeemed') {
        setRedeemMsg({ ok: true, text: `Погашено. Остаток на карте: ${fmt(res.remainingBalance ?? 0)}` })
        setRedeemAmount('')
      } else {
        setRedeemMsg({ ok: false, text: REDEEM_GIFT_CARD_OUTCOME[res.outcome] ?? res.outcome })
      }
    } catch (err) {
      setRedeemMsg({ ok: false, text: err instanceof ApiError ? err.message : 'Не удалось погасить карту' })
    } finally {
      setRedeeming(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Card className="p-5">
        <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
          <GiftIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
          Выпустить подарочную карту
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Карта анонимна и определяется только кодом — привязка к клиенту не нужна.
        </p>
        <form onSubmit={handleIssue} className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto_1fr_auto]">
          <input type="number" min={0} step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} placeholder="Сумма" className={inputClass} />
          <input value={currency} onChange={(e) => setCurrency(e.target.value)} placeholder="TJS" className={clsx(inputClass, 'w-20 shrink-0')} />
          <input type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} className={inputClass} />
          <button type="submit" disabled={issuing || !amount} className={clsx(primaryBtnClass, 'shrink-0 px-5')}>
            <PlusIcon width={15} height={15} />
            {issuing ? 'Выпускаем…' : 'Выпустить'}
          </button>
        </form>
        <p className="mt-2 text-[11px] text-[color:var(--admin-text-tertiary)]">
          Срок действия необязателен — оставьте пустым для бессрочной карты.
        </p>
        {issueError && <div className="mt-3 text-[12px] font-medium text-[#f87171]">{issueError}</div>}
        {issuedCode && (
          <div className="mt-4 rounded-xl bg-[color:var(--admin-accent-soft)] p-4">
            <div className="mb-2 text-[12.5px] font-semibold text-[color:var(--admin-accent)]">
              Сохраните этот код — он больше нигде не отображается
            </div>
            <div className="flex items-center gap-2">
              <code className="flex-1 select-all break-all rounded-lg bg-[color:var(--admin-card)] px-3 py-2 text-[15px] font-bold tracking-wide text-[color:var(--admin-text)]">
                {issuedCode.code}
              </code>
              <button onClick={handleCopy} className="shrink-0 rounded-lg bg-[color:var(--admin-accent)] px-3 py-2 text-[12px] font-bold text-white hover:opacity-90">
                {copied ? 'Скопировано ✓' : 'Копировать'}
              </button>
            </div>
            <div className="mt-1.5 text-[11px] text-[color:var(--admin-text-tertiary)]">ID карты: {issuedCode.giftCardId}</div>
          </div>
        )}
      </Card>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
            <SearchIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
            Проверить баланс
          </div>
          <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">Доступно без входа в систему — по коду карты.</p>
          <form onSubmit={handleCheck} className="flex flex-col gap-3">
            <input value={checkCode} onChange={(e) => setCheckCode(e.target.value)} placeholder="Код карты" className={inputClass} />
            <button type="submit" disabled={checking || !checkCode.trim()} className={secondaryBtnClass}>
              {checking ? 'Проверяем…' : 'Проверить'}
            </button>
            {checkError && <div className="text-[12px] font-medium text-[#f87171]">{checkError}</div>}
            {checkResult &&
              (checkResult.found ? (
                <div className="rounded-xl bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[12.5px] text-[color:var(--admin-text)]">
                  <div>
                    Баланс: <span className="font-bold">{fmt(checkResult.balance ?? 0)}</span> {checkResult.currency}
                  </div>
                  <div className="mt-1 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                    {checkResult.isActive ? 'Активна' : 'Неактивна'}
                    {checkResult.expiresAt ? ` · действует до ${new Date(checkResult.expiresAt).toLocaleDateString('ru-RU')}` : ''}
                  </div>
                </div>
              ) : (
                <div className="flex items-center gap-2 rounded-xl bg-[#fbbf2422] px-3.5 py-2.5 text-[12.5px] font-semibold text-[#fbbf24]">
                  <AlertIcon width={14} height={14} />
                  Карта с таким кодом не найдена
                </div>
              ))}
          </form>
        </Card>

        <Card className="p-5">
          <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
            <CardIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
            Погасить (списать с карты)
          </div>
          <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">Клиент расплачивается подарочной картой при покупке.</p>
          <form onSubmit={handleRedeem} className="flex flex-col gap-3">
            <input value={redeemCode} onChange={(e) => setRedeemCode(e.target.value)} placeholder="Код карты" className={inputClass} />
            <input type="number" min={0} step="0.01" value={redeemAmount} onChange={(e) => setRedeemAmount(e.target.value)} placeholder="Сумма" className={inputClass} />
            <button type="submit" disabled={redeeming || !redeemCode.trim() || !redeemAmount} className={primaryBtnClass}>
              {redeeming ? 'Погашаем…' : 'Погасить'}
            </button>
            <FeedbackLine msg={redeemMsg} />
          </form>
        </Card>
      </div>
    </div>
  )
}

/* ---------- Магазинный кредит ---------- */

function StoreCreditSection({ storeId }: { storeId: number | null }) {
  const [issueCustomerId, setIssueCustomerId] = useState('')
  const [issueAmount, setIssueAmount] = useState('')
  const [issueCurrency, setIssueCurrency] = useState('TJS')
  const [issuing, setIssuing] = useState(false)
  const [issueMsg, setIssueMsg] = useState<Feedback>(null)

  async function handleIssue(e: FormEvent) {
    e.preventDefault()
    if (!storeId) return
    const cid = Number(issueCustomerId)
    const amt = Number(issueAmount)
    if (!cid || !amt || amt <= 0 || issuing) return
    setIssuing(true)
    setIssueMsg(null)
    try {
      const res = await issueStoreCredit(storeId, cid, amt, issueCurrency.trim() || 'TJS')
      if (res.outcome === 'Issued') {
        setIssueMsg({ ok: true, text: `Начислено. Новый баланс: ${fmt(res.newBalance ?? 0)} ${issueCurrency}` })
        setIssueAmount('')
      } else {
        setIssueMsg({ ok: false, text: ISSUE_CREDIT_OUTCOME[res.outcome] ?? res.outcome })
      }
    } catch (err) {
      setIssueMsg({ ok: false, text: err instanceof ApiError ? err.message : 'Не удалось начислить кредит' })
    } finally {
      setIssuing(false)
    }
  }

  const [redeemCustomerId, setRedeemCustomerId] = useState('')
  const [redeemAmount, setRedeemAmount] = useState('')
  const [redeeming, setRedeeming] = useState(false)
  const [redeemMsg, setRedeemMsg] = useState<Feedback>(null)

  async function handleRedeem(e: FormEvent) {
    e.preventDefault()
    if (!storeId) return
    const cid = Number(redeemCustomerId)
    const amt = Number(redeemAmount)
    if (!cid || !amt || amt <= 0 || redeeming) return
    setRedeeming(true)
    setRedeemMsg(null)
    try {
      const res = await redeemStoreCredit(storeId, cid, amt)
      if (res.outcome === 'Redeemed') {
        setRedeemMsg({ ok: true, text: `Списано. Новый баланс: ${fmt(res.newBalance ?? 0)}` })
        setRedeemAmount('')
      } else {
        setRedeemMsg({ ok: false, text: REDEEM_CREDIT_OUTCOME[res.outcome] ?? res.outcome })
      }
    } catch (err) {
      setRedeemMsg({ ok: false, text: err instanceof ApiError ? err.message : 'Не удалось списать кредит' })
    } finally {
      setRedeeming(false)
    }
  }

  const [balanceCustomerId, setBalanceCustomerId] = useState('')
  const [balanceChecking, setBalanceChecking] = useState(false)
  const [balanceError, setBalanceError] = useState('')
  const [balanceResult, setBalanceResult] = useState<StoreCreditBalance | null>(null)

  async function handleCheckBalance(e: FormEvent) {
    e.preventDefault()
    if (!storeId) return
    const cid = Number(balanceCustomerId)
    if (!cid || balanceChecking) return
    setBalanceChecking(true)
    setBalanceError('')
    setBalanceResult(null)
    try {
      const res = await getStoreCreditBalance(storeId, cid)
      if (res.outcome === 'Found') {
        setBalanceResult(res)
      } else {
        setBalanceError(CREDIT_BALANCE_OUTCOME[res.outcome] ?? res.outcome)
      }
    } catch (err) {
      setBalanceError(err instanceof ApiError ? err.message : 'Не удалось получить баланс')
    } finally {
      setBalanceChecking(false)
    }
  }

  if (!storeId) {
    return <Card className="p-8 text-center text-[14px] text-[color:var(--admin-text-secondary)]">Магазин не выбран</Card>
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <Card className="p-5">
        <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
          <CashIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
          Начислить кредит
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">Например, вместо возврата денег при отмене продажи.</p>
        <form onSubmit={handleIssue} className="flex flex-col gap-3">
          <input type="number" value={issueCustomerId} onChange={(e) => setIssueCustomerId(e.target.value)} placeholder="ID клиента" className={inputClass} />
          <div className="flex gap-2">
            <input type="number" min={0} step="0.01" value={issueAmount} onChange={(e) => setIssueAmount(e.target.value)} placeholder="Сумма" className={inputClass} />
            <input value={issueCurrency} onChange={(e) => setIssueCurrency(e.target.value)} placeholder="TJS" className={clsx(inputClass, 'w-20 shrink-0')} />
          </div>
          <button type="submit" disabled={issuing || !issueCustomerId || !issueAmount} className={primaryBtnClass}>
            <PlusIcon width={15} height={15} />
            {issuing ? 'Начисляем…' : 'Начислить'}
          </button>
          <FeedbackLine msg={issueMsg} />
        </form>
      </Card>

      <Card className="p-5">
        <div className="mb-1 text-[15px] font-bold text-[color:var(--admin-text)]">Списать кредит</div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">Клиент расплачивается накопленным кредитом.</p>
        <form onSubmit={handleRedeem} className="flex flex-col gap-3">
          <input type="number" value={redeemCustomerId} onChange={(e) => setRedeemCustomerId(e.target.value)} placeholder="ID клиента" className={inputClass} />
          <input type="number" min={0} step="0.01" value={redeemAmount} onChange={(e) => setRedeemAmount(e.target.value)} placeholder="Сумма" className={inputClass} />
          <button type="submit" disabled={redeeming || !redeemCustomerId || !redeemAmount} className={secondaryBtnClass}>
            {redeeming ? 'Списываем…' : 'Списать'}
          </button>
          <FeedbackLine msg={redeemMsg} />
        </form>
      </Card>

      <Card className="p-5">
        <div className="mb-1 flex items-center gap-2 text-[15px] font-bold text-[color:var(--admin-text)]">
          <SearchIcon width={16} height={16} className="text-[color:var(--admin-accent)]" />
          Баланс кредита
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">Проверить остаток кредита клиента в этом магазине.</p>
        <form onSubmit={handleCheckBalance} className="flex flex-col gap-3">
          <input type="number" value={balanceCustomerId} onChange={(e) => setBalanceCustomerId(e.target.value)} placeholder="ID клиента" className={inputClass} />
          <button type="submit" disabled={balanceChecking || !balanceCustomerId} className={secondaryBtnClass}>
            <SearchIcon width={14} height={14} />
            {balanceChecking ? 'Проверяем…' : 'Проверить'}
          </button>
          {balanceError && <div className="text-[12px] font-medium text-[#f87171]">{balanceError}</div>}
          {balanceResult && (
            <div className="rounded-xl bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[12.5px] text-[color:var(--admin-text)]">
              Баланс: <span className="font-bold">{fmt(balanceResult.balance ?? 0)}</span> {balanceResult.currency}
            </div>
          )}
        </form>
      </Card>
    </div>
  )
}

/* ---------- shell ---------- */

type TabId = 'customers' | 'loyalty' | 'giftcards' | 'credit'

const TAB_ITEMS: { id: TabId; label: string }[] = [
  { id: 'customers', label: 'Клиенты' },
  { id: 'loyalty', label: 'Лояльность' },
  { id: 'giftcards', label: 'Подарочные карты' },
  { id: 'credit', label: 'Магазинный кредит' },
]

function tabIcon(id: TabId) {
  switch (id) {
    case 'customers':
      return <UsersIcon width={15} height={15} />
    case 'loyalty':
      return <StarIcon width={15} height={15} />
    case 'giftcards':
      return <GiftIcon width={15} height={15} />
    case 'credit':
      return <CashIcon width={15} height={15} />
  }
}

export function CustomersPage() {
  const { storeId } = useAuth()
  const [tab, setTab] = useState<TabId>('customers')
  const [customers, setCustomers] = useState<Customer[]>([])

  const rememberCustomer = useCallback((c: Customer) => {
    setCustomers((cur) => {
      const idx = cur.findIndex((x) => x.customerId === c.customerId)
      if (idx === -1) return [c, ...cur]
      const next = [...cur]
      next[idx] = { ...next[idx], ...c }
      return next
    })
  }, [])

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="flex flex-wrap gap-2">
        {TAB_ITEMS.map((t) => (
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
            {tabIcon(t.id)}
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'customers' && <CustomersSection customers={customers} onRemember={rememberCustomer} />}
      {tab === 'loyalty' && <LoyaltySection storeId={storeId} />}
      {tab === 'giftcards' && <GiftCardsSection />}
      {tab === 'credit' && <StoreCreditSection storeId={storeId} />}
    </div>
  )
}
