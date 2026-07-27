import { useCallback, useEffect, useState } from 'react'
import { favoritesApi, ApiError, type Favorite } from '../../lib/api'
import { HeartIcon, CloseIcon } from '../../components/icons'

export function FavoritesPage() {
  const [items, setItems] = useState<Favorite[] | null>(null)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      setItems((await favoritesApi.getFavorites()).favorites)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить избранное')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  async function remove(f: Favorite) {
    setBusyId(f.favoriteId)
    try {
      await favoritesApi.removeFavorite(f.type, f.entityId)
      setItems((cur) => cur?.filter((x) => x.favoriteId !== f.favoriteId) ?? null)
    } catch {
      await load()
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-1 text-[22px] font-extrabold tracking-tight">Избранное</h1>
      <p className="mb-6 text-[14px] text-[color:var(--text-secondary)]">Товары и магазины, которые вы сохранили</p>

      {items === null && !error && <p className="py-10 text-center text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p>}
      {error && <p className="py-10 text-center text-[14px] text-[color:var(--text-secondary)]">{error}</p>}
      {items && items.length === 0 && (
        <div className="rounded-3xl bg-[color:var(--bg-card)] p-10 text-center shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">
          <HeartIcon width={28} height={28} className="mx-auto mb-3 text-[color:var(--text-tertiary)]" />
          <p className="text-[14px] font-semibold">Пока ничего нет в избранном</p>
          <p className="mt-1 text-[13px] text-[color:var(--text-secondary)]">Найдите товар на странице сканирования и нажмите «В избранное»</p>
        </div>
      )}
      {items && items.length > 0 && (
        <div className="flex flex-col gap-2.5">
          {items.map((f) => (
            <div
              key={f.favoriteId}
              className="flex items-center justify-between gap-3 rounded-2xl bg-[color:var(--bg-card)] p-4 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]"
            >
              <div className="flex items-center gap-3">
                <span className="grid h-10 w-10 place-items-center rounded-xl bg-[color:var(--color-brand-light)] text-[color:var(--color-brand)]">
                  <HeartIcon width={17} height={17} />
                </span>
                <div>
                  <div className="text-[14px] font-bold">{f.type === 'Product' ? 'Товар' : 'Магазин'} #{f.entityId}</div>
                </div>
              </div>
              <button
                onClick={() => remove(f)}
                disabled={busyId === f.favoriteId}
                className="grid h-9 w-9 place-items-center rounded-full text-[color:var(--text-tertiary)] hover:bg-[color:var(--bg-section)] hover:text-[color:var(--text-primary)] disabled:opacity-40"
              >
                <CloseIcon width={15} height={15} />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
