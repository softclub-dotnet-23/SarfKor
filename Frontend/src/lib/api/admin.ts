import { apiFetch } from './client'
import type { StoreEmployeeRole } from './stores'

// Platform-operator surface (ADMIN_PROMPT.md) — stores, tax settings, admin accounts, audit log.
// No moderation left here at all: products/reports are never manually reviewed anymore, see
// subscriptions.ts / adminUsers.ts / metrics.ts for the rest of the Admin console's API surface.

export type StoreStatus = 'PendingApproval' | 'Active' | 'Suspended' | 'Blocked' | 'Archived' | 'Rejected'
export type SubscriptionStatus = 'Trial' | 'Active' | 'PastDue' | 'Suspended' | 'Cancelled'
export type TaxRegime = 'General' | 'Simplified'

export interface AdminStoreListItem {
  storeId: number
  name: string
  address: string
  status: StoreStatus
  ownerUserId: string
  ownerEmail: string | null
  subscriptionStatus: SubscriptionStatus | null
  subscriptionPlanName: string | null
  subscriptionCurrentPeriodEndsAt: string | null
}

export function getStores(params: {
  skip?: number
  take?: number
  status?: StoreStatus
  subscriptionStatus?: SubscriptionStatus
  connectedFrom?: string
  connectedTo?: string
  search?: string
  sortBy?: string
  sortDescending?: boolean
}) {
  return apiFetch<{ stores: AdminStoreListItem[]; totalCount: number }>('/api/admin/stores', { query: params })
}

export interface AdminStoreSubscription {
  storeSubscriptionId: number
  subscriptionPlanId: number
  subscriptionPlanName: string
  status: SubscriptionStatus
  startedAt: string
  currentPeriodEndsAt: string
  priceAtIssueAmount: number
  priceAtIssueCurrency: string
  note: string | null
}

export interface AdminStoreDetail {
  outcome: 'Found' | 'NotFound'
  storeId: number
  name: string | null
  address: string | null
  latitude: number | null
  longitude: number | null
  status: StoreStatus | null
  statusReason: string | null
  statusChangedAt: string | null
  ownerUserId: string | null
  ownerEmail: string | null
  isVatPayer: boolean | null
  taxRegime: TaxRegime | null
  subscription: AdminStoreSubscription | null
}

export function getStoreDetail(storeId: number) {
  return apiFetch<AdminStoreDetail>(`/api/admin/stores/${storeId}`)
}

export interface AdminStoreDiagnostics {
  outcome: 'Found' | 'NotFound'
  storeId: number
  storeStatus: StoreStatus | null
  ownerLastLoginAt: string | null
  lastSaleAt: string | null
  storeLocationsOwnedByThisOwner: number | null
  employeeCount: number | null
  distinctProductsInStock: number | null
  totalStockUnits: number | null
  subscriptionStatus: SubscriptionStatus | null
  subscriptionPlanName: string | null
  subscriptionCurrentPeriodEndsAt: string | null
}

export function getStoreDiagnostics(storeId: number) {
  return apiFetch<AdminStoreDiagnostics>(`/api/admin/stores/${storeId}/diagnostics`)
}

export interface AdminStoreLocation {
  storeId: number
  name: string
  address: string
  status: StoreStatus
}

// "Торговые точки" tab -- every Store row owned by the same owner as this one (a Store row *is* a
// single physical location; an owner with several shops has several Store rows).
export function getStoreLocations(storeId: number) {
  return apiFetch<{ outcome: string; locations: AdminStoreLocation[] | null }>(`/api/admin/stores/${storeId}/locations`)
}

export interface AdminStoreEmployee {
  storeEmployeeId: number
  userId: string
  email: string | null
  role: StoreEmployeeRole
  addedAt: string
  scheduleStart: string | null
  scheduleEnd: string | null
}

// "Сотрудники" tab -- deliberately carries no salary figures (see backend's
// GetStoreEmployeesForAdminQueryHandler comment: same trust boundary as store diagnostics).
export function getStoreEmployees(storeId: number) {
  return apiFetch<{ outcome: string; employees: AdminStoreEmployee[] | null }>(`/api/admin/stores/${storeId}/employees`)
}

export function approveStore(storeId: number) {
  return apiFetch<{ outcome: string }>(`/api/admin/stores/${storeId}/approve`, { method: 'POST' })
}

export function changeStoreStatus(storeId: number, newStatus: StoreStatus, reason: string) {
  return apiFetch<{ outcome: string }>(`/api/admin/stores/${storeId}/status`, {
    method: 'POST',
    body: { newStatus, reason },
  })
}

export function updateStoreTaxSettings(storeId: number, isVatPayer: boolean, taxRegime: TaxRegime) {
  return apiFetch<{ outcome: string }>(`/api/admin/stores/${storeId}/tax-settings`, {
    method: 'PUT',
    body: { isVatPayer, taxRegime },
  })
}

export function inviteAdmin(email: string) {
  return apiFetch<{ outcome: string; adminInvitationId: number | null }>('/api/admin/invitations', {
    method: 'POST',
    body: { email },
  })
}

export interface AuditLogEntry {
  auditLogId: number
  performedByUserId: string
  performedByEmail: string | null
  action: string
  entityType: string
  entityId: number
  details: string | null
  reason: string | null
  ipAddress: string | null
  beforeStateJson: string | null
  afterStateJson: string | null
  occurredAt: string
}

export function getRecentAuditLogs(count = 20) {
  return apiFetch<{ logs: AuditLogEntry[] }>('/api/admin/audit-logs/recent', { query: { count } })
}

export function getAuditLog(params: {
  skip?: number; take?: number; performedByUserId?: string; action?: string; entityType?: string
  entityId?: number; from?: string; to?: string
}) {
  return apiFetch<{ entries: AuditLogEntry[]; totalCount: number }>('/api/admin/audit-logs', { query: params })
}
