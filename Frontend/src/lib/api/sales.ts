import { apiFetch } from './client'

export interface SaleLine {
  productId: number
  quantity: number
}

export interface ProcessSaleRequest {
  storeId: number
  idempotencyKey: string
  currency: string
  lines: SaleLine[]
}

export interface ProcessSaleResult {
  outcome: 'Completed' | 'StoreNotFound' | 'Forbidden' | 'ProductNotFound' | 'PriceNotFound' | 'InsufficientStock'
  saleTransactionId?: string
  totalAmount?: number
  currency?: string
  failedProductId?: number
}

export function processSale(req: ProcessSaleRequest) {
  return apiFetch<ProcessSaleResult>('/api/sales', { method: 'POST', body: req })
}

export function voidSale(saleTransactionId: string, reason: string) {
  return apiFetch<{ outcome: string; voidedAt?: string }>(`/api/sales/${saleTransactionId}/void`, {
    method: 'POST',
    body: { reason },
  })
}

export interface CashierShift {
  cashierShiftId: string
  cashierUserId: string
  openingCash: number
  expectedCash?: number
  closingCash?: number
  currency: string
  startedAt: string
  endedAt?: string
}

export function openCashierShift(storeId: number, openingCash: number, currency: string) {
  return apiFetch<{ outcome: string; cashierShiftId?: string }>('/api/cashier-shifts/open', {
    method: 'POST',
    body: { storeId, openingCash, currency },
  })
}

export function closeCashierShift(cashierShiftId: string, closingCash: number) {
  return apiFetch<{ outcome: string; expectedCash?: number; closingCash?: number; discrepancy?: number }>(
    `/api/cashier-shifts/${cashierShiftId}/close`,
    { method: 'POST', body: { closingCash } },
  )
}

export function getCashierShifts(storeId: number) {
  return apiFetch<{ outcome: string; shifts?: CashierShift[] }>(`/api/stores/${storeId}/cashier-shifts`)
}
