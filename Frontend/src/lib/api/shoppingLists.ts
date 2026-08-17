import { apiFetch } from './client'

/**
 * Note the shape: an item carries a productId and a quantity, and nothing else.
 * The backend has no product-by-id endpoint, so a name can only be attached by a
 * caller that already knows it (i.e. that scanned the barcode). Anything showing
 * these items has to be honest about that rather than inventing a label.
 */
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
  return apiFetch<{ shoppingListId: number }>('/api/shopping-lists', {
    method: 'POST',
    body: { name },
  })
}

export function addShoppingListItem(listId: number, productId: number, quantity: number) {
  return apiFetch<{ outcome: string; itemId: number | null }>(`/api/shopping-lists/${listId}/items`, {
    method: 'POST',
    body: { productId, quantity },
  })
}

export function removeShoppingListItem(listId: number, itemId: number) {
  return apiFetch<{ outcome: string }>(`/api/shopping-lists/${listId}/items/${itemId}`, {
    method: 'DELETE',
  })
}
