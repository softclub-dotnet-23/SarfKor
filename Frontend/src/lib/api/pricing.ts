import { apiFetch } from './client'

export function submitPriceUpdate(productId: number, storeId: number, price: number, currency: string) {
  return apiFetch<{ priceEntryId: number; recordedAt: string }>('/api/prices', {
    method: 'POST',
    body: { productId, storeId, price, currency },
  })
}

export type RaisePriceEntryDisputeOutcome = 'Raised' | 'PriceEntryNotFound'

export function raisePriceEntryDispute(priceEntryId: number, reason: string) {
  return apiFetch<{ outcome: RaisePriceEntryDisputeOutcome; disputeId?: number }>(
    `/api/price-entries/${priceEntryId}/dispute`,
    { method: 'POST', body: { reason } },
  )
}
