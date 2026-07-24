import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { PlusIcon, EditIcon, TrashIcon, CheckIcon, XIcon } from '../components/icons'
import { catalogApi, ApiError, type Brand, type Category, type TaxRate } from '../../lib/api'

function RowActions({ onEdit, onDelete, busy }: { onEdit: () => void; onDelete: () => void; busy: boolean }) {
  return (
    <div className="flex shrink-0 gap-1.5">
      <button
        onClick={onEdit}
        disabled={busy}
        aria-label="Изменить"
        className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)] disabled:opacity-50"
      >
        <EditIcon width={14} height={14} />
      </button>
      <button
        onClick={onDelete}
        disabled={busy}
        aria-label="Удалить"
        className="grid h-8 w-8 place-items-center rounded-lg text-[#f87171] hover:bg-[#f8717118] disabled:opacity-50"
      >
        <TrashIcon width={14} height={14} />
      </button>
    </div>
  )
}

function SectionShell({ title, error, children }: { title: string; error: string; children: React.ReactNode }) {
  return (
    <Card className="p-5">
      <h2 className="mb-4 text-[15px] font-bold text-[color:var(--admin-text)]">{title}</h2>
      {error && <div className="mb-3 rounded-lg bg-[#f8717118] px-3 py-2 text-[12px] font-medium text-[#f87171]">{error}</div>}
      {children}
    </Card>
  )
}

function BrandsSection() {
  const [brands, setBrands] = useState<Brand[] | null>(null)
  const [error, setError] = useState('')
  const [newName, setNewName] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingName, setEditingName] = useState('')
  const [busyId, setBusyId] = useState<number | 'new' | null>(null)

  const load = useCallback(async () => {
    try {
      const res = await catalogApi.getBrands()
      setBrands(res.brands)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить бренды')
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
      await catalogApi.createBrand(newName.trim())
      setNewName('')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось создать бренд')
    } finally {
      setBusyId(null)
    }
  }

  async function handleSaveEdit(id: number) {
    if (!editingName.trim()) return
    setBusyId(id)
    setError('')
    try {
      await catalogApi.updateBrand(id, editingName.trim())
      setEditingId(null)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить бренд')
    } finally {
      setBusyId(null)
    }
  }

  async function handleDelete(id: number) {
    setBusyId(id)
    setError('')
    try {
      await catalogApi.deleteBrand(id)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить бренд — возможно, он используется товарами')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <SectionShell title="Бренды" error={error}>
      <div className="flex flex-col gap-2">
        {brands?.map((b) => (
          <div key={b.brandId} className="flex items-center gap-2 rounded-lg bg-[color:var(--admin-hover)] px-3 py-2">
            {editingId === b.brandId ? (
              <>
                <input
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  autoFocus
                  className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <button onClick={() => handleSaveEdit(b.brandId)} disabled={busyId === b.brandId} className="grid h-8 w-8 place-items-center rounded-lg text-[#34d399] hover:bg-[#34d39918]">
                  <CheckIcon width={14} height={14} />
                </button>
                <button onClick={() => setEditingId(null)} className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]">
                  <XIcon width={14} height={14} />
                </button>
              </>
            ) : (
              <>
                <span className="min-w-0 flex-1 truncate text-[13px] font-medium text-[color:var(--admin-text)]">{b.name}</span>
                <RowActions
                  busy={busyId === b.brandId}
                  onEdit={() => {
                    setEditingId(b.brandId)
                    setEditingName(b.name)
                  }}
                  onDelete={() => handleDelete(b.brandId)}
                />
              </>
            )}
          </div>
        ))}
        {brands?.length === 0 && <p className="py-3 text-center text-[12px] text-[color:var(--admin-text-tertiary)]">Брендов пока нет</p>}
      </div>
      <div className="mt-3 flex gap-2">
        <input
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleCreate()}
          placeholder="Новый бренд"
          className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <button
          onClick={handleCreate}
          disabled={busyId === 'new' || !newName.trim()}
          className="flex shrink-0 items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent)] px-3 py-2 text-[12.5px] font-semibold text-white disabled:opacity-50"
        >
          <PlusIcon width={13} height={13} />
          Добавить
        </button>
      </div>
    </SectionShell>
  )
}

