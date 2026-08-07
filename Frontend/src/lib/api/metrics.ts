import { apiFetch } from './client'
import type { StoreStatus } from './admin'

export interface PlanSubscriberCount {
  subscriptionPlanName: string
  count: number
}

export interface ProblemStore {
  storeId: number
  storeName: string
  reportCount: number
}

export interface SilentStore {
  storeId: number
  storeName: string
  lastSaleAt: string
}

export interface NoSalesStore {
  storeId: number
  storeName: string
  connectedAt: string
}

export interface PlatformMetrics {
  storesByStatus: Partial<Record<StoreStatus, number>>
  activeSubscriptionsByPlan: PlanSubscriberCount[]
  activeSubscriptionsTotal: number
  estimatedMonthlyRevenue: number
  revenueCurrency: string
  trialsEndingThisWeek: number
  pastDueCount: number
  salesLast7Days: number
  salesLast30Days: number
  newPriceEntriesLast7Days: number
  activeConsumersLast30Days: number
  storesWithNoSales: NoSalesStore[]
  silentStores: SilentStore[]
  problemStores: ProblemStore[]
}

export function getPlatformMetrics() {
  return apiFetch<PlatformMetrics>('/api/admin/metrics/summary')
}

export interface MetricsDay {
  date: string
  sales: number
  newStores: number
}

export function getMetricsTimeSeries(from: string, to: string) {
  return apiFetch<{ days: MetricsDay[] }>('/api/admin/metrics/time-series', { query: { from, to } })
}
