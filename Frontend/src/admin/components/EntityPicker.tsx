import { useCallback, useEffect, useId, useMemo, useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useIsMobile } from '../../hooks/useMediaQuery'
import { lockBodyScroll, unlockBodyScroll } from '../../lib/scrollLock'
import { Loading } from './Loading'
import { SearchIcon, XIcon, ChevronDownIcon, AlertIcon } from './icons'

// Same scheme split as every other shared component here ('admin' for the StorePartner/Cashier
// cabinets, 'mod' for the platform Admin console) — see Select.tsx, this component's closest
// sibling, for the pattern this borrows its trigger/panel classes from almost verbatim.
const SCHEMES = {
  admin: {
    trigger: 'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)] focus-visible:border-[color:var(--admin-accent)]',
    placeholder: 'text-[color:var(--admin-text-tertiary)]',
    chevron: 'text-[color:var(--admin-text-tertiary)]',
    panel: 'border-[color:var(--admin-border)] bg-[color:var(--admin-card)] shadow-[var(--admin-shadow)]',
    searchField: 'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)] placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]',
    option: 'text-[color:var(--admin-text)]',
    optionActive: 'bg-[color:var(--admin-accent-soft)]',
    optionSelected: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    faint: 'text-[color:var(--admin-text-tertiary)]',
    accent: 'text-[color:var(--admin-accent)]',
    danger: 'text-[color:var(--admin-danger)]',
    chip: 'bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]',
    border: 'border-[color:var(--admin-border)]',
    retryBtn: 'bg-[color:var(--admin-accent)]',
    sheetBg: 'bg-[color:var(--admin-card)]',
  },
  mod: {
    trigger: 'border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] text-[color:var(--mod-text)] focus-visible:border-[color:var(--mod-accent)]',
    placeholder: 'text-[color:var(--mod-faint)]',
    chevron: 'text-[color:var(--mod-faint)]',
    panel: 'border-[color:var(--mod-border)] bg-[color:var(--mod-panel)] shadow-[var(--mod-shadow)]',
    searchField: 'border-[color:var(--mod-border)] bg-[color:var(--mod-panel2)] text-[color:var(--mod-text)] placeholder:text-[color:var(--mod-faint)] focus:border-[color:var(--mod-accent)]',
    option: 'text-[color:var(--mod-text)]',
    optionActive: 'bg-[color:var(--mod-accent-dim)]',
    optionSelected: 'bg-[color:var(--mod-accent-dim)] text-[color:var(--mod-accent2)]',
    faint: 'text-[color:var(--mod-faint)]',
    accent: 'text-[color:var(--mod-accent2)]',
    danger: 'text-[color:var(--mod-danger)]',
    chip: 'bg-[color:var(--mod-accent-dim)] text-[color:var(--mod-accent2)]',
    border: 'border-[color:var(--mod-border)]',
    retryBtn: 'bg-[color:var(--mod-accent)]',
    sheetBg: 'bg-[color:var(--mod-panel)]',
  },
} as const

type Scheme = keyof typeof SCHEMES

export interface EntityPickerPage<T> {
  items: T[]
  totalCount: number
}

interface BaseProps<T> {
  /** Fetches one page — called on open, on each debounced keystroke (skip resets to 0), and again
   *  (with a growing skip) as the list is scrolled. Search is server-side by design (ADMIN task:
   *  "запрос на сервер, а не фильтрация всего списка на клиенте"). */
  fetchPage: (args: { search: string; skip: number; take: number }) => Promise<EntityPickerPage<T>>
  getId: (item: T) => string | number
  /** Closed-trigger / chip label. */
  getLabel: (item: T) => string
  /** Rich row content inside the open panel — name + brand/barcode/category/price, etc. */
  renderOption: (item: T) => ReactNode
  placeholder?: string
  scheme?: Scheme
  disabled?: boolean
  ariaLabel: string
  /** Extra control rendered next to the search field inside the panel (ProductPicker's barcode-scan button). */
  headerAction?: ReactNode
  pageSize?: number
  searchDebounceMs?: number
  className?: string
  emptyHint?: string
}

interface SingleProps<T> extends BaseProps<T> {
  multiple?: false
  value: T | null
  onChange: (value: T | null) => void
}

interface MultiProps<T> extends BaseProps<T> {
  multiple: true
  value: T[]
  onChange: (value: T[]) => void
}

export type EntityPickerProps<T> = SingleProps<T> | MultiProps<T>

const FOCUSABLE_ROW = 'min-h-11'

