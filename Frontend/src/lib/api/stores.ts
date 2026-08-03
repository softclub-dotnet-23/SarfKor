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

export type AddStoreEmployeeOutcome = 'Added' | 'StoreNotFound' | 'Forbidden' | 'AlreadyEmployed' | 'Invited'

// The owner only has the employee's email to go on (there's no user directory) -- if nobody's
// registered with it yet, the backend sends them an invite email instead of failing (Invited).
export function addStoreEmployee(storeId: number, employeeEmail: string, role: StoreEmployeeRole = 'Cashier') {
  return apiFetch<{ outcome: AddStoreEmployeeOutcome; storeEmployeeId?: number }>(`/api/stores/${storeId}/employees`, {
    method: 'POST',
    body: { employeeEmail, role },
  })
}

export function removeStoreEmployee(storeEmployeeId: number) {
  return apiFetch<{ outcome: string }>(`/api/store-employees/${storeEmployeeId}`, { method: 'DELETE' })
}

export function getStoreEmployees(storeId: number) {
  return apiFetch<{ outcome: string; employees?: StoreEmployee[] }>(`/api/stores/${storeId}/employees`)
}
