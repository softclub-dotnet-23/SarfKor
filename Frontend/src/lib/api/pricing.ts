import { apiFetch } from './client'

export function submitPriceUpdate(productId: number, storeId: number, price: number, currency: string) {
  return apiFetch<{ priceEntryId: number; recordedAt: string }>('/api/prices', {
    method: 'POST',
    body: { productId, storeId, price, currency },
  })
}
