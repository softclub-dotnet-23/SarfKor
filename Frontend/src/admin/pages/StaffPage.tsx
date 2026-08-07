import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Select } from '../components/Select'
import { Badge } from '../components/Badge'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { FormModal, FormField } from '../components/FormModal'
import { Toast } from '../components/Toast'
import { ClockIcon, ShieldIcon, AlertIcon, PlusIcon, TrashIcon, RefreshIcon } from '../components/icons'
import { useAuth } from '../../auth/AuthContext'
import { useT } from '../../i18n/translations'
import { useLocaleFormat } from '../../i18n/format'
import {
  storesApi,
  salesApi,
  meApi,
  ApiError,
  type CashierShift,
  type CashierAnomaly,
  type StoreEmployee,
  type StoreEmployeeRole,
  type StoreEmployeeInvitation,
  type MyStore,
} from '../../lib/api'
import { daysAgo, today } from '../lib/dates'

function shortId(id: string, myId: string | undefined, t: (key: 'partner.staff.you') => string) {
  if (id === myId) return t('partner.staff.you')
  return id.slice(0, 8) + '…'
}

function InviteEmployeeModal({
  open,
  onClose,
  storeId,
  onSuccess,
}: {
  open: boolean
  onClose: () => void
  storeId: number | null
  onSuccess: () => Promise<void> | void
}) {
  const t = useT()
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<StoreEmployeeRole>('Cashier')
  const [emailError, setEmailError] = useState('')
  const [ownedStores, setOwnedStores] = useState<MyStore[]>([])
  const [targetStoreId, setTargetStoreId] = useState<number | null>(storeId)

  useEffect(() => {
    if (!open) return
    setEmail('')
    setRole('Cashier')
    setEmailError('')
    setTargetStoreId(storeId)
    // Only fetched to populate the "торговая точка" picker when the owner actually has more than
    // one store -- the common case (one store) never shows a picker at all, current storeId is
    // used directly.
    meApi.getMyStores().then((res) => setOwnedStores(res.stores.filter((s) => s.role === 'Owner'))).catch(() => setOwnedStores([]))
  }, [open, storeId])

  async function submit() {
    const trimmed = email.trim()
    if (!trimmed) {
      setEmailError(t('partner.staff.emailRequired'))
      throw new Error(t('partner.staff.emailRequired'))
    }
    if (!targetStoreId) throw new Error(t('partner.staff.storeNotSelected'))
    const res = await storesApi.createStoreEmployeeInvitation(targetStoreId, trimmed, role)
    if (res.outcome === 'Sent') {
      await onSuccess()
    } else if (res.outcome === 'AlreadyEmployed') {
      throw new Error(t('partner.staff.alreadyEmployed'))
    } else if (res.outcome === 'Forbidden') {
      throw new Error(t('partner.staff.forbidden'))
    } else {
      throw new Error(t('partner.staff.storeNotFound'))
    }
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title={t('partner.staff.inviteModalTitle')}
      isDirty={!!email}
      onSubmit={submit}
      submitLabel={t('partner.staff.sendInvite')}
      submitBusyLabel={t('partner.staff.sending')}
      scheme="admin"
    >
      <FormField label={t('partner.staff.emailLabel')} required error={emailError} scheme="admin">
        <input
          type="email"
          value={email}
          onChange={(e) => {
            setEmail(e.target.value)
            setEmailError('')
          }}
          placeholder="cashier@sarfkor.tj"
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label={t('partner.staff.roleLabel')} scheme="admin">
        <Select
          value={role}
          onChange={(v) => setRole(v as StoreEmployeeRole)}
          options={[
            { value: 'Cashier', label: t('partner.staff.roleCashier') },
            { value: 'Owner', label: t('partner.staff.roleOwner') },
          ]}
        />
      </FormField>
      {ownedStores.length > 1 && (
        <FormField label={t('partner.staff.storeLabel')} scheme="admin">
          <Select
            value={String(targetStoreId ?? '')}
            onChange={(v) => setTargetStoreId(Number(v))}
            options={ownedStores.map((s) => ({ value: String(s.storeId), label: s.name }))}
          />
        </FormField>
      )}
      <p className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.inviteHint')}</p>
    </FormModal>
  )
}

