import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Select } from '../components/Select'
import { Badge } from '../components/Badge'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { FormModal, FormField } from '../components/FormModal'
import { Toast } from '../components/Toast'
import { ClockIcon, ShieldIcon, AlertIcon, PlusIcon, RefreshIcon, KeyIcon, CheckIcon, EditIcon, TrashIcon } from '../components/icons'
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
  type StoreEmployeeInvitation,
  type MyStore,
} from '../../lib/api'
import { daysAgo, today } from '../lib/dates'

function shortId(id: string, myId: string | undefined, t: (key: 'partner.staff.you') => string) {
  if (id === myId) return t('partner.staff.you')
  return id.slice(0, 8) + '…'
}

// "Смена" is a picklist on the form, not a free time-range entry — the backend still just stores a
// concrete ScheduleStart/ScheduleEnd (TimeOnly), these presets are the frontend's own vocabulary for
// picking a sensible pair. "HH:mm:00" matches System.Text.Json's default TimeOnly serialization.
const SHIFT_PRESETS: { value: string; label: string; start: string; end: string }[] = [
  { value: 'morning', start: '08:00:00', end: '14:00:00', label: '' },
  { value: 'day', start: '14:00:00', end: '20:00:00', label: '' },
  { value: 'full', start: '09:00:00', end: '21:00:00', label: '' },
  { value: 'evening', start: '18:00:00', end: '00:00:00', label: '' },
]

