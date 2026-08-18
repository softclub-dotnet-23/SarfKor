import { EntityPicker, type EntityPickerProps } from './EntityPicker'
import { meApi, type MyStoreSearchItem } from '../../lib/api'

function StoreRow({ item }: { item: MyStoreSearchItem }) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13.5px] font-medium">{item.name}</div>
      {item.address && <div className="truncate text-[11.5px] text-[color:var(--admin-text-tertiary)]">{item.address}</div>}
    </div>
  )
}

interface StorePickerBaseProps {
  scheme?: 'admin'
  placeholder?: string
  disabled?: boolean
  className?: string
  /** Excludes one store from the results — e.g. a transfer's "from" store shouldn't also be
   *  pickable as the "to" store. */
  excludeStoreId?: number
}

interface SingleStorePickerProps extends StorePickerBaseProps {
  multiple?: false
  value: MyStoreSearchItem | null
  onChange: (value: MyStoreSearchItem | null) => void
}

interface MultiStorePickerProps extends StorePickerBaseProps {
  multiple: true
  value: MyStoreSearchItem[]
  onChange: (value: MyStoreSearchItem[]) => void
}

/**
 * Searchable picker over the stores the caller owns (GET /api/me/stores/search) -- replaces every
 * "введите ID магазина вручную" field in the project. Owner-only by construction (the backend
 * query never looks at another owner's rows), so this is only ever meaningful for a StorePartner
 * picking among their own shops (e.g. a stock transfer's destination), not a general store lookup.
 */
export function StorePicker(props: SingleStorePickerProps | MultiStorePickerProps) {
  const { scheme = 'admin', placeholder = 'Название или адрес…', disabled, className, excludeStoreId } = props

  async function fetchPage({ search, skip, take }: { search: string; skip: number; take: number }) {
    const res = await meApi.searchMyStores({ search: search || undefined, skip, take })
    const items = excludeStoreId ? res.stores.filter((s) => s.storeId !== excludeStoreId) : res.stores
    return { items, totalCount: excludeStoreId ? res.totalCount - (res.stores.length - items.length) : res.totalCount }
  }

  const shared = {
    fetchPage,
    getId: (item: MyStoreSearchItem) => item.storeId,
    getLabel: (item: MyStoreSearchItem) => item.name,
    renderOption: (item: MyStoreSearchItem) => <StoreRow item={item} />,
    placeholder,
    scheme,
    disabled,
    className,
    ariaLabel: 'Поиск магазина',
    emptyHint: 'Проверьте название или адрес',
  }

  const pickerProps: EntityPickerProps<MyStoreSearchItem> = props.multiple
    ? { ...shared, multiple: true, value: props.value, onChange: props.onChange }
    : { ...shared, multiple: false, value: props.value, onChange: props.onChange }

  return <EntityPicker {...pickerProps} />
}
