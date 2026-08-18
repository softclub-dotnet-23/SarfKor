import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState, classifyError, type ErrorKind } from '../components/ErrorState'
import { errorMessage } from '../../lib/errorKind'
import { EmptyState } from '../components/EmptyState'
import { Select } from '../components/Select'
import { SectionSelect } from '../components/SectionSelect'
import { Badge } from '../components/Badge'
import { DateField } from '../components/DateField'
import { FormModal, FormField } from '../components/FormModal'
import { AddButton } from '../components/Button'
import { ChevronDownIcon, TagIcon, PercentIcon, SearchIcon, EditIcon, TrashIcon, GridIcon } from '../components/icons'
import {
  catalogApi,
  type Category,
  type Brand,
  type DuplicateBrandGroup,
  type TaxRate,
} from '../../lib/api'

type MainTab = 'categories' | 'brands' | 'tax-rates'

function fmtDate(iso?: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

/* ---------- Категории ---------- */
// A drag-and-drop tree is a lot of code for a "rare-edit tool" (ADMIN_PROMPT.md §2.8) --
// reassigning the parent via a dropdown + a plain order number delivers the same "move/reorder"
// capability with far less surface to get wrong.

function CategoryFormFields({
  name, setName, parentId, setParentId, displayOrder, setDisplayOrder, isHidden, setIsHidden, all, excludeId,
}: {
  name: string
  setName: (v: string) => void
  parentId: string
  setParentId: (v: string) => void
  displayOrder: string
  setDisplayOrder: (v: string) => void
  isHidden: boolean
  setIsHidden: (v: boolean) => void
  all: Category[]
  excludeId?: number
}) {
  return (
    <>
      <FormField label="Название" required scheme="admin">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label="Родительская категория" scheme="admin">
        <Select
          scheme="admin"
          value={parentId}
          onChange={setParentId}
          placeholder="Без родителя (верхний уровень)"
          options={all.filter((c) => c.categoryId !== excludeId).map((c) => ({ value: String(c.categoryId), label: c.name }))}
        />
      </FormField>
      <FormField label="Порядок" scheme="admin">
        <input
          value={displayOrder}
          onChange={(e) => setDisplayOrder(e.target.value.replace(/[^0-9]/g, ''))}
          type="number"
          className="w-24 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <label className="flex items-center gap-2 text-[12.5px] font-semibold text-[color:var(--admin-text)]">
        <input type="checkbox" checked={isHidden} onChange={(e) => setIsHidden(e.target.checked)} className="h-3.5 w-3.5 accent-[color:var(--admin-accent)]" />
        Скрыта из каталога
      </label>
    </>
  )
}

function CreateCategoryModal({ open, onClose, all, onCreated }: { open: boolean; onClose: () => void; all: Category[]; onCreated: () => Promise<void> }) {
  const [name, setName] = useState('')
  const [parentId, setParentId] = useState('')
  const [nameError, setNameError] = useState('')

  function handleClose() {
    setName('')
    setParentId('')
    setNameError('')
    onClose()
  }

  async function submit() {
    if (!name.trim()) {
      setNameError('Укажите название')
      throw new Error('Укажите название')
    }
    const res = await catalogApi.createCategory(name.trim(), parentId ? Number(parentId) : undefined)
    if (res.outcome === 'ParentCategoryNotFound') throw new Error('Родительская категория не найдена')
    await onCreated()
    handleClose()
  }

  return (
    <FormModal open={open} onClose={handleClose} title="Новая категория" isDirty={!!name || !!parentId} onSubmit={submit} submitLabel="Добавить" scheme="admin">
      <FormField label="Название" required error={nameError} scheme="admin">
        <input
          value={name}
          onChange={(e) => {
            setName(e.target.value)
            setNameError('')
          }}
          placeholder="Например, «Молочные продукты»"
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label="Родительская категория" scheme="admin">
        <Select scheme="admin" value={parentId} onChange={setParentId} placeholder="Без родителя (верхний уровень)" options={all.map((c) => ({ value: String(c.categoryId), label: c.name }))} />
      </FormField>
    </FormModal>
  )
}

function EditCategoryModal({ category, all, onClose, onSaved }: { category: Category | null; all: Category[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const [name, setName] = useState('')
  const [parentId, setParentId] = useState('')
  const [displayOrder, setDisplayOrder] = useState('0')
  const [isHidden, setIsHidden] = useState(false)

  useEffect(() => {
    if (!category) return
    setName(category.name)
    setParentId(category.parentCategoryId ? String(category.parentCategoryId) : '')
    setDisplayOrder(String(category.displayOrder))
    setIsHidden(category.isHidden)
  }, [category])

  async function submit() {
    if (!category) return
    if (!name.trim()) throw new Error('Укажите название')
    const res = await catalogApi.updateCategory(category.categoryId, name.trim(), parentId ? Number(parentId) : undefined, Number(displayOrder) || 0, isHidden)
    if (res.outcome !== 'Updated') {
      throw new Error(
        res.outcome === 'ParentCategoryNotFound' ? 'Родительская категория не найдена'
          : res.outcome === 'SelfReference' ? 'Категория не может быть родителем самой себя'
            : res.outcome,
      )
    }
    await onSaved()
  }

  async function handleDelete() {
    if (!category) return
    if (!window.confirm(`Удалить категорию «${category.name}»?`)) return
    try {
      const res = await catalogApi.deleteCategory(category.categoryId)
      if (res.outcome !== 'Deleted') {
        window.alert('Категория используется товарами или подкатегориями — сначала перенесите их.')
        return
      }
      await onSaved()
    } catch (err) {
      console.error('Failed to delete category:', err)
      window.alert('Не удалось удалить категорию')
    }
  }

  return (
    <FormModal open={!!category} onClose={onClose} title="Изменить категорию" isDirty submitLabel="Сохранить" scheme="admin" onSubmit={submit}>
      {category && (
        <>
          <CategoryFormFields
            name={name} setName={setName} parentId={parentId} setParentId={setParentId}
            displayOrder={displayOrder} setDisplayOrder={setDisplayOrder} isHidden={isHidden} setIsHidden={setIsHidden}
            all={all} excludeId={category.categoryId}
          />
          <button
            type="button"
            onClick={handleDelete}
            className="mt-4 w-full rounded-xl border border-[color:var(--admin-danger)] px-4 py-2.5 text-[12.5px] font-bold text-[color:var(--admin-danger)]"
          >
            Удалить категорию
          </button>
        </>
      )}
    </FormModal>
  )
}

function CategoryNode({ node, depth, byParent, onEdit }: {
  node: Category
  depth: number
  byParent: Map<number | undefined, Category[]>
  onEdit: (category: Category) => void
}) {
  const [expanded, setExpanded] = useState(depth === 0)
  const children = byParent.get(node.categoryId) ?? []

  return (
    <div>
      <div className="flex items-center gap-1.5 rounded-lg px-2 py-1.5 hover:bg-[color:var(--admin-hover)]" style={{ paddingLeft: 8 + depth * 20 }}>
        {children.length > 0 ? (
          <button onClick={() => setExpanded((v) => !v)} className="grid h-5 w-5 shrink-0 place-items-center text-[color:var(--admin-text-tertiary)]">
            <ChevronDownIcon width={11} height={11} className={expanded ? '' : '-rotate-90'} />
          </button>
        ) : (
          <span className="w-5 shrink-0" />
        )}
        <span className={`flex-1 truncate text-[12.5px] ${node.isHidden ? 'text-[color:var(--admin-text-tertiary)] line-through' : 'text-[color:var(--admin-text)]'} ${depth === 0 ? 'font-bold' : 'font-medium'}`}>
          {node.name}
        </span>
        {node.isHidden && <Badge scheme="admin" variant="neutral" size="sm">скрыта</Badge>}
        <button onClick={() => onEdit(node)} className="grid h-6 w-6 shrink-0 place-items-center rounded text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]">
          <EditIcon width={13} height={13} />
        </button>
      </div>
      {expanded && children.map((c) => (
        <CategoryNode key={c.categoryId} node={c} depth={depth + 1} byParent={byParent} onEdit={onEdit} />
      ))}
    </div>
  )
}

function CategoriesSection({ createOpen, onCloseCreate }: { createOpen: boolean; onCloseCreate: () => void }) {
  const [categories, setCategories] = useState<Category[] | null>(null)
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [editingCategory, setEditingCategory] = useState<Category | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      setCategories((await catalogApi.getCategories()).categories)
    } catch (err) {
      console.error('Failed to load categories:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить категории')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const byParent = useMemo(() => {
    const map = new Map<number | undefined, Category[]>()
    for (const c of categories ?? []) {
      // The API serializes "no parent" as JSON null, not an omitted field -- Category's TS type
      // marks parentCategoryId optional, but at runtime the value IS null, never undefined. Using
      // `c.parentCategoryId` directly as the map key meant root categories were filed under the
      // key `null` while the lookup below asked for `undefined`, a different Map key -- so `roots`
      // was always empty and the tree silently rendered "Категорий нет" no matter how much data
      // came back. Normalizing null -> undefined here makes insertion and lookup agree.
      const key = c.parentCategoryId ?? undefined
      if (!map.has(key)) map.set(key, [])
      map.get(key)!.push(c)
    }
    for (const list of map.values()) list.sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name))
    return map
  }, [categories])

  const roots = byParent.get(undefined) ?? []

  return (
    <div>
      <Card scheme="admin" className="p-3">
        {categories === null && !error && <Loading scheme="admin" />}
        {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
        {categories && roots.length === 0 && (
          <EmptyState scheme="admin" tone="neutral" icon={<GridIcon width={22} height={22} />} title="Категорий нет" body="Добавьте первую категорию" />
        )}
        {roots.map((r) => (
          <CategoryNode key={r.categoryId} node={r} depth={0} byParent={byParent} onEdit={setEditingCategory} />
        ))}
      </Card>

      <CreateCategoryModal open={createOpen} onClose={onCloseCreate} all={categories ?? []} onCreated={load} />
      <EditCategoryModal category={editingCategory} all={categories ?? []} onClose={() => setEditingCategory(null)} onSaved={async () => { await load(); setEditingCategory(null) }} />
    </div>
  )
}

/* ---------- Бренды ---------- */

function BrandsSection() {
  const [brands, setBrands] = useState<Brand[] | null>(null)
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [duplicates, setDuplicates] = useState<DuplicateBrandGroup[] | null>(null)
  const [dupError, setDupError] = useState('')
  const [dupErrorKind, setDupErrorKind] = useState<ErrorKind>('unknown')
  const [renamingId, setRenamingId] = useState<number | null>(null)
  const [renameValue, setRenameValue] = useState('')

  const load = useCallback(async (term: string) => {
    setError('')
    try {
      setBrands((await catalogApi.getBrands(term || undefined)).brands)
    } catch (err) {
      console.error('Failed to load brands:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить бренды')
    }
  }, [])

  const loadDuplicates = useCallback(async () => {
    setDupError('')
    try {
      setDuplicates((await catalogApi.getBrandDuplicateCandidates()).groups)
    } catch (err) {
      console.error('Failed to load duplicate brand candidates:', err)
      setDupErrorKind(classifyError(err))
      setDupError('Не удалось найти дубликаты')
    }
  }, [])

  useEffect(() => {
    load(search)
  }, [load, search])

  useEffect(() => {
    loadDuplicates()
  }, [loadDuplicates])

  async function handleRename(brandId: number) {
    if (!renameValue.trim()) return
    try {
      await catalogApi.updateBrand(brandId, renameValue.trim())
      setRenamingId(null)
      await load(search)
    } catch (err) {
      console.error('Failed to rename brand:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось переименовать бренд')
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <Card scheme="admin" className="p-5">
        <div className="mb-3 flex items-center gap-2 text-[13.5px] font-bold text-[color:var(--admin-text)]">
          <TagIcon width={16} height={16} />
          Похожие бренды — объединение дубликатов
        </div>
        {duplicates === null && !dupError && <Loading scheme="admin" />}
        {dupError && <ErrorState scheme="admin" message={dupError} kind={dupErrorKind} onRetry={loadDuplicates} />}
        {duplicates && duplicates.length === 0 && <p className="text-[12.5px] text-[color:var(--admin-text-tertiary)]">Явных дубликатов не найдено.</p>}
        {duplicates && duplicates.length > 0 && (
          <div className="flex flex-col gap-3">
            {duplicates.map((g) => (
              <DuplicateGroupRow key={g.normalizedKey} group={g} onMerged={() => { loadDuplicates(); load(search) }} />
            ))}
          </div>
        )}
      </Card>

      <div>
        <div className="relative mb-3 max-w-md">
          <SearchIcon width={15} height={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)]" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Поиск бренда…"
            className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] py-2.5 pl-9 pr-3.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
          />
        </div>
        <Card scheme="admin" className="overflow-hidden">
          {brands === null && !error && <Loading scheme="admin" />}
          {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={() => load(search)} />}
          {brands && brands.length === 0 && <EmptyState scheme="admin" title="Брендов не найдено" />}
          {brands && brands.length > 0 && (
            <div className="flex flex-col">
              {brands.map((b) => (
                <div key={b.brandId} className="flex items-center justify-between gap-2 border-b border-[color:var(--admin-border)] px-4 py-2.5 last:border-0">
                  {renamingId === b.brandId ? (
                    <div className="flex flex-1 gap-2">
                      <input
                        value={renameValue}
                        onChange={(e) => setRenameValue(e.target.value)}
                        autoFocus
                        className="flex-1 rounded-lg border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-2.5 py-1.5 text-[12.5px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
                      />
                      <button onClick={() => handleRename(b.brandId)} className="rounded-lg bg-[color:var(--admin-accent)] px-3 py-1.5 text-[11.5px] font-bold text-[color:var(--admin-accent-fg)]">
                        ОК
                      </button>
                      <button onClick={() => setRenamingId(null)} className="rounded-lg border border-[color:var(--admin-border)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--admin-text)]">
                        Отмена
                      </button>
                    </div>
                  ) : (
                    <>
                      <span className="text-[13px] font-semibold text-[color:var(--admin-text)]">{b.name}</span>
                      <div className="flex shrink-0 items-center gap-3">
                        <span className="font-[JetBrains_Mono,monospace] text-[11.5px] text-[color:var(--admin-text-tertiary)]">{b.productCount} товаров</span>
                        <button
                          onClick={() => { setRenamingId(b.brandId); setRenameValue(b.name) }}
                          className="grid h-7 w-7 place-items-center rounded text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]"
                        >
                          <EditIcon width={13} height={13} />
                        </button>
                      </div>
                    </>
                  )}
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  )
}

function DuplicateGroupRow({ group, onMerged }: { group: DuplicateBrandGroup; onMerged: () => void }) {
  const [targetId, setTargetId] = useState(String(group.brands[0]?.brandId ?? ''))
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const totalProducts = group.brands.reduce((sum, b) => sum + b.productCount, 0)

  async function handleMerge() {
    if (!targetId || busy) return
    const sourceIds = group.brands.filter((b) => String(b.brandId) !== targetId).map((b) => b.brandId)
    if (sourceIds.length === 0) return
    setBusy(true)
    setError('')
    try {
      await catalogApi.mergeBrands(Number(targetId), sourceIds)
      onMerged()
    } catch (err) {
      console.error('Failed to merge brands:', err)
      setError(errorMessage(err, 'Не удалось объединить бренды'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="rounded-xl bg-[color:var(--admin-hover)] p-3.5">
      <div className="mb-2 flex flex-wrap gap-1.5">
        {group.brands.map((b) => (
          <label key={b.brandId} className="flex items-center gap-1.5 rounded-full border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-2.5 py-1 text-[12px]">
            <input type="radio" name={group.normalizedKey} checked={String(b.brandId) === targetId} onChange={() => setTargetId(String(b.brandId))} className="accent-[color:var(--admin-accent)]" />
            <span className="font-semibold text-[color:var(--admin-text)]">{b.name}</span>
            <span className="text-[color:var(--admin-text-tertiary)]">({b.productCount})</span>
          </label>
        ))}
      </div>
      {error && <p className="mb-2 text-[11.5px] font-medium text-[color:var(--admin-danger)]">{error}</p>}
      <div className="flex items-center justify-between gap-2">
        <span className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Все {totalProducts} товаров перейдут выбранному бренду, остальные {group.brands.length - 1} исчезнут.
        </span>
        <button onClick={handleMerge} disabled={busy} className="shrink-0 rounded-lg bg-[color:var(--admin-accent)] px-3.5 py-1.5 text-[11.5px] font-bold text-[color:var(--admin-accent-fg)] disabled:opacity-50">
          {busy ? 'Секунду…' : 'Объединить'}
        </button>
      </div>
    </div>
  )
}

/* ---------- Налоговые ставки ---------- */

function TaxRateFormFields({
  name, setName, percentage, setPercentage, categoryId, setCategoryId, effectiveFrom, setEffectiveFrom, effectiveTo, setEffectiveTo, categories, nameError, percentageError,
}: {
  name: string
  setName: (v: string) => void
  percentage: string
  setPercentage: (v: string) => void
  categoryId: string
  setCategoryId: (v: string) => void
  effectiveFrom: string
  setEffectiveFrom: (v: string) => void
  effectiveTo: string
  setEffectiveTo: (v: string) => void
  categories: Category[]
  nameError?: string
  percentageError?: string
}) {
  return (
    <>
      <FormField label="Название" required error={nameError} scheme="admin">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Например, «НДС»"
          className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label="Ставка, %" required error={percentageError} scheme="admin">
        <input
          value={percentage}
          onChange={(e) => setPercentage(e.target.value)}
          type="number"
          min={0}
          max={100}
          step="0.01"
          className="w-32 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
        />
      </FormField>
      <FormField label="Категория" scheme="admin">
        <Select scheme="admin" value={categoryId} onChange={setCategoryId} placeholder="Все категории" options={categories.map((c) => ({ value: String(c.categoryId), label: c.name }))} />
      </FormField>
      <FormField label="Действует" scheme="admin">
        <div className="flex gap-2">
          <DateField value={effectiveFrom} onChange={setEffectiveFrom} title="Действует с" outputFormat="dateOnly" />
          <DateField value={effectiveTo} onChange={setEffectiveTo} title="Действует по" outputFormat="dateOnly" />
        </div>
      </FormField>
    </>
  )
}

function CreateTaxRateModal({ open, onClose, categories, onCreated }: { open: boolean; onClose: () => void; categories: Category[]; onCreated: () => Promise<void> }) {
  const [name, setName] = useState('')
  const [percentage, setPercentage] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [effectiveFrom, setEffectiveFrom] = useState('')
  const [effectiveTo, setEffectiveTo] = useState('')
  const [nameError, setNameError] = useState('')
  const [percentageError, setPercentageError] = useState('')

  function reset() {
    setName('')
    setPercentage('')
    setCategoryId('')
    setEffectiveFrom('')
    setEffectiveTo('')
    setNameError('')
    setPercentageError('')
  }

  function handleClose() {
    reset()
    onClose()
  }

  async function submit() {
    let hasError = false
    if (!name.trim()) {
      setNameError('Укажите название')
      hasError = true
    }
    const pct = Number(percentage)
    if (percentage === '' || Number.isNaN(pct) || pct < 0 || pct > 100) {
      setPercentageError('Укажите ставку от 0 до 100')
      hasError = true
    }
    if (hasError) throw new Error('Проверьте поля формы')
    await catalogApi.createTaxRate(name.trim(), pct, categoryId ? Number(categoryId) : undefined, effectiveFrom || undefined, effectiveTo || undefined)
    await onCreated()
    handleClose()
  }

  return (
    <FormModal open={open} onClose={handleClose} title="Новая налоговая ставка" isDirty={!!(name || percentage || categoryId || effectiveFrom || effectiveTo)} onSubmit={submit} submitLabel="Добавить" scheme="admin">
      <TaxRateFormFields
        name={name} setName={setName} percentage={percentage} setPercentage={setPercentage}
        categoryId={categoryId} setCategoryId={setCategoryId} effectiveFrom={effectiveFrom} setEffectiveFrom={setEffectiveFrom}
        effectiveTo={effectiveTo} setEffectiveTo={setEffectiveTo} categories={categories} nameError={nameError} percentageError={percentageError}
      />
    </FormModal>
  )
}

function TaxRatesSection({ createOpen, onCloseCreate }: { createOpen: boolean; onCloseCreate: () => void }) {
  const [rates, setRates] = useState<TaxRate[] | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [error, setError] = useState('')
  const [errorKind, setErrorKind] = useState<ErrorKind>('unknown')
  const [deleteError, setDeleteError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const [taxRes, catRes] = await Promise.all([catalogApi.getTaxRates(), catalogApi.getCategories()])
      setRates(taxRes.taxRates)
      setCategories(catRes.categories)
    } catch (err) {
      console.error('Failed to load tax rates:', err)
      setErrorKind(classifyError(err))
      setError('Не удалось загрузить налоговые ставки')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function handleDelete(taxRateId: number, name: string) {
    if (!window.confirm(`Удалить ставку «${name}»?`)) return
    setDeleteError('')
    try {
      await catalogApi.deleteTaxRate(taxRateId)
      await load()
    } catch (err) {
      console.error('Failed to delete tax rate:', err)
      setDeleteError(errorMessage(err, 'Не удалось удалить ставку'))
    }
  }

  const categoryName = (id?: number | null) => categories.find((c) => c.categoryId === id)?.name

  return (
    <div>
      {deleteError && <p className="mb-3 text-[12px] font-medium text-[color:var(--admin-danger)]">{deleteError}</p>}

      <Card scheme="admin" className="overflow-hidden">
        {rates === null && !error && <Loading scheme="admin" />}
        {error && <ErrorState scheme="admin" message={error} kind={errorKind} onRetry={load} />}
        {rates && rates.length === 0 && <EmptyState scheme="admin" icon={<PercentIcon width={22} height={22} />} title="Ставок пока нет" />}
        {rates && rates.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-[13px]">
              <thead>
                <tr className="border-b border-[color:var(--admin-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">
                  <th className="px-4 py-3">Название</th>
                  <th className="px-4 py-3">Ставка</th>
                  <th className="px-4 py-3">Категория</th>
                  <th className="px-4 py-3">Действует</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {rates.map((t) => (
                  <tr key={t.taxRateId} className="border-b border-[color:var(--admin-border)] last:border-0">
                    <td className="px-4 py-3 font-semibold text-[color:var(--admin-text)]">{t.name}</td>
                    <td className="px-4 py-3 font-[JetBrains_Mono,monospace] font-bold text-[color:var(--admin-accent)]">{t.percentage}%</td>
                    <td className="px-4 py-3 text-[color:var(--admin-text-secondary)]">{categoryName(t.categoryId) ?? 'Все категории'}</td>
                    <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[11.5px] text-[color:var(--admin-text-tertiary)]">
                      {t.effectiveFrom || t.effectiveTo ? `${fmtDate(t.effectiveFrom)} – ${fmtDate(t.effectiveTo)}` : 'бессрочно'}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button onClick={() => handleDelete(t.taxRateId, t.name)} className="grid h-7 w-7 place-items-center rounded text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-danger)]">
                        <TrashIcon width={13} height={13} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <CreateTaxRateModal open={createOpen} onClose={onCloseCreate} categories={categories} onCreated={load} />
    </div>
  )
}

/* ---------- page ---------- */

const TAB_OPTIONS = [
  { value: 'categories' as const, label: 'Категории', icon: <GridIcon width={15} height={15} /> },
  { value: 'brands' as const, label: 'Бренды', icon: <TagIcon width={15} height={15} /> },
  { value: 'tax-rates' as const, label: 'Налоговые ставки', icon: <PercentIcon width={15} height={15} /> },
]

export function AdminReferencePage() {
  const [params, setParams] = useSearchParams()
  const tabParam = params.get('tab')
  const tab: MainTab = tabParam === 'brands' || tabParam === 'tax-rates' ? tabParam : 'categories'
  const [createOpen, setCreateOpen] = useState(false)

  const addLabel = tab === 'categories' ? 'Добавить категорию' : tab === 'tax-rates' ? 'Добавить ставку' : null

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 flex items-center justify-between gap-3">
        <SectionSelect value={tab} onChange={(v) => setParams(v === 'categories' ? {} : { tab: v })} options={TAB_OPTIONS} ariaLabel="Раздел справочников" />
        {addLabel && <AddButton onClick={() => setCreateOpen(true)}>{addLabel}</AddButton>}
      </div>

      {tab === 'categories' && <CategoriesSection createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />}
      {tab === 'brands' && <BrandsSection />}
      {tab === 'tax-rates' && <TaxRatesSection createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />}
    </div>
  )
}
