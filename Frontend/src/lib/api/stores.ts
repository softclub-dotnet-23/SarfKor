import { apiFetch } from './client'
import type { SubscriptionStatus } from './admin'

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

// All payload fields below are null (not omitted) on every outcome except 'Found' -- see
// GetStoreDashboardResult.cs/GetDailySalesReportResult.cs/GetProfitReportResult.cs, same
// discriminated-outcome shape as everywhere else in this API. The previous "always present"
// typing here was never actually enforced by any runtime check the compiler could see (callers
// happen to early-return on outcome !== 'Found', but nothing tied that branch to these fields
// becoming safe) -- currency specifically really could arrive null before the report handlers'
// zero-sales-period fix (`?? "TJS"`), which this typing would have hidden from tsc entirely.
export interface StoreDashboard {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  todaySalesCount: number | null
  todayRevenue: number | null
  currency: string | null
  productsInStockCount: number | null
}

export function getStoreDashboard(storeId: number) {
  return apiFetch<StoreDashboard>(`/api/stores/${storeId}/dashboard`)
}

export interface MyStoreSubscriptionStatus {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  status: SubscriptionStatus | null
  currentPeriodEndsAt: string | null
  isOwner: boolean | null
}

// Backs SubscriptionInactiveBanner (ErrorState.tsx) -- the "когда закончилась подписка, к кому
// обратиться" detail a 402 on any write action doesn't carry on its own.
export function getMySubscriptionStatus(storeId: number) {
  return apiFetch<MyStoreSubscriptionStatus>(`/api/stores/${storeId}/subscription-status`)
}

export interface DailySalesReport {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  date: string | null
  salesCount: number | null
  revenue: number | null
  currency: string | null
}

export function getDailySalesReport(storeId: number, date: string) {
  return apiFetch<DailySalesReport>(`/api/stores/${storeId}/reports/daily-sales`, { query: { date } })
}

