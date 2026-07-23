import { apiFetch } from './client'

export interface StockLevel {
  productId: number
  quantity: number
}

// Note: this endpoint returns a raw array, not a wrapped { outcome, ... }
// object like most others — confirmed against the backend source.
export function getStockLevels(storeId: number) {
  return apiFetch<StockLevel[]>('/api/stock', { query: { storeId } })
}

export function recordStockReceipt(storeId: number, productId: number, quantity: number, supplierId?: number) {
  return apiFetch<{ outcome: string; stockMovementId?: string }>('/api/stock/receipts', {
    method: 'POST',
    body: { storeId, productId, quantity, supplierId },
  })
}

export function setCostPrice(storeId: number, productId: number, amount: number, currency: string) {
  return apiFetch<{ outcome: string; costPriceId?: string }>('/api/stock/cost-price', {
    method: 'POST',
    body: { storeId, productId, amount, currency },
  })
}
