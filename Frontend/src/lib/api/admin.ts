import { apiFetch } from './client'

export interface PriceEntryDispute {
  disputeId: number
  priceEntryId: number
  disputedByUserId: string
  reason: string
  createdAt: string
}

export function getPendingPriceEntryDisputes() {
  return apiFetch<{ disputes: PriceEntryDispute[] }>('/api/admin/price-entry-disputes/pending')
}

export function resolvePriceEntryDispute(disputeId: number, uphold: boolean) {
  return apiFetch<{ outcome: 'Upheld' | 'Dismissed' | 'NotFound' | 'AlreadyResolved' }>(
    `/api/admin/price-entry-disputes/${disputeId}/resolve`,
    { method: 'POST', body: { uphold } },
  )
}

export interface ReportDispute {
  disputeId: number
  reportId: number
  disputedByUserId: string
  reason: string
  createdAt: string
}

export function getPendingReportDisputes() {
  return apiFetch<{ disputes: ReportDispute[] }>('/api/admin/report-disputes/pending')
}

export function resolveReportDispute(disputeId: number, uphold: boolean) {
  return apiFetch<{ outcome: 'Upheld' | 'Dismissed' | 'NotFound' | 'AlreadyResolved' }>(
    `/api/admin/report-disputes/${disputeId}/resolve`,
    { method: 'POST', body: { uphold } },
  )
}

export interface ProductSubmission {
  submissionId: number
  barcode: string
  name: string
  categoryId: number
  brandId: number
  countryOfOrigin: string
  submittedByUserId: string
  createdAt: string
}

export function getPendingProductSubmissions() {
  return apiFetch<{ submissions: ProductSubmission[] }>('/api/admin/products/pending')
}

export function moderateProductSubmission(submissionId: number, approve: boolean, reason?: string) {
  return apiFetch<{ outcome: 'Approved' | 'Rejected' | 'NotFound' | 'AlreadyModerated'; productId?: number }>(
    `/api/admin/products/${submissionId}/moderate`,
    { method: 'POST', body: { approve, reason } },
  )
}

export interface Report {
  reportId: number
  userId: string
  productId: number
  storeId?: number
  type: 'WrongPrice' | 'OutOfStock' | 'ReceiptMismatch' | 'Other'
  description: string
  createdAt: string
}

export function getPendingReports() {
  return apiFetch<{ reports: Report[] }>('/api/admin/reports/pending')
}

export function moderateReport(reportId: number, resolve: boolean, reason?: string) {
  return apiFetch<{ outcome: 'Resolved' | 'Rejected' | 'NotFound' | 'AlreadyModerated' }>(
    `/api/admin/reports/${reportId}/moderate`,
    { method: 'POST', body: { resolve, reason } },
  )
}

export interface Brand {
  brandId: number
  name: string
}

export function getBrands() {
  return apiFetch<{ brands: Brand[] }>('/api/catalog/brands')
}

export function createBrand(name: string) {
  return apiFetch<{ brandId: number }>('/api/catalog/brands', { method: 'POST', body: { name } })
}

export interface Category {
  categoryId: number
  name: string
  parentCategoryId?: number
}

export function getCategories() {
  return apiFetch<{ categories: Category[] }>('/api/catalog/categories')
}

export function createCategory(name: string, parentCategoryId?: number) {
  return apiFetch<{ outcome: 'Created' | 'ParentCategoryNotFound'; categoryId?: number }>('/api/catalog/categories', {
    method: 'POST',
    body: { name, parentCategoryId },
  })
}

export interface TaxRate {
  taxRateId: number
  name: string
  percentage: number
  categoryId?: number
}

export function getTaxRates() {
  return apiFetch<{ taxRates: TaxRate[] }>('/api/catalog/tax-rates')
}

export function createTaxRate(name: string, percentage: number, categoryId?: number) {
  return apiFetch<{ taxRateId: number }>('/api/catalog/tax-rates', {
    method: 'POST',
    body: { name, percentage, categoryId },
  })
}

export interface AuditLogEntry {
  auditLogId: number
  performedByUserId: string
  action: string
  entityType: string
  entityId: number
  details?: string
  occurredAt: string
}

export function getRecentAuditLogs(count = 20) {
  return apiFetch<{ logs: AuditLogEntry[] }>('/api/admin/audit-logs/recent', { query: { count } })
}
