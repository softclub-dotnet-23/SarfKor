import { apiFetch } from './client'

export interface CreateStoreRequest {
  name: string
  address: string
  latitude: number
  longitude: number
}

export function createStore(req: CreateStoreRequest) {
  return apiFetch<{ storeId: number }>('/api/stores', { method: 'POST', body: req })
}

export interface UpdateStoreRequest {
  name: string
  address: string
  latitude: number
  longitude: number
}

export function updateStore(storeId: number, req: UpdateStoreRequest) {
  return apiFetch<{ outcome: 'Updated' | 'StoreNotFound' | 'Forbidden' }>(`/api/stores/${storeId}`, {
    method: 'PATCH',
    body: req,
  })
}

export interface StoreDashboard {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  todaySalesCount: number
  todayRevenue: number
  currency: string
  productsInStockCount: number
}

export function getStoreDashboard(storeId: number) {
  return apiFetch<StoreDashboard>(`/api/stores/${storeId}/dashboard`)
}

export interface DailySalesReport {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  date: string
  salesCount: number
  revenue: number
  currency: string
}

export function getDailySalesReport(storeId: number, date: string) {
  return apiFetch<DailySalesReport>(`/api/stores/${storeId}/reports/daily-sales`, { query: { date } })
}

export interface ProfitReport {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  fromDate: string
  toDate: string
  revenue: number
  totalCost: number
  profit: number
  currency: string
}

export function getProfitReport(storeId: number, from: string, to: string) {
  return apiFetch<ProfitReport>(`/api/stores/${storeId}/reports/profit`, { query: { from, to } })
}

export interface CashierAnomaly {
  cashierUserId: string
  totalSales: number
  voidedSales: number
  voidRate: number
  isAnomalous: boolean
}

export function getCashierAnomalies(storeId: number, from: string, to: string) {
  return apiFetch<{ outcome: string; cashiers: CashierAnomaly[] }>(
    `/api/stores/${storeId}/reports/cashier-anomalies`,
    { query: { from, to } },
  )
}

export interface ReorderAlert {
  productId: number
  productName: string
  currentQuantity: number
  thresholdQuantity: number
  reorderQuantity: number
  preferredSupplierId?: number
}

export function getReorderAlerts(storeId: number) {
  return apiFetch<{ outcome: string; alerts?: ReorderAlert[] }>(`/api/stores/${storeId}/reorder-alerts`)
}

export type StoreEmployeeRole = 'Owner' | 'Cashier'

export interface StoreEmployee {
  storeEmployeeId: number
  userId: string
  role: StoreEmployeeRole
  addedAt: string
}

export function removeStoreEmployee(storeEmployeeId: number) {
  return apiFetch<{ outcome: string }>(`/api/store-employees/${storeEmployeeId}`, { method: 'DELETE' })
}

export function getStoreEmployees(storeId: number) {
  return apiFetch<{ outcome: string; employees?: StoreEmployee[] }>(`/api/stores/${storeId}/employees`)
}

// Every new employee — existing account or not — now goes through an emailed link they click and
// confirm themselves; there is no more direct "add and attach immediately" path (see
// AcceptInvitePage / the /invite/:token route). Outcome deliberately never distinguishes "email
// belongs to an existing account" from "brand new" (email enumeration) — only AlreadyEmployed
// (already on *this* store's own team, which the owner can already see) is a distinct case.
export type CreateStoreEmployeeInvitationOutcome = 'Sent' | 'StoreNotFound' | 'Forbidden' | 'AlreadyEmployed'

export function createStoreEmployeeInvitation(storeId: number, email: string, role: StoreEmployeeRole) {
  return apiFetch<{ outcome: CreateStoreEmployeeInvitationOutcome; invitationId?: number }>(
    `/api/stores/${storeId}/employee-invitations`,
    { method: 'POST', body: { email, role } },
  )
}

export type StoreEmployeeInvitationStatus = 'Pending' | 'Accepted' | 'Revoked' | 'Expired'

export interface StoreEmployeeInvitation {
  invitationId: number
  email: string
  role: StoreEmployeeRole
  status: StoreEmployeeInvitationStatus
  expiresAt: string
  createdAt: string
  lastSentAt: string
}

export function getStoreEmployeeInvitations(storeId: number, status?: StoreEmployeeInvitationStatus) {
  return apiFetch<{ outcome: string; invitations?: StoreEmployeeInvitation[] }>(`/api/stores/${storeId}/employee-invitations`, {
    query: { status },
  })
}

export function revokeStoreEmployeeInvitation(invitationId: number) {
  return apiFetch<{ outcome: string }>(`/api/store-employee-invitations/${invitationId}/revoke`, { method: 'POST' })
}

export function resendStoreEmployeeInvitation(invitationId: number) {
  return apiFetch<{ outcome: string }>(`/api/store-employee-invitations/${invitationId}/resend`, { method: 'POST' })
}
