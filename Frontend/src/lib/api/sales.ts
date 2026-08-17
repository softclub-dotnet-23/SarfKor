import { apiFetch } from './client'

export interface SaleLine {
  productId: number
  quantity: number
}

export interface BundleLine {
  productBundleId: number
  quantity: number
}

export interface ProcessSaleRequest {
  storeId: number
  idempotencyKey: string
  currency: string
  lines: SaleLine[]
  giftCardCode?: string
  customerId?: number
  applyStoreCredit?: boolean
  bundleLines?: BundleLine[]
}

export interface ProcessSaleResultLine {
  saleLineItemId: number
  productId: number
  quantity: number
  unitPrice: number
}

export interface ProcessSaleResult {
  outcome:
    | 'Completed'
    | 'StoreNotFound'
    | 'Forbidden'
    | 'ProductNotFound'
    | 'PriceNotFound'
    | 'InsufficientStock'
    | 'GiftCardNotFound'
    | 'GiftCardNotUsable'
    | 'CustomerNotFound'
    | 'BundleNotFound'
  // All of these are the "payload" fields, present with a null value (not omitted -- see
  // ProcessSaleResult.cs) on every outcome except 'Completed'. Typed as `| null`, not just `?`,
  // so a check like `!== undefined` can't let a real null through the way it did for
  // CashierShift.closingCash (see StaffPage.tsx's fix).
  saleTransactionId: number | null
  totalAmount: number | null
  currency: string | null
  failedProductId: number | null
  giftCardAmountApplied: number | null
  amountDue: number | null
  storeCreditAmountApplied: number | null
  lines: ProcessSaleResultLine[] | null
}

export function processSale(req: ProcessSaleRequest) {
  return apiFetch<ProcessSaleResult>('/api/sales', { method: 'POST', body: req })
}

export type VoidSaleOutcome = 'Voided' | 'NotFound' | 'Forbidden' | 'AlreadyVoided'

export function voidSale(saleTransactionId: number, reason: string) {
  return apiFetch<{ outcome: VoidSaleOutcome; voidedAt: string | null }>(`/api/sales/${saleTransactionId}/void`, {
    method: 'POST',
    body: { reason },
  })
}

export type RecordCommissionOutcome = 'Recorded' | 'SaleNotFound' | 'Forbidden'

export function recordCommission(saleTransactionId: number, amount: number, currency: string) {
  return apiFetch<{ outcome: RecordCommissionOutcome; commissionId: number | null }>(
    `/api/sales/${saleTransactionId}/commission`,
    { method: 'POST', body: { amount, currency } },
  )
}

export interface Commission {
  commissionId: number
  cashierUserId: string
  amount: number
  currency: string
  createdAt: string
}

export type GetCommissionsOutcome = 'Found' | 'SaleNotFound' | 'Forbidden'

export function getCommissionsForSale(saleTransactionId: number) {
  return apiFetch<{ outcome: GetCommissionsOutcome; commissions: Commission[] | null }>(
    `/api/sales/${saleTransactionId}/commissions`,
  )
}

export interface ReturnLine {
  saleLineItemId: number
  quantity: number
}

export type ProcessReturnOutcome =
  | 'Processed'
  | 'SaleNotFound'
  | 'Forbidden'
  | 'SaleNotCompleted'
  | 'LineNotFound'
  | 'ExceedsAvailableQuantity'

export function processReturn(saleTransactionId: number, lines: ReturnLine[], reason: string) {
  return apiFetch<{ outcome: ProcessReturnOutcome; saleReturnId: number | null; totalRefund: number | null; failedSaleLineItemId: number | null }>(
    `/api/sales/${saleTransactionId}/return`,
    { method: 'POST', body: { lines, reason } },
  )
}

export interface ReturnLineDetail {
  saleLineItemId: number
  quantity: number
  refundAmount: number
}

export interface SaleReturn {
  saleReturnId: number
  reason: string
  createdAt: string
  lines: ReturnLineDetail[]
}

export type GetReturnsOutcome = 'Found' | 'SaleNotFound' | 'Forbidden'

export function getReturnsForSale(saleTransactionId: number) {
  return apiFetch<{ outcome: GetReturnsOutcome; returns: SaleReturn[] | null }>(`/api/sales/${saleTransactionId}/returns`)
}

export interface CashierShift {
  cashierShiftId: number
  cashierUserId: string
  openingCash: number
  // The backend serializes these as JSON `null` for a shift that's still open, not by omitting
  // the key -- `?: number` alone types this as `number | undefined` and lets `!== undefined`
  // guards pass straight through a real `null`, which is exactly the crash this type was hiding
  // (money(null) -> null.toLocaleString()). See StaffPage.tsx's shift history render.
  expectedCash: number | null
  closingCash: number | null
  currency: string
  startedAt: string
  endedAt: string | null
}

export function openCashierShift(storeId: number, openingCash: number, currency: string) {
  return apiFetch<{ outcome: string; cashierShiftId: number | null }>('/api/cashier-shifts/open', {
    method: 'POST',
    body: { storeId, openingCash, currency },
  })
}

export function closeCashierShift(cashierShiftId: number, closingCash: number) {
  return apiFetch<{ outcome: string; expectedCash: number | null; closingCash: number | null; discrepancy: number | null }>(
    `/api/cashier-shifts/${cashierShiftId}/close`,
    { method: 'POST', body: { closingCash } },
  )
}

export function getCashierShifts(storeId: number) {
  return apiFetch<{ outcome: string; shifts: CashierShift[] | null }>(`/api/stores/${storeId}/cashier-shifts`)
}