function InvitationRow({ invitation, onChanged }: { invitation: StoreEmployeeInvitation; onChanged: () => Promise<void> }) {
  const t = useT()
  const { date } = useLocaleFormat()
  const [busy, setBusy] = useState<'resend' | 'revoke' | null>(null)
  const [error, setError] = useState('')
  const [resent, setResent] = useState(false)

  async function handleResend() {
    setBusy('resend')
    setError('')
    try {
      await storesApi.resendStoreEmployeeInvitation(invitation.invitationId)
      setResent(true)
      setTimeout(() => setResent(false), 2500)
      await onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('partner.staff.resendError'))
    } finally {
      setBusy(null)
    }
  }

  async function handleRevoke() {
    if (!window.confirm(t('partner.staff.revokeConfirm', { email: invitation.email }))) return
    setBusy('revoke')
    setError('')
    try {
      await storesApi.revokeStoreEmployeeInvitation(invitation.invitationId)
      await onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('partner.staff.revokeError'))
      setBusy(null)
    }
  }

  const statusLabel =
    invitation.status === 'Pending' ? t('partner.staff.statusPending')
    : invitation.status === 'Expired' ? t('partner.staff.expired')
    : invitation.status === 'Revoked' ? t('partner.staff.revoked')
    : t('partner.staff.pending')

  return (
    <div className="flex flex-col gap-2.5 rounded-[14px] bg-[color:var(--admin-hover)] p-3.5 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <span className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{invitation.email}</span>
          <Badge variant={invitation.status === 'Expired' ? 'neutral' : invitation.status === 'Revoked' ? 'danger' : 'warning'} size="sm" scheme="admin">
            {statusLabel}
          </Badge>
        </div>
        <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
          {invitation.role === 'Owner' ? t('partner.staff.roleOwner') : t('partner.staff.roleCashier')}
          {invitation.status === 'Pending' && ` · ${t('partner.staff.expiresOn', { date: date(invitation.expiresAt) })}`}
        </div>
        {error && <div className="mt-1 text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
      </div>
      {invitation.status === 'Pending' && (
        <div className="flex shrink-0 items-center gap-1.5">
          <button
            onClick={handleResend}
            disabled={busy !== null}
            className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-card)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] disabled:opacity-50"
          >
            <RefreshIcon width={13} height={13} />
            {resent ? t('partner.staff.resent') : t('partner.staff.resend')}
          </button>
          <button
            onClick={handleRevoke}
            disabled={busy !== null}
            className="rounded-lg bg-[color:var(--admin-danger-dim)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-danger)] hover:opacity-80 disabled:opacity-50"
          >
            {t('partner.staff.revoke')}
          </button>
        </div>
      )}
    </div>
  )
}

