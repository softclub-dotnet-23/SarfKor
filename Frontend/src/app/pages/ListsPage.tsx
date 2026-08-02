import { useState, type FormEvent } from 'react'
import {
  productsApi,
  shoppingListsApi,
  type ShoppingList,
  type StoreBasket,
} from '../../lib/api'
import {
  Button,
  EmptyState,
  ErrorState,
  LINE,
  Reveal,
  SectionTitle,
  Skeleton,
  Spinner,
  TXT,
  km,
  money,
  useAsync,
} from '../ui'

/**
 * Shopping lists, and the one query that makes them worth keeping: compare-basket
 * asks the backend which single store is cheapest for the whole basket, which no
 * amount of client-side summing of individual scans can answer correctly.
 *
 * Items carry a productId and a quantity and nothing else — the backend has no
 * product-by-id route — so the rows say "Товар #123" rather than inventing a name.
 */
export function ListsPage() {
  const lists = useAsync(() => shoppingListsApi.getShoppingLists(), [])
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)

  async function create(e: FormEvent) {
    e.preventDefault()
    const n = name.trim()
    if (!n) return
    setBusy(true)
    try {
      await shoppingListsApi.createShoppingList(n)
      setName('')
      lists.reload()
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <Reveal>
        <p
          className="mb-5 text-[10px] font-bold uppercase tracking-[0.28em]"
          style={{ color: TXT.rest }}
        >
          Списки покупок
        </p>
        <h1 className="mb-10 text-[clamp(30px,4.6vw,46px)] font-extrabold leading-[1.04] tracking-[-0.04em]">
          Соберите корзину дешевле
        </h1>
      </Reveal>

      <Reveal i={1}>
        <form onSubmit={create} className="mb-14 flex items-end gap-4">
          <label className="flex-1">
            <span
              className="mb-2 block text-[10px] font-bold uppercase tracking-[0.16em]"
              style={{ color: TXT.rest }}
            >
              Новый список
            </span>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              maxLength={80}
              placeholder="Например, «Продукты на неделю»"
              className="w-full border-0 bg-transparent pb-3 text-[16px] text-[color:var(--app-text-primary)] caret-[color:var(--app-text-primary)] placeholder:text-[color:var(--app-text-rest)]"
              style={{ borderBottom: `1px solid ${LINE}` }}
            />
          </label>
          <Button type="submit" disabled={busy || !name.trim()}>
            {busy && <Spinner dark />}
            Создать
          </Button>
        </form>
      </Reveal>

      {lists.loading && (
        <div className="flex flex-col gap-4">
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} h={92} className="rounded-2xl" />
          ))}
        </div>
      )}

      {lists.error && <ErrorState message={lists.error} onRetry={lists.reload} />}

      {!lists.loading && !lists.error && lists.data && (
        lists.data.lists.length === 0 ? (
          <EmptyState
            title="Пока нет ни одного списка"
            body="Создайте список, затем добавляйте в него товары со страницы сравнения цен."
          />
        ) : (
          <div className="flex flex-col gap-10">
            {lists.data.lists.map((l, i) => (
              <Reveal key={l.shoppingListId} i={2 + i}>
                <ListCard list={l} onChanged={lists.reload} />
              </Reveal>
            ))}
          </div>
        )
      )}
    </>
  )
}

function ListCard({
  list,
  onChanged,
}: {
  list: ShoppingList
  onChanged: () => void
}) {
  const [basket, setBasket] = useState<StoreBasket[] | null>(null)
  const [comparing, setComparing] = useState(false)
  const [err, setErr] = useState('')

  const productIds = list.items.map((it) => it.productId)

  async function compare() {
    if (productIds.length === 0) return
    setComparing(true)
    setErr('')
    try {
      const res = await productsApi.compareBasket(productIds)
      setBasket([...res.stores].sort((a, b) => a.totalPrice - b.totalPrice))
    } catch {
      setErr('Не удалось сравнить корзину')
    } finally {
      setComparing(false)
    }
  }

  async function removeItem(itemId: number) {
    try {
      await shoppingListsApi.removeShoppingListItem(list.shoppingListId, itemId)
      setBasket(null) // the basket no longer matches the list
      onChanged()
    } catch {
      /* row simply stays */
    }
  }

  const best = basket?.[0]

  return (
    <section className="rounded-2xl border p-7" style={{ borderColor: LINE }}>
      <div className="mb-6 flex items-baseline justify-between gap-4">
        <h2 className="truncate text-[19px] font-bold tracking-tight text-[color:var(--app-text-primary)]">{list.name}</h2>
        <span className="shrink-0 text-[11px] tabular-nums" style={{ color: TXT.rest }}>
          {list.items.length} поз.
        </span>
      </div>

      {list.items.length === 0 ? (
        <p className="text-[13.5px]" style={{ color: TXT.rest }}>
          Список пуст. Отсканируйте товар и добавьте его отсюда.
        </p>
      ) : (
        <>
          <ul className="mb-7 flex flex-col">
            {list.items.map((it) => (
              <li
                key={it.itemId}
                className="flex items-center justify-between gap-4 border-b py-3"
                style={{ borderColor: LINE }}
              >
                <span className="min-w-0 truncate text-[14px] text-[color:var(--app-text-primary)]">
                  Товар #{it.productId}
                  <span className="ml-2 text-[12px]" style={{ color: TXT.rest }}>
                    × {it.quantity}
                  </span>
                </span>
                <button
                  onClick={() => removeItem(it.itemId)}
                  aria-label={`Убрать товар ${it.productId}`}
                  className="shrink-0 text-[12px] transition-colors duration-300 hover:text-[color:var(--app-text-primary)]"
                  style={{ color: TXT.rest }}
                >
                  Убрать
                </button>
              </li>
            ))}
          </ul>

          <Button variant="ghost" onClick={compare} disabled={comparing}>
            {comparing && <Spinner />}
            Где вся корзина дешевле
          </Button>

          {err && (
            <p className="mt-4 text-[13px]" style={{ color: TXT.secondary }}>
              {err}
            </p>
          )}

          {basket && (
            <div className="mt-8">
              {basket.length === 0 ? (
                <p className="text-[13.5px]" style={{ color: TXT.rest }}>
                  Ни один магазин не имеет всех товаров из списка.
                </p>
              ) : (
                <>
                  <SectionTitle>Итог по магазинам</SectionTitle>
                  <ul className="flex flex-col">
                    {basket.map((s, i) => (
                      <li
                        key={s.storeId}
                        className="flex items-baseline justify-between gap-5 border-b py-4"
                        style={{ borderColor: LINE }}
                      >
                        <span className="min-w-0">
                          <span className="block truncate text-[15px] font-semibold text-[color:var(--app-text-primary)]">
                            {s.storeName}
                          </span>
                          <span className="mt-0.5 block text-[12px]" style={{ color: TXT.rest }}>
                            {km(s.distanceKm) ?? 'Расстояние неизвестно'}
                            {i === 0 && best && <> · дешевле всего</>}
                            {i > 0 && best && (
                              <> · +{money(s.totalPrice - best.totalPrice, s.currency)}</>
                            )}
                          </span>
                        </span>
                        <span
                          className="shrink-0 text-[17px] font-bold tabular-nums"
                          style={{ color: i === 0 ? TXT.primary : TXT.secondary }}
                        >
                          {money(s.totalPrice, s.currency)}
                        </span>
                      </li>
                    ))}
                  </ul>
                </>
              )}
            </div>
          )}
        </>
      )}
    </section>
  )
}
