import { apiFetch } from './client'

// Points-based loyalty programs, one per store, with per-customer accounts
// that earn/redeem points against it.

export type CreateLoyaltyProgramOutcome = 'Created' | 'StoreNotFound' | 'Forbidden' | 'AlreadyExists'

export function createLoyaltyProgram(storeId: number, pointsPerCurrencyUnit: number, redemptionRate: number) {
  return apiFetch<{ outcome: CreateLoyaltyProgramOutcome; loyaltyProgramId?: number }>('/api/loyalty-programs', {
    method: 'POST',
    body: { storeId, pointsPerCurrencyUnit, redemptionRate },
  })
}

export interface LoyaltyProgram {
  loyaltyProgramId: number | null
  pointsPerCurrencyUnit: number | null
  redemptionRate: number | null
  isActive: boolean | null
}

// Public read — no auth required, matches backend [AllowAnonymous].
export function getLoyaltyProgram(storeId: number) {
  return apiFetch<LoyaltyProgram>(`/api/stores/${storeId}/loyalty-program`, { auth: false })
}

export type EnrollOutcome = 'Enrolled' | 'AlreadyEnrolled' | 'CustomerNotFound' | 'ProgramNotFound' | 'Forbidden'

export function enrollCustomerInLoyalty(customerId: number, loyaltyProgramId: number) {
  return apiFetch<{ outcome: EnrollOutcome; loyaltyAccountId?: number }>('/api/loyalty-accounts/enroll', {
    method: 'POST',
    body: { customerId, loyaltyProgramId },
  })
}

export type EarnOutcome = 'Earned' | 'AccountNotFound' | 'Forbidden'

export function earnLoyaltyPoints(loyaltyAccountId: number, points: number, saleTransactionId?: number) {
  return apiFetch<{ outcome: EarnOutcome; newBalance?: number }>(`/api/loyalty-accounts/${loyaltyAccountId}/earn`, {
    method: 'POST',
    body: { points, saleTransactionId },
  })
}

export type RedeemOutcome = 'Redeemed' | 'AccountNotFound' | 'Forbidden' | 'InsufficientPoints'

export function redeemLoyaltyPoints(loyaltyAccountId: number, points: number) {
  return apiFetch<{ outcome: RedeemOutcome; newBalance?: number }>(`/api/loyalty-accounts/${loyaltyAccountId}/redeem`, {
    method: 'POST',
    body: { points },
  })
}

export interface LoyaltyAccount {
  loyaltyAccountId: number | null
  pointsBalance: number | null
}

export function getLoyaltyAccount(customerId: number, loyaltyProgramId: number) {
  return apiFetch<LoyaltyAccount>('/api/loyalty-accounts', {
    query: { customerId, loyaltyProgramId },
  })
}