export function EntityPicker<T>(props: EntityPickerProps<T>) {
  const { fetchPage, getId, getLabel, renderOption, placeholder = 'Найти…', scheme = 'admin', disabled, ariaLabel, headerAction, pageSize = 20, searchDebounceMs = 300, className = '', emptyHint } = props
  const t = SCHEMES[scheme]
  const isMobile = useIsMobile()
  const listId = useId()

  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [items, setItems] = useState<T[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState('')
  const [activeIndex, setActiveIndex] = useState(-1)

  const rootRef = useRef<HTMLDivElement>(null)
  const searchInputRef = useRef<HTMLInputElement>(null)
  const listRef = useRef<HTMLDivElement>(null)
  const sentinelRef = useRef<HTMLDivElement>(null)
  const requestIdRef = useRef(0)

  const selectedList: T[] = useMemo(
    () => (props.multiple ? props.value : props.value ? [props.value] : []),
    [props.multiple, props.value],
  )
  const selectedIds = useMemo(() => new Set(selectedList.map(getId)), [selectedList, getId])

  const loadPage = useCallback(
    async (searchTerm: string, skip: number, append: boolean) => {
      const requestId = ++requestIdRef.current
      if (append) setLoadingMore(true)
      else {
        setLoading(true)
        setActiveIndex(-1)
      }
      setError('')
      try {
        const page = await fetchPage({ search: searchTerm, skip, take: pageSize })
        if (requestId !== requestIdRef.current) return
        setItems((prev) => (append ? [...prev, ...page.items] : page.items))
        setTotalCount(page.totalCount)
      } catch {
        if (requestId === requestIdRef.current) setError('Не удалось загрузить список')
      } finally {
        if (requestId === requestIdRef.current) {
          setLoading(false)
          setLoadingMore(false)
        }
      }
    },
    [fetchPage, pageSize],
  )

  // Open -> first page. Debounced re-search on every keystroke while open.
  useEffect(() => {
    if (!open) return
    const timer = setTimeout(() => loadPage(query, 0, false), query === '' ? 0 : searchDebounceMs)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, query, searchDebounceMs])

  // Infinite scroll — loads the next page shortly before the sentinel actually reaches view.
  useEffect(() => {
    if (!open) return
    const sentinel = sentinelRef.current
    const root = listRef.current
    if (!sentinel || !root) return
    const io = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting || loading || loadingMore) return
        if (items.length >= totalCount) return
        loadPage(query, items.length, true)
      },
      { root, rootMargin: '160px' },
    )
    io.observe(sentinel)
    return () => io.disconnect()
  }, [open, items.length, totalCount, loading, loadingMore, query, loadPage])

  useEffect(() => {
    if (!open) return
    function onDocClick(e: MouseEvent) {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) closePanel()
    }
    document.addEventListener('mousedown', onDocClick)
    return () => document.removeEventListener('mousedown', onDocClick)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  useEffect(() => {
    if (open) requestAnimationFrame(() => searchInputRef.current?.focus())
  }, [open])

  useEffect(() => {
    if (!open || !isMobile) return
    lockBodyScroll()
    return unlockBodyScroll
  }, [open, isMobile])

  function openPanel() {
    if (disabled) return
    setQuery('')
    setItems([])
    setTotalCount(0)
    setOpen(true)
  }

  function closePanel() {
    setOpen(false)
  }

  function selectItem(item: T) {
    if (props.multiple) {
      const id = getId(item)
      const next = selectedIds.has(id) ? props.value.filter((v) => getId(v) !== id) : [...props.value, item]
      props.onChange(next)
    } else {
      props.onChange(item)
      closePanel()
    }
  }

  function clearValue(e: React.MouseEvent) {
    e.stopPropagation()
    if (props.multiple) props.onChange([])
    else props.onChange(null)
  }

  function removeChip(e: React.MouseEvent, id: string | number) {
    e.stopPropagation()
    if (props.multiple) props.onChange(props.value.filter((v) => getId(v) !== id))
  }

  function onSearchKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActiveIndex((i) => {
        const next = Math.min(i + 1, items.length - 1)
        scrollActiveIntoView(next)
        return next
      })
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActiveIndex((i) => {
        const next = Math.max(i - 1, 0)
        scrollActiveIntoView(next)
        return next
      })
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (activeIndex >= 0 && activeIndex < items.length) selectItem(items[activeIndex])
    } else if (e.key === 'Escape') {
      e.preventDefault()
      closePanel()
    } else if (e.key === 'Tab') {
      closePanel()
    }
  }

  function scrollActiveIntoView(index: number) {
    requestAnimationFrame(() => {
      const el = listRef.current?.querySelector<HTMLElement>(`[data-idx="${index}"]`)
      el?.scrollIntoView({ block: 'nearest' })
    })
  }

  const hasValue = selectedList.length > 0

  const panelBody = (
    <>
      <div className={`flex shrink-0 items-center gap-2 border-b p-3 ${c.border}`}>
        <div className={`relative flex-1`}>
          <SearchIcon width={15} height={15} className={`pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 ${c.faint}`} />
          <input
            ref={searchInputRef}
            role="combobox"
            aria-expanded={open}
            aria-controls={listId}
            aria-autocomplete="list"
            aria-activedescendant={activeIndex >= 0 && items[activeIndex] ? `${listId}-opt-${getId(items[activeIndex])}` : undefined}
            aria-label={ariaLabel}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={onSearchKeyDown}
            placeholder={placeholder}
            className={`w-full rounded-xl border py-2.5 pl-9 pr-3 text-[13.5px] outline-none ${c.searchField}`}
          />
        </div>
        {headerAction}
        {isMobile && (
          <button type="button" onClick={closePanel} aria-label="Закрыть" className={`grid h-10 w-10 shrink-0 place-items-center rounded-xl ${c.faint}`}>
            <XIcon width={17} height={17} />
          </button>
        )}
      </div>

      <div ref={listRef} id={listId} role="listbox" aria-multiselectable={props.multiple} className="min-h-0 flex-1 overflow-y-auto p-1.5">
        {loading && (
          <div className="py-2">
            <Loading scheme={scheme} label="Ищем…" />
          </div>
        )}

        {!loading && error && (
          <div className="flex flex-col items-center gap-3 px-4 py-10 text-center">
            <AlertIcon width={22} height={22} className={c.danger} />
            <p className={`text-[13px] ${c.faint}`}>{error}</p>
            <button type="button" onClick={() => loadPage(query, 0, false)} className={`rounded-lg px-3.5 py-2 text-[12.5px] font-semibold text-white ${c.retryBtn}`}>
              Повторить
            </button>
          </div>
        )}

        {!loading && !error && items.length === 0 && (
          <div className="px-4 py-10 text-center">
            <p className={`text-[13px] font-medium ${c.option}`}>Ничего не найдено</p>
            <p className={`mt-1 text-[12px] ${c.faint}`}>{emptyHint ?? 'Попробуйте изменить запрос — другое название, штрихкод или бренд'}</p>
          </div>
        )}

        {!loading &&
          !error &&
          items.map((item, idx) => {
            const id = getId(item)
            const selected = selectedIds.has(id)
            const active = idx === activeIndex
            return (
              <div
                key={id}
                id={`${listId}-opt-${id}`}
                data-idx={idx}
                role="option"
                aria-selected={selected}
                onMouseEnter={() => setActiveIndex(idx)}
                onClick={() => selectItem(item)}
                className={`flex cursor-pointer items-center gap-2 rounded-lg px-2.5 py-2 ${FOCUSABLE_ROW} ${
                  selected ? c.optionSelected : active ? c.optionActive : c.option
                }`}
              >
                <div className="min-w-0 flex-1">{renderOption(item)}</div>
              </div>
            )
          })}

        {!loading && !error && items.length < totalCount && <div ref={sentinelRef} className="h-1" />}
        {loadingMore && (
          <div className="py-2">
            <Loading scheme={scheme} label="Загружаем ещё…" />
          </div>
        )}
      </div>
    </>
  )

  return (
    <div ref={rootRef} className={`relative ${className}`}>
      <button
        type="button"
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => (open ? closePanel() : openPanel())}
        className={`flex min-h-[44px] w-full items-center gap-2 rounded-xl border py-2 pl-3.5 pr-2 text-left outline-none transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${c.trigger}`}
      >
        <div className="flex min-w-0 flex-1 flex-wrap items-center gap-1.5">
          {!hasValue && <span className={`truncate text-[13px] ${c.placeholder}`}>{placeholder}</span>}
          {!props.multiple && props.value && <span className="truncate text-[13px] font-medium">{getLabel(props.value)}</span>}
          {props.multiple &&
            props.value.map((v) => (
              <span key={getId(v)} className={`flex items-center gap-1 rounded-full py-1 pl-2.5 pr-1.5 text-[12px] font-semibold ${c.chip}`}>
                <span className="max-w-[160px] truncate">{getLabel(v)}</span>
                <span
                  role="button"
                  tabIndex={-1}
                  onClick={(e) => removeChip(e, getId(v))}
                  aria-label={`Убрать ${getLabel(v)}`}
                  className="grid h-4 w-4 shrink-0 place-items-center rounded-full hover:bg-black/10"
                >
                  <XIcon width={10} height={10} />
                </span>
              </span>
            ))}
        </div>
        {hasValue && (
          <span role="button" tabIndex={-1} onClick={clearValue} aria-label="Очистить" className={`grid h-6 w-6 shrink-0 place-items-center rounded-full ${c.faint} hover:bg-black/5`}>
            <XIcon width={13} height={13} />
          </span>
        )}
        <ChevronDownIcon width={14} height={14} className={`shrink-0 transition-transform ${c.chevron} ${open ? 'rotate-180' : ''}`} />
      </button>

      <AnimatePresence>
        {open && !isMobile && (
          <motion.div
            initial={{ opacity: 0, y: -6, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.15, ease: 'easeOut' }}
            className={`absolute z-30 mt-1.5 flex max-h-[70vh] w-full min-w-[320px] flex-col overflow-hidden rounded-xl border ${c.panel}`}
          >
            {panelBody}
          </motion.div>
        )}
      </AnimatePresence>

      {isMobile &&
        createPortal(
          <AnimatePresence>
            {open && (
              <motion.div
                initial={{ opacity: 0, y: '100%' }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: '100%' }}
                transition={{ type: 'spring', stiffness: 380, damping: 36 }}
                className={`${scheme === 'admin' ? 'admin-shell' : 'mod-shell'} fixed inset-0 z-100 flex flex-col ${c.sheetBg}`}
                role="dialog"
                aria-modal="true"
                aria-label={ariaLabel}
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
