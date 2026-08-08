import { useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { AnimatePresence, motion } from 'framer-motion'
import clsx from 'clsx'
import { useIsMobile } from '../../hooks/useMediaQuery'
import { lockBodyScroll, unlockBodyScroll } from '../../lib/scrollLock'
import { useFloatingPosition } from '../../lib/useFloatingPosition'
import { catalogApi, type Category } from '../../lib/api'
import { SearchIcon, XIcon, ChevronDownIcon } from './icons'

const SCHEMES = {
  admin: {
    trigger: 'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)] focus-visible:border-[color:var(--admin-accent)]',
    placeholder: 'text-[color:var(--admin-text-tertiary)]',
    chevron: 'text-[color:var(--admin-text-tertiary)]',
    // Opaque --admin-sidebar, not the translucent --admin-card "glass" tone -- this panel portals
    // to document.body and floats over arbitrary page content (a table, a form), so it needs a
    // surface that reads as solid regardless of what's behind it.
    panel: 'border-[color:var(--admin-border)] bg-[color:var(--admin-sidebar)] shadow-[var(--admin-shadow)]',
    searchField: 'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)] placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]',
    row: 'text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]',
    rowSelected: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    faint: 'text-[color:var(--admin-text-tertiary)]',
    border: 'border-[color:var(--admin-border)]',
    // Opaque page background, not the translucent --admin-card "glass" tone -- see SectionSelect.tsx
    // for why: a full-screen sheet needs a solid surface, and --admin-card at ~4.5% alpha in dark
    // mode let the page underneath show straight through it.
    sheetBg: 'bg-[color:var(--admin-content)]',
  },
} as const

interface CategoryPickerProps {
  value: Category | null
  onChange: (value: Category | null) => void
  scheme?: keyof typeof SCHEMES
  placeholder?: string
  disabled?: boolean
  className?: string
}

/** Searchable category TREE, not a flat list — parent/child structure matters for browsing, and a
 *  cascade (CategoryPicker -> ProductPicker) is only worth having if picking the category is
 *  actually faster than scrolling a flat alphabetical dump. Always optional at every call site;
 *  this component itself has no notion of "required". */
export function CategoryPicker({ value, onChange, scheme = 'admin', placeholder = 'Все категории', disabled, className = '' }: CategoryPickerProps) {
  const t = SCHEMES[scheme]
  const isMobile = useIsMobile()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [categories, setCategories] = useState<Category[] | null>(null)
  const [expanded, setExpanded] = useState<Set<number>>(new Set())
  const rootRef = useRef<HTMLDivElement>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const pos = useFloatingPosition(rootRef, open && !isMobile)

  useEffect(() => {
    if (!open || categories !== null) return
    catalogApi.getCategories().then((res) => setCategories(res.categories)).catch(() => setCategories([]))
  }, [open, categories])

  useEffect(() => {
    if (open) requestAnimationFrame(() => searchRef.current?.focus())
  }, [open])

  useEffect(() => {
    if (!open) return
    function onDocClick(e: MouseEvent) {
      const target = e.target as Node
      if (rootRef.current?.contains(target) || panelRef.current?.contains(target)) return
      setOpen(false)
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onDocClick)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDocClick)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  useEffect(() => {
    if (!open || !isMobile) return
    lockBodyScroll()
    return unlockBodyScroll
  }, [open, isMobile])

  // Expand the path down to whatever's currently selected, so opening the picker doesn't hide
  // the answer behind three collapsed parents.
  useEffect(() => {
    if (!open || !value || !categories) return
    const byId = new Map(categories.map((c) => [c.categoryId, c]))
    const ancestors = new Set<number>()
    let cur = byId.get(value.categoryId)
    while (cur?.parentCategoryId) {
      ancestors.add(cur.parentCategoryId)
      cur = byId.get(cur.parentCategoryId)
    }
    setExpanded((prev) => new Set([...prev, ...ancestors]))
  }, [open, value, categories])

  const term = query.trim().toLowerCase()
  const { childrenByParent, visibleIds } = useMemo(() => {
    const list = categories ?? []
    const byParent = new Map<number | null, Category[]>()
    for (const c of list) {
      const key = c.parentCategoryId ?? null
      if (!byParent.has(key)) byParent.set(key, [])
      byParent.get(key)!.push(c)
    }
    for (const arr of byParent.values()) arr.sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name))

    if (!term) return { childrenByParent: byParent, visibleIds: null as Set<number> | null }

    const byId = new Map(list.map((c) => [c.categoryId, c]))
    const matches = list.filter((c) => c.name.toLowerCase().includes(term))
    const visible = new Set<number>()
    for (const m of matches) {
      let cur: Category | undefined = m
      while (cur) {
        visible.add(cur.categoryId)
        cur = cur.parentCategoryId ? byId.get(cur.parentCategoryId) : undefined
      }
    }
    return { childrenByParent: byParent, visibleIds: visible }
  }, [categories, term])

  function toggleExpand(id: number) {
    setExpanded((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  function select(cat: Category | null) {
    onChange(cat)
    setOpen(false)
  }

  function renderNode(cat: Category, depth: number): React.ReactNode {
    if (visibleIds && !visibleIds.has(cat.categoryId)) return null
    const children = childrenByParent.get(cat.categoryId) ?? []
    const isExpanded = term ? true : expanded.has(cat.categoryId)
    const hasChildren = children.length > 0
    const selected = value?.categoryId === cat.categoryId
    return (
      <div key={cat.categoryId}>
        <div
          role="option"
          aria-selected={selected}
          className={clsx('flex min-h-10 cursor-pointer items-center gap-1.5 rounded-lg pr-2.5', selected ? t.rowSelected : t.row)}
          style={{ paddingLeft: 8 + depth * 18 }}
          onClick={() => select(cat)}
        >
          {hasChildren ? (
            <span
              role="button"
              tabIndex={-1}
              onClick={(e) => {
                e.stopPropagation()
                toggleExpand(cat.categoryId)
              }}
              className={clsx('grid h-6 w-6 shrink-0 place-items-center rounded-md', t.faint)}
            >
              <ChevronDownIcon width={13} height={13} className={clsx('transition-transform', !isExpanded && '-rotate-90')} />
            </span>
          ) : (
            <span className="w-6 shrink-0" />
          )}
          <span className="truncate py-2 text-[13px] font-medium">{cat.name}</span>
        </div>
        {hasChildren && isExpanded && children.map((child) => renderNode(child, depth + 1))}
      </div>
    )
  }

  const topLevel = childrenByParent.get(null) ?? []

  const panelBody = (
    <>
      <div className={clsx('flex shrink-0 items-center gap-2 border-b p-3', t.border)}>
        <div className="relative flex-1">
          <SearchIcon width={15} height={15} className={clsx('pointer-events-none absolute left-3 top-1/2 -translate-y-1/2', t.faint)} />
          <input
            ref={searchRef}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Найти категорию…"
            aria-label="Поиск категории"
            className={clsx('w-full rounded-xl border py-2.5 pl-9 pr-3 text-[13.5px] outline-none', t.searchField)}
          />
        </div>
        {isMobile && (
          <button type="button" onClick={() => setOpen(false)} aria-label="Закрыть" className={clsx('grid h-10 w-10 shrink-0 place-items-center rounded-xl', t.faint)}>
            <XIcon width={17} height={17} />
          </button>
        )}
      </div>
      <div role="listbox" className="min-h-0 flex-1 overflow-y-auto p-1.5">
        {categories === null && <div className={clsx('px-4 py-8 text-center text-[13px]', t.faint)}>Загрузка…</div>}
        {categories !== null && (
          <div
            role="option"
            aria-selected={!value}
            onClick={() => select(null)}
            className={clsx('flex min-h-10 cursor-pointer items-center rounded-lg px-2.5 text-[13px] font-medium', !value ? t.rowSelected : t.row)}
          >
            Все категории
          </div>
        )}
        {categories !== null && topLevel.length === 0 && (
          <div className={clsx('px-4 py-8 text-center text-[13px]', t.faint)}>Категорий пока нет</div>
        )}
        {categories !== null && term && visibleIds?.size === 0 && (
          <div className={clsx('px-4 py-8 text-center text-[13px]', t.faint)}>Ничего не найдено</div>
        )}
        {categories !== null && topLevel.map((cat) => renderNode(cat, 0))}
      </div>
    </>
  )

  return (
    <div ref={rootRef} className={clsx('relative', className)}>
      <button
        type="button"
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
        className={clsx(
          'flex min-h-[44px] w-full items-center gap-2 rounded-xl border py-2 pl-3.5 pr-2 text-left outline-none transition-colors disabled:cursor-not-allowed disabled:opacity-50',
          t.trigger,
        )}
      >
        <span className={clsx('min-w-0 flex-1 truncate text-[13px]', !value && t.placeholder, value && 'font-medium')}>
          {value ? value.name : placeholder}
        </span>
        {value && (
          <span
            role="button"
            tabIndex={-1}
            onClick={(e) => {
              e.stopPropagation()
              onChange(null)
            }}
            aria-label="Очистить"
            className={clsx('grid h-6 w-6 shrink-0 place-items-center rounded-full hover:bg-[color:var(--admin-hover)]', t.faint)}
          >
            <XIcon width={13} height={13} />
          </span>
        )}
        <ChevronDownIcon width={14} height={14} className={clsx('shrink-0 transition-transform', t.chevron, open && 'rotate-180')} />
      </button>

      {!isMobile &&
        createPortal(
          <AnimatePresence>
            {open && pos && (
              <motion.div
                ref={panelRef}
                initial={{ opacity: 0, y: -6, scale: 0.98 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: -6, scale: 0.98 }}
                transition={{ duration: 0.15, ease: 'easeOut' }}
                style={{ position: 'fixed', left: pos.left, width: Math.max(pos.width, 280), top: pos.top, bottom: pos.bottom, maxHeight: pos.maxHeight }}
                // admin-shell: portaled to document.body, outside the page's own .admin-shell
                // wrapper, so without re-declaring the scope here every --admin-* custom property
                // below (including t.panel's background) is undefined at this node -- the panel
                // rendered as a plain white box with black text (the browser's own UA defaults)
                // instead of the current theme's surface.
                className={clsx('admin-shell z-popover flex flex-col overflow-hidden rounded-xl border', t.panel)}
              >
                {panelBody}
              </motion.div>
            )}
          </AnimatePresence>,
          document.body,
        )}

      {isMobile &&
        createPortal(
          <AnimatePresence>
            {open && (
              <motion.div
                initial={{ opacity: 0, y: '100%' }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: '100%' }}
                transition={{ type: 'spring', stiffness: 380, damping: 36 }}
                className={clsx('admin-shell', 'fixed inset-0 z-modal flex flex-col', t.sheetBg)}
                role="dialog"
                aria-modal="true"
                aria-label="Выбор категории"
              >
                {panelBody}
              </motion.div>
            )}
          </AnimatePresence>,
          document.body,
        )}
    </div>
  )
}