// Dynamically-built key ("partner.staff.shiftPreset.morning" etc.) can't be checked against useT's
// literal-union key type at this call site, so this accepts the wider signature deliberately --
// useT()'s stricter function is still assignable in (every real key these two helpers build is
// one that actually exists in both dictionaries, see translations.ts).
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function shiftPresets(t: (key: any) => string) {
  return SHIFT_PRESETS.map((p) => ({ ...p, label: t(`partner.staff.shiftPreset.${p.value}`) }))
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function shiftLabel(t: (key: any) => string, start?: string | null, end?: string | null) {
  if (!start || !end) return null
  const preset = SHIFT_PRESETS.find((p) => p.start === start && p.end === end)
  if (preset) return t(`partner.staff.shiftPreset.${preset.value}`)
  return `${start.slice(0, 5)}–${end.slice(0, 5)}`
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
  const [emailError, setEmailError] = useState('')
  const [ownedStores, setOwnedStores] = useState<MyStore[]>([])
  const [targetStoreId, setTargetStoreId] = useState<number | null>(storeId)

  useEffect(() => {
    if (!open) return
    setEmail('')
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
    // Owner is the only role this mechanism still creates — a cashier is created directly with a
    // password instead (CreateCashierModal below), never invited by email.
    const res = await storesApi.createStoreEmployeeInvitation(targetStoreId, trimmed, 'Owner')
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
          placeholder="owner2@sarfkor.tj"
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
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

const phonePlaceholder = '+992 __ ___ __ __'

// Deliberately separate from InviteEmployeeModal above: a cashier hired in person doesn't need the
// emailed-link round-trip (they may have no email-checking habit at all, or the shop owner is
// setting them up while they stand right there), so this creates a real, working account
// immediately and hands the owner a generated password to relay to them on the spot. Only ever a
// brand-new account -- an already-registered email is rejected rather than silently touching that
// account's password.
function CreateCashierModal({
  open,
  onClose,
  storeId,
  onCreated,
}: {
  open: boolean
  onClose: () => void
  storeId: number | null
  onCreated: (result: { email: string; password: string }) => void
}) {
  const t = useT()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [shift, setShift] = useState('full')
  const [firstNameError, setFirstNameError] = useState('')
  const [lastNameError, setLastNameError] = useState('')
  const [emailError, setEmailError] = useState('')
  const [phoneError, setPhoneError] = useState('')
  const [ownedStores, setOwnedStores] = useState<MyStore[]>([])
  const [targetStoreId, setTargetStoreId] = useState<number | null>(storeId)

  useEffect(() => {
    if (!open) return
    setFirstName('')
    setLastName('')
    setEmail('')
    setPhone('')
    setShift('full')
    setFirstNameError('')
    setLastNameError('')
    setEmailError('')
    setPhoneError('')
    setTargetStoreId(storeId)
    meApi.getMyStores().then((res) => setOwnedStores(res.stores.filter((s) => s.role === 'Owner'))).catch(() => setOwnedStores([]))
  }, [open, storeId])

  async function submit() {
    const trimmedFirst = firstName.trim()
    const trimmedLast = lastName.trim()
    const trimmedEmail = email.trim()
    const trimmedPhone = phone.trim()
    let hasError = false
    if (!trimmedFirst) {
      setFirstNameError(t('partner.staff.nameRequired'))
      hasError = true
    }
    if (!trimmedLast) {
      setLastNameError(t('partner.staff.lastNameRequired'))
      hasError = true
    }
    if (!trimmedEmail) {
      setEmailError(t('partner.staff.emailRequired'))
      hasError = true
    }
    if (!trimmedPhone) {
      setPhoneError(t('partner.staff.phoneRequired'))
      hasError = true
    }
    if (hasError) throw new Error(t('partner.staff.fixFieldsError'))
    if (!targetStoreId) throw new Error(t('partner.staff.storeNotSelected'))

    const preset = shiftPresets(t).find((p) => p.value === shift)
    const res = await storesApi.createCashierAccount(targetStoreId, {
      firstName: trimmedFirst,
      lastName: trimmedLast,
      email: trimmedEmail,
      phoneNumber: trimmedPhone,
      scheduleStart: preset?.start,
      scheduleEnd: preset?.end,
    })
    if (res.outcome === 'Created') {
      onCreated({ email: res.email!, password: res.password! })
    } else if (res.outcome === 'EmailAlreadyRegistered') {
      setEmailError(t('partner.staff.emailAlreadyRegistered'))
      throw new Error(t('partner.staff.emailAlreadyRegistered'))
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
      title={t('partner.staff.createCashierModalTitle')}
      isDirty={!!firstName || !!lastName || !!email || !!phone}
      onSubmit={submit}
      submitLabel={t('partner.staff.createCashierSubmit')}
      submitBusyLabel={t('partner.staff.creating')}
      scheme="admin"
    >
      <div className="grid grid-cols-2 gap-3">
        <FormField label={t('partner.staff.firstNameLabel')} required error={firstNameError} scheme="admin">
          <input
            value={firstName}
            onChange={(e) => {
              setFirstName(e.target.value)
              setFirstNameError('')
            }}
            placeholder={t('partner.staff.firstNamePlaceholder')}
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </FormField>
        <FormField label={t('partner.staff.lastNameLabel')} required error={lastNameError} scheme="admin">
          <input
            value={lastName}
            onChange={(e) => {
              setLastName(e.target.value)
              setLastNameError('')
            }}
            placeholder={t('partner.staff.lastNamePlaceholder')}
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </FormField>
      </div>
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
      <FormField label={t('partner.staff.phoneLabel')} required error={phoneError} scheme="admin">
        <input
          type="tel"
          value={phone}
          onChange={(e) => {
            setPhone(e.target.value)
            setPhoneError('')
          }}
          placeholder={phonePlaceholder}
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label={t('partner.staff.shiftLabel')} scheme="admin">
        <Select value={shift} onChange={setShift} options={shiftPresets(t).map((p) => ({ value: p.value, label: p.label }))} />
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
      <p className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.createCashierHint')}</p>
    </FormModal>
  )
}

/** Shows a freshly generated password exactly once — nothing on the client or server keeps a copy of
 *  it after this closes, so the copy button here is the owner's only chance to grab it. Shared by
 *  both CreateCashierModal's result and the "Сбросить пароль" action. */
function CashierCredentialsModal({
  result,
  onClose,
}: {
  result: { email: string; password: string; title: string; hint: string } | null
  onClose: () => void
}) {
  const t = useT()
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    if (result) setCopied(false)
  }, [result])

  async function copy() {
    if (!result) return
    await navigator.clipboard.writeText(result.password)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <FormModal
      open={!!result}
      onClose={onClose}
      title={result?.title ?? ''}
      onSubmit={async () => {}}
      submitLabel={t('partner.staff.done')}
      cancelLabel={t('partner.staff.done')}
      scheme="admin"
    >
      {result && (
        <div className="flex flex-col gap-4">
          <p className="text-[12.5px] leading-relaxed text-[color:var(--admin-text-secondary)]">{result.hint}</p>
          <FormField label={t('partner.staff.emailLabel')} scheme="admin">
            <div className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)]">
              {result.email}
            </div>
          </FormField>
          <FormField label={t('partner.staff.passwordLabel')} scheme="admin">
            <div className="flex items-center gap-2">
              <div className="flex-1 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 font-mono text-[14px] font-semibold tracking-wide text-[color:var(--admin-text)]">
                {result.password}
              </div>
              <button
                type="button"
                onClick={copy}
                className="flex shrink-0 items-center gap-1.5 rounded-xl bg-[color:var(--admin-accent)] px-3.5 py-2.5 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90"
              >
                {copied ? <CheckIcon width={14} height={14} /> : null}
                {copied ? t('partner.staff.copied') : t('partner.staff.copyPassword')}
              </button>
            </div>
          </FormField>
        </div>
      )}
    </FormModal>
  )
}

/** "Изменить" — name/phone/shift only; email is the login and store isn't reassignable from here. */
function EditCashierModal({
  employee,
  onClose,
  onSaved,
}: {
  employee: StoreEmployee | null
  onClose: () => void
  onSaved: () => Promise<void> | void
}) {
  const t = useT()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [shift, setShift] = useState('full')
  const [firstNameError, setFirstNameError] = useState('')
  const [lastNameError, setLastNameError] = useState('')
  const [phoneError, setPhoneError] = useState('')

  useEffect(() => {
    if (!employee) return
    setFirstName(employee.firstName ?? '')
    setLastName(employee.lastName ?? '')
    setPhone(employee.phoneNumber ?? '')
    const preset = SHIFT_PRESETS.find((p) => p.start === employee.scheduleStart && p.end === employee.scheduleEnd)
    setShift(preset?.value ?? 'full')
    setFirstNameError('')
    setLastNameError('')
    setPhoneError('')
  }, [employee])

  async function submit() {
    if (!employee) return
    const trimmedFirst = firstName.trim()
    const trimmedLast = lastName.trim()
    const trimmedPhone = phone.trim()
    let hasError = false
    if (!trimmedFirst) {
      setFirstNameError(t('partner.staff.nameRequired'))
      hasError = true
    }
    if (!trimmedLast) {
      setLastNameError(t('partner.staff.lastNameRequired'))
      hasError = true
    }
    if (!trimmedPhone) {
      setPhoneError(t('partner.staff.phoneRequired'))
      hasError = true
    }
    if (hasError) throw new Error(t('partner.staff.fixFieldsError'))

    const preset = shiftPresets(t).find((p) => p.value === shift)
    const res = await storesApi.updateStoreEmployee(employee.storeEmployeeId, {
      firstName: trimmedFirst,
      lastName: trimmedLast,
      phoneNumber: trimmedPhone,
      scheduleStart: preset?.start,
      scheduleEnd: preset?.end,
      // This form has no salary field -- pass the employee's own current value straight through so
      // an edit here can never silently clear it (code review 2026-08-10 finding #1).
      monthlySalaryAmount: employee.monthlySalaryAmount,
      monthlySalaryCurrency: employee.monthlySalaryCurrency,
    })
    if (res.outcome === 'Updated') {
      await onSaved()
    } else if (res.outcome === 'SubscriptionInactive') {
      throw new Error(t('partner.staff.subscriptionInactive'))
    } else if (res.outcome === 'Forbidden') {
      throw new Error(t('partner.staff.forbidden'))
    } else {
      throw new Error(t('partner.staff.employeeNotFound'))
    }
  }

  return (
    <FormModal
      open={!!employee}
      onClose={onClose}
      title={t('partner.staff.editCashierTitle')}
      isDirty
      onSubmit={submit}
      submitLabel={t('common.save')}
      submitBusyLabel={t('common.saving')}
      scheme="admin"
    >
      <div className="grid grid-cols-2 gap-3">
        <FormField label={t('partner.staff.firstNameLabel')} required error={firstNameError} scheme="admin">
          <input
            value={firstName}
            onChange={(e) => {
              setFirstName(e.target.value)
              setFirstNameError('')
            }}
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </FormField>
        <FormField label={t('partner.staff.lastNameLabel')} required error={lastNameError} scheme="admin">
          <input
            value={lastName}
            onChange={(e) => {
              setLastName(e.target.value)
              setLastNameError('')
            }}
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </FormField>
      </div>
      <FormField label={t('partner.staff.phoneLabel')} required error={phoneError} scheme="admin">
        <input
          type="tel"
          value={phone}
          onChange={(e) => {
            setPhone(e.target.value)
            setPhoneError('')
          }}
          placeholder={phonePlaceholder}
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label={t('partner.staff.shiftLabel')} scheme="admin">
        <Select value={shift} onChange={setShift} options={shiftPresets(t).map((p) => ({ value: p.value, label: p.label }))} />
      </FormField>
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

function EmployeeRow({
  employee,
  isSelf,
  onChanged,
  onEdit,
  onPasswordReset,
}: {
  employee: StoreEmployee
  isSelf: boolean
  onChanged: () => Promise<void>
  onEdit: () => void
  onPasswordReset: (result: { email: string; password: string; title: string; hint: string }) => void
}) {
  const t = useT()
  const { date } = useLocaleFormat()
  const [busy, setBusy] = useState<'reset' | 'toggle' | 'remove' | null>(null)
  const [error, setError] = useState('')

  // A directly-created Cashier has both firstName and lastName. A co-owner attached via the
  // email-invite flow only ever has firstName populated, as a fallback to their UserProfile.
  // DisplayName (see GetStoreEmployeesQueryHandler) -- lastName stays null there, so this can't
  // require both to be present or a co-owner's row falls straight back to a raw id fragment,
  // exactly the thing this list exists to avoid (code review 2026-08-10 finding #5).
  const displayName = [employee.firstName, employee.lastName].filter(Boolean).join(' ') || shortId(employee.userId, isSelf ? employee.userId : undefined, t)
  const shift = shiftLabel(t, employee.scheduleStart, employee.scheduleEnd)

  async function handleResetPassword() {
    if (!window.confirm(t('partner.staff.resetPasswordConfirm', { name: displayName }))) return
    setBusy('reset')
    setError('')
    try {
      const res = await storesApi.resetCashierPassword(employee.storeEmployeeId)
      if (res.outcome === 'Reset' && res.password) {
        onPasswordReset({
          email: employee.email ?? '',
          password: res.password,
          title: t('partner.staff.passwordResetTitle'),
          hint: t('partner.staff.passwordResetHint'),
        })
      } else if (res.outcome === 'Forbidden') {
        setError(t('partner.staff.forbidden'))
      } else {
        setError(t('partner.staff.employeeNotFound'))
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('partner.staff.resetPasswordError'))
    } finally {
      setBusy(null)
    }
  }

  async function handleToggleActive() {
    const nextActive = !employee.isActive
    if (!nextActive && !window.confirm(t('partner.staff.disableConfirm', { name: displayName }))) return
    setBusy('toggle')
    setError('')
    try {
      const res = await storesApi.setStoreEmployeeActive(employee.storeEmployeeId, nextActive)
      if (res.outcome === 'Updated') {
        await onChanged()
      } else if (res.outcome === 'Forbidden') {
        setError(t('partner.staff.forbidden'))
      } else {
        setError(t('partner.staff.employeeNotFound'))
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('partner.staff.toggleActiveError'))
    } finally {
      setBusy(null)
    }
  }

  // Code review 2026-08-10 finding #4: the redesign dropped the old per-row "remove" action
  // entirely and never replaced it for co-owners specifically (a Cashier now has "Отключить" as
  // its intended lifecycle-pause action instead, per the original task spec -- that part was
  // deliberate and stays as-is). Owners were left with zero management actions at all, including
  // no way to undo a mistaken co-owner invite. Reusing the pre-existing removeStoreEmployee
  // endpoint (already gated by IsOperationalAsync, see StoresController).
  async function handleRemoveOwner() {
    if (!window.confirm(t('partner.staff.removeOwnerConfirm', { name: displayName }))) return
    setBusy('remove')
    setError('')
    try {
      const res = await storesApi.removeStoreEmployee(employee.storeEmployeeId)
      if (res.outcome === 'Removed') {
        await onChanged()
      } else if (res.outcome === 'Forbidden') {
        setError(t('partner.staff.noAccessRemove'))
      } else {
        setError(t('partner.staff.employeeNotFound'))
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('partner.staff.removeError'))
    } finally {
      setBusy(null)
    }
  }

  return (
    <div className="flex flex-col gap-2.5 rounded-[14px] bg-[color:var(--admin-hover)] p-3.5 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <span className="truncate text-[13px] font-semibold text-[color:var(--admin-text)]">{displayName}</span>
          <Badge scheme="admin" variant={employee.isActive ? 'success' : 'neutral'} size="sm">
            {employee.isActive ? t('partner.staff.statusActive') : t('partner.staff.statusDisabled')}
          </Badge>
        </div>
        <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
          <span>{employee.role === 'Owner' ? t('partner.staff.roleOwner') : t('partner.staff.roleCashier')}</span>
          {employee.email && <span>· {employee.email}</span>}
          {employee.phoneNumber && <span>· {employee.phoneNumber}</span>}
          {shift && <span>· {shift}</span>}
          <span>· {t('partner.staff.employedSince', { date: date(employee.addedAt) })}</span>
        </div>
        {error && <div className="mt-1 text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
      </div>
      {!isSelf && employee.role === 'Cashier' && (
        <div className="flex shrink-0 flex-wrap items-center gap-1.5">
          <button
            onClick={onEdit}
            disabled={busy !== null}
            aria-label={t('partner.staff.editEmployee')}
            className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-card)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] disabled:opacity-50"
          >
            <EditIcon width={13} height={13} />
            {t('partner.staff.editEmployee')}
          </button>
          <button
            onClick={handleResetPassword}
            disabled={busy !== null}
            className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-card)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-text-secondary)] hover:text-[color:var(--admin-text)] disabled:opacity-50"
          >
            <KeyIcon width={13} height={13} />
            {t('partner.staff.resetPassword')}
          </button>
          <button
            onClick={handleToggleActive}
            disabled={busy !== null}
            className={`rounded-lg px-3 py-1.5 text-[11.5px] font-semibold disabled:opacity-50 ${
              employee.isActive
                ? 'bg-[color:var(--admin-danger-dim)] text-[color:var(--admin-danger)] hover:opacity-80'
                : 'bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)] hover:opacity-80'
            }`}
          >
            {employee.isActive ? t('partner.staff.disable') : t('partner.staff.enable')}
          </button>
        </div>
      )}
      {!isSelf && employee.role === 'Owner' && (
        <div className="flex shrink-0 flex-wrap items-center gap-1.5">
          <button
            onClick={handleRemoveOwner}
            disabled={busy !== null}
            aria-label={t('partner.staff.removeEmployee')}
            className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-danger-dim)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-danger)] hover:opacity-80 disabled:opacity-50"
          >
            <TrashIcon width={13} height={13} />
            {t('partner.staff.removeEmployee')}
          </button>
        </div>
      )}
    </div>
  )
}