function CategoriesSection() {
  const [categories, setCategories] = useState<Category[] | null>(null)
  const [error, setError] = useState('')
  const [newName, setNewName] = useState('')
  const [newParentId, setNewParentId] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingName, setEditingName] = useState('')
  const [editingParentId, setEditingParentId] = useState('')
  const [busyId, setBusyId] = useState<number | 'new' | null>(null)

  const load = useCallback(async () => {
    try {
      const res = await catalogApi.getCategories()
      setCategories(res.categories)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить категории')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  function parentName(id?: number) {
    if (!id) return null
    return categories?.find((c) => c.categoryId === id)?.name ?? `#${id}`
  }

  async function handleCreate() {
    if (!newName.trim()) return
    setBusyId('new')
    setError('')
    try {
      await catalogApi.createCategory(newName.trim(), newParentId ? Number(newParentId) : undefined)
      setNewName('')
      setNewParentId('')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось создать категорию')
    } finally {
      setBusyId(null)
    }
  }

  async function handleSaveEdit(id: number) {
    if (!editingName.trim()) return
    setBusyId(id)
    setError('')
    try {
      await catalogApi.updateCategory(id, editingName.trim(), editingParentId ? Number(editingParentId) : undefined)
      setEditingId(null)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить категорию')
    } finally {
      setBusyId(null)
    }
  }

  async function handleDelete(id: number) {
    setBusyId(id)
    setError('')
    try {
      await catalogApi.deleteCategory(id)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить категорию — возможно, она используется')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <SectionShell title="Категории" error={error}>
      <div className="flex flex-col gap-2">
        {categories?.map((c) => (
          <div key={c.categoryId} className="flex items-center gap-2 rounded-lg bg-[color:var(--admin-hover)] px-3 py-2">
            {editingId === c.categoryId ? (
              <>
                <input
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  autoFocus
                  className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <input
                  value={editingParentId}
                  onChange={(e) => setEditingParentId(e.target.value)}
                  placeholder="ID родителя"
                  inputMode="numeric"
                  className="w-28 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <button onClick={() => handleSaveEdit(c.categoryId)} disabled={busyId === c.categoryId} className="grid h-8 w-8 place-items-center rounded-lg text-[#34d399] hover:bg-[#34d39918]">
                  <CheckIcon width={14} height={14} />
                </button>
                <button onClick={() => setEditingId(null)} className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]">
                  <XIcon width={14} height={14} />
                </button>
              </>
            ) : (
              <>
                <span className="min-w-0 flex-1 truncate text-[13px] font-medium text-[color:var(--admin-text)]">
                  {c.name}
                  {c.parentCategoryId && (
                    <span className="ml-1.5 text-[11px] font-normal text-[color:var(--admin-text-tertiary)]">
                      ⤷ {parentName(c.parentCategoryId)}
                    </span>
                  )}
                </span>
                <RowActions
                  busy={busyId === c.categoryId}
                  onEdit={() => {
                    setEditingId(c.categoryId)
                    setEditingName(c.name)
                    setEditingParentId(c.parentCategoryId ? String(c.parentCategoryId) : '')
                  }}
                  onDelete={() => handleDelete(c.categoryId)}
                />
              </>
            )}
          </div>
        ))}
        {categories?.length === 0 && <p className="py-3 text-center text-[12px] text-[color:var(--admin-text-tertiary)]">Категорий пока нет</p>}
      </div>
      <div className="mt-3 flex gap-2">
        <input
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="Новая категория"
          className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <input
          value={newParentId}
          onChange={(e) => setNewParentId(e.target.value)}
          placeholder="ID родителя (необяз.)"
          inputMode="numeric"
          className="w-36 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <button
          onClick={handleCreate}
          disabled={busyId === 'new' || !newName.trim()}
          className="flex shrink-0 items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent)] px-3 py-2 text-[12.5px] font-semibold text-white disabled:opacity-50"
        >
          <PlusIcon width={13} height={13} />
          Добавить
        </button>
      </div>
    </SectionShell>
  )
}

function TaxRatesSection() {
  const [taxRates, setTaxRates] = useState<TaxRate[] | null>(null)
  const [error, setError] = useState('')
  const [newName, setNewName] = useState('')
  const [newPercentage, setNewPercentage] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingName, setEditingName] = useState('')
  const [editingPercentage, setEditingPercentage] = useState('')
  const [busyId, setBusyId] = useState<number | 'new' | null>(null)

  const load = useCallback(async () => {
    try {
      const res = await catalogApi.getTaxRates()
      setTaxRates(res.taxRates)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить налоговые ставки')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function handleCreate() {
    if (!newName.trim() || !newPercentage) return
    setBusyId('new')
    setError('')
    try {
      await catalogApi.createTaxRate(newName.trim(), Number(newPercentage))
      setNewName('')
      setNewPercentage('')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось создать ставку')
    } finally {
      setBusyId(null)
    }
  }

  async function handleSaveEdit(id: number, categoryId?: number) {
    if (!editingName.trim() || !editingPercentage) return
    setBusyId(id)
    setError('')
    try {
      await catalogApi.updateTaxRate(id, editingName.trim(), Number(editingPercentage), categoryId)
      setEditingId(null)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить ставку')
    } finally {
      setBusyId(null)
    }
  }

  async function handleDelete(id: number) {
    setBusyId(id)
    setError('')
    try {
      await catalogApi.deleteTaxRate(id)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить ставку — возможно, она используется')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <SectionShell title="Налоговые ставки" error={error}>
      <div className="flex flex-col gap-2">
        {taxRates?.map((t) => (
          <div key={t.taxRateId} className="flex items-center gap-2 rounded-lg bg-[color:var(--admin-hover)] px-3 py-2">
            {editingId === t.taxRateId ? (
              <>
                <input
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  autoFocus
                  className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <input
                  value={editingPercentage}
                  onChange={(e) => setEditingPercentage(e.target.value)}
                  type="number"
                  className="w-20 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
                />
                <button onClick={() => handleSaveEdit(t.taxRateId, t.categoryId)} disabled={busyId === t.taxRateId} className="grid h-8 w-8 place-items-center rounded-lg text-[#34d399] hover:bg-[#34d39918]">
                  <CheckIcon width={14} height={14} />
                </button>
                <button onClick={() => setEditingId(null)} className="grid h-8 w-8 place-items-center rounded-lg text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]">
                  <XIcon width={14} height={14} />
                </button>
              </>
            ) : (
              <>
                <span className="min-w-0 flex-1 truncate text-[13px] font-medium text-[color:var(--admin-text)]">
                  {t.name} <span className="text-[color:var(--admin-text-tertiary)]">— {t.percentage}%</span>
                </span>
                <RowActions
                  busy={busyId === t.taxRateId}
                  onEdit={() => {
                    setEditingId(t.taxRateId)
                    setEditingName(t.name)
                    setEditingPercentage(String(t.percentage))
                  }}
                  onDelete={() => handleDelete(t.taxRateId)}
                />
              </>
            )}
          </div>
        ))}
        {taxRates?.length === 0 && <p className="py-3 text-center text-[12px] text-[color:var(--admin-text-tertiary)]">Ставок пока нет</p>}
      </div>
      <div className="mt-3 flex gap-2">
        <input
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="Название (напр. НДС)"
          className="min-w-0 flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <input
          value={newPercentage}
          onChange={(e) => setNewPercentage(e.target.value)}
          type="number"
          placeholder="%"
          className="w-20 shrink-0 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2 text-[13px] outline-none focus:border-[color:var(--admin-accent)]"
        />
        <button
          onClick={handleCreate}
          disabled={busyId === 'new' || !newName.trim() || !newPercentage}
          className="flex shrink-0 items-center gap-1.5 rounded-lg bg-[color:var(--admin-accent)] px-3 py-2 text-[12.5px] font-semibold text-white disabled:opacity-50"
        >
          <PlusIcon width={13} height={13} />
          Добавить
        </button>
      </div>
    </SectionShell>
  )
}

export function CatalogTab() {
  return (
    <div className="grid grid-cols-1 gap-5 lg:grid-cols-3">
      <BrandsSection />
      <CategoriesSection />
      <TaxRatesSection />
    </div>
  )
}
