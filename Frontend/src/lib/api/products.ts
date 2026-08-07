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

export interface ProductSearchItem {
  productId: number
  name: string
  barcode: string
  brandName?: string
  categoryName?: string
  price?: number
  currency?: string
}

// Backs the shared entity picker (EntityPicker/ProductPicker) -- server-side search by
// name/barcode/brand at once, so nothing in the UI ever has to ask someone to type a numeric
// product ID by hand. storeId is optional and, when given, annotates each row with that store's
// current price.
export function searchProducts(params: { search?: string; categoryId?: number; storeId?: number; skip?: number; take?: number }) {
  return apiFetch<{ items: ProductSearchItem[]; totalCount: number }>('/api/products/search', {
    query: params,
  })
}

export interface SubmitNewProductRequest {
  barcode: string
  name: string
  categoryId: number
  brandId: number
  countryOfOrigin: string
}

// Moderation is gone -- every submission (partner or plain user) publishes a Product immediately,
// so the response always carries productId alongside the provenance-only productSubmissionId.
export function submitNewProduct(req: SubmitNewProductRequest) {
  return apiFetch<{ outcome: string; productSubmissionId?: number; productId?: number }>('/api/products/submissions', {
    method: 'POST',
    body: req,
  })
}

