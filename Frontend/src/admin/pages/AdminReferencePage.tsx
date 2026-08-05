import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { Loading } from '../components/Loading'
import { ErrorState } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { Select } from '../components/Select'
import { Badge } from '../components/Badge'
import { ChevronDownIcon, TagIcon, PercentIcon, SearchIcon, EditIcon, TrashIcon, GridIcon } from '../components/icons'
import {
  catalogApi,
  ApiError,
  type Category,
  type Brand,
  type DuplicateBrandGroup,
  type TaxRate,
} from '../../lib/api'

type MainTab = 'categories' | 'brands' | 'tax-rates'

function fmtDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })
}

/* ---------- Категории ---------- */
// A drag-and-drop tree is a lot of code for a "rare-edit tool" (ADMIN_PROMPT.md §2.8) --
// reassigning the parent via a dropdown + a plain order number delivers the same "move/reorder"
// capability with far less surface to get wrong.

function CategoryEditForm({ category, all, onSaved, onCancel }: { category: Category; all: Category[]; onSaved: () => void; onCancel: () => void }) {
  const [name, setName] = useState(category.name)
  const [parentId, setParentId] = useState(category.parentCategoryId ? String(category.parentCategoryId) : '')
  const [displayOrder, setDisplayOrder] = useState(String(category.displayOrder))
  const [isHidden, setIsHidden] = useState(category.isHidden)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function handleSave() {
    if (!name.trim() || busy) return
    setBusy(true)
    setError('')
    try {
      const res = await catalogApi.updateCategory(
        category.categoryId, name.trim(), parentId ? Number(parentId) : undefined, Number(displayOrder) || 0, isHidden,
      )
      if (res.outcome !== 'Updated') {
        setError(
          res.outcome === 'ParentCategoryNotFound'
            ? 'Родительская категория не найдена'
            : res.outcome === 'SelfReference'
              ? 'Категория не может быть родителем самой себя'
              : res.outcome,
        )
        return
      }
      onSaved()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить категорию')
    } finally {
      setBusy(false)
    }
  }

  async function handleDelete() {
    setBusy(true)
    setError('')
    try {
      const res = await catalogApi.deleteCategory(category.categoryId)
      if (res.outcome !== 'Deleted') {
        setError('outcome' in res ? String(res.outcome) : 'Не удалось удалить')
        return
      }
      onSaved()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Категория используется товарами или подкатегориями — сначала перенесите их.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="rounded-lg bg-[color:var(--mod-panel2)] p-3">
      <div className="mb-2 flex flex-wrap gap-2">
        <input value={name} onChange={(e) => setName(e.target.value)} className="min-w-[160px] flex-1 rounded-lg border border-[color:var(--mod-border)] bg-[color:var(--mod-panel)] px-2.5 py-1.5 text-[12.5px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <Select
          scheme="mod"
          size="sm"
          value={parentId}
          onChange={setParentId}
          placeholder="Без родителя (верхний уровень)"
          className="min-w-[220px]"
          options={all.filter((c) => c.categoryId !== category.categoryId).map((c) => ({ value: String(c.categoryId), label: c.name }))}
        />
        <input
          value={displayOrder}
          onChange={(e) => setDisplayOrder(e.target.value.replace(/[^0-9]/g, ''))}
          type="number"
          className="w-20 rounded-lg border border-[color:var(--mod-border)] bg-[color:var(--mod-panel)] px-2.5 py-1.5 text-[12.5px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
          title="Порядок"
        />
      </div>
      <label className="mb-2 flex items-center gap-2 text-[12px] font-semibold text-[color:var(--mod-text)]">
        <input type="checkbox" checked={isHidden} onChange={(e) => setIsHidden(e.target.checked)} className="h-3.5 w-3.5 accent-[color:var(--mod-accent)]" />
        Скрыта из каталога
      </label>
      {error && <p className="mb-2 text-[11.5px] font-medium text-[color:var(--mod-danger)]">{error}</p>}
      <div className="flex gap-2">
        <button onClick={handleSave} disabled={busy} className="rounded-lg bg-[color:var(--mod-accent)] px-3 py-1.5 text-[11.5px] font-bold text-white disabled:opacity-50">
          Сохранить
        </button>
        <button onClick={handleDelete} disabled={busy} className="rounded-lg border border-[color:var(--mod-danger)] px-3 py-1.5 text-[11.5px] font-bold text-[color:var(--mod-danger)] disabled:opacity-50">
          Удалить
        </button>
        <button onClick={onCancel} className="rounded-lg border border-[color:var(--mod-border)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--mod-text)]">
          Отмена
        </button>
      </div>
    </div>
  )
}

function CategoryNode({ node, depth, byParent, all, editingId, setEditingId, onSaved }: {
  node: Category
  depth: number
  byParent: Map<number | undefined, Category[]>
  all: Category[]
  editingId: number | null
  setEditingId: (id: number | null) => void
  onSaved: () => void
}) {
  const [expanded, setExpanded] = useState(depth === 0)
  const children = byParent.get(node.categoryId) ?? []
  const isEditing = editingId === node.categoryId

  return (
    <div>
      <div className="flex items-center gap-1.5 rounded-lg px-2 py-1.5 hover:bg-[color:var(--mod-panel2)]" style={{ paddingLeft: 8 + depth * 20 }}>
        {children.length > 0 ? (
          <button onClick={() => setExpanded((v) => !v)} className="grid h-5 w-5 shrink-0 place-items-center text-[color:var(--mod-faint)]">
            <ChevronDownIcon width={11} height={11} className={expanded ? '' : '-rotate-90'} />
          </button>
        ) : (
          <span className="w-5 shrink-0" />
        )}
        <span className={`flex-1 truncate text-[12.5px] ${node.isHidden ? 'text-[color:var(--mod-faint)] line-through' : 'text-[color:var(--mod-text)]'} ${depth === 0 ? 'font-bold' : 'font-medium'}`}>
          {node.name}
        </span>
        {node.isHidden && <Badge scheme="mod" variant="neutral" size="sm">скрыта</Badge>}
        <button onClick={() => setEditingId(isEditing ? null : node.categoryId)} className="grid h-6 w-6 shrink-0 place-items-center rounded text-[color:var(--mod-faint)] hover:text-[color:var(--mod-text)]">
          <EditIcon width={13} height={13} />
        </button>
      </div>
      {isEditing && (
        <div style={{ marginLeft: 8 + depth * 20 }} className="my-1">
          <CategoryEditForm category={node} all={all} onSaved={() => { setEditingId(null); onSaved() }} onCancel={() => setEditingId(null)} />
        </div>
      )}
      {expanded && children.map((c) => (
        <CategoryNode key={c.categoryId} node={c} depth={depth + 1} byParent={byParent} all={all} editingId={editingId} setEditingId={setEditingId} onSaved={onSaved} />
      ))}
    </div>
  )
}

function CategoriesSection() {
  const [categories, setCategories] = useState<Category[] | null>(null)
  const [error, setError] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [newName, setNewName] = useState('')
  const [newParentId, setNewParentId] = useState('')
  const [createBusy, setCreateBusy] = useState(false)
  const [createError, setCreateError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      setCategories((await catalogApi.getCategories()).categories)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить категории')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const byParent = useMemo(() => {
    const map = new Map<number | undefined, Category[]>()
    for (const c of categories ?? []) {
      const key = c.parentCategoryId
      if (!map.has(key)) map.set(key, [])
      map.get(key)!.push(c)
    }
    for (const list of map.values()) list.sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name))
    return map
  }, [categories])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    if (!newName.trim() || createBusy) return
    setCreateBusy(true)
    setCreateError('')
    try {
      const res = await catalogApi.createCategory(newName.trim(), newParentId ? Number(newParentId) : undefined)
      if (res.outcome === 'ParentCategoryNotFound') {
        setCreateError('Родительская категория не найдена')
        return
      }
      setNewName('')
      setNewParentId('')
      await load()
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : 'Не удалось создать категорию')
    } finally {
      setCreateBusy(false)
    }
  }

  const roots = byParent.get(undefined) ?? []

  return (
    <div>
      <form onSubmit={handleCreate} className="mb-4 flex flex-wrap gap-2">
        <input
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="Название новой категории"
          className="min-w-[180px] flex-1 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
        />
        <Select
          scheme="mod"
          value={newParentId}
          onChange={setNewParentId}
          placeholder="Без родителя"
          className="min-w-[200px]"
          options={(categories ?? []).map((c) => ({ value: String(c.categoryId), label: c.name }))}
        />
        <button type="submit" disabled={createBusy || !newName.trim()} className="rounded-xl bg-[color:var(--mod-accent)] px-4 py-2.5 text-[13px] font-bold text-white disabled:opacity-50">
          Добавить
        </button>
      </form>
      {createError && <p className="mb-3 text-[12px] font-medium text-[color:var(--mod-danger)]">{createError}</p>}

      <Card scheme="mod" className="p-3">
        {categories === null && !error && <Loading scheme="mod" />}
        {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
        {categories && roots.length === 0 && <EmptyState scheme="mod" title="Категорий нет" />}
        {roots.map((r) => (
          <CategoryNode key={r.categoryId} node={r} depth={0} byParent={byParent} all={categories ?? []} editingId={editingId} setEditingId={setEditingId} onSaved={load} />
        ))}
      </Card>
    </div>
  )
}

