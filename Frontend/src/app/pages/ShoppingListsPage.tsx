import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { shoppingListsApi, productsApi, ApiError, type ShoppingList, type StoreBasket } from '../../lib/api'
import { ListIcon, CloseIcon, MapPinIcon } from '../../components/icons'

function ListCard({ list, onChanged }: { list: ShoppingList; onChanged: () => void }) {
  const [productId, setProductId] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [comparing, setComparing] = useState(false)
  const [comparison, setComparison] = useState<StoreBasket[] | null>(null)
  const [compareError, setCompareError] = useState('')

  async function addItem(e: FormEvent) {
    e.preventDefault()
    const pid = Number(productId)
    const qty = Number(quantity) || 1
    if (!pid) return
    setBusy(true)
    setError('')
    try {
      const res = await shoppingListsApi.addShoppingListItem(list.shoppingListId, pid, qty)
      if (res.outcome !== 'Added') {
        setError(res.outcome === 'Forbidden' ? 'Нет доступа к этому списку' : 'Список не найден')
        return
      }
      setProductId('')
      setQuantity('1')
      onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось добавить товар')
    } finally {
      setBusy(false)
    }
  }

  async function removeItem(itemId: number) {
    try {
      await shoppingListsApi.removeShoppingListItem(list.shoppingListId, itemId)
      onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить товар')
    }
  }

  async function compare() {
    setComparing(true)
    setCompareError('')
    setComparison(null)
    try {
      const ids = list.items.map((i) => i.productId)
      const res = await productsApi.compareBasket(ids)
      setComparison([...res.stores].sort((a, b) => a.totalPrice - b.totalPrice))
    } catch (err) {
      setCompareError(err instanceof ApiError ? err.message : 'Не удалось сравнить магазины')
    } finally {
      setComparing(false)
    }
  }

  return (
    <div className="rounded-3xl bg-[color:var(--bg-card)] p-5 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">
      <div className="mb-3 flex items-center justify-between gap-3">
        <h3 className="text-[16px] font-bold">{list.name}</h3>
        {list.items.length > 0 && (
          <button
            onClick={compare}
            disabled={comparing}
            className="shrink-0 rounded-full bg-[color:var(--color-brand)] px-4 py-2 text-[12.5px] font-bold text-white transition-transform hover:scale-[1.02] disabled:opacity-50"
          >
            {comparing ? 'Считаем…' : 'Сравнить магазины'}
          </button>
        )}
      </div>

      {list.items.length === 0 ? (
        <p className="text-[13px] text-[color:var(--text-tertiary)]">Список пуст</p>
      ) : (
        <div className="mb-3 flex flex-col gap-1.5">
          {list.items.map((item) => (
            <div key={item.itemId} className="flex items-center justify-between rounded-xl bg-[color:var(--bg-section)] px-3 py-2 text-[13.5px]">
              <span>
                Товар #{item.productId} <span className="text-[color:var(--text-tertiary)]">× {item.quantity}</span>
              </span>
              <button onClick={() => removeItem(item.itemId)} className="text-[color:var(--text-tertiary)] hover:text-[color:var(--text-primary)]">
                <CloseIcon width={13} height={13} />
              </button>
            </div>
          ))}
        </div>
      )}

      {compareError && <p className="mb-2 text-[12.5px] text-[color:var(--text-secondary)]">{compareError}</p>}
      {comparison && (
        <div className="mb-3 flex flex-col gap-1.5">
          {comparison.length === 0 && <p className="text-[13px] text-[color:var(--text-tertiary)]">Нет данных по ценам для этой корзины</p>}
          {comparison.map((s, i) => (
            <div
              key={s.storeId}
              className={`flex items-center justify-between rounded-xl px-3 py-2 text-[13.5px] ${i === 0 ? 'bg-[color:var(--color-brand-light)] font-bold' : 'bg-[color:var(--bg-section)]'}`}
            >
              <span className="flex items-center gap-1.5">
                <MapPinIcon width={14} height={14} className={i === 0 ? 'text-[color:var(--color-brand)]' : 'text-[color:var(--text-tertiary)]'} />
                {s.storeName}
              </span>
              <span>
                {s.totalPrice.toFixed(2)} {s.currency}
              </span>
            </div>
          ))}
        </div>
      )}

      <form onSubmit={addItem} className="flex flex-wrap items-center gap-2 border-t border-[color:var(--border-subtle)] pt-3">
        <input
          value={productId}
          onChange={(e) => setProductId(e.target.value)}
          placeholder="ID товара"
          type="number"
          className="w-28 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--color-brand)]"
        />
        <input
          value={quantity}
          onChange={(e) => setQuantity(e.target.value)}
          placeholder="Кол-во"
          type="number"
          min={1}
          className="w-20 rounded-lg border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-2.5 py-1.5 text-[13px] outline-none focus:border-[color:var(--color-brand)]"
        />
        <button
          type="submit"
          disabled={busy || !productId}
          className="rounded-lg bg-[color:var(--text-primary)] px-4 py-1.5 text-[12.5px] font-bold text-[color:var(--bg-app)] disabled:opacity-50"
        >
          Добавить
        </button>
        {error && <span className="text-[12px] text-[color:var(--text-secondary)]">{error}</span>}
      </form>
    </div>
  )
}

export function ShoppingListsPage() {
  const [lists, setLists] = useState<ShoppingList[] | null>(null)
  const [error, setError] = useState('')
  const [newName, setNewName] = useState('')
  const [creating, setCreating] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      setLists((await shoppingListsApi.getShoppingLists()).lists)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить списки покупок')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function createList(e: FormEvent) {
    e.preventDefault()
    if (!newName.trim()) return
    setCreating(true)
    try {
      await shoppingListsApi.createShoppingList(newName.trim())
      setNewName('')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось создать список')
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-1 text-[22px] font-extrabold tracking-tight">Списки покупок</h1>
      <p className="mb-6 text-[14px] text-[color:var(--text-secondary)]">Соберите корзину и сравните, где дешевле</p>

      <form onSubmit={createList} className="mb-6 flex gap-2">
        <input
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="Название списка, например «Выходные»"
          className="min-w-0 flex-1 rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-card)] px-3.5 py-3 text-[14px] outline-none focus:border-[color:var(--color-brand)]"
        />
        <button
          type="submit"
          disabled={creating || !newName.trim()}
          className="shrink-0 rounded-xl bg-[color:var(--color-brand)] px-5 py-3 text-[14px] font-bold text-white disabled:opacity-50"
        >
          Создать
        </button>
      </form>

      {lists === null && !error && <p className="py-10 text-center text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p>}
      {error && <p className="py-6 text-center text-[14px] text-[color:var(--text-secondary)]">{error}</p>}
      {lists && lists.length === 0 && (
        <div className="rounded-3xl bg-[color:var(--bg-card)] p-10 text-center shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">
          <ListIcon width={28} height={28} className="mx-auto mb-3 text-[color:var(--text-tertiary)]" />
          <p className="text-[14px] font-semibold">Списков пока нет</p>
        </div>
      )}
      {lists && lists.length > 0 && (
        <div className="flex flex-col gap-4">
          {lists.map((l) => (
            <ListCard key={l.shoppingListId} list={l} onChanged={load} />
          ))}
        </div>
      )}
    </div>
  )
}