export interface ProfitReport {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  fromDate: string | null
  toDate: string | null
  revenue: number | null
  totalCost: number | null
  profit: number | null
  currency: string | null
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
  return apiFetch<{ outcome: string; cashiers: CashierAnomaly[] | null }>(
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
  preferredSupplierId: number | null
}

export function getReorderAlerts(storeId: number) {
  return apiFetch<{ outcome: string; alerts: ReorderAlert[] | null }>(`/api/stores/${storeId}/reorder-alerts`)
}

export type StoreEmployeeRole = 'Owner' | 'Cashier'

export interface StoreEmployee {
  storeEmployeeId: number
  userId: string
  role: StoreEmployeeRole
  addedAt: string
  firstName: string | null
  lastName: string | null
  email: string | null
  phoneNumber: string | null
  scheduleStart: string | null
  scheduleEnd: string | null
  isActive: boolean
  monthlySalaryAmount: number | null
  monthlySalaryCurrency: string | null
}

export function removeStoreEmployee(storeEmployeeId: number) {
  return apiFetch<{ outcome: string }>(`/api/store-employees/${storeEmployeeId}`, { method: 'DELETE' })
}

export function getStoreEmployees(storeId: number) {
  return apiFetch<{ outcome: string; employees: StoreEmployee[] | null }>(`/api/stores/${storeId}/employees`)
}

export type UpdateStoreEmployeeOutcome = 'Updated' | 'NotFound' | 'Forbidden' | 'SubscriptionInactive'

// firstName/lastName/phoneNumber: omit a field to leave it unchanged (this is also the "изменить"
// action on a cashier's card, which only ever edits a subset at once).
//
// monthlySalaryAmount/Currency are NOT "omit to leave unchanged" like the three fields above --
// the backend command treats a null salary pair as "clear the salary" (predates this session, see
// UpdateStoreEmployeeCommand's own doc comment). Code review 2026-08-10 finding #1: this function
// used to hardcode both to null on every call, which meant using "Изменить" to fix a cashier's
// phone number silently erased any salary that had been set through any other means (SQL, a future
// payroll screen). Callers MUST now explicitly pass the salary they want to end up with --
// EditCashierModal passes the employee's own current value straight through, so an edit that has
// nothing to do with salary leaves it exactly as it was.
export function updateStoreEmployee(
  storeEmployeeId: number,
  fields: {
    firstName?: string
    lastName?: string
    phoneNumber?: string
    scheduleStart?: string | null
    scheduleEnd?: string | null
    monthlySalaryAmount?: number | null
    monthlySalaryCurrency?: string | null
  },
) {
  return apiFetch<{ outcome: UpdateStoreEmployeeOutcome }>(`/api/store-employees/${storeEmployeeId}`, {
    method: 'PATCH',
    body: {
      monthlySalaryAmount: fields.monthlySalaryAmount ?? null,
      monthlySalaryCurrency: fields.monthlySalaryCurrency ?? null,
      scheduleStart: fields.scheduleStart,
      scheduleEnd: fields.scheduleEnd,
      firstName: fields.firstName,
      lastName: fields.lastName,
      phoneNumber: fields.phoneNumber,
    },
  })
}

export type ResetCashierPasswordOutcome = 'Reset' | 'NotFound' | 'Forbidden'

export function resetCashierPassword(storeEmployeeId: number) {
  return apiFetch<{ outcome: ResetCashierPasswordOutcome; password: string | null }>(
    `/api/store-employees/${storeEmployeeId}/reset-password`,
    { method: 'POST' },
  )
}

export type SetStoreEmployeeActiveOutcome = 'Updated' | 'NotFound' | 'Forbidden'

export function setStoreEmployeeActive(storeEmployeeId: number, isActive: boolean) {
  return apiFetch<{ outcome: SetStoreEmployeeActiveOutcome }>(`/api/store-employees/${storeEmployeeId}/active`, {
    method: 'POST',
    body: { isActive },
  })
}

// Every new employee — existing account or not — now goes through an emailed link they click and
// confirm themselves; there is no more direct "add and attach immediately" path (see
// AcceptInvitePage / the /invite/:token route). Outcome deliberately never distinguishes "email
// belongs to an existing account" from "brand new" (email enumeration) — only AlreadyEmployed
// (already on *this* store's own team, which the owner can already see) is a distinct case.
export type CreateStoreEmployeeInvitationOutcome = 'Sent' | 'StoreNotFound' | 'Forbidden' | 'AlreadyEmployed'

export function createStoreEmployeeInvitation(storeId: number, email: string, role: StoreEmployeeRole) {
  return apiFetch<{ outcome: CreateStoreEmployeeInvitationOutcome; invitationId: number | null }>(
    `/api/stores/${storeId}/employee-invitations`,
    { method: 'POST', body: { email, role } },
  )
}

// Deliberately separate from createStoreEmployeeInvitation above — no email round-trip, returns a
// real, immediately-usable password once (never retrievable again after this response).
export type CreateCashierAccountOutcome = 'Created' | 'StoreNotFound' | 'Forbidden' | 'EmailAlreadyRegistered'

export function createCashierAccount(
  storeId: number,
  fields: { firstName: string; lastName: string; email: string; phoneNumber: string; scheduleStart?: string; scheduleEnd?: string },
) {
  return apiFetch<{ outcome: CreateCashierAccountOutcome; email: string | null; password: string | null }>(
    `/api/stores/${storeId}/cashier-accounts`,
    { method: 'POST', body: fields },
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
  return apiFetch<{ outcome: string; invitations: StoreEmployeeInvitation[] | null }>(`/api/stores/${storeId}/employee-invitations`, {
    query: { status },
  })
}

export function revokeStoreEmployeeInvitation(invitationId: number) {
  return apiFetch<{ outcome: string }>(`/api/store-employee-invitations/${invitationId}/revoke`, { method: 'POST' })
}

export function resendStoreEmployeeInvitation(invitationId: number) {
  return apiFetch<{ outcome: string }>(`/api/store-employee-invitations/${invitationId}/resend`, { method: 'POST' })
}