/* ---------- Бренды ---------- */

function BrandsSection() {
  const [brands, setBrands] = useState<Brand[] | null>(null)
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [duplicates, setDuplicates] = useState<DuplicateBrandGroup[] | null>(null)
  const [dupError, setDupError] = useState('')
  const [renamingId, setRenamingId] = useState<number | null>(null)
  const [renameValue, setRenameValue] = useState('')

  const load = useCallback(async (term: string) => {
    setError('')
    try {
      setBrands((await catalogApi.getBrands(term || undefined)).brands)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить бренды')
    }
  }, [])

  const loadDuplicates = useCallback(async () => {
    setDupError('')
    try {
      setDuplicates((await catalogApi.getBrandDuplicateCandidates()).groups)
    } catch (err) {
      setDupError(err instanceof ApiError ? err.message : 'Не удалось найти дубликаты')
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
      setError(err instanceof ApiError ? err.message : 'Не удалось переименовать бренд')
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <Card scheme="mod" className="p-5">
        <div className="mb-3 flex items-center gap-2 text-[13.5px] font-bold text-[color:var(--mod-text)]">
          <TagIcon width={16} height={16} />
          Похожие бренды — объединение дубликатов
        </div>
        {duplicates === null && !dupError && <Loading scheme="mod" />}
        {dupError && <ErrorState scheme="mod" message={dupError} onRetry={loadDuplicates} />}
        {duplicates && duplicates.length === 0 && <p className="text-[12.5px] text-[color:var(--mod-faint)]">Явных дубликатов не найдено.</p>}
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
          <SearchIcon width={15} height={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[color:var(--mod-faint)]" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Поиск бренда…"
            className="w-full rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] py-2.5 pl-9 pr-3.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
          />
        </div>
        <Card scheme="mod" className="overflow-hidden">
          {brands === null && !error && <Loading scheme="mod" />}
          {error && <ErrorState scheme="mod" message={error} onRetry={() => load(search)} />}
          {brands && brands.length === 0 && <EmptyState scheme="mod" title="Брендов не найдено" />}
          {brands && brands.length > 0 && (
            <div className="flex flex-col">
              {brands.map((b) => (
                <div key={b.brandId} className="flex items-center justify-between gap-2 border-b border-[color:var(--mod-border)] px-4 py-2.5 last:border-0">
                  {renamingId === b.brandId ? (
                    <div className="flex flex-1 gap-2">
                      <input
                        value={renameValue}
                        onChange={(e) => setRenameValue(e.target.value)}
                        autoFocus
                        className="flex-1 rounded-lg border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-2.5 py-1.5 text-[12.5px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]"
                      />
                      <button onClick={() => handleRename(b.brandId)} className="rounded-lg bg-[color:var(--mod-accent)] px-3 py-1.5 text-[11.5px] font-bold text-white">
                        ОК
                      </button>
                      <button onClick={() => setRenamingId(null)} className="rounded-lg border border-[color:var(--mod-border)] px-3 py-1.5 text-[11.5px] font-semibold text-[color:var(--mod-text)]">
                        Отмена
                      </button>
                    </div>
                  ) : (
                    <>
                      <span className="text-[13px] font-semibold text-[color:var(--mod-text)]">{b.name}</span>
                      <div className="flex shrink-0 items-center gap-3">
                        <span className="font-[JetBrains_Mono,monospace] text-[11.5px] text-[color:var(--mod-faint)]">{b.productCount} товаров</span>
                        <button
                          onClick={() => { setRenamingId(b.brandId); setRenameValue(b.name) }}
                          className="grid h-7 w-7 place-items-center rounded text-[color:var(--mod-faint)] hover:text-[color:var(--mod-text)]"
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
      setError(err instanceof ApiError ? err.message : 'Не удалось объединить бренды')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="rounded-xl bg-[color:var(--mod-panel2)] p-3.5">
      <div className="mb-2 flex flex-wrap gap-1.5">
        {group.brands.map((b) => (
          <label key={b.brandId} className="flex items-center gap-1.5 rounded-full border border-[color:var(--mod-border)] bg-[color:var(--mod-panel)] px-2.5 py-1 text-[12px]">
            <input type="radio" name={group.normalizedKey} checked={String(b.brandId) === targetId} onChange={() => setTargetId(String(b.brandId))} className="accent-[color:var(--mod-accent)]" />
            <span className="font-semibold text-[color:var(--mod-text)]">{b.name}</span>
            <span className="text-[color:var(--mod-faint)]">({b.productCount})</span>
          </label>
        ))}
      </div>
      {error && <p className="mb-2 text-[11.5px] font-medium text-[color:var(--mod-danger)]">{error}</p>}
      <div className="flex items-center justify-between gap-2">
        <span className="text-[11.5px] text-[color:var(--mod-faint)]">
          Все {totalProducts} товаров перейдут выбранному бренду, остальные {group.brands.length - 1} исчезнут.
        </span>
        <button onClick={handleMerge} disabled={busy} className="shrink-0 rounded-lg bg-[color:var(--mod-accent)] px-3.5 py-1.5 text-[11.5px] font-bold text-white disabled:opacity-50">
          {busy ? 'Секунду…' : 'Объединить'}
        </button>
      </div>
    </div>
  )
}

/* ---------- Налоговые ставки ---------- */

function TaxRatesSection() {
  const [rates, setRates] = useState<TaxRate[] | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [percentage, setPercentage] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [effectiveFrom, setEffectiveFrom] = useState('')
  const [effectiveTo, setEffectiveTo] = useState('')
  const [busy, setBusy] = useState(false)
  const [formError, setFormError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const [taxRes, catRes] = await Promise.all([catalogApi.getTaxRates(), catalogApi.getCategories()])
      setRates(taxRes.taxRates)
      setCategories(catRes.categories)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить налоговые ставки')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    const pct = Number(percentage)
    if (!name.trim() || Number.isNaN(pct) || busy) return
    setBusy(true)
    setFormError('')
    try {
      await catalogApi.createTaxRate(name.trim(), pct, categoryId ? Number(categoryId) : undefined, effectiveFrom || undefined, effectiveTo || undefined)
      setName('')
      setPercentage('')
      setCategoryId('')
      setEffectiveFrom('')
      setEffectiveTo('')
      await load()
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Не удалось создать ставку')
    } finally {
      setBusy(false)
    }
  }

  async function handleDelete(taxRateId: number) {
    try {
      await catalogApi.deleteTaxRate(taxRateId)
      await load()
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Не удалось удалить ставку')
    }
  }

  const categoryName = (id?: number) => categories.find((c) => c.categoryId === id)?.name

  return (
    <div>
      <form onSubmit={handleCreate} className="mb-4 flex flex-wrap gap-2">
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Название" className="min-w-[140px] flex-1 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3.5 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <input value={percentage} onChange={(e) => setPercentage(e.target.value)} type="number" min={0} max={100} step="0.01" placeholder="%" className="w-20 rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <Select scheme="mod" value={categoryId} onChange={setCategoryId} placeholder="Все категории" className="min-w-[170px]" options={categories.map((c) => ({ value: String(c.categoryId), label: c.name }))} />
        <input type="date" value={effectiveFrom} onChange={(e) => setEffectiveFrom(e.target.value)} title="Действует с" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <input type="date" value={effectiveTo} onChange={(e) => setEffectiveTo(e.target.value)} title="Действует по" className="rounded-xl border border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] px-3 py-2.5 text-[13px] text-[color:var(--mod-text)] outline-none focus:border-[color:var(--mod-accent)]" />
        <button type="submit" disabled={busy || !name.trim() || percentage === ''} className="rounded-xl bg-[color:var(--mod-accent)] px-4 py-2.5 text-[13px] font-bold text-white disabled:opacity-50">
          Добавить
        </button>
      </form>
      {formError && <p className="mb-3 text-[12px] font-medium text-[color:var(--mod-danger)]">{formError}</p>}

      <Card scheme="mod" className="overflow-hidden">
        {rates === null && !error && <Loading scheme="mod" />}
        {error && <ErrorState scheme="mod" message={error} onRetry={load} />}
        {rates && rates.length === 0 && <EmptyState scheme="mod" icon={<PercentIcon width={22} height={22} />} title="Ставок пока нет" />}
        {rates && rates.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-[13px]">
              <thead>
                <tr className="border-b border-[color:var(--mod-border)] text-left text-[11px] font-bold uppercase tracking-wide text-[color:var(--mod-faint)]">
                  <th className="px-4 py-3">Название</th>
                  <th className="px-4 py-3">Ставка</th>
                  <th className="px-4 py-3">Категория</th>
                  <th className="px-4 py-3">Действует</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {rates.map((t) => (
                  <tr key={t.taxRateId} className="border-b border-[color:var(--mod-border)] last:border-0">
                    <td className="px-4 py-3 font-semibold text-[color:var(--mod-text)]">{t.name}</td>
                    <td className="px-4 py-3 font-[JetBrains_Mono,monospace] font-bold text-[color:var(--mod-accent2)]">{t.percentage}%</td>
                    <td className="px-4 py-3 text-[color:var(--mod-muted)]">{categoryName(t.categoryId) ?? 'Все категории'}</td>
                    <td className="px-4 py-3 font-[JetBrains_Mono,monospace] text-[11.5px] text-[color:var(--mod-faint)]">
                      {t.effectiveFrom || t.effectiveTo ? `${fmtDate(t.effectiveFrom)} – ${fmtDate(t.effectiveTo)}` : 'бессрочно'}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button onClick={() => handleDelete(t.taxRateId)} className="grid h-7 w-7 place-items-center rounded text-[color:var(--mod-faint)] hover:text-[color:var(--mod-danger)]">
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
    </div>
  )
}

/* ---------- page ---------- */

export function AdminReferencePage() {
  const [params, setParams] = useSearchParams()
  const tabParam = params.get('tab')
  const tab: MainTab = tabParam === 'brands' || tabParam === 'tax-rates' ? tabParam : 'categories'

  return (
    <div style={{ animation: 'mod-fade-in .3s ease' }}>
      <div className="mb-4 flex gap-1 rounded-lg bg-[color:var(--mod-panel2)] p-1" style={{ width: 'fit-content' }}>
        {(
          [
            ['categories', 'Категории', GridIcon],
            ['brands', 'Бренды', TagIcon],
            ['tax-rates', 'Налоговые ставки', PercentIcon],
          ] as [MainTab, string, typeof GridIcon][]
        ).map(([id, label, Icon]) => (
          <button
            key={id}
            onClick={() => setParams(id === 'categories' ? {} : { tab: id })}
            className={`flex items-center gap-1.5 rounded-md px-4 py-2 text-[12.5px] font-bold transition-colors ${
              tab === id ? 'bg-[color:var(--mod-accent)] text-white' : 'text-[color:var(--mod-muted)] hover:text-[color:var(--mod-text)]'
            }`}
          >
            <Icon width={14} height={14} />
            {label}
          </button>
        ))}
      </div>

      {tab === 'categories' && <CategoriesSection />}
      {tab === 'brands' && <BrandsSection />}
      {tab === 'tax-rates' && <TaxRatesSection />}
    </div>
  )
}
