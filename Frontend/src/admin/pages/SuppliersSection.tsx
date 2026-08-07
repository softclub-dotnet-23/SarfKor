import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { FormModal, FormField } from '../components/FormModal'
import { TruckIcon, PlusIcon, EditIcon, TrashIcon } from '../components/icons'
import { suppliersApi, ApiError, type Supplier } from '../../lib/api'

function SupplierFormFields({
  name, setName, phone, setPhone, email, setEmail, nameError,
}: {
  name: string; setName: (v: string) => void
  phone: string; setPhone: (v: string) => void
  email: string; setEmail: (v: string) => void
  nameError?: string
}) {
  const fieldClass = 'w-full rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]'
  return (
    <>
      <FormField label="Название" required error={nameError} scheme="admin">
        <input value={name} onChange={(e) => setName(e.target.value)} className={fieldClass} />
      </FormField>
      <FormField label="Телефон" scheme="admin">
        <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="Необязательно" className={fieldClass} />
      </FormField>
      <FormField label="Email" scheme="admin">
        <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Необязательно" type="email" className={fieldClass} />
      </FormField>
    </>
  )
}

function SupplierFormModal({ supplier, storeId, onClose, onSaved }: { supplier: Supplier | 'create' | null; storeId: number | null; onClose: () => void; onSaved: () => Promise<void> }) {
  const isEdit = supplier !== null && supplier !== 'create'
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [nameError, setNameError] = useState('')

  useEffect(() => {
    if (isEdit) {
      const s = supplier as Supplier
      setName(s.name)
      setPhone(s.contactPhone ?? '')
      setEmail(s.contactEmail ?? '')
    } else {
      setName('')
      setPhone('')
      setEmail('')
    }
    setNameError('')
  }, [supplier, isEdit])

  async function submit() {
    if (!name.trim()) {
      setNameError('Укажите название')
      throw new Error('Укажите название')
    }
    if (isEdit) {
      await suppliersApi.updateSupplier((supplier as Supplier).supplierId, name.trim(), phone.trim() || undefined, email.trim() || undefined)
    } else {
      if (!storeId) throw new Error('Магазин не выбран')
      await suppliersApi.createSupplier(storeId, name.trim(), phone.trim() || undefined, email.trim() || undefined)
    }
    await onSaved()
  }

  return (
    <FormModal
      open={supplier !== null}
      onClose={onClose}
      title={isEdit ? 'Изменить поставщика' : 'Новый поставщик'}
      isDirty={!!(name || phone || email)}
      onSubmit={submit}
      submitLabel={isEdit ? 'Сохранить' : 'Добавить поставщика'}
      scheme="admin"
    >
      <SupplierFormFields name={name} setName={setName} phone={phone} setPhone={setPhone} email={email} setEmail={setEmail} nameError={nameError} />
    </FormModal>
  )
}

export function SuppliersSection({ storeId }: { storeId: number | null }) {
  const [suppliers, setSuppliers] = useState<Supplier[] | null>(null)
  const [error, setError] = useState('')
  const [editingSupplier, setEditingSupplier] = useState<Supplier | 'create' | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)

  const load = useCallback(async () => {
    if (!storeId) return
    try {
      const res = await suppliersApi.getSuppliers(storeId)
      setSuppliers(res.suppliers)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить поставщиков')
    }
  }, [storeId])

  useEffect(() => {
    load()
  }, [load])

  async function handleDelete(id: number) {
    if (!window.confirm('Удалить поставщика?')) return
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
      <div className="mb-5 flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <TruckIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Поставщики</span>
        </div>
        <button
          onClick={() => setEditingSupplier('create')}
          className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent)] px-3.5 py-2 text-[12.5px] font-semibold text-white"
        >
          <PlusIcon width={13} height={13} />
          Добавить
        </button>
      </div>

      {error && <div className="mb-3 rounded-lg bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">{error}</div>}

      <div className="flex flex-col gap-2">
        {suppliers?.map((s) => (
          <div key={s.supplierId} className="flex flex-wrap items-center gap-2 rounded-xl bg-[color:var(--admin-hover)] px-4 py-3">
            <div className="min-w-0 flex-1">
              <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">{s.name}</div>
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                {[s.contactPhone, s.contactEmail].filter(Boolean).join(' · ') || 'Контакты не указаны'}
              </div>
            </div>
            <div className="flex shrink-0 gap-1.5">
              <button
                onClick={() => setEditingSupplier(s)}
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
                className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-danger)] hover:bg-[color:var(--admin-danger-dim)] disabled:opacity-50"
              >
                <TrashIcon width={14} height={14} />
              </button>
            </div>
          </div>
        ))}
        {suppliers?.length === 0 && (
          <p className="py-4 text-center text-[12.5px] text-[color:var(--admin-text-tertiary)]">Поставщиков пока нет</p>
        )}
      </div>

      <SupplierFormModal supplier={editingSupplier} storeId={storeId} onClose={() => setEditingSupplier(null)} onSaved={async () => { await load(); setEditingSupplier(null) }} />
    </Card>
  )
}
