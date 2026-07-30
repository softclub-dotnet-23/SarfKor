import { apiFetch } from './client'

// Originally a consumer-crowdsourcing endpoint (any authenticated user could report a
// price they saw) — the backend never restricted it to the store's owner, so it works
// unchanged as the StorePartner's own way to set what PriceEntry customers/POS see now
// that there's no consumer app submitting prices anymore.
export function submitPriceUpdate(productId: number, storeId: number, price: number, currency: string) {
  return apiFetch<{ priceEntryId: number; recordedAt: string }>('/api/prices', {
    method: 'POST',
    body: { productId, storeId, price, currency },
  })
}
