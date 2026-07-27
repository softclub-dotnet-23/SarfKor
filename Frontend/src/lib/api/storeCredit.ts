import { apiFetch } from './client'

// Per-store, per-customer store-credit balance (e.g. issued for a refund
// instead of cash/card reversal).

export type IssueStoreCreditOutcome = 'Issued' | 'StoreNotFound' | 'CustomerNotFound' | 'Forbidden'

export function issueStoreCredit(storeId: number, customerId: number, amount: number, currency: string) {
  return apiFetch<{ outcome: IssueStoreCreditOutcome; newBalance?: number }>('/api/store-credit/issue', {
    method: 'POST',
    body: { storeId, customerId, amount, currency },
  })
}

export type RedeemStoreCreditOutcome = 'Redeemed' | 'StoreNotFound' | 'Forbidden' | 'NoCreditOnFile' | 'InsufficientBalance'

export function redeemStoreCredit(storeId: number, customerId: number, amount: number) {
  return apiFetch<{ outcome: RedeemStoreCreditOutcome; newBalance?: number }>('/api/store-credit/redeem', {
    method: 'POST',
    body: { storeId, customerId, amount },
  })
}

export interface StoreCreditBalance {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  balance: number | null
  currency: string | null
}

export function getStoreCreditBalance(storeId: number, customerId: number) {
  return apiFetch<StoreCreditBalance>('/api/store-credit', {
    query: { storeId, customerId },
  })
}