function EmployeesSection() {
  const { storeId, user } = useAuth()
  const t = useT()
  const [employees, setEmployees] = useState<StoreEmployee[] | null>(null)
  const [invitations, setInvitations] = useState<StoreEmployeeInvitation[]>([])
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [inviteOpen, setInviteOpen] = useState(false)
  const [createCashierOpen, setCreateCashierOpen] = useState(false)
  const [editingEmployee, setEditingEmployee] = useState<StoreEmployee | null>(null)
  const [credentialsResult, setCredentialsResult] = useState<{ email: string; password: string; title: string; hint: string } | null>(null)
  const [toastMessage, setToastMessage] = useState('')

  const load = useCallback(async () => {
    if (!storeId) return
    setError('')
    try {
      const res = await storesApi.getStoreEmployees(storeId)
      if (res.outcome === 'Found') {
        setEmployees(res.employees ?? [])
      } else {
        setErrorKind(res.outcome === 'Forbidden' ? 'forbidden' : 'notFound')
        setError(res.outcome === 'Forbidden' ? t('partner.staff.noStoreAccess') : t('partner.staff.storeNotFound'))
      }
    } catch (err) {
      // Never show err.message here: for a genuine 500 that's the raw backend exception text,
      // not something written for a user to read. The console gets the real thing; the screen
      // gets a safe, translated, kind-specific line instead (see ErrorState).
      // eslint-disable-next-line no-console
      console.error('Failed to load store employees:', err)
      setErrorKind(classifyError(err))
      setError(t('partner.staff.loadEmployeesError'))
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

  async function handleCashierCreated(result: { email: string; password: string }) {
    await load()
    setCreateCashierOpen(false)
    setCredentialsResult({ ...result, title: t('partner.staff.cashierCreatedTitle'), hint: t('partner.staff.cashierCreatedHint') })
  }

  async function handleEmployeeSaved() {
    await load()
    setEditingEmployee(null)
    setToastMessage(t('partner.staff.employeeSaved'))
    setTimeout(() => setToastMessage(''), 3200)
  }

  return (
    <Card className="p-5">
      <div className="mb-4 flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <ShieldIcon width={17} height={17} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">{t('partner.staff.employeesTitle')}</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setCreateCashierOpen(true)}
            className="flex items-center gap-1.5 rounded-xl bg-[color:var(--admin-accent)] px-3.5 py-2 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] hover:opacity-90"
          >
            <PlusIcon width={14} height={14} />
            {t('partner.staff.createCashierButton')}
          </button>
          <button
            onClick={() => setInviteOpen(true)}
            className="flex items-center gap-1.5 rounded-xl border border-[color:var(--admin-border)] px-3.5 py-2 text-[12.5px] font-semibold text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]"
          >
            {t('partner.staff.inviteButton')}
          </button>
        </div>
      </div>

      {error && <ErrorState message={error} kind={errorKind} onRetry={load} />}

      <div className="flex flex-col gap-2.5">
        {employees === null && !error && (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">{t('common.loading')}</div>
        )}
        {!error && invitations.map((inv) => (
          <InvitationRow key={inv.invitationId} invitation={inv} onChanged={load} />
        ))}
        {employees?.map((emp) => (
          <EmployeeRow
            key={emp.storeEmployeeId}
            employee={emp}
            isSelf={emp.userId === user?.userId}
            onChanged={load}
            onEdit={() => setEditingEmployee(emp)}
            onPasswordReset={setCredentialsResult}
          />
        ))}
        {employees?.length === 0 && invitations.length === 0 && (
          <div className="py-6 text-center text-[13px] text-[color:var(--admin-text-tertiary)]">{t('partner.staff.noEmployees')}</div>
        )}
      </div>

      <InviteEmployeeModal open={inviteOpen} onClose={() => setInviteOpen(false)} storeId={storeId} onSuccess={handleInviteSuccess} />
      <CreateCashierModal open={createCashierOpen} onClose={() => setCreateCashierOpen(false)} storeId={storeId} onCreated={handleCashierCreated} />
      <EditCashierModal employee={editingEmployee} onClose={() => setEditingEmployee(null)} onSaved={handleEmployeeSaved} />
      <CashierCredentialsModal result={credentialsResult} onClose={() => setCredentialsResult(null)} />
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
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')

  const ROLE_ACCESS = [
    { role: t('partner.staff.roleUser'), access: [t('partner.staff.roleUserAccess1'), t('partner.staff.roleUserAccess2'), t('partner.staff.roleUserAccess3')] },
    {
      role: t('partner.staff.roleStorePartner'),
      access: [t('partner.staff.roleStorePartnerAccess1'), t('partner.staff.roleStorePartnerAccess2'), t('partner.staff.roleStorePartnerAccess3')],
    },
    { role: t('partner.staff.roleAdmin'), access: [t('partner.staff.roleAdminAccess1'), t('partner.staff.roleAdminAccess2')] },
  ]

  const load = useCallback(
    async (signal?: { cancelled: boolean }) => {
      if (!storeId) return
      setLoading(true)
      setError('')
      // Independent try/catch per endpoint — a 403 on cashier anomalies (owner-only)
      // must not blank out the shifts/KPI sections a non-owner employee can still see.
      try {
        const shiftsRes = await salesApi.getCashierShifts(storeId)
        if (signal?.cancelled) return
        setShifts(shiftsRes.shifts ?? [])
      } catch (err) {
        if (signal?.cancelled) return
        // Same rule as the employees load below: never surface err.message here, it's the raw
        // backend exception text for anything past a curated 4xx, not user-facing copy.
        // eslint-disable-next-line no-console
        console.error('Failed to load cashier shifts:', err)
        setErrorKind(classifyError(err))
        setError(t('partner.staff.loadShiftsError'))
      }

      try {
        const anomaliesRes = await storesApi.getCashierAnomalies(storeId, daysAgo(29), today())
        if (signal?.cancelled) return
        setAnomalies(anomaliesRes.cashiers ?? [])
      } catch (err) {
        if (signal?.cancelled) return
        if (err instanceof ApiError && err.status === 403) setAnomaliesForbidden(true)
        else console.error('Failed to load cashier anomaly report:', err) // eslint-disable-line no-console
      }

      if (!signal?.cancelled) setLoading(false)
    },
    [storeId, t],
  )

  useEffect(() => {
    const signal = { cancelled: false }
    load(signal)
    return () => {
      signal.cancelled = true
    }
  }, [load])

  if (loading) {
    return <Loading label={t('common.loading')} />
  }

  if (error || !shifts) {
    return (
      <Card>
        <ErrorState message={error || t('common.notFound')} kind={errorKind} onRetry={() => load()} />
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
                  className="grid h-10 w-10 shrink-0 place-items-center rounded-xl text-[13px] font-bold text-[color:var(--admin-accent-fg)]"
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
                  {/* Backend sends null (not a missing key) for a shift that's still open --
                      != null intentionally catches both null and undefined; a strict !==
                      undefined check let a real null slip through into money(), which crashed
                      on null.toLocaleString() for every store with an open shift. */}
                  {s.closingCash != null && <div>{t('partner.staff.shiftClosing')}: {money(s.closingCash)} {s.currency}</div>}
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