function EmployeesSection() {
  const { storeId, user } = useAuth()
  const t = useT()
  const { date } = useLocaleFormat()
  const [employees, setEmployees] = useState<StoreEmployee[] | null>(null)
  const [invitations, setInvitations] = useState<StoreEmployeeInvitation[]>([])
  const [error, setError] = useState('')
  const [removingId, setRemovingId] = useState<number | null>(null)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [toastMessage, setToastMessage] = useState('')

  const load = useCallback(async () => {
    if (!storeId) return
    setError('')
    try {
      const res = await storesApi.getStoreEmployees(storeId)
      if (res.outcome === 'Found') {
        setEmployees(res.employees ?? [])
      } else {
        setError(res.outcome === 'Forbidden' ? t('partner.staff.noStoreAccess') : t('partner.staff.storeNotFound'))
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('partner.staff.loadEmployeesError'))
    }
    try {
      const invRes = await storesApi.getStoreEmployeeInvitations(storeId, 'Pending')
      setInvitations(invRes.invitations ?? [])
    } catch {
      // Non-fatal -- the employee list above is the primary content; the pending-invites strip
      // just won't show if this fails.
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [storeId, t])

  useEffect(() => {
    load()
  }, [load])

  async function handleInviteSuccess() {
    await load()
    setInviteOpen(false)
    setToastMessage(t('partner.staff.inviteSent'))
    setTimeout(() => setToastMessage(''), 3200)
  }

  async function handleRemove(storeEmployeeId: number) {
    setRemovingId(storeEmployeeId)
    setError('')
    try {
      const res = await storesApi.removeStoreEmployee(storeEmployeeId)
      if (res.outcome === 'Removed') {
        await load()
      } else if (res.outcome === 'Forbidden') {
        setError(t('partner.staff.noAccessRemove'))
      } else {
        setError(t('partner.staff.employeeNotFound'))
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('partner.staff.removeError'))
    } finally {
      setRemovingId(null)
    }
  }

  return (
    <Card className="p-5">
      <div className="mb-4 flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <ShieldIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">{t('partner.staff.employeesTitle')}</span>
        </div>
        <button
          onClick={() => setInviteOpen(true)}
          className="flex items-center gap-1.5 rounded-xl bg-[color:var(--admin-accent)] px-3.5 py-2 text-[12.5px] font-semibold text-white hover:opacity-90"
        >
          <PlusIcon width={14} height={14} />
          {t('partner.staff.inviteButton')}
        </button>
      </div>

      {error && <div className="mb-3 text-[12px] font-medium text-[color:var(--admin-danger)]">{error}</div>}

      <div className="flex flex-col gap-2.5">
        {employees === null && !error && (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">{t('common.loading')}</div>
        )}
        {invitations.map((inv) => (
          <InvitationRow key={inv.invitationId} invitation={inv} onChanged={load} />
        ))}
        {employees?.map((emp) => (
          <div
            key={emp.storeEmployeeId}
            className="flex items-center justify-between gap-3 rounded-[14px] bg-[color:var(--admin-hover)] p-3.5"
          >
            <div className="min-w-0">
              <div className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">
                {shortId(emp.userId, user?.userId, t)}
              </div>
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                {emp.role === 'Owner' ? t('partner.staff.roleOwner') : t('partner.staff.roleCashier')} · {t('partner.staff.employedSince', { date: date(emp.addedAt) })}
              </div>
            </div>
            <button
              onClick={() => handleRemove(emp.storeEmployeeId)}
              disabled={removingId === emp.storeEmployeeId}
              aria-label={t('partner.staff.removeEmployee')}
              className="grid h-8 w-8 shrink-0 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-danger-dim)] hover:text-[color:var(--admin-danger)] disabled:opacity-50"
            >
              <TrashIcon width={14} height={14} />
            </button>
          </div>
        ))}
        {employees?.length === 0 && invitations.length === 0 && (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.noEmployees')}</div>
        )}
      </div>

      <InviteEmployeeModal open={inviteOpen} onClose={() => setInviteOpen(false)} storeId={storeId} onSuccess={handleInviteSuccess} />
      <Toast open={!!toastMessage} variant="success" scheme="admin">
        {toastMessage}
      </Toast>
    </Card>
  )
}

export function StaffPage() {
  const { storeId, user } = useAuth()
  const t = useT()
  const { dateTime, time, money } = useLocaleFormat()
  const [shifts, setShifts] = useState<CashierShift[] | null>(null)
  const [anomalies, setAnomalies] = useState<CashierAnomaly[]>([])
  const [anomaliesForbidden, setAnomaliesForbidden] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const ROLE_ACCESS = [
    { role: t('partner.staff.roleUser'), access: [t('partner.staff.roleUserAccess1'), t('partner.staff.roleUserAccess2'), t('partner.staff.roleUserAccess3')] },
    {
      role: t('partner.staff.roleStorePartner'),
      access: [t('partner.staff.roleStorePartnerAccess1'), t('partner.staff.roleStorePartnerAccess2'), t('partner.staff.roleStorePartnerAccess3')],
    },
    { role: t('partner.staff.roleAdmin'), access: [t('partner.staff.roleAdminAccess1'), t('partner.staff.roleAdminAccess2')] },
  ]

  useEffect(() => {
    if (!storeId) { setLoading(false); return }
    let cancelled = false
    async function load() {
      // Independent try/catch per endpoint — a 403 on cashier anomalies (owner-only)
      // must not blank out the shifts/KPI sections a non-owner employee can still see.
      try {
        const shiftsRes = await salesApi.getCashierShifts(storeId!)
        if (cancelled) return
        setShifts(shiftsRes.shifts ?? [])
      } catch (err) {
        if (cancelled) return
        setError(err instanceof ApiError ? err.message : t('partner.staff.loadEmployeesError'))
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [storeId])

  if (loading) {
    return <Loading label={t('common.loading')} />
  }

  if (error || !shifts) {
    return (
      <Card>
        <ErrorState message={error || t('common.notFound')} />
      </Card>
    )
  }

  const openShifts = shifts.filter((s) => !s.endedAt)
  const sortedShifts = [...shifts].sort((a, b) => b.startedAt.localeCompare(a.startedAt))

  return (
    <div className="mx-auto flex max-w-[1400px] flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">{t('partner.staff.statsShiftsTotal')}</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-text)]">{shifts.length}</div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">{t('partner.staff.statsShiftsOpen')}</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-success)]">{openShifts.length}</div>
        </Card>
        <Card className="p-5">
          <div className="text-[13px] text-[color:var(--admin-text-secondary)]">{t('partner.staff.statsActiveCashiers')}</div>
          <div className="mt-2 text-[26px] font-extrabold text-[color:var(--admin-text)]">{anomalies.length}</div>
        </Card>
      </div>

      <EmployeesSection />

      <Card className="p-5">
        <div className="mb-4 flex items-center gap-2">
          <ClockIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">{t('partner.staff.shiftsTitle')}</span>
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
                  {shortId(s.cashierUserId, user?.userId, t).charAt(0).toUpperCase()}
                </span>
                <div>
                  <div className="text-[13.5px] font-semibold text-[color:var(--admin-text)]">
                    {shortId(s.cashierUserId, user?.userId, t)}
                  </div>
                  <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                    {dateTime(s.startedAt, { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                    {s.endedAt ? ` — ${time(s.endedAt)}` : ''}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-4">
                <div className="text-right text-[12px] text-[color:var(--admin-text-secondary)]">
                  <div>{t('partner.staff.shiftOpening')}: {money(s.openingCash)} {s.currency}</div>
                  {s.closingCash !== undefined && <div>{t('partner.staff.shiftClosing')}: {money(s.closingCash)} {s.currency}</div>}
                </div>
                <span
                  className={`shrink-0 rounded-full px-3 py-1.5 text-[11px] font-semibold ${
                    s.endedAt ? 'bg-[color:var(--admin-border)] text-[color:var(--admin-text-tertiary)]' : 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]'
                  }`}
                >
                  {s.endedAt ? t('partner.staff.shiftClosed') : t('partner.staff.shiftOpen')}
                </span>
              </div>
            </div>
          ))}
          {sortedShifts.length === 0 && (
            <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.noShiftsYet')}</div>
          )}
        </div>
      </Card>

      <Card className="p-5">
        <div className="mb-1 flex items-center gap-2">
          <AlertIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">{t('partner.staff.anomalyTitle')}</span>
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.anomalyHint')}</p>
        {anomaliesForbidden ? (
          <div className="py-10 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.anomalyOwnerOnly')}</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[520px] border-collapse text-left text-[13px]">
              <thead>
                <tr className="text-[11px] uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                  <th className="pb-3 font-semibold">{t('partner.staff.colCashier')}</th>
                  <th className="pb-3 font-semibold">{t('partner.staff.colSales')}</th>
                  <th className="pb-3 font-semibold">{t('partner.staff.colVoided')}</th>
                  <th className="pb-3 font-semibold">{t('partner.staff.colVoidRate')}</th>
                  <th className="pb-3 font-semibold" />
                </tr>
              </thead>
              <tbody>
                {anomalies.map((a) => (
                  <tr key={a.cashierUserId} className="border-t border-[color:var(--admin-border)]">
                    <td className="py-3 pr-3 font-semibold text-[color:var(--admin-text)]">{shortId(a.cashierUserId, user?.userId, t)}</td>
                    <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{a.totalSales}</td>
                    <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{a.voidedSales}</td>
                    <td className="py-3 pr-3 text-[color:var(--admin-text-secondary)]">{(a.voidRate * 100).toFixed(1)}%</td>
                    <td className="py-3">
                      {a.isAnomalous && (
                        <span className="rounded-full bg-[color:var(--admin-danger-dim)] px-2.5 py-1 text-[11px] font-semibold text-[color:var(--admin-danger)]">
                          {t('partner.staff.anomalyBadge')}
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
                {anomalies.length === 0 && (
                  <tr>
                    <td colSpan={5} className="py-10 text-center text-[color:var(--admin-text-tertiary)]">
                      {t('partner.staff.noSales30d')}
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
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">{t('partner.staff.roleAccessTitle')}</span>
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.roleAccessHint')}</p>
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
