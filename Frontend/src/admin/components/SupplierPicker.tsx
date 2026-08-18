import { useRef } from 'react'
import { EntityPicker, type EntityPickerProps } from './EntityPicker'
import { suppliersApi, type Supplier } from '../../lib/api'

function SupplierRow({ item }: { item: Supplier }) {
  const meta = [item.contactPhone, item.contactEmail].filter(Boolean).join(' · ')
  return (
    <div className="min-w-0">
      <div className="truncate text-[13.5px] font-medium">{item.name}</div>
      {meta && <div className="truncate text-[11.5px] text-[color:var(--admin-text-tertiary)]">{meta}</div>}
    </div>
  )
}

interface SupplierPickerProps {
  storeId: number
  value: Supplier | null
  onChange: (value: Supplier | null) => void
  scheme?: 'admin'
  placeholder?: string
  disabled?: boolean
  className?: string
}

/**
 * Searchable supplier select. A store's suppliers are always a short, store-scoped list (unlike
 * products/stores, there's no dedicated paginated search endpoint for them, same reasoning
 * CategoryPicker.tsx documents for its own tree) -- fetched once via the existing GET
 * /api/suppliers (already owner/employee-checked), searched client-side. Replaces every raw
 * supplier-id input/select-by-number in the project.
 */
export function SupplierPicker({ storeId, value, onChange, scheme = 'admin', placeholder = 'Название поставщика…', disabled, className }: SupplierPickerProps) {
  const cacheRef = useRef<{ storeId: number; suppliers: Supplier[] } | null>(null)

  async function fetchPage({ search, skip, take }: { search: string; skip: number; take: number }) {
    if (cacheRef.current?.storeId !== storeId) {
      const res = await suppliersApi.getSuppliers(storeId)
      cacheRef.current = { storeId, suppliers: res.suppliers }
    }
    const term = search.trim().toLowerCase()
    const filtered = term ? cacheRef.current.suppliers.filter((s) => s.name.toLowerCase().includes(term)) : cacheRef.current.suppliers
    return { items: filtered.slice(skip, skip + take), totalCount: filtered.length }
  }

  const pickerProps: EntityPickerProps<Supplier> = {
    fetchPage,
    getId: (item) => item.supplierId,
    getLabel: (item) => item.name,
    renderOption: (item) => <SupplierRow item={item} />,
    placeholder,
    scheme,
    disabled,
    className,
    ariaLabel: 'Поиск поставщика',
    emptyHint: 'Проверьте название',
    multiple: false,
    value,
    onChange,
  }

  return <EntityPicker {...pickerProps} />
}
