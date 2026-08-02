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
// auth defaults to true here (not false): the backend accepts this endpoint anonymously,
// but when a JWT is present it also widens results to include the caller's own store even
// while that store is still Pending admin approval -- needed for InventoryPage/PosPage, which
// share this same call. apiFetch only *attaches* the token if one exists, so a signed-out
// consumer on ProductPage is unaffected.
export function scanBarcode(barcode: string, lat?: number, lng?: number) {
  return apiFetch<ScanBarcodeResult>(`/api/products/scan/${encodeURIComponent(barcode)}`, {
    query: { lat, lng },
  })
}

/**
 * "Which single store is cheapest for this whole basket." One of the few places
 * the backend does real cross-store work for a consumer, so the basket page is
 * built on it rather than on client-side summing of individual scans.
 * productIds repeats in the query string; buildUrl() in client.ts already appends
 * arrays that way.
 */
export interface StoreBasket {
  storeId: number
  storeName: string
  totalPrice: number
  currency: string
  distanceKm?: number
}

export function compareBasket(productIds: number[], lat?: number, lng?: number) {
  return apiFetch<{ stores: StoreBasket[] }>('/api/products/compare-basket', {
    query: { productIds, lat, lng },
    auth: false,
  })
}

export interface MostScannedProduct {
  productId: number
  productName: string
  totalScans: number
}

export function getMostScannedProducts(limit = 10) {
  return apiFetch<{ products: MostScannedProduct[] }>('/api/products/most-scanned', {
    query: { limit },
    auth: false,
  })
}

/**
 * Recording a scan is separate from reading the price (ScanBarcodeQuery stays
 * read-only) and works signed-out too — the backend leaves UserId null then.
 * Fire-and-forget from the UI: a failed analytics write must never surface as a
 * failed price lookup.
 */
export function recordScan(productId: number, storeId?: number) {
  return apiFetch<{ outcome: string; scanId?: number }>('/api/scans', {
    method: 'POST',
    body: { productId, storeId },
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

// A StorePartner's submission is trusted and created directly -- the response already carries
// productId, no self-approve call needed (backend: CreateDirectly = User.IsInRole("StorePartner")).
// Any other authenticated caller instead gets a pending ProductSubmission (productSubmissionId,
// no productId yet) that stays in the moderation queue for Admin, or can be self-approved via
// selfApproveNewProduct below if the submitter turns out to also hold StorePartner.
export function submitNewProduct(req: SubmitNewProductRequest) {
  return apiFetch<{ outcome: string; productSubmissionId?: number; productId?: number }>('/api/products/submissions', {
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

