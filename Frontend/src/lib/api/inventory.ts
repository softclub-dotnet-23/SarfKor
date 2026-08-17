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
  // Backend sends a number here (int? StockMovementId), not a string -- was typed as an optional
  // string, which is wrong on two axes: primitive type and nullability.
  return apiFetch<{ outcome: string; stockMovementId: number | null }>('/api/stock/receipts', {
    method: 'POST',
    body: { storeId, productId, quantity, supplierId },
  })
}

export function setCostPrice(storeId: number, productId: number, amount: number, currency: string) {
  // Same as stockMovementId above -- backend's CostPriceId is int?, not string.
  return apiFetch<{ outcome: string; costPriceId: number | null }>('/api/stock/cost-price', {
    method: 'POST',
    body: { storeId, productId, amount, currency },
  })
}
