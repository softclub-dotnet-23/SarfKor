import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { TruckIcon, PlusIcon, EditIcon, TrashIcon, CheckIcon, XIcon } from '../components/icons'
import { suppliersApi, ApiError, type Supplier } from '../../lib/api'

export function SuppliersSection() {
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
    try {
      const res = await suppliersApi.getSuppliers()
      setSuppliers(res.suppliers)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить поставщиков')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function handleCreate() {
    if (!newName.trim()) return
    setBusyId('new')
    setError('')
    try {
      await suppliersApi.createSupplier(newName.trim(), newPhone.trim() || undefined, newEmail.trim() || undefined)
      setNewName('')
      setNewPhone('')
      setNewEmail('')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось добавить поставщика')
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
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить поставщика')
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
      setError(
        err instanceof ApiError ? err.message : 'Не удалось удалить поставщика — возможно, он используется в поставках или заказах',
      )
    } finally {
      setBusyId(null)
    }
  }

  return (
    <Card className="p-6">
      <div className="mb-5 flex items-center gap-2">
        <TruckIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
        <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Поставщики</span>
      </div>

      {error && <div className="mb-3 rounded-lg bg-[#f8717118] px-3.5 py-2.5 text-[12.5px] font-medium text-[#f87171]">{error}</div>}

      <div className="flex flex-col gap-2">
        {suppliers?.map((s) => (
          <div key={s.supplierId} className="flex flex-wrap items-center gap-2 rounded-xl bg-[color:var(--admin-hover)] px-4 py-3">
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
                  disabled={busyId === s.supplierId}
                  className="grid h-8 w-8 place-items-center rounded-lg text-[#34d399] hover:bg-[#34d39918]"
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
                    disabled={busyId === s.supplierId}
                    aria-label="Изменить"
                    className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-card)] disabled:opacity-50"
                  >
                    <EditIcon width={14} height={14} />
                  </button>
                  <button
                    onClick={() => handleDelete(s.supplierId)}
                    disabled={busyId === s.supplierId}
                    aria-label="Удалить"
                    className="grid h-8 w-8 place-items-center rounded-lg text-[#f87171] hover:bg-[#f8717118] disabled:opacity-50"
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
          disabled={busyId === 'new' || !newName.trim()}
          className="flex shrink-0 items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent)] px-4 py-2 text-[12.5px] font-semibold text-white disabled:opacity-50"
        >
          <PlusIcon width={13} height={13} />
          Добавить
        </button>
      </div>
    </Card>
  )
}
