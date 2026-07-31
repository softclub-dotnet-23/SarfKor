import { useState } from 'react'
import { favoritesApi } from '../../lib/api'
import { EmptyState, ErrorState, LINE, Reveal, Skeleton, TXT, useAsync } from '../ui'

/**
 * Favourites come back as {type, entityId} and nothing more — there is no
 * product-by-id or store-by-id route to resolve them against, so the rows show
 * what the backend actually returned rather than a fabricated name.
 */
export function FavoritesPage() {
  const favs = useAsync(() => favoritesApi.getFavorites(), [])
  const [removing, setRemoving] = useState<number | null>(null)

  async function remove(type: favoritesApi.FavoriteType, entityId: number, favoriteId: number) {
    setRemoving(favoriteId)
    try {
      await favoritesApi.removeFavorite(type, entityId)
      favs.reload()
    } finally {
      setRemoving(null)
    }
  }

  return (
    <>
      <Reveal>
        <p
          className="mb-5 text-[10px] font-bold uppercase tracking-[0.28em]"
          style={{ color: TXT.rest }}
        >
          Избранное
        </p>
        <h1 className="mb-12 text-[clamp(30px,4.6vw,46px)] font-extrabold leading-[1.04] tracking-[-0.04em]">
          Сохранённое
        </h1>
      </Reveal>

      {favs.loading && (
        <div className="flex flex-col">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="border-b py-5" style={{ borderColor: LINE }}>
              <Skeleton h={14} w={`${38 + i * 9}%`} />
            </div>
          ))}
        </div>
      )}

      {favs.error && <ErrorState message={favs.error} onRetry={favs.reload} />}

      {!favs.loading && !favs.error && favs.data && (
        favs.data.favorites.length === 0 ? (
          <EmptyState
            title="Здесь пока пусто"
            body="Отсканируйте товар и нажмите «В избранное» — он появится в этом списке."
          />
        ) : (
          <ul className="flex flex-col">
            {favs.data.favorites.map((f, i) => (
              <Reveal key={f.favoriteId} i={i}>
                <li
                  className="flex items-center justify-between gap-5 border-b py-5"
                  style={{ borderColor: LINE }}
                >
                  <span className="min-w-0">
                    <span className="block text-[15px] font-semibold text-white">
                      {f.type === 'Product' ? 'Товар' : 'Магазин'} #{f.entityId}
                    </span>
                    <span className="mt-0.5 block text-[12px]" style={{ color: TXT.rest }}>
                      {f.type === 'Product' ? 'Товар в избранном' : 'Магазин в избранном'}
                    </span>
                  </span>
                  <button
                    onClick={() => remove(f.type, f.entityId, f.favoriteId)}
                    disabled={removing === f.favoriteId}
                    className="shrink-0 text-[12px] transition-colors duration-300 hover:text-white disabled:opacity-40"
                    style={{ color: TXT.rest }}
                  >
                    Убрать
                  </button>
                </li>
              </Reveal>
            ))}
          </ul>
        )
      )}
    </>
  )
}
