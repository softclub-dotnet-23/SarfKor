import { apiFetch } from './client'
import type { SubscriptionStatus } from './admin'

export interface SubscriptionPlan {
  subscriptionPlanId: number
  name: string
  code: string
  monthlyPriceAmount: number
  monthlyPriceCurrency: string
  maxStores: number | null
  maxEmployees: number | null
  features: string[]
  isActive: boolean
}

export function getSubscriptionPlans(includeInactive = false) {
  return apiFetch<{ plans: SubscriptionPlan[] }>('/api/admin/subscriptions/plans', { query: { includeInactive } })
}

export function createSubscriptionPlan(input: {
  name: string; code: string; monthlyPriceAmount: number; monthlyPriceCurrency: string
  maxStores?: number; maxEmployees?: number; features?: string[]
}) {
  return apiFetch<{ outcome: string; subscriptionPlanId: number | null }>('/api/admin/subscriptions/plans', {
    method: 'POST',
    body: input,
  })
}

export function updateSubscriptionPlan(subscriptionPlanId: number, input: {
  name: string; monthlyPriceAmount: number; monthlyPriceCurrency: string
  maxStores?: number; maxEmployees?: number; features?: string[]; isActive: boolean
}) {
  return apiFetch<{ outcome: string }>(`/api/admin/subscriptions/plans/${subscriptionPlanId}`, {
    method: 'PUT',
    body: input,
  })
}

export interface StoreSubscriptionListItem {
  storeSubscriptionId: number
  storeId: number
  storeName: string
  subscriptionPlanId: number
  subscriptionPlanName: string
  status: SubscriptionStatus
  startedAt: string
  currentPeriodEndsAt: string
  priceAtIssueAmount: number
  priceAtIssueCurrency: string
}

export function getStoreSubscriptions(params: {
  skip?: number; take?: number; status?: SubscriptionStatus; subscriptionPlanId?: number; storeSearch?: string
}) {
  return apiFetch<{ subscriptions: StoreSubscriptionListItem[]; totalCount: number }>('/api/admin/subscriptions', { query: params })
}

export interface ExpiringSubscription {
  storeSubscriptionId: number
  storeId: number
  storeName: string
  subscriptionPlanName: string
  currentPeriodEndsAt: string
}

export function getExpiringSoonSubscriptions(withinDays = 7) {
  return apiFetch<{ subscriptions: ExpiringSubscription[] }>('/api/admin/subscriptions/expiring-soon', { query: { withinDays } })
}

export function getPastDueSubscriptions() {
  return apiFetch<{ subscriptions: ExpiringSubscription[] }>('/api/admin/subscriptions/past-due')
}

export function changeStoreSubscriptionPlan(storeSubscriptionId: number, newSubscriptionPlanId: number) {
  return apiFetch<{ outcome: string }>(`/api/admin/subscriptions/${storeSubscriptionId}/plan`, {
    method: 'POST',
    body: { newSubscriptionPlanId },
  })
}

export function cancelStoreSubscription(storeSubscriptionId: number, reason: string) {
  return apiFetch<{ outcome: string }>(`/api/admin/subscriptions/${storeSubscriptionId}/cancel`, {
    method: 'POST',
    body: { reason },
  })
}

export type SubscriptionPaymentMethod = 'Cash' | 'BankTransfer' | 'Card' | 'Other'

export function recordSubscriptionPayment(storeSubscriptionId: number, input: {
  amount: number; currency: string; periodStart: string; periodEnd: string
  method: SubscriptionPaymentMethod; comment?: string
}) {
  return apiFetch<{ outcome: string; subscriptionPaymentId: number | null; newPeriodEndsAt: string | null }>(
    `/api/admin/subscriptions/${storeSubscriptionId}/payments`,
    { method: 'POST', body: input },
  )
}

export function reverseSubscriptionPayment(subscriptionPaymentId: number, reason: string) {
  return apiFetch<{ outcome: string; reversalPaymentId: number | null }>(`/api/admin/subscriptions/payments/${subscriptionPaymentId}/reverse`, {
    method: 'POST',
    body: { reason },
  })
}

export interface SubscriptionPayment {
  subscriptionPaymentId: number
  storeSubscriptionId: number
  storeId: number
  storeName: string
  amount: number
  currency: string
  periodStart: string
  periodEnd: string
  method: SubscriptionPaymentMethod
  comment: string | null
  recordedByUserId: string | null
  recordedByEmail: string | null
  recordedAt: string
  isReversal: boolean
  reversedPaymentId: number | null
}

export function getSubscriptionPayments(params: { skip?: number; take?: number; storeId?: number; from?: string; to?: string }) {
  return apiFetch<{ payments: SubscriptionPayment[]; totalCount: number }>('/api/admin/subscriptions/payments', { query: params })
}
