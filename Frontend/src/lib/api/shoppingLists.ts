import { apiFetch } from './client'

export interface ShoppingListItem {
  itemId: number
  productId: number
  quantity: number
}

export interface ShoppingList {
  shoppingListId: number
  name: string
  items: ShoppingListItem[]
}

export function getShoppingLists() {
  return apiFetch<{ lists: ShoppingList[] }>('/api/shopping-lists')
}

export function createShoppingList(name: string) {
  return apiFetch<{ shoppingListId: number }>('/api/shopping-lists', { method: 'POST', body: { name } })
}

export function addShoppingListItem(listId: number, productId: number, quantity: number) {
  return apiFetch<{ outcome: 'Added' | 'ListNotFound' | 'Forbidden'; itemId?: number }>(
    `/api/shopping-lists/${listId}/items`,
    { method: 'POST', body: { productId, quantity } },
  )
}

export function removeShoppingListItem(listId: number, itemId: number) {
  return apiFetch<{ outcome: 'Removed' | 'ListNotFound' | 'ItemNotFound' | 'Forbidden' }>(
    `/api/shopping-lists/${listId}/items/${itemId}`,
    { method: 'DELETE' },
  )
}
