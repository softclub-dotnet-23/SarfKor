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

export interface ProductDetail {
  productId: number
  productName: string
  barcode: string
  categoryId: number
  brandId: number
  countryOfOrigin: string
}

export function getProductById(productId: number) {
  return apiFetch<ProductDetail>(`/api/products/${productId}`)
}

export function scanBarcode(barcode: string, lat?: number, lng?: number) {
  return apiFetch<ScanBarcodeResult>(`/api/products/scan/${encodeURIComponent(barcode)}`, {
    query: { lat, lng },
    auth: false,
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

