import { useCallback, useEffect, useState } from 'react'
import { Panel, SectionHeader } from '../cabinet/components/primitives'
import { PlusIcon, EditIcon, TrashIcon, CheckIcon, XIcon } from '../components/icons'
import { suppliersApi, type Supplier } from '../../lib/api'
import { describeError, classifyError } from '../../lib/errorKind'
import { useT } from '../../i18n/translations'
import { useSubscriptionGate } from '../../lib/subscriptionGate'

export function SuppliersSection({ storeId }: { storeId: number | null }) {
  const t = useT()
  const gate = useSubscriptionGate()
  const blockedTitle = gate.isOwner ? t('common.errorSubscriptionInactiveOwner') : t('common.errorSubscriptionInactiveCashier')
  const [suppliers, setSuppliers] = useState<Supplier[] | null>(null)
  const [error, setError] = useState('')

  const [newName, setNewName] = useState('')
  const [newPhone, setNewPhone] = useState('')
  const [newEmail, setNewEmail] = useState('')

  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingName, setEditingName] = useState('')
  const [editingPhone, setEditingPhone] = useState('')
  const [editingEmail, setEditingEmail] = useState('')

  const [busyId, setBusyId] = useState<number | 'new' | null>(null)

  const load = useCallback(async () => {
    if (!storeId) return
    try {
      const res = await suppliersApi.getSuppliers(storeId)
      setSuppliers(res.suppliers)
    } catch (err) {
      setError(describeError(err, t, { isOwner: gate.isOwner }))
    }
  }, [storeId, t, gate.isOwner])

  useEffect(() => {
    load()
  }, [load])

  async function handleCreate() {
    if (!newName.trim() || !storeId) return
    setBusyId('new')
    setError('')
    try {
      await suppliersApi.createSupplier(storeId, newName.trim(), newPhone.trim() || undefined, newEmail.trim() || undefined)
      setNewName('')
      setNewPhone('')
      setNewEmail('')
      await load()
    } catch (err) {
      setError(describeError(err, t, { isOwner: gate.isOwner }))
    } finally {
      setBusyId(null)
    }
  }

  async function handleSaveEdit(id: number) {
    if (!editingName.trim()) return
    setBusyId(id)
    setError('')
    try {
      await suppliersApi.updateSupplier(id, editingName.trim(), editingPhone.trim() || undefined, editingEmail.trim() || undefined)
      setEditingId(null)
      await load()
    } catch (err) {
      setError(describeError(err, t, { isOwner: gate.isOwner }))
    } finally {
      setBusyId(null)
    }
  }

  async function handleDelete(id: number) {
    setBusyId(id)
    setError('')
    try {
      await suppliersApi.deleteSupplier(id)
      await load()
    } catch (err) {
      // Conflict specifically gets a more useful hint than the generic dictionary line -- a 409
      // here is always DeleteSupplierOutcome.InUse, so naming the likely cause (still referenced by
      // stock movements/orders/reorder rules) beats "конфликтует с текущим состоянием" for the one
      // case where we actually know more than the generic bucket does.
      setError(
        classifyError(err) === 'conflict'
          ? 'Не удалось удалить поставщика — он ещё используется в поставках или заказах'
          : describeError(err, t, { isOwner: gate.isOwner }),
      )
    } finally {
      setBusyId(null)
    }
  }

  return (
    <Panel>
      <SectionHeader title="Поставщики" />

      {error && <div className="mb-3 rounded-2xl bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">{error}</div>}

      <div className="flex flex-col gap-2">
        {suppliers?.map((s) => (
          <div key={s.supplierId} className="flex flex-wrap items-center gap-2 rounded-2xl bg-[color:var(--admin-hover)] px-4 py-3">
            {editingId === s.supplierId ? (
              <>
                <input
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  autoFocus
                  placeholder="Название"
                  className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <input
                  value={editingPhone}
                  onChange={(e) => setEditingPhone(e.target.value)}
                  placeholder="Телефон"
                  className="w-36 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <input
                  value={editingEmail}
                  onChange={(e) => setEditingEmail(e.target.value)}
                  placeholder="Email"
                  className="w-44 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <button
                  onClick={() => handleSaveEdit(s.supplierId)}
                  disabled={busyId === s.supplierId || (!gate.loading && !gate.isOperational)}
                  title={!gate.loading && !gate.isOperational ? blockedTitle : undefined}
                  className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-success)] hover:bg-[color:var(--admin-success-dim)] disabled:opacity-50"
                >
                  <CheckIcon width={14} height={14} />
                </button>
                <button
                  onClick={() => setEditingId(null)}
                  className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-card)]"
                >
                  <XIcon width={14} height={14} />
                </button>
              </>
            ) : (
              <>
                <div className="min-w-0 flex-1">
                  <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">{s.name}</div>
                  <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                    {[s.contactPhone, s.contactEmail].filter(Boolean).join(' · ') || 'Контакты не указаны'}
                  </div>
                </div>
                <div className="flex shrink-0 gap-1.5">
                  <button
                    onClick={() => {
                      setEditingId(s.supplierId)
                      setEditingName(s.name)
                      setEditingPhone(s.contactPhone ?? '')
                      setEditingEmail(s.contactEmail ?? '')
                    }}
                    disabled={busyId === s.supplierId || (!gate.loading && !gate.isOperational)}
                    title={!gate.loading && !gate.isOperational ? blockedTitle : undefined}
                    aria-label="Изменить"
                    className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-card)] disabled:opacity-50"
                  >
                    <EditIcon width={14} height={14} />
                  </button>
                  <button
                    onClick={() => handleDelete(s.supplierId)}
                    disabled={busyId === s.supplierId || (!gate.loading && !gate.isOperational)}
                    title={!gate.loading && !gate.isOperational ? blockedTitle : undefined}
                    aria-label="Удалить"
                    className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-danger)] hover:bg-[color:var(--admin-danger-dim)] disabled:opacity-50"
                  >
                    <TrashIcon width={14} height={14} />
                  </button>
                </div>
              </>
            )}
          </div>
        ))}
        {suppliers?.length === 0 && (
          <p className="py-4 text-center text-[12.5px] text-[color:var(--admin-text-tertiary)]">Поставщиков пока нет</p>
        )}
      </div>

      <div className="mt-4 flex flex-wrap gap-2 border-t border-[color:var(--admin-border)] pt-4">
        <input
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="Название поставщика"
          className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <input
          value={newPhone}
          onChange={(e) => setNewPhone(e.target.value)}
          placeholder="Телефон (необяз.)"
          className="w-36 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <input
          value={newEmail}
          onChange={(e) => setNewEmail(e.target.value)}
          placeholder="Email (необяз.)"
          className="w-44 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <button
          onClick={handleCreate}
          disabled={busyId === 'new' || !newName.trim() || (!gate.loading && !gate.isOperational)}
          title={!gate.loading && !gate.isOperational ? blockedTitle : undefined}
          className="flex shrink-0 items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent)] px-4 py-2 text-[12.5px] font-semibold text-[color:var(--admin-accent-fg)] disabled:opacity-50"
        >
          <PlusIcon width={13} height={13} />
          Добавить
        </button>
      </div>
    </Panel>
  )
}
