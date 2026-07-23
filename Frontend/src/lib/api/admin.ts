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
