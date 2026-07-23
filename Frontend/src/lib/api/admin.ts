import { apiFetch } from './client'

// Platform moderation — Admin role only. Distinct from the StorePartner cabinet
// (`stores.ts`, `inventory.ts`, etc.): nothing here is scoped to a store.

export interface ProductSubmission {
  productSubmissionId: number
  barcode: string
  name: string
  categoryId: number
  brandId: number
  countryOfOrigin: string
  submittedByUserId: string
  createdAt: string
}

export function getPendingProductSubmissions() {
  return apiFetch<{ submissions: ProductSubmission[] }>('/api/admin/products/submissions/pending')
}

export function moderateProductSubmission(submissionId: number, approve: boolean, reason?: string) {
  return apiFetch<{ outcome: string; productId?: number }>(`/api/admin/products/${submissionId}/moderate`, {
    method: 'POST',
    body: { approve, reason },
  })
}

// ReportType: 0 WrongPrice, 1 OutOfStock, 2 ReceiptMismatch, 3 Other (matches Domain.Feedback.ReportType).
export const REPORT_TYPE_LABELS = ['Неверная цена', 'Нет в наличии', 'Расхождение с чеком', 'Другое'] as const

export interface Report {
  reportId: number
  userId: string
  productId: number
  storeId?: number
  type: number
  description: string
  createdAt: string
}

export function getPendingReports() {
  return apiFetch<{ reports: Report[] }>('/api/admin/reports/pending')
}

export function moderateReport(reportId: number, resolve: boolean, reason?: string) {
  return apiFetch<{ outcome: string }>(`/api/admin/reports/${reportId}/moderate`, {
    method: 'POST',
    body: { resolve, reason },
  })
}

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
  return apiFetch<{ outcome: string }>(`/api/admin/price-entry-disputes/${disputeId}/resolve`, {
    method: 'POST',
    body: { uphold },
  })
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
  return apiFetch<{ outcome: string }>(`/api/admin/report-disputes/${disputeId}/resolve`, {
    method: 'POST',
    body: { uphold },
  })
}
