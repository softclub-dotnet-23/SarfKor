import { useEffect, useState } from 'react'
import { EntityPicker, type EntityPickerProps } from './EntityPicker'
import { BarcodeScannerView } from './BarcodeScannerView'
import { CameraIcon } from './icons'
import { useBarcodeScanner } from '../../hooks/useBarcodeScanner'
import { productsApi, type ProductSearchItem } from '../../lib/api'

function fmtPrice(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function ProductRow({ item }: { item: ProductSearchItem }) {
  const meta = [item.brandName, item.barcode, item.categoryName].filter(Boolean).join(' · ')
  return (
    <div className="flex items-center justify-between gap-2">
      <div className="min-w-0">
        <div className="truncate text-[13.5px] font-medium">{item.name}</div>
        {meta && <div className="truncate text-[11.5px] text-[color:var(--admin-text-tertiary)]">{meta}</div>}
      </div>
      {item.price != null && (
        <span className="shrink-0 text-[12.5px] font-semibold text-[color:var(--admin-text-tertiary)]">
          {fmtPrice(item.price)} {item.currency}
        </span>
      )}
    </div>
  )
}

interface ProductPickerBaseProps {
  categoryId?: number
  storeId?: number
  scheme?: 'admin'
  placeholder?: string
  disabled?: boolean
  className?: string
  /** Shows the camera-scan button next to the search field — the primary way to identify a
   *  product in this project, front and center wherever the picker is used from a phone
   *  (Cashier, StorePartner on a phone). Off by default for desktop-only spots. */
  scanEnabled?: boolean
}

interface SingleProductPickerProps extends ProductPickerBaseProps {
  multiple?: false
  value: ProductSearchItem | null
  onChange: (value: ProductSearchItem | null) => void
}

interface MultiProductPickerProps extends ProductPickerBaseProps {
  multiple: true
  value: ProductSearchItem[]
  onChange: (value: ProductSearchItem[]) => void
}

function ScanButton({ onDetected }: { onDetected: (code: string) => void }) {
  const [open, setOpen] = useState(false)
  const scanner = useBarcodeScanner({ onDetect: (code) => { onDetected(code); setOpen(false) } })

  useEffect(() => {
    if (open) scanner.start()
    else scanner.stop()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const accent = 'var(--admin-accent)'
  const accentSoft = 'var(--admin-accent-soft)'
  const text = 'var(--admin-text-secondary)'

  return (
    <div className="relative shrink-0">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        title={open ? 'Скрыть камеру' : 'Сканировать штрихкод'}
        aria-pressed={open}
        className="grid h-10 w-10 place-items-center rounded-xl border"
        style={{
          borderColor: open ? accent : 'transparent',
          background: open ? accentSoft : 'transparent',
          color: open ? accent : text,
        }}
      >
        <CameraIcon width={17} height={17} />
      </button>
      {open && (
        <div className="absolute right-0 top-full z-40 mt-2 w-[min(320px,80vw)]">
          <BarcodeScannerView videoRef={scanner.videoRef} phase={scanner.phase} onStart={scanner.start} className="aspect-video w-full" />
        </div>
      )}
    </div>
  )
}

/**
 * Searchable product select — replaces every "введите ID товара вручную" field in the project.
 * Wraps the generic EntityPicker with product search (name/barcode/brand at once, server-side,
 * see /api/products/search) and an optional camera barcode-scan shortcut. `categoryId` narrows
 * results to a cascade's chosen category but is never required — typing a name or barcode always
 * works on its own.
 */
export function ProductPicker(props: SingleProductPickerProps | MultiProductPickerProps) {
  const { categoryId, storeId, scheme = 'admin', placeholder = 'Название, штрихкод или бренд…', disabled, className, scanEnabled } = props

  async function fetchPage({ search, skip, take }: { search: string; skip: number; take: number }) {
    const res = await productsApi.searchProducts({ search: search || undefined, categoryId, storeId, skip, take })
    return { items: res.items, totalCount: res.totalCount }
  }

  const headerAction = scanEnabled ? (
    <ScanButton
      onDetected={async (code) => {
        // A scanned barcode is a complete, exact identifier -- go straight to a 1-item search
        // and auto-select on an exact hit instead of just dropping the code into the search box
        // and making the cashier tap the result too.
        const res = await productsApi.searchProducts({ search: code, storeId, take: 1 })
        const hit = res.items[0]
        if (!hit || hit.barcode !== code) return
        if (props.multiple) {
          if (!props.value.some((v) => v.productId === hit.productId)) props.onChange([...props.value, hit])
        } else {
          props.onChange(hit)
        }
      }}
    />
  ) : undefined

  const shared = {
    fetchPage,
    getId: (item: ProductSearchItem) => item.productId,
    getLabel: (item: ProductSearchItem) => item.name,
    renderOption: (item: ProductSearchItem) => <ProductRow item={item} />,
    placeholder,
    scheme,
    disabled,
    className,
    ariaLabel: 'Поиск товара',
    headerAction,
    emptyHint: 'Проверьте написание или отсканируйте штрихкод',
  }

  const pickerProps: EntityPickerProps<ProductSearchItem> = props.multiple
    ? { ...shared, multiple: true, value: props.value, onChange: props.onChange }
    : { ...shared, multiple: false, value: props.value, onChange: props.onChange }

  return <EntityPicker {...pickerProps} />
}
