import { apiFetch } from './client'

// Backend serialises enums as strings (JsonStringEnumConverter, Program.cs), so
// these travel as "Product"/"Store" rather than 0/1.
export type FavoriteType = 'Store' | 'Product'

export interface Favorite {
  favoriteId: number
  type: FavoriteType
  entityId: number
}

export function getFavorites() {
  return apiFetch<{ favorites: Favorite[] }>('/api/favorites')
}

export function addFavorite(type: FavoriteType, entityId: number) {
  return apiFetch<{ favoriteId: number }>('/api/favorites', {
    method: 'POST',
    body: { type, entityId },
  })
}

// DELETE binds both values from the query string, not from a body.
export function removeFavorite(type: FavoriteType, entityId: number) {
  return apiFetch<{ outcome: string }>('/api/favorites', {
    method: 'DELETE',
    query: { type, entityId },
  })
}
