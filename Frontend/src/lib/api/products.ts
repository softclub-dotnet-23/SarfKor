import { apiFetch } from './client'

export interface ScanResultStore {
  storeId: number
  storeName: string
  price: number
  currency: string
  distanceKm?: number
  priceEntryId: number
}

export interface ScanBarcodeResult {
  productId: number
  productName: string
  stores: ScanResultStore[]
}

// Only exact-barcode lookup exists on the backend today — there is no
// name/keyword product search endpoint (see Backend audit notes).
export function scanBarcode(barcode: string, lat?: number, lng?: number) {
  return apiFetch<ScanBarcodeResult>(`/api/products/scan/${encodeURIComponent(barcode)}`, {
    query: { lat, lng },
    auth: false,
  })
}

export function getTopSellingProducts(storeId?: number, limit = 10) {
  return apiFetch<{ products: { productId: number; productName: string; totalQuantity: number }[] }>(
    '/api/products/top-selling',
    { query: { storeId, limit }, auth: false },
  )
}

export interface SubmitNewProductRequest {
  barcode: string
  name: string
  categoryId: number
  brandId: number
  countryOfOrigin: string
}

// Files an unrecognized barcode as a pending submission -- does not create the Product directly.
// Any authenticated user can submit. A StorePartner can immediately self-approve their own
// submission via selfApproveNewProduct below (no Admin needed); anyone else's stays in the queue
// for Admin moderation (see /admin/moderation).
export function submitNewProduct(req: SubmitNewProductRequest) {
  return apiFetch<{ outcome: string; productSubmissionId?: number }>('/api/products/submissions', {
    method: 'POST',
    body: req,
  })
}

// Restricted to the submission's own submitter -- the backend rejects (403) an attempt to
// self-approve someone else's pending submission.
export function selfApproveNewProduct(productSubmissionId: number) {
  return apiFetch<{ outcome: string; productId?: number }>(`/api/products/submissions/${productSubmissionId}/self-approve`, {
    method: 'POST',
  })
}

