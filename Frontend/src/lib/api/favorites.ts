import { apiFetch } from './client'

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
  return apiFetch<{ favoriteId: number }>('/api/favorites', { method: 'POST', body: { type, entityId } })
}

export function removeFavorite(type: FavoriteType, entityId: number) {
  return apiFetch<{ outcome: 'Removed' | 'NotFound' }>('/api/favorites', {
    method: 'DELETE',
    query: { type, entityId },
  })
}
